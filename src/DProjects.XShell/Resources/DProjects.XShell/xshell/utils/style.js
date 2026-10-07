
// process a fetched stylesheet or inline CSS using the declaring resource as its base
export async function processStyle({ src, context, css }) {
    const bases = createBases(context);
    const source = createInitialSource(src, bases);
    return await loadCss(source, bases, new Set(), css);
}

// fetch and process one stylesheet
async function loadCss(source, bases, loading, css) {
    if (loading.has(source.requestUrl)) throw new Error(`Circular CSS @import detected: ${source.requestUrl}`);
    loading.add(source.requestUrl);

    try {
        // fetch only when the declaring CSS source was not provided
        if (css === undefined) {
            const response = await fetch(source.requestUrl);
            if (!response.ok) throw new Error(`Error ${response.status}: ${response.statusText}: ${source.requestUrl}`);
            css = await response.text();
        }

        // inline imports before rewriting resource URLs
        css = await resolveImports(css, source, bases, loading);
        css = resolveUrls(css, source, bases);
        return css;
    } finally {
        loading.delete(source.requestUrl);
    }
}

// recursively resolve and inline CSS imports
async function resolveImports(css, source, bases, loading) {
    const imports = findImports(css);
    if (!imports.length) return css;

    let result = "";
    let position = 0;

    for (const item of imports) {
        result += css.slice(position, item.start);
        const importedSource = resolveImportSource(item.url, source, bases);
        const importedCss = await loadCss(importedSource, bases, loading);
        result += wrapImportedCss(importedCss, item.qualifiers);
        position = item.end;
    }

    result += css.slice(position);
    return result;
}

// rewrite CSS url(...) references
function resolveUrls(css, source, bases) {
    let result = "";
    let position = 0;

    while (position < css.length) {
        // preserve comments
        if (css.startsWith("/*", position)) {
            const end = css.indexOf("*/", position + 2);
            const next = end < 0 ? css.length : end + 2;
            result += css.slice(position, next);
            position = next;
            continue;
        }

        // preserve ordinary strings
        if (css[position] === "'" || css[position] === '"') {
            const end = findStringEnd(css, position);
            result += css.slice(position, end);
            position = end;
            continue;
        }

        // rewrite url(...) references
        const parsed = readUrl(css, position);
        if (parsed) {
            result += parsed.prefix;
            result += resolveResourceUrl(parsed.url, source, bases);
            result += parsed.suffix;
            position = parsed.end;
            continue;
        }

        result += css[position++];
    }

    return result;
}

// resolve one CSS resource URL according to XShell URL semantics
function resolveResourceUrl(value, source, bases) {
    const url = value.trim();

    // preserve empty and fragment-only references
    if (!url || url.startsWith("#")) return url;

    // reject configuration-only physical URLs
    if (/^url:/i.test(url)) {
        throw new Error(`The 'url:' scheme is not supported in CSS resource references in '${source.requestUrl}'.`);
    }

    // preserve protocol-relative URLs
    if (url.startsWith("//")) return url;

    // preserve absolute and special URLs
    if (hasScheme(url)) return url;

    // resolve /foo against the module root
    if (url.startsWith("/") && source.scope === "module") {
        return new URL(url.replace(/^\/+/, ""), bases.moduleVirtualBaseUrl).href;
    }

    // resolve /foo against the application root for app-scoped CSS
    if (url.startsWith("/") && source.scope === "app") {
        return new URL(url.replace(/^\/+/, ""), bases.appBaseUrl).href;
    }

    // use normal URL semantics for external or custom CSS
    if (url.startsWith("/")) return new URL(url, source.requestUrl).href;

    // resolve ./foo and ../foo relative to the logical module stylesheet
    if (source.scope === "module") {
        const resolved = new URL(url, source.requestUrl).href;
        assertInside(resolved, bases.moduleVirtualBaseUrl, `Module URL '${url}' in '${source.requestUrl}' escapes the module root.`);
        return resolved;
    }

    // resolve relative app URLs without escaping the application root
    if (source.scope === "app") {
        const resolved = new URL(url, source.requestUrl).href;
        assertInside(resolved, bases.appBaseUrl, `Application URL '${url}' in '${source.requestUrl}' escapes the application root.`);
        return resolved;
    }

    // resolve normal relative URLs for external or custom CSS
    return new URL(url, source.requestUrl).href;
}

