using System.Text;

namespace DProjects.XShell.Services {

    public class ModuleFileCompilerCss {

        // methods
        public ModuleFileCompiler.FileContent Compile(ModuleFileCompilerContext context, string css) {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(css);

            // rewrite only CSS resource-reference constructs
            var result = new StringBuilder(css.Length);
            var index = 0;
            while (index < css.Length) {
                if (IsCommentStart(css, index)) {
                    AppendComment(css, result, ref index);
                } else if (css[index] == '\'' || css[index] == '"') {
                    AppendString(css, result, ref index);
                } else if (IsImportStart(css, index)) {
                    AppendImport(css, result, context.RelativePath, ref index);
                } else if (IsUrlFunctionStart(css, index, out var openingParenthesis)) {
                    AppendUrlFunction(css, result, context.RelativePath, openingParenthesis, ref index);
                } else {
                    result.Append(css[index]);
                    index++;
                }
            }
            return new ModuleFileCompiler.FileContent { ContentType = "text/css", Content = result.ToString() };
        }

        // methods (private)
        private static void AppendComment(string css, StringBuilder result, ref int index) {
            // preserve the complete comment without inspecting URL-looking text
            var end = css.IndexOf("*/", index + 2, StringComparison.Ordinal);
            var length = end < 0 ? css.Length - index : end + 2 - index;
            result.Append(css, index, length);
            index += length;
        }
        private static void AppendString(string css, StringBuilder result, ref int index) {
            // preserve ordinary CSS strings without inspecting their contents
            var end = FindStringEnd(css, index);
            result.Append(css, index, end - index);
            index = end;
        }
        private static void AppendImport(string css, StringBuilder result, string relativePath, ref int index) {
            // preserve the at-keyword and trivia before its resource token
            const int importLength = 7;
            result.Append(css, index, importLength);
            index += importLength;
            while (index < css.Length) {
                if (char.IsWhiteSpace(css[index])) {
                    result.Append(css[index]);
                    index++;
                } else if (IsCommentStart(css, index)) {
                    AppendComment(css, result, ref index);
                } else {
                    break;
                }
            }

            // rewrite only the quoted form; url(...) is handled by the main scanner
            if (index >= css.Length || (css[index] != '\'' && css[index] != '"')) return;
            var quote = css[index];
            var end = FindStringEnd(css, index);
            if (end > index + 1 && css[end - 1] == quote) {
                var quotedUrl = css.Substring(index + 1, end - index - 2);
                result.Append(quote);
                result.Append(ModuleFileCompilerUrl.Normalize(relativePath, quotedUrl));
                result.Append(quote);
                index = end;
            }
        }
        private static void AppendUrlFunction(string css, StringBuilder result, string relativePath, int openingParenthesis, ref int index) {
            // preserve the function spelling and whitespace before the URL token
            result.Append(css, index, openingParenthesis - index + 1);
            index = openingParenthesis + 1;
            while (index < css.Length && char.IsWhiteSpace(css[index])) {
                result.Append(css[index]);
                index++;
            }
            if (index >= css.Length) return;

            // rewrite quoted URL tokens while retaining their original quote style
            if (css[index] == '\'' || css[index] == '"') {
                var quote = css[index];
                var end = FindStringEnd(css, index);
                if (end <= index + 1 || css[end - 1] != quote) {
                    result.Append(css, index, css.Length - index);
                    index = css.Length;
                    return;
                }
                var url = css.Substring(index + 1, end - index - 2);
                result.Append(quote);
                result.Append(ModuleFileCompilerUrl.Normalize(relativePath, url));
                result.Append(quote);
                index = end;
                return;
            }

            // rewrite an unquoted URL token and preserve surrounding whitespace
            var tokenStart = index;
            var tokenEnd = FindUrlFunctionEnd(css, tokenStart);
            if (tokenEnd < 0) {
                result.Append(css, index, css.Length - index);
                index = css.Length;
                return;
            }
            var contentEnd = tokenEnd;
            while (contentEnd > tokenStart && char.IsWhiteSpace(css[contentEnd - 1])) contentEnd--;
            var unquotedUrl = css.Substring(tokenStart, contentEnd - tokenStart);
            result.Append(unquotedUrl.Contains("/*", StringComparison.Ordinal) ? unquotedUrl : ModuleFileCompilerUrl.Normalize(relativePath, unquotedUrl));
            result.Append(css, contentEnd, tokenEnd - contentEnd + 1);
            index = tokenEnd + 1;
        }
        private static bool IsCommentStart(string css, int index) {
            return css[index] == '/' && index + 1 < css.Length && css[index + 1] == '*';
        }
        private static bool IsImportStart(string css, int index) {
            const string import = "@import";
            if (index + import.Length > css.Length || !css.AsSpan(index, import.Length).Equals(import, StringComparison.OrdinalIgnoreCase)) return false;
            return index + import.Length == css.Length || !IsIdentifierCharacter(css[index + import.Length]);
        }
        private static bool IsUrlFunctionStart(string css, int index, out int openingParenthesis) {
            openingParenthesis = -1;
            if (index > 0 && IsIdentifierCharacter(css[index - 1])) return false;
            const string url = "url";
            if (index + url.Length > css.Length || !css.AsSpan(index, url.Length).Equals(url, StringComparison.OrdinalIgnoreCase)) return false;
            var next = index + url.Length;
            if (next < css.Length && IsIdentifierCharacter(css[next])) return false;
            while (next < css.Length && char.IsWhiteSpace(css[next])) next++;
            if (next >= css.Length || css[next] != '(') return false;
            openingParenthesis = next;
            return true;
        }
        private static bool IsIdentifierCharacter(char character) {
            return character == '-' || character == '_' || character == '\\' || char.IsLetterOrDigit(character) || character >= 0x80;
        }
        private static int FindStringEnd(string css, int start) {
            // skip escaped quotes and escaped characters inside the string
            var quote = css[start];
            var index = start + 1;
            while (index < css.Length) {
                if (css[index] == '\\' && index + 1 < css.Length) {
                    index += 2;
                } else if (css[index] == quote) {
                    return index + 1;
                } else {
                    index++;
                }
            }
            return css.Length;
        }
        private static int FindUrlFunctionEnd(string css, int start) {
            // skip escaped closing parentheses in unquoted URL tokens
            var index = start;
            while (index < css.Length) {
                if (css[index] == '\\' && index + 1 < css.Length) {
                    index += 2;
                } else if (css[index] == ')') {
                    return index;
                } else {
                    index++;
                }
            }
            return -1;
        }
    }
}
