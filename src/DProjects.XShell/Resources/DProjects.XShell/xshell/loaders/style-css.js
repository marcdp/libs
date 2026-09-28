// export
export default class LoaderStyleCss {

    // methods
    async load(src, context) {
        const moduleBasePath = context.resourceDefinition.modulePath;
        const css = await this._loadCss(src, moduleBasePath, new Set());

        // create stylesheet
        const styleSheet = new CSSStyleSheet();
        await styleSheet.replace(css);

        return styleSheet;
    }

    // methods (private)
    async _loadCss(src, moduleBasePath, loading) {
        const sourceUrl = this._resolveModuleUrl(src, moduleBasePath);

        // detect circular imports
        if (loading.has(sourceUrl)) throw new Error(`Circular CSS @import detected: ${sourceUrl}`);
        loading.add(sourceUrl);

        try {
            // fetch stylesheet
            const response = await fetch(sourceUrl);
            if (!response.ok) throw new Error(`Error ${response.status}: ${response.statusText}: ${sourceUrl}`);

            let css = await response.text();

            // recursively inline imported stylesheets
            css = await this._resolveImports(css, moduleBasePath, loading);

            // rewrite resource urls using the root module path
            css = this._resolveUrls(css, moduleBasePath);

            return css;
        } finally {
            loading.delete(sourceUrl);
        }
    }

    async _resolveImports(css, moduleBasePath, loading) {
        const imports = this._findImports(css);
        if (!imports.length) return css;

        let result = "";
        let position = 0;

        for (const item of imports) {
            result += css.slice(position, item.start);

            // import URLs are always relative to the root module path, not to the importing stylesheet
            const importUrl = this._resolveModuleUrl(item.url, moduleBasePath);
            const importedCss = await this._loadCss(importUrl, moduleBasePath, loading);

            result += this._wrapImportedCss(importedCss, item.qualifiers);
            position = item.end;
        }

        result += css.slice(position);
        return result;
    }

    _resolveUrls(css, moduleBasePath) {
        let result = "";
        let position = 0;

        while (position < css.length) {
            // comments
            if (css.startsWith("/*", position)) {
                const end = css.indexOf("*/", position + 2);
                const next = end < 0 ? css.length : end + 2;
                result += css.slice(position, next);
                position = next;
                continue;
            }

            // ordinary strings
            if (css[position] == "'" || css[position] == '"') {
                const end = this._findStringEnd(css, position);
                result += css.slice(position, end);
                position = end;
                continue;
            }

            // url(...)
            const parsed = this._readUrl(css, position);
            if (parsed) {
                result += parsed.prefix;
                result += this._resolveResourceUrl(parsed.url, moduleBasePath);
                result += parsed.suffix;
                position = parsed.end;
                continue;
            }

            result += css[position++];
        }

        return result;
    }

    _resolveModuleUrl(url, moduleBasePath) {
        url = url.trim();

        // external/absolute URLs
        if (this._hasScheme(url) || url.startsWith("//")) return url;

        // all local URLs are relative to the root of the module
        const base = new URL(moduleBasePath.replace(/\/+$/, "") + "/", window.location.origin);
        return new URL(url.replace(/^\/+/, ""), base).toString();
    }

    _resolveResourceUrl(url, moduleBasePath) {
        url = url.trim();

        // do not rewrite URLs that do not belong to the module
        if (!url || url.startsWith("#") || this._hasScheme(url) || url.startsWith("//")) return url;

        return this._resolveModuleUrl(url, moduleBasePath);
    }

    _findImports(css) {
        const result = [];
        let position = 0;

        while (position < css.length) {
            // comments
            if (css.startsWith("/*", position)) {
                const end = css.indexOf("*/", position + 2);
                position = end < 0 ? css.length : end + 2;
                continue;
            }

            // strings
            if (css[position] == "'" || css[position] == '"') {
                position = this._findStringEnd(css, position);
                continue;
            }

            // @import
            if (!this._isImportStart(css, position)) {
                position++;
                continue;
            }

            const start = position;
            position += 7;

            // whitespace/comments between @import and URL
            position = this._skipTrivia(css, position);

            let url = null;

            // @import "file.css"
            if (css[position] == "'" || css[position] == '"') {
                const quoteStart = position;
                const end = this._findStringEnd(css, position);
                if (end <= quoteStart + 1 || css[end - 1] != css[quoteStart]) {
                    throw new Error(`Invalid CSS @import near offset ${start}`);
                }

                url = this._decodeCssString(css.slice(quoteStart + 1, end - 1));
                position = end;
            } else {
                // @import url(...)
                const parsed = this._readUrl(css, position);
                if (!parsed) {
                    throw new Error(`Invalid CSS @import near offset ${start}`);
                }

                url = parsed.url;
                position = parsed.end;
            }

            // everything after the URL up to ';' represents import qualifiers/media query
            const qualifiersStart = position;
            position = this._findImportEnd(css, position);

            if (position >= css.length || css[position] != ";") {
                throw new Error(`CSS @import near offset ${start} is not terminated with ';'`);
            }

            const qualifiers = css.slice(qualifiersStart, position).trim();
            position++;

            result.push({
                start,
                end: position,
                url,
                qualifiers
            });
        }

        return result;
    }

