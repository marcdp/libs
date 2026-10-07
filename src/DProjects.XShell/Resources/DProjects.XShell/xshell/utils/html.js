import {combineUrls, resolveAppUrl} from "./urls.js";
import { processStyle, rewriteStyleUrls } from "./style.js";

// rules
const rules = [
    // navigation
    { selector: "a", attr: "href", type:"navigation"},
    { selector: "area", attr: "href", type:"navigation"},
    { selector: "form", attr: "action", type:"navigation"},
    { selector: "button[formaction]", attr: "formaction", type:"navigation"},
    { selector: "x-page", attr: "src", type:"virtual_navigation"},
    { selector: "x-anchor", attr: "href", type:"virtual_navigation"},
    // resources
    { selector: "img", attr: "src", type:"resource" },
    { selector: "img", attr: "srcset", type:"resource" },
    { selector: "source", attr: "src", type:"resource" },
    { selector: "source", attr: "srcset", type:"resource" },
    { selector: "link", attr: "href", type:"resource" },
    { selector: "script", attr: "src", type:"resource" },
    { selector: "iframe", attr: "src", type:"resource" },
    { selector: "video", attr: "poster", type:"resource" },
    { selector: "audio", attr: "src", type:"resource" },
    { selector: "embed", attr: "src", type:"resource" },
    { selector: "object", attr: "data", type:"resource" },
    { selector: "object", attr: "archive", type:"resource" },
    { selector: "input[type=image]", attr: "src", type:"resource" },
    { selector: "track", attr: "src", type:"resource" },
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
        if (rule.attr == attr && element.matches(rule.selector)) {
            return attr == "srcset" && rule.type == "resource" ? rewriteSrcset(element, value, context) : rewrite(element, attr, rule.type, value, context);
        }
    }
    return value;
}

