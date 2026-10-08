
// class
export function absolutizeUrl(url) {
    if (url.startsWith("/")) url = window.location.origin + url;
    return url;
}
export function resolveAppUrl(value, appBaseUrl) {
    // resolve the logical application namespace without allowing traversal above its root
    const logicalRoot = new URL("https://xshell-app.invalid/__xshell_app_root__/");
    const authored = value.substring(4).trim().replace(/^\/+/, "");
    const logicalUrl = new URL(authored, logicalRoot);
    if (logicalUrl.origin !== logicalRoot.origin || !logicalUrl.pathname.startsWith(logicalRoot.pathname)) {
        throw new Error(`Application URL '${value}' escapes the application root.`);
    }
    const relative = logicalUrl.pathname.substring(logicalRoot.pathname.length) + logicalUrl.search + logicalUrl.hash;
    return new URL(relative, appBaseUrl).href;
}
export function combineUrls(a, b) {
    if (a.indexOf("?") != -1) a = a.substring(0, a.indexOf("?"));
    if (/^[A-Za-z][A-Za-z0-9+.-]*:/.test(b)) return b;
    if (b.startsWith("/")) {
        if (a.indexOf("://") != -1) {
            let i = a.indexOf("/", a.indexOf("://") + 3);
            if (i != -1) a = a.substring(0, i);
            return a + b;
        }
        return b;
    } else if (b.startsWith("./") || b==".") {
        if (a.endsWith("/")) {
            a = a.substring(0, a.length - 1);
        } else if (a.length > 0) {
            a = a.substring(0, a.lastIndexOf("/"));
        }
        return a + b.substring(1);
    } else if (b.startsWith("../")) {
        if (a.endsWith("/")) {
            a = a.substring(0, a.length - 1);
        } else if (a.length > 0) {
            a = a.substring(0, a.lastIndexOf("/"));
        }
        let result = a + "/" + b;
        if (result.startsWith("/")) {
            const normalizedUrl = new URL(result, window.location.origin);
            result = normalizedUrl.pathname;
        } else {
            result = (new URL(result)).toString();
        }
        return result;
    } else {
        if (a.endsWith("/")) { 
            //empty
        } else if (a.indexOf("/") != -1) {
            a = a.substring(0, a.lastIndexOf("/") + 1);
        }
        return a + b;
    }
};