// resolve one CSS @import in its logical namespace
function resolveImportSource(value, source, bases) {
    const url = value.trim();
    if (!url) throw new Error(`CSS @import in '${source.requestUrl}' declares an empty URL.`);

    // reject configuration-only physical imports
    if (/^url:/i.test(url)) {
        throw new Error(`The 'url:' scheme is not supported in CSS resource references in '${source.requestUrl}'.`);
    }

    // resolve absolute and protocol-relative imports
    if (url.startsWith("//") || hasScheme(url)) {
        const requestUrl = new URL(url, source.requestUrl).href;
        return { requestUrl, scope: "external" };
    }

    // resolve module-root imports
    if (url.startsWith("/") && source.scope === "module") {
        const requestUrl = new URL(url.replace(/^\/+/, ""), bases.moduleVirtualBaseUrl).href;
        return { requestUrl, scope: "module" };
    }

    // resolve application-root imports
    if (url.startsWith("/") && source.scope === "app") {
        const requestUrl = new URL(url.replace(/^\/+/, ""), bases.appBaseUrl).href;
        return { requestUrl, scope: "app" };
    }

    // resolve root URLs normally for external or custom CSS
    if (url.startsWith("/")) {
        const requestUrl = new URL(url, source.requestUrl).href;
        return { requestUrl, scope: source.scope };
    }

    // resolve module-relative imports
    if (source.scope === "module") {
        const requestUrl = new URL(url, source.requestUrl).href;
        assertInside(requestUrl, bases.moduleVirtualBaseUrl, `Module CSS @import '${url}' in '${source.requestUrl}' escapes the module root.`);
        return { requestUrl, scope: "module" };
    }

    // resolve application-relative imports
    if (source.scope === "app") {
        const requestUrl = new URL(url, source.requestUrl).href;
        assertInside(requestUrl, bases.appBaseUrl, `Application CSS @import '${url}' in '${source.requestUrl}' escapes the application root.`);
        return { requestUrl, scope: "app" };
    }

    // resolve external or custom relative imports
    const requestUrl = new URL(url, source.requestUrl).href;
    return { requestUrl, scope: source.scope };
}

// rewrite CSS resource URLs without loading @import declarations
export function rewriteStyleUrls({ src, context, css }) {
    const bases = createBases(context);
    return resolveUrls(css, createInitialSource(src, bases), bases);
}

// rewrite one already-parsed declaration value without introducing a second CSS parser
export function rewriteStyleDeclarationValue({ src, context, value }) {
    const prefix = "x{v:";
    const suffix = "}";
    const css = rewriteStyleUrls({ src, context, css: `${prefix}${value}${suffix}` });
    return css.slice(prefix.length, css.length - suffix.length);
}

// create application and optional virtual-module bases
function createBases(context) {
    const modulePath = context?.resourceDefinition?.modulePath;
    if (modulePath != null && (typeof modulePath !== "string" || !modulePath.startsWith("/"))) {
        throw new Error("CSS resourceDefinition.modulePath must be an absolute path.");
    }

    const appBasePath = context?.appBasePath || "";
    const appBaseUrl = createAppBaseUrl(appBasePath);
    const moduleVirtualBaseUrl = modulePath == null ? null : new URL(`${modulePath.replace(/^\/+/, "")}/`, appBaseUrl).href;

    return { appBaseUrl, moduleVirtualBaseUrl };
}