    _wrapImportedCss(css, qualifiers) {
        if (!qualifiers) return css;

        /*
         * Standard media-query imports:
         *
         *   @import "/print.css" print;
         *
         * become:
         *
         *   @media print {
         *       ...
         *   }
         *
         * CSS import layer/supports qualifiers need semantic wrappers and are
         * intentionally handled explicitly rather than silently changing them.
         */
        if (/^layer(?:\s|\(|$)/i.test(qualifiers) || /^supports\s*\(/i.test(qualifiers)) {
            throw new Error(`Unsupported CSS @import qualifiers: ${qualifiers}`);
        }

        return `@media ${qualifiers} {\n${css}\n}`;
    }

    _readUrl(css, start) {
        if (!this._isUrlStart(css, start)) return null;

        let position = start + 3;

        // whitespace between url and (
        while (position < css.length && /\s/.test(css[position])) position++;
        if (css[position] != "(") return null;
        position++;

        // preserve trivia after (
        while (position < css.length && /\s/.test(css[position])) position++;

        const quote = css[position] == "'" || css[position] == '"' ? css[position++] : null;
        const valueStart = position;

        if (quote) {
            while (position < css.length) {
                if (css[position] == "\\") {
                    position += Math.min(2, css.length - position);
                    continue;
                }
                if (css[position] == quote) break;
                position++;
            }

            if (position >= css.length) return null;
        } else {
            while (position < css.length && css[position] != ")") {
                if (css[position] == "\\") {
                    position += Math.min(2, css.length - position);
                    continue;
                }
                position++;
            }
        }

        let valueEnd = position;
        if (!quote) {
            while (valueEnd > valueStart && /\s/.test(css[valueEnd - 1])) valueEnd--;
        }

        const value = this._decodeCssString(css.slice(valueStart, valueEnd));

        if (quote) position++;

        while (position < css.length && /\s/.test(css[position])) position++;
        if (position >= css.length || css[position] != ")") return null;

        position++;

        return {
            url: value,
            prefix: css.slice(start, valueStart),
            suffix: css.slice(valueEnd, position),
            end: position
        };
    }

    _isImportStart(css, position) {
        const keyword = "@import";

        if (position + keyword.length > css.length) return false;
        if (css.slice(position, position + keyword.length).toLowerCase() != keyword) return false;

        const next = css[position + keyword.length];
        return next == null || /\s/.test(next) || next == "'" || next == '"' || next.toLowerCase() == "u";
    }

    _isUrlStart(css, position) {
        if (css.slice(position, position + 3).toLowerCase() != "url") return false;

        const previous = position > 0 ? css[position - 1] : "";
        if (previous && /[a-zA-Z0-9_-]/.test(previous)) return false;

        let next = position + 3;
        while (next < css.length && /\s/.test(css[next])) next++;

        return css[next] == "(";
    }

    _findImportEnd(css, position) {
        let parentheses = 0;

        while (position < css.length) {
            if (css.startsWith("/*", position)) {
                const end = css.indexOf("*/", position + 2);
                return end < 0 ? css.length : this._findImportEnd(css, end + 2);
            }

            if (css[position] == "'" || css[position] == '"') {
                position = this._findStringEnd(css, position);
                continue;
            }

            if (css[position] == "(") {
                parentheses++;
            } else if (css[position] == ")") {
                parentheses--;
            } else if (css[position] == ";" && parentheses == 0) {
                return position;
            }

            position++;
        }

        return position;
    }

    _findStringEnd(css, start) {
        const quote = css[start];
        let position = start + 1;

        while (position < css.length) {
            if (css[position] == "\\") {
                position += Math.min(2, css.length - position);
            } else if (css[position++] == quote) {
                return position;
            }
        }

        return css.length;
    }

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

    _decodeCssString(value) {
        // Preserve CSS escapes except the escaped character itself.
        // URL values produced by the server compiler normally need no decoding.
        let result = "";

        for (let position = 0; position < value.length; position++) {
            if (value[position] == "\\" && position + 1 < value.length) {
                result += value[++position];
            } else {
                result += value[position];
            }
        }

        return result;
    }

    _hasScheme(url) {
        return /^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(url);
    }
};