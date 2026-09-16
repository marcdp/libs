import {combineUrls} from "../utils/urls.js";

// rules
const rules = [
    // navigation
    { selector: "a", attr: "href", type:"navigation"},
    { selector: "area", attr: "href", type:"navigation"},
    { selector: "form", attr: "action", type:"navigation"},
    { selector: "button[formaction]", attr: "formaction", type:"navigation"},
    { selector: "x-page", attr: "src", type:"virtual_navigation"},
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

// export
export function normalizeModuleResourceUrl(url, modulePath, resourcePath) {
    if (url.indexOf(":") != -1) {
        return url;
    } else if (url.startsWith("xshell/")) {
        return url;
    } else if (url.startsWith("/")) {
        return modulePath + url;
    } else {
        return combineUrls(resourcePath, url);
    }    
}

// export
export function rewrite( el, attr, type, url, context ) {
    //if (url.indexOf("colibri")!=-1) debugger;
    if (url.indexOf(":") != -1) {
        return url;
    } else if (url.startsWith("xshell/")) {
        return url;
    } else if (type == "resource") {
        if (url.startsWith("/")) {
            return context.appBase + context.resourceDefinition.modulePath + url;
        } else{
            return context.appBase + combineUrls(context.resourcePath, url);
        }
    } else if (type == "navigation") {
        let virtualUrl = null;
        if (url.startsWith("/")) {
            virtualUrl = context.resourceDefinition.modulePath + url;
        } else if (url.startsWith("#")) {
            virtualUrl = context.resourcePath + url;
        } else {
            virtualUrl = combineUrls(context.resourcePath, url);
        }
        let realUrl = null;
        if (context.navigationMode == "hash") {
            realUrl = context.navigationHashPrefix + virtualUrl;
        } else {
            realUrl = context.appBase + virtualUrl;
        }
        return realUrl;
    } else if (type == "virtual_navigation") {
        let virtualUrl = null;
        if (url.startsWith("/")) {
            virtualUrl = context.resourceDefinition.modulePath + url;
        } else if (url.startsWith("#")) {
            virtualUrl = context.resourcePath + url;
        } else {
            virtualUrl = combineUrls(context.resourcePath, url);
        }
        return virtualUrl;
    } else {
        throw new Error("Unknown rewrite type: " + type);
    }
}

// rewrite document resource URLs
export function rewriteDocumentUrls(doc, context) {
    // simple attribute rewrites
    for (const { selector, attr, type } of rules) {
        doc.querySelectorAll(selector).forEach(el => {
            const oldUrl = el.getAttribute(attr);
            if (!oldUrl) return;
            const newUrl = rewrite(el, attr, type, oldUrl, context);
            if (newUrl !== oldUrl) el.setAttribute(attr, newUrl);
        });
    }
    // inline css styles
    doc.querySelectorAll("[style]").forEach(el => {
        const oldStyle = el.getAttribute("style");
        if (!oldStyle) return;
        const newStyle = oldStyle.replace(/url\(([^)]+)\)/g, (match, url) => {
            // strip quotes
            const clean = url.trim().replace(/^['"]|['"]$/g, "");
            return `url("${rewrite(el, "style", "", clean, context)}")`;
        });
        el.setAttribute("style", newStyle);
    });
    // CSS inside <style> tags
    doc.querySelectorAll("style").forEach(style => {
        let css = style.textContent;
        css = css.replace(/url\(([^)]+)\)/g, (match, url) => {
            const clean = url.trim().replace(/^['"]|['"]$/g, "");
            return `url("${rewrite(style, "textContent", "", clean, context)}")`;
        });
        style.textContent = css;
    });
    // scripts imports
    const scripts = doc.querySelectorAll('script[type="module"]');
    for (const script of scripts) {
        if (!script.src) {
            const original = script.textContent;
            // regex to detect static imports
            const rewritten = original.replace(
                /import\s+([^'"]*)['"]([^'"]+)['"]/g,
                (match, bindings, importPath) => {
                    // resolve url
                    return `import ${bindings}"${rewrite("script", "textContent", "",  importPath, context)}"`;
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


