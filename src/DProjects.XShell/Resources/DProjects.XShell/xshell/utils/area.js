export function normalizeAreaPrefix(prefix) {
    if (!prefix || prefix === "/") return "";
    return "/" + prefix.replace(/^\/+|\/+$/g, "");
}
