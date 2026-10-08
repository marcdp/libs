import { rewriteStyleUrls } from "./style.js";

// rules
const rules = [
    { selector: "a", attr: "href", type: "navigation" },
    { selector: "area", attr: "href", type: "navigation" },
    { selector: "form", attr: "action", type: "navigation" },
    { selector: "button[formaction]", attr: "formaction", type: "navigation" },
    { selector: "x-page", attr: "src", type: "virtual_navigation" },
    { selector: "x-anchor", attr: "href", type: "virtual_navigation" },
    { selector: "img", attr: "src", type: "resource" },
    { selector: "img", attr: "srcset", type: "resource" },
    { selector: "source", attr: "src", type: "resource" },
    { selector: "source", attr: "srcset", type: "resource" },
    { selector: "link", attr: "href", type: "resource" },
    { selector: "script", attr: "src", type: "resource" },
    { selector: "iframe", attr: "src", type: "resource" },
    { selector: "video", attr: "poster", type: "resource" },
    { selector: "video", attr: "src", type: "resource" },
    { selector: "audio", attr: "src", type: "resource" },
    { selector: "embed", attr: "src", type: "resource" },
    { selector: "object", attr: "data", type: "resource" },
    { selector: "input[type=image]", attr: "src", type: "resource" },
    { selector: "track", attr: "src", type: "resource" }
];

// add a rewrite rule
export function addRewriteRule(selector, attr, type) {
    rules.push({ selector, attr, type });
}

// rewrite a static attribute emitted by a precompiled X template handler
export function rewriteTemplateAttribute(tag, attrs, attr, value, context) {
    if (!value) return value;
    const element = document.createElement(tag);
    for (const [name, staticValue] of Object.entries(attrs)) element.setAttribute(name, staticValue);
    for (const rule of rules) {
        if (rule.attr === attr && element.matches(rule.selector)) {
            return attr === "srcset" && rule.type === "resource" ? rewriteSrcset(value, context) : rewrite(element, attr, rule.type, value, context);
        }
    }
    return value;
}

// retain the public normalizer for logical module references
export function normalizeModuleResourceUrl(url, modulePath, resourcePath) {
    rejectConfigurationScheme(url, "resource");
    if (hasScheme(url) || url.startsWith("//") || url.startsWith("xshell/")) return url;
    const source = createSource({ appBasePath: "", resourceDefinition: { modulePath }, resourcePath });
    return resolveLocalUrl(url, source, "resource", true);
}

// rewrite a static HTML resource or navigation attribute
export function rewrite(el, attr, type, url, context) {
    void el;
    rejectConfigurationScheme(url, attr);
    if (hasScheme(url) || url.startsWith("//")) return url;
    const source = createSource(context);
    if (type === "resource") return url.startsWith("#") ? url : resolveLocalUrl(url, source, type, false);
    if (type === "virtual_navigation") return resolveLocalUrl(url, source, type, true);
    if (type === "navigation") {
        const virtualUrl = resolveLocalUrl(url, source, type, true);
        if (!source.moduleBase) return virtualUrl;
        return context.navigationMode === "hash" ? context.navigationHashPrefix + virtualUrl
            : source.absoluteOutput ? new URL(virtualUrl.replace(/^\/+/, ""), source.appBase).href : source.appPath + virtualUrl;
    }
    throw new Error("Unknown rewrite type: " + type);
}

// rewrite each srcset candidate without treating commas inside URLs as separators
function rewriteSrcset(value, context) {
    let result = "";
    let position = 0;
    let copiedUntil = 0;
    while (position < value.length) {
        while (position < value.length && /[\t\n\f\r ,]/.test(value[position])) position++;
        if (position === value.length) break;
        const urlStart = position;
        while (position < value.length && !/[\t\n\f\r ]/.test(value[position])) position++;
        let urlEnd = position;
        while (urlEnd > urlStart && value[urlEnd - 1] === ",") urlEnd--;
        result += value.slice(copiedUntil, urlStart) + rewrite(null, "srcset", "resource", value.slice(urlStart, urlEnd), context);
        copiedUntil = urlEnd;
        if (urlEnd === position) {
            let inParens = false;
            while (position < value.length) {
                if (value[position] === "(") inParens = true;
                else if (value[position] === ")") inParens = false;
                else if (value[position] === "," && !inParens) break;
                position++;
            }
        }
    }
    return result + value.slice(copiedUntil);
}