// create an application base from either the configured root path or an absolute test/custom base
function createAppBaseUrl(appBasePath) {
    if (/^[a-zA-Z][a-zA-Z0-9+.-]*:\/\//.test(appBasePath)) return new URL(appBasePath.replace(/\/?$/, "/")).href;
    return new URL(joinRootPath(appBasePath), window.location.origin).href;
}

// describe the declaring resource in its logical namespace
function createInitialSource(src, bases) {

    // reject configuration-only physical sources
    if (/^url:/i.test(src)) {
        throw new Error(`The 'url:' scheme is not supported in CSS resource references: '${src}'.`);
    }

    // resolve external root stylesheets
    if (src.startsWith("//") || hasScheme(src)) {
        const requestUrl = new URL(src, window.location.origin).href;
        return { requestUrl, scope: "external" };
    }

    // resolve the normal virtual module stylesheet
    const requestUrl = new URL(src, bases.moduleVirtualBaseUrl || window.location.origin).href;
    if (bases.moduleVirtualBaseUrl && isInside(requestUrl, bases.moduleVirtualBaseUrl)) return { requestUrl, scope: "module" };

    // fall back to normal URL semantics for custom or external resources
    return { requestUrl, scope: "external" };
}

// scan CSS and return every @import declaration
function findImports(css) {
    const result = [];
    let position = 0;

    while (position < css.length) {
        // skip comments
        if (css.startsWith("/*", position)) {
            const end = css.indexOf("*/", position + 2);
            position = end < 0 ? css.length : end + 2;
            continue;
        }

        // skip ordinary strings
        if (css[position] === "'" || css[position] === '"') {
            position = findStringEnd(css, position);
            continue;
        }

        // continue until an @import is found
        if (!isImportStart(css, position)) {
            position++;
            continue;
        }

        // read the @import URL
        const start = position;
        position += 7;
        position = skipTrivia(css, position);
        let url = null;

        if (css[position] === "'" || css[position] === '"') {
            const quoteStart = position;
            const end = findStringEnd(css, position);
            if (end <= quoteStart + 1 || css[end - 1] !== css[quoteStart]) throw new Error(`Invalid CSS @import near offset ${start}`);
            url = decodeCssString(css.slice(quoteStart + 1, end - 1));
            position = end;
        } else {
            const parsed = readUrl(css, position);
            if (!parsed) throw new Error(`Invalid CSS @import near offset ${start}`);
            url = parsed.url;
            position = parsed.end;
        }

        // read media or other import qualifiers
        const qualifiersStart = position;
        position = findImportEnd(css, position);
        if (position >= css.length || css[position] !== ";") throw new Error(`CSS @import near offset ${start} is not terminated with ';'`);
        const qualifiers = css.slice(qualifiersStart, position).trim();
        position++;

        result.push({ start, end: position, url, qualifiers });
    }

    return result;
}

// preserve supported @import media qualifiers after inlining
function wrapImportedCss(css, qualifiers) {
    if (!qualifiers) return css;
    if (/^layer(?:\s|\(|$)/i.test(qualifiers) || /^supports\s*\(/i.test(qualifiers)) {
        throw new Error(`Unsupported CSS @import qualifiers: ${qualifiers}`);
    }
    return `@media ${qualifiers} {\n${css}\n}`;
}

// parse one CSS url(...) function without changing its formatting
function readUrl(css, start) {
    if (!isUrlStart(css, start)) return null;

    let position = start + 3;

    // consume whitespace before the opening parenthesis
    while (position < css.length && /\s/.test(css[position])) position++;
    if (css[position] !== "(") return null;
    position++;

    // consume whitespace before the URL value
    while (position < css.length && /\s/.test(css[position])) position++;

    const quote = css[position] === "'" || css[position] === '"' ? css[position++] : null;
    const valueStart = position;

    // find the end of the quoted or unquoted value
    if (quote) {
        while (position < css.length) {
            if (css[position] === "\\") {
                position += Math.min(2, css.length - position);
                continue;
            }
            if (css[position] === quote) break;
            position++;
        }
        if (position >= css.length) return null;
    } else {
        while (position < css.length && css[position] !== ")") {
            if (css[position] === "\\") {
                position += Math.min(2, css.length - position);
                continue;
            }
            position++;
        }
    }

    // remove trailing whitespace from unquoted URL values
    let valueEnd = position;
    if (!quote) while (valueEnd > valueStart && /\s/.test(css[valueEnd - 1])) valueEnd--;

    const value = decodeCssString(css.slice(valueStart, valueEnd));

    // consume the closing quote and parenthesis
    if (quote) position++;
    while (position < css.length && /\s/.test(css[position])) position++;
    if (position >= css.length || css[position] !== ")") return null;
    position++;

    return { url: value, prefix: css.slice(start, valueStart), suffix: css.slice(valueEnd, position), end: position };
}

// detect the beginning of a CSS @import declaration
function isImportStart(css, position) {
    const keyword = "@import";
    if (position + keyword.length > css.length) return false;
    if (css.slice(position, position + keyword.length).toLowerCase() !== keyword) return false;
    const next = css[position + keyword.length];
    return next == null || /\s/.test(next) || next === "'" || next === '"' || next.toLowerCase() === "u";
}

// detect the beginning of a CSS url(...) function
function isUrlStart(css, position) {
    if (css.slice(position, position + 3).toLowerCase() !== "url") return false;
    const previous = position > 0 ? css[position - 1] : "";
    if (previous && /[a-zA-Z0-9_-]/.test(previous)) return false;

    let next = position + 3;
    while (next < css.length && /\s/.test(css[next])) next++;
    return css[next] === "(";
}

// find the terminating semicolon of an @import declaration
function findImportEnd(css, position) {
    let parentheses = 0;

    while (position < css.length) {
        if (css.startsWith("/*", position)) {
            const end = css.indexOf("*/", position + 2);
            return end < 0 ? css.length : findImportEnd(css, end + 2);
        }

        if (css[position] === "'" || css[position] === '"') {
            position = findStringEnd(css, position);
            continue;
        }

        if (css[position] === "(") parentheses++;
        else if (css[position] === ")") parentheses--;
        else if (css[position] === ";" && parentheses === 0) return position;

        position++;
    }

    return position;
}

// find the end of a CSS quoted string while honoring escapes
function findStringEnd(css, start) {
    const quote = css[start];
    let position = start + 1;

    while (position < css.length) {
        if (css[position] === "\\") position += Math.min(2, css.length - position);
        else if (css[position++] === quote) return position;
    }

    return css.length;
}

// skip whitespace and CSS comments
function skipTrivia(css, position) {
    while (position < css.length) {
        if (/\s/.test(css[position])) {
            position++;
        } else if (css.startsWith("/*", position)) {
            const end = css.indexOf("*/", position + 2);
            if (end < 0) return css.length;
            position = end + 2;
        } else {
            break;
        }
    }
    return position;
}

// decode simple escaped characters from CSS URL strings
function decodeCssString(value) {
    let result = "";
    for (let position = 0; position < value.length; position++) {
        if (value[position] === "\\" && position + 1 < value.length) result += value[++position];
        else result += value[position];
    }
    return result;
}

// combine application-root path segments into a directory path
function joinRootPath(...parts) {
    const path = parts
        .filter(part => typeof part === "string" && part.length > 0)
        .map(part => part.replace(/^\/+|\/+$/g, ""))
        .filter(Boolean)
        .join("/");
    return "/" + path + (path ? "/" : "");
}

// test whether one URL belongs to a URL namespace
function isInside(url, baseUrl) {
    const target = new URL(url);
    const base = new URL(baseUrl);
    return target.origin === base.origin && (target.pathname === base.pathname.slice(0, -1) || target.pathname.startsWith(base.pathname));
}

// reject URL traversal outside a logical namespace
function assertInside(url, baseUrl, message) {
    if (!isInside(url, baseUrl)) throw new Error(message);
}

// detect standard absolute or special URI schemes
function hasScheme(url) {
    return /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(url);
}
