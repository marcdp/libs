
// class
export function absolutizeUrl(url) {
    if (url.startsWith("/")) url = window.location.origin + url;
    return url;
}
export function combineUrls(a, b) {
    if (a.indexOf("?") != -1) a = a.substring(0, a.indexOf("?"));
    if (b.indexOf(":") != -1) return b;
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

