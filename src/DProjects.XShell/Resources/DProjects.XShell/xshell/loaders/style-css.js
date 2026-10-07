// class
export default class LoaderStyleCss {

    // load the root stylesheet
    async load(src, context) {
        const bases = this._createBases(context);
        const source = this._createInitialSource(src, bases);
        const css = await this._loadCss(source, bases, new Set());

        // create the browser stylesheet
        const styleSheet = new CSSStyleSheet();
        await styleSheet.replace(css);
        return styleSheet;
    }

    // fetch and process one stylesheet
    async _loadCss(source, bases, loading) {
        if (loading.has(source.requestUrl)) throw new Error(`Circular CSS @import detected: ${source.requestUrl}`);
        loading.add(source.requestUrl);

        try {
            // fetch the stylesheet
            const response = await fetch(source.requestUrl);
            if (!response.ok) throw new Error(`Error ${response.status}: ${response.statusText}: ${source.requestUrl}`);
            let css = await response.text();

            // inline imports before rewriting resource URLs
            css = await this._resolveImports(css, source, bases, loading);
            css = this._resolveUrls(css, source, bases);
            return css;
        } finally {
            loading.delete(source.requestUrl);
        }
    }

    // recursively resolve and inline CSS imports
    async _resolveImports(css, source, bases, loading) {
        const imports = this._findImports(css);
        if (!imports.length) return css;

        let result = "";
        let position = 0;

        for (const item of imports) {
            result += css.slice(position, item.start);
            const importedSource = this._resolveImportSource(item.url, source, bases);
            const importedCss = await this._loadCss(importedSource, bases, loading);
            result += this._wrapImportedCss(importedCss, item.qualifiers);
            position = item.end;
        }

        result += css.slice(position);
        return result;
    }

    // rewrite CSS url(...) references
    _resolveUrls(css, source, bases) {
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
                const end = this._findStringEnd(css, position);
                result += css.slice(position, end);
                position = end;
                continue;
            }

            // rewrite url(...) references
            const parsed = this._readUrl(css, position);
            if (parsed) {
                result += parsed.prefix;
                result += this._resolveResourceUrl(parsed.url, source, bases);
                result += parsed.suffix;
                position = parsed.end;
                continue;
            }

            result += css[position++];
        }

        return result;
    }

    // resolve one CSS resource URL according to XShell URL semantics
    _resolveResourceUrl(value, source, bases) {
        const url = value.trim();

        // preserve empty and fragment-only references
        if (!url || url.startsWith("#")) return url;

        // resolve application-root URLs
        if (url.startsWith("app:")) return this._resolveAppUrl(url, bases);

        // resolve physical declaring-file URLs
        if (url.startsWith("url:")) {
            if (!source.physicalUrl) throw new Error(`Cannot resolve physical URL '${url}' from '${source.requestUrl}': module assetsUrl is unavailable.`);
            return new URL(url.substring(4).trim(), source.physicalUrl).href;
        }

        // preserve protocol-relative URLs
        if (url.startsWith("//")) return url;

        // preserve absolute and special URLs
        if (this._hasScheme(url)) return url;

        // resolve /foo against the module root
        if (url.startsWith("/") && source.scope === "module") {
            return new URL(url.replace(/^\/+/, ""), bases.moduleVirtualBaseUrl).href;
        }

        // resolve /foo against the application root for app-scoped CSS
        if (url.startsWith("/") && source.scope === "app") {
            return new URL(url.replace(/^\/+/, ""), bases.appBaseUrl).href;
        }

        // use normal URL semantics for external or physical CSS
        if (url.startsWith("/")) return new URL(url, source.requestUrl).href;

        // resolve ./foo and ../foo relative to the logical module stylesheet
        if (source.scope === "module" && source.moduleUrl) {
            const resolved = new URL(url, source.moduleUrl).href;
            this._assertInside(resolved, bases.moduleVirtualBaseUrl, `Module URL '${url}' in '${source.moduleUrl}' escapes the module root.`);
            return resolved;
        }

        // resolve relative app URLs without escaping the application root
        if (source.scope === "app") {
            const resolved = new URL(url, source.requestUrl).href;
            this._assertInside(resolved, bases.appBaseUrl, `Application URL '${url}' in '${source.requestUrl}' escapes the application root.`);
            return resolved;
        }

        // resolve normal relative URLs for external or physical CSS
        return new URL(url, source.requestUrl).href;
    }

    // resolve one CSS @import into its virtual and physical coordinates
    _resolveImportSource(value, source, bases) {
        const url = value.trim();
        if (!url) throw new Error(`CSS @import in '${source.requestUrl}' declares an empty URL.`);

        // resolve app: imports
        if (url.startsWith("app:")) {
            const requestUrl = this._resolveAppUrl(url, bases);
            return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: "app" };
        }

        // resolve url: imports against the physical declaring stylesheet
        if (url.startsWith("url:")) {
            if (!source.physicalUrl) throw new Error(`Cannot resolve physical CSS @import '${url}' from '${source.requestUrl}': module assetsUrl is unavailable.`);
            const requestUrl = new URL(url.substring(4).trim(), source.physicalUrl).href;
            const moduleUrl = this._mapPhysicalToVirtual(requestUrl, bases);
            return { requestUrl, moduleUrl, physicalUrl: requestUrl, scope: moduleUrl ? "module" : "physical" };
        }

        // resolve absolute and protocol-relative imports
        if (url.startsWith("//") || this._hasScheme(url)) {
            const requestUrl = new URL(url, source.requestUrl).href;
            return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: "external" };
        }

        // resolve module-root imports
        if (url.startsWith("/") && source.scope === "module") {
            const moduleUrl = new URL(url.replace(/^\/+/, ""), bases.moduleVirtualBaseUrl).href;
            return {
                requestUrl: moduleUrl,
                moduleUrl,
                physicalUrl: this._mapVirtualToPhysical(moduleUrl, bases),
                scope: "module"
            };
        }

        // resolve application-root imports
        if (url.startsWith("/") && source.scope === "app") {
            const requestUrl = new URL(url.replace(/^\/+/, ""), bases.appBaseUrl).href;
            return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: "app" };
        }

        // resolve root URLs normally for external or physical CSS
        if (url.startsWith("/")) {
            const requestUrl = new URL(url, source.requestUrl).href;
            return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: source.scope };
        }

        // resolve module-relative imports
        if (source.scope === "module" && source.moduleUrl) {
            const moduleUrl = new URL(url, source.moduleUrl).href;
            this._assertInside(moduleUrl, bases.moduleVirtualBaseUrl, `Module CSS @import '${url}' in '${source.moduleUrl}' escapes the module root.`);
            return {
                requestUrl: moduleUrl,
                moduleUrl,
                physicalUrl: this._mapVirtualToPhysical(moduleUrl, bases),
                scope: "module"
            };
        }

        // resolve application-relative imports
        if (source.scope === "app") {
            const requestUrl = new URL(url, source.requestUrl).href;
            this._assertInside(requestUrl, bases.appBaseUrl, `Application CSS @import '${url}' in '${source.requestUrl}' escapes the application root.`);
            return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: "app" };
        }

        // resolve external or explicitly physical relative imports
        const requestUrl = new URL(url, source.requestUrl).href;
        return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: source.scope };
    }

    // create application, virtual-module, and physical-module bases
    _createBases(context) {
        const modulePath = context.resourceDefinition?.modulePath;
        if (typeof modulePath !== "string" || !modulePath.startsWith("/")) throw new Error("style-css requires resourceDefinition.modulePath.");

        const appBasePath = context.appBasePath || "";
        const appBaseUrl = new URL(this._joinRootPath(appBasePath), window.location.origin).href;
        const moduleVirtualBaseUrl = new URL(this._joinRootPath(appBasePath, modulePath), window.location.origin).href;
        const moduleAssetsUrl = context.moduleAssetsUrl ?? context.resourceDefinition?.assetsUrl ?? null;
        const modulePhysicalBaseUrl = moduleAssetsUrl ? this._ensureDirectoryUrl(moduleAssetsUrl) : null;

        return { appBaseUrl, moduleVirtualBaseUrl, modulePhysicalBaseUrl };
    }

    // describe the root stylesheet in virtual and physical coordinates
    _createInitialSource(src, bases) {
        // resolve app: root stylesheets
        if (src.startsWith("app:")) {
            const requestUrl = this._resolveAppUrl(src, bases);
            return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: "app" };
        }

        // resolve url: root stylesheets
        if (src.startsWith("url:")) {
            if (!bases.modulePhysicalBaseUrl) throw new Error(`Cannot resolve physical stylesheet '${src}': module assetsUrl is unavailable.`);
            const requestUrl = new URL(src.substring(4).trim(), bases.modulePhysicalBaseUrl).href;
            const moduleUrl = this._mapPhysicalToVirtual(requestUrl, bases);
            return { requestUrl, moduleUrl, physicalUrl: requestUrl, scope: moduleUrl ? "module" : "physical" };
        }

        // resolve external root stylesheets
        if (src.startsWith("//") || this._hasScheme(src)) {
            const requestUrl = new URL(src, window.location.origin).href;
            return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: "external" };
        }

        // resolve the normal virtual module stylesheet
        const requestUrl = new URL(src, bases.moduleVirtualBaseUrl).href;
        if (this._isInside(requestUrl, bases.moduleVirtualBaseUrl)) {
            return {
                requestUrl,
                moduleUrl: requestUrl,
                physicalUrl: this._mapVirtualToPhysical(requestUrl, bases),
                scope: "module"
            };
        }

        // fall back to normal external URL semantics
        return { requestUrl, moduleUrl: null, physicalUrl: requestUrl, scope: "external" };
    }

    // resolve app: using the application root while preventing traversal above it
    _resolveAppUrl(value, bases) {
        const logicalRoot = new URL("https://xshell-app.invalid/__xshell_app_root__/");
        const authored = value.substring(4).trim().replace(/^\/+/, "");
        const logicalUrl = new URL(authored, logicalRoot);

        if (logicalUrl.origin !== logicalRoot.origin || !logicalUrl.pathname.startsWith(logicalRoot.pathname)) {
            throw new Error(`Application URL '${value}' escapes the application root.`);
        }

        const relative = logicalUrl.pathname.substring(logicalRoot.pathname.length) + logicalUrl.search + logicalUrl.hash;
        return new URL(relative, bases.appBaseUrl).href;
    }

    // map a module virtual URL to the corresponding physical assetsUrl
    _mapVirtualToPhysical(url, bases) {
        if (!bases.modulePhysicalBaseUrl) return null;

        const target = new URL(url);
        const virtualBase = new URL(bases.moduleVirtualBaseUrl);
        if (target.origin !== virtualBase.origin || !target.pathname.startsWith(virtualBase.pathname)) return null;

        const relativePath = target.pathname.substring(virtualBase.pathname.length);
        const physical = new URL(relativePath, bases.modulePhysicalBaseUrl);
        physical.search = target.search;
        physical.hash = target.hash;
        return physical.href;
    }

    // map a physical module URL back into the virtual module namespace
    _mapPhysicalToVirtual(url, bases) {
        if (!bases.modulePhysicalBaseUrl) return null;

        const target = new URL(url);
        const physicalBase = new URL(bases.modulePhysicalBaseUrl);
        if (target.origin !== physicalBase.origin || !target.pathname.startsWith(physicalBase.pathname)) return null;

        const relativePath = target.pathname.substring(physicalBase.pathname.length);
        const virtual = new URL(relativePath, bases.moduleVirtualBaseUrl);
        virtual.search = target.search;
        virtual.hash = target.hash;
        return virtual.href;
    }

    // scan CSS and return every @import declaration
    _findImports(css) {
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
                position = this._findStringEnd(css, position);
                continue;
            }

            // continue until an @import is found
            if (!this._isImportStart(css, position)) {
                position++;
                continue;
            }

            // read the @import URL
            const start = position;
            position += 7;
            position = this._skipTrivia(css, position);
            let url = null;

            if (css[position] === "'" || css[position] === '"') {
                const quoteStart = position;
                const end = this._findStringEnd(css, position);
                if (end <= quoteStart + 1 || css[end - 1] !== css[quoteStart]) throw new Error(`Invalid CSS @import near offset ${start}`);
                url = this._decodeCssString(css.slice(quoteStart + 1, end - 1));
                position = end;
            } else {
                const parsed = this._readUrl(css, position);
                if (!parsed) throw new Error(`Invalid CSS @import near offset ${start}`);
                url = parsed.url;
                position = parsed.end;
            }

            // read media or other import qualifiers
            const qualifiersStart = position;
            position = this._findImportEnd(css, position);
            if (position >= css.length || css[position] !== ";") throw new Error(`CSS @import near offset ${start} is not terminated with ';'`);
            const qualifiers = css.slice(qualifiersStart, position).trim();
            position++;

            result.push({ start, end: position, url, qualifiers });
        }

        return result;
    }

    // preserve supported @import media qualifiers after inlining
    _wrapImportedCss(css, qualifiers) {
        if (!qualifiers) return css;
        if (/^layer(?:\s|\(|$)/i.test(qualifiers) || /^supports\s*\(/i.test(qualifiers)) {
            throw new Error(`Unsupported CSS @import qualifiers: ${qualifiers}`);
        }
        return `@media ${qualifiers} {\n${css}\n}`;
    }

    // parse one CSS url(...) function without changing its formatting
    _readUrl(css, start) {
        if (!this._isUrlStart(css, start)) return null;

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

        const value = this._decodeCssString(css.slice(valueStart, valueEnd));

        // consume the closing quote and parenthesis
        if (quote) position++;
        while (position < css.length && /\s/.test(css[position])) position++;
        if (position >= css.length || css[position] !== ")") return null;
        position++;

        return { url: value, prefix: css.slice(start, valueStart), suffix: css.slice(valueEnd, position), end: position };
    }

    // detect the beginning of a CSS @import declaration
    _isImportStart(css, position) {
        const keyword = "@import";
        if (position + keyword.length > css.length) return false;
        if (css.slice(position, position + keyword.length).toLowerCase() !== keyword) return false;
        const next = css[position + keyword.length];
        return next == null || /\s/.test(next) || next === "'" || next === '"' || next.toLowerCase() === "u";
    }

    // detect the beginning of a CSS url(...) function
    _isUrlStart(css, position) {
        if (css.slice(position, position + 3).toLowerCase() !== "url") return false;
        const previous = position > 0 ? css[position - 1] : "";
        if (previous && /[a-zA-Z0-9_-]/.test(previous)) return false;

        let next = position + 3;
        while (next < css.length && /\s/.test(css[next])) next++;
        return css[next] === "(";
    }

    // find the terminating semicolon of an @import declaration
    _findImportEnd(css, position) {
        let parentheses = 0;

        while (position < css.length) {
            if (css.startsWith("/*", position)) {
                const end = css.indexOf("*/", position + 2);
                return end < 0 ? css.length : this._findImportEnd(css, end + 2);
            }

            if (css[position] === "'" || css[position] === '"') {
                position = this._findStringEnd(css, position);
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
    _findStringEnd(css, start) {
        const quote = css[start];
        let position = start + 1;

        while (position < css.length) {
            if (css[position] === "\\") position += Math.min(2, css.length - position);
            else if (css[position++] === quote) return position;
        }

        return css.length;
    }

    // skip whitespace and CSS comments
    _skipTrivia(css, position) {
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
    _decodeCssString(value) {
        let result = "";
        for (let position = 0; position < value.length; position++) {
            if (value[position] === "\\" && position + 1 < value.length) result += value[++position];
            else result += value[position];
        }
        return result;
    }

    // combine application-root path segments into a directory path
    _joinRootPath(...parts) {
        const path = parts
            .filter(part => typeof part === "string" && part.length > 0)
            .map(part => part.replace(/^\/+|\/+$/g, ""))
            .filter(Boolean)
            .join("/");
        return "/" + path + (path ? "/" : "");
    }

    // normalize an absolute URL as a directory base
    _ensureDirectoryUrl(value) {
        const url = new URL(value);
        url.search = "";
        url.hash = "";
        if (!url.pathname.endsWith("/")) url.pathname += "/";
        return url.href;
    }

    // test whether one URL belongs to a URL namespace
    _isInside(url, baseUrl) {
        const target = new URL(url);
        const base = new URL(baseUrl);
        return target.origin === base.origin && (target.pathname === base.pathname.slice(0, -1) || target.pathname.startsWith(base.pathname));
    }

    // reject URL traversal outside a logical namespace
    _assertInside(url, baseUrl, message) {
        if (!this._isInside(url, baseUrl)) throw new Error(message);
    }

    // detect standard absolute or special URI schemes
    _hasScheme(url) {
        return /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(url);
    }
}