// export
export function normalizeModuleResourceUrl(url, modulePath, resourcePath) {
    if (/^url:/i.test(url)) throw new Error("The 'url:' scheme is not supported in template resource references.");
    if (url.indexOf(":") != -1 || url.startsWith("//")) {
        return url;
    } else if (url.startsWith("xshell/")) {
        return url;
    } else if (url.startsWith("/")) {
        return modulePath + url;
    } else {
        const suffixStart = url.startsWith("../") ? url.search(/[?#]/) : -1;
        return combineUrls(resourcePath, url) + (suffixStart < 0 ? "" : url.slice(suffixStart));
    }    
}

// export
export function rewrite( el, attr, type, url, context ) {
    //if (url.indexOf("colibri")!=-1) debugger;
    if (/^url:/i.test(url)) throw new Error(`The 'url:' scheme is not supported in template resource references (${attr}).`);
    if (type == "resource" && url.startsWith("app:")) {
        const appBaseUrl = new URL((context.appBasePath || "").replace(/\/?$/, "/"), window.location.origin).href;
        return resolveAppUrl(url, appBaseUrl);
    }
    if (url.indexOf(":") != -1 || url.startsWith("//")) {
        return url;
    } else if (type == "resource") {
        if (url.startsWith("#")) return url;
        if (url.startsWith("/")) {
            return context.appBasePath + context.resourceDefinition.modulePath + url;
        } else{
            const suffixStart = url.startsWith("../") ? url.search(/[?#]/) : -1;
            return context.appBasePath + combineUrls(context.resourcePath, url) + (suffixStart < 0 ? "" : url.slice(suffixStart));
        }
    } else if (type == "navigation") {
        let virtualUrl = null;
        if (url.startsWith("/")) {
            virtualUrl = context.resourceDefinition.modulePath + url;
        } else if (url.startsWith("#")) {
            virtualUrl = context.resourcePath + url;
        } else {
            const suffixStart = url.startsWith("../") ? url.search(/[?#]/) : -1;
            virtualUrl = combineUrls(context.resourcePath, url) + (suffixStart < 0 ? "" : url.slice(suffixStart));
        }
        let realUrl = null;
        if (context.navigationMode == "hash") {
            realUrl = context.navigationHashPrefix + virtualUrl;
        } else {
            realUrl = context.appBasePath + virtualUrl;
        }
        return realUrl;
    } else if (type == "virtual_navigation") {
        let virtualUrl = null;
        if (url.startsWith("/")) {
            virtualUrl = context.resourceDefinition.modulePath + url;
        } else if (url.startsWith("#")) {
            virtualUrl = context.resourcePath + url;
        } else {
            const suffixStart = url.startsWith("../") ? url.search(/[?#]/) : -1;
            virtualUrl = combineUrls(context.resourcePath, url) + (suffixStart < 0 ? "" : url.slice(suffixStart));
        }
        return virtualUrl;
    } else {
        throw new Error("Unknown rewrite type: " + type);
    }
}

// rewrite each srcset candidate without treating commas inside URLs as separators
function rewriteSrcset(el, value, context) {
    let result = "";
    let position = 0;
    let copiedUntil = 0;
    while (position < value.length) {
        while (position < value.length && /[\t\n\f\r ,]/.test(value[position])) position++;
        if (position == value.length) break;
        const urlStart = position;
        while (position < value.length && !/[\t\n\f\r ]/.test(value[position])) position++;
        let urlEnd = position;
        while (urlEnd > urlStart && value[urlEnd - 1] == ",") urlEnd--;
        result += value.slice(copiedUntil, urlStart) + rewrite(el, "srcset", "resource", value.slice(urlStart, urlEnd), context);
        copiedUntil = urlEnd;
        if (urlEnd == position) {
            let inParens = false;
            while (position < value.length) {
                if (value[position] == "(") inParens = true;
                else if (value[position] == ")") inParens = false;
                else if (value[position] == "," && !inParens) break;
                position++;
            }
        }
    }
    return result + value.slice(copiedUntil);
}

// rewrite document resource URLs
export async function rewriteDocumentUrls(doc, context) {
    // simple attribute rewrites
    for (const { selector, attr, type } of rules) {
        doc.querySelectorAll(selector).forEach(el => {
            const oldUrl = el.getAttribute(attr);
            if (!oldUrl) return;
            const newUrl = attr == "srcset" && type == "resource" ? rewriteSrcset(el, oldUrl, context) : rewrite(el, attr, type, oldUrl, context);
            if (newUrl !== oldUrl) el.setAttribute(attr, newUrl);
        });
    }
    // delegate inline declaration URL processing to the canonical CSS scanner
    doc.querySelectorAll("[style]").forEach(el => {
        const oldStyle = el.getAttribute("style");
        if (!oldStyle) return;
        const newStyle = rewriteStyleUrls({ src: getTemplateCssSource(context), context, css: oldStyle });
        el.setAttribute("style", newStyle);
    });
    // process stylesheet elements through the full shared CSS pipeline, including imports
    for (const style of doc.querySelectorAll("style")) {
        style.textContent = await processStyle({ src: getTemplateCssSource(context), context, css: style.textContent || "" });
    }
    // scripts imports
    const scripts = doc.querySelectorAll('script[type="module"]');
    for (const script of scripts) {
        if (!script.src) {
            const original = script.textContent;
            // regex to detect static imports
            const rewritten = original.replace(
                /import\s+([^'"]*)['"]([^'"]+)['"]/g,
                (match, bindings, importPath) => {
                    // normalize module paths before applying the application base path
                    const normalized = normalizeModuleResourceUrl(importPath, context.resourceDefinition.modulePath, context.resourcePath);
                    return `import ${bindings}"${normalized.startsWith("/") && !normalized.startsWith("//") ? context.appBasePath + normalized : normalized}"`;
                }
            );
            // create a new script because textContent would reset execution
            const newScript = document.createElement('script');
            newScript.type = 'module';
            newScript.textContent = rewritten;
            // replace content
            script.replaceWith(newScript);
        }
    }
    // return
    return doc;
}

// use the declaring template resource as the CSS source base
export function getTemplateCssSource(context) {
    const appBasePath = context.appBasePath || "";
    const root = /^[a-zA-Z][a-zA-Z0-9+.-]*:\/\//.test(appBasePath) ? new URL(appBasePath).pathname.replace(/\/$/, "") : appBasePath;
    return `${root}${context.resourcePath}`;
}


