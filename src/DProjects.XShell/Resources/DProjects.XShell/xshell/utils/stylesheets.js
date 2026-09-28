// loadStyleSheetRecursive.js

export async function loadStyleSheetRecursive(src) {

    const loaded = new Set();

    async function loadCss(url) {

        // Convert to absolute URL
        const absoluteUrl = new URL(url, document.baseURI).href;

        // Avoid circular/duplicate imports
        if (loaded.has(absoluteUrl)) {
            return "";
        }

        loaded.add(absoluteUrl);

        // Load CSS
        const response = await fetch(absoluteUrl);

        if (!response.ok) {
            throw new Error(
                `Unable to load stylesheet '${absoluteUrl}': ` +
                `${response.status} ${response.statusText}`
            );
        }

        let css = await response.text();

        // Resolve relative url(...) references before merging the stylesheet
        css = resolveUrls(css, absoluteUrl);

        // Resolve @imports recursively
        css = await resolveImports(css, absoluteUrl);

        return css;
    }

    async function resolveImports(css, baseUrl) {

        // Supports:
        //
        // @import "file.css";
        // @import 'file.css';
        // @import url("file.css");
        // @import url('file.css');
        // @import url(file.css);

        const regexp =
            /@import\s+(?:url\(\s*)?(?:"([^"]+)"|'([^']+)'|([^"')\s]+))\s*\)?\s*;/gi;

        let result = "";
        let lastIndex = 0;

        for (const match of css.matchAll(regexp)) {

            result += css.substring(lastIndex, match.index);

            const importSrc =
                match[1] ??
                match[2] ??
                match[3];

            const importUrl = new URL(importSrc, baseUrl).href;

            const importedCss = await loadCss(importUrl);

            result += importedCss;

            lastIndex = match.index + match[0].length;
        }

        result += css.substring(lastIndex);

        return result;
    }

    function resolveUrls(css, baseUrl) {

        return css.replace(
            /url\(\s*(?:"([^"]+)"|'([^']+)'|([^"')\s]+))\s*\)/gi,
            (match, doubleQuoted, singleQuoted, unquoted) => {

                const value =
                    doubleQuoted ??
                    singleQuoted ??
                    unquoted;

                // Leave absolute/special URLs untouched
                if (
                    value.startsWith("data:") ||
                    value.startsWith("blob:") ||
                    value.startsWith("#") ||
                    /^[a-z][a-z0-9+.-]*:/i.test(value)
                ) {
                    return match;
                }

                const absoluteUrl = new URL(value, baseUrl).href;

                return `url("${absoluteUrl}")`;
            }
        );
    }

    const css = await loadCss(src);

    const styleSheet = new CSSStyleSheet();

    await styleSheet.replace(css);

    return styleSheet;
}