// rewrite document resource URLs and literal CSS with their respective semantic owners
export function rewriteDocumentUrls(doc, context) {
    for (const { selector, attr, type } of rules) {
        doc.querySelectorAll(selector).forEach(el => {
            const oldUrl = el.getAttribute(attr);
            if (!oldUrl) return;
            const newUrl = attr === "srcset" && type === "resource" ? rewriteSrcset(oldUrl, context) : rewrite(el, attr, type, oldUrl, context);
            if (newUrl !== oldUrl) el.setAttribute(attr, newUrl);
        });
    }
    const cssSource = getTemplateCssSource(context);
    doc.querySelectorAll("[style]").forEach(el => {
        const oldStyle = el.getAttribute("style");
        const newStyle = rewriteStyleUrls({ src: cssSource, context, css: oldStyle });
        if (newStyle !== oldStyle) el.setAttribute("style", newStyle);
    });
    doc.querySelectorAll("style").forEach(el => {
        el.textContent = rewriteStyleUrls({ src: cssSource, context, css: el.textContent });
    });
    // ordinary HTML templates support a limited static import form in inline module scripts
    for (const script of doc.querySelectorAll('script[type="module"]')) {
        if (script.src) continue;
        const original = script.textContent;
        const rewritten = original.replace(/(^|[;\n])([ \t]*import[ \t]+(?:[^'";\n]*?\bfrom[ \t]+)?)(['"])([^'"\n]+)\3/gm,
            (match, prefix, statement, quote, specifier) => prefix + statement + quote + (specifier.startsWith("xshell/") ? specifier : rewrite(null, "import", "resource", specifier, context)) + quote);
        if (rewritten === original) continue;
        const replacement = document.createElement("script");
        replacement.type = "module";
        replacement.textContent = rewritten;
        script.replaceWith(replacement);
    }
    return doc;
}

// use the actual declaring URL for custom resources and the logical URL for module resources
export function getTemplateCssSource(context) {
    const source = createSource(context);
    return source.moduleBase ? new URL(source.requestUrl).pathname : source.requestUrl;
}

// create the declaring resource and the optional module namespace
function createSource(context) {
    const origin = window.location.origin;
    const appBasePath = context.appBasePath || "";
    const appBase = new URL(appBasePath.replace(/\/?$/, "/") || "/", origin);
    const modulePath = context.resourceDefinition?.modulePath;
    const moduleBase = modulePath == null ? null : new URL(modulePath.replace(/^\/+|\/+$/g, "") + "/", appBase);
    const resourcePath = context.resourcePath;
    const requestUrl = moduleBase ? new URL(resourcePath.replace(/^\/+/, ""), appBase).href : new URL(resourcePath, appBase).href;
    return { appBase, appPath: appBase.pathname.replace(/\/$/, ""), moduleBase, requestUrl, absoluteOutput: hasScheme(appBasePath) };
}

// resolve one URL within a module or against a custom declaring URL
function resolveLocalUrl(url, source, type, logical) {
    const resolved = source.moduleBase && url.startsWith("/")
        ? new URL(url.replace(/^\/+/, ""), source.moduleBase)
        : new URL(url, source.requestUrl);
    if (source.moduleBase && !insideModule(resolved, source.moduleBase)) {
        throw new Error(`Template ${type} URL '${url}' escapes the module root.`);
    }
    if (!source.moduleBase) return resolved.href;
    return logical ? resolved.pathname.slice(source.appPath.length) + resolved.search + resolved.hash
        : source.absoluteOutput ? resolved.href : resolved.pathname + resolved.search + resolved.hash;
}

// keep configuration-only schemes out of authored runtime references
function rejectConfigurationScheme(url, attr) {
    const scheme = /^(app|url):/i.exec(url)?.[1]?.toLowerCase();
    if (scheme) throw new Error(`The '${scheme}:' scheme is not supported in template resource references (${attr}).`);
}

// verify a normalized URL remains inside its module namespace
function insideModule(url, base) {
    return url.origin === base.origin && url.pathname.startsWith(base.pathname);
}

// detect actual URI schemes without mistaking colons in query strings for schemes
function hasScheme(url) {
    return /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(url);
}
