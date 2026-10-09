using System.Text;

namespace DProjects.XShell.Services {

    internal class ModuleFileCompilerHtml {

        // vars
        // keep these built-in selectors aligned with utils/html.js; runtime-added rules have no server counterpart
        internal static readonly (string Selector, string Attribute)[] UrlRules = [
            ("a", "href"), ("area", "href"), ("form", "action"), ("button[formaction]", "formaction"),
            ("x-page", "src"), ("x-anchor", "href"), ("img", "src"), ("img", "srcset"),
            ("source", "src"), ("source", "srcset"), ("link", "href"), ("script", "src"),
            ("iframe", "src"), ("video", "poster"), ("video", "src"), ("audio", "src"),
            ("embed", "src"), ("object", "data"), ("input[type=image]", "src"), ("track", "src")
        ];

        // methods
        public ModuleFileCompiler.FileContent Compile(ModuleFileCompilerContext context, string html) {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(html);

            // preserve source text and replace only supported start-tag attribute values
            var result = new StringBuilder(html.Length);
            var position = 0;
            while (position < html.Length) {
                var tagStart = html.IndexOf('<', position);
                if (tagStart < 0) {
                    result.Append(html, position, html.Length - position);
                    break;
                }
                result.Append(html, position, tagStart - position);
                if (html.AsSpan(tagStart).StartsWith("<!--", StringComparison.Ordinal)) {
                    var commentEnd = html.IndexOf("-->", tagStart + 4, StringComparison.Ordinal);
                    var end = commentEnd < 0 ? html.Length : commentEnd + 3;
                    result.Append(html, tagStart, end - tagStart);
                    position = end;
                } else if (!TryReadStartTag(html, tagStart, out var nameStart, out var nameEnd, out var tagEnd)) {
                    result.Append('<');
                    position = tagStart + 1;
                } else {
                    var name = html.AsSpan(nameStart, nameEnd - nameStart);
                    AppendProcessedTag(context, html, result, tagStart, nameStart, nameEnd, tagEnd);
                    position = tagEnd + 1;
                    // keep script and style bodies opaque to the HTML attribute scanner
                    if (name.Equals("script", StringComparison.OrdinalIgnoreCase) || name.Equals("style", StringComparison.OrdinalIgnoreCase)) {
                        var rawEnd = FindRawTextElementEnd(html, position, name);
                        result.Append(html, position, rawEnd - position);
                        position = rawEnd;
                    }
                }
            }
            return new ModuleFileCompiler.FileContent { ContentType = "text/html", Content = result.ToString() };
        }

        // methods (private)
        private static void AppendProcessedTag(ModuleFileCompilerContext context, string html, StringBuilder result, int tagStart, int nameStart, int nameEnd, int tagEnd) {
            // collect static attributes before rewriting so selectors work regardless of attribute order
            var attributes = new List<(string Name, string Value, int Start, int End, char Quote)>();
            var position = nameEnd;
            while (position < tagEnd) {
                while (position < tagEnd && (char.IsWhiteSpace(html[position]) || html[position] == '/')) position++;
                if (position >= tagEnd) break;
                var attributeNameStart = position;
                while (position < tagEnd && IsAttributeNameCharacter(html[position])) position++;
                if (position == attributeNameStart) {
                    position++;
                    continue;
                }
                var attributeName = html[attributeNameStart..position];
                while (position < tagEnd && char.IsWhiteSpace(html[position])) position++;
                if (position >= tagEnd || html[position] != '=') continue;
                position++;
                while (position < tagEnd && char.IsWhiteSpace(html[position])) position++;
                if (position >= tagEnd) break;

                // identify the exact value substring without consuming its quote delimiters
                var quote = html[position] is '\'' or '"' ? html[position++] : '\0';
                var valueStart = position;
                if (quote == '\0') {
                    while (position < tagEnd && !char.IsWhiteSpace(html[position]) && !(html[position] == '/' && position + 1 == tagEnd)) position++;
                } else {
                    while (position < tagEnd && html[position] != quote) position++;
                }
                var valueEnd = position;
                if (quote != '\0' && position < tagEnd) position++;
                attributes.Add((attributeName, html[valueStart..valueEnd], valueStart, valueEnd, quote));
            }
            var elementName = html.AsSpan(nameStart, nameEnd - nameStart);
            var isImageInput = elementName.Equals("input", StringComparison.OrdinalIgnoreCase) &&
                attributes.Any(attribute => attribute.Name.Equals("type", StringComparison.OrdinalIgnoreCase) && attribute.Value.Equals("image", StringComparison.OrdinalIgnoreCase));
            var lastCopied = tagStart;
            foreach (var attribute in attributes) {
                var replacement = ProcessAttributeValue(context, elementName, attribute.Name, isImageInput, attribute.Value, attribute.Quote);
                var value = attribute.Value;
                if (replacement == value) continue;
                result.Append(html, lastCopied, attribute.Start - lastCopied);
                result.Append(replacement);
                lastCopied = attribute.End;
            }
            result.Append(html, lastCopied, tagEnd + 1 - lastCopied);
        }
        private static string ProcessAttributeValue(ModuleFileCompilerContext context, ReadOnlySpan<char> elementName, string name, bool isImageInput, string value, char quote) {
            // keep inline CSS separate from the element-aware HTML URL rules
            if (name.Equals("style", StringComparison.OrdinalIgnoreCase)) {
                var css = new ModuleFileCompilerCss().Compile(context, DecodeAttributeDelimiter(value, quote)).Content;
                return EscapeAttributeValue(css, quote);
            }
            if (!IsUrlAttribute(elementName, name, isImageInput)) return value;
            if (name.Equals("srcset", StringComparison.OrdinalIgnoreCase)) return NormalizeSrcset(context.RelativePath, value);
            return ModuleFileCompilerResourceUrl.Normalize(context.RelativePath, value);
        }
        private static string NormalizeSrcset(string relativePath, string value) {
            // rewrite each URL token independently while retaining candidate whitespace, commas, and descriptors
            var result = new StringBuilder(value.Length);
            var position = 0;
            while (position < value.Length) {
                while (position < value.Length && (char.IsWhiteSpace(value[position]) || value[position] == ',')) result.Append(value[position++]);
                if (position >= value.Length) break;
                var urlStart = position;
                while (position < value.Length && !char.IsWhiteSpace(value[position])) position++;
                var urlEnd = position;
                while (urlEnd > urlStart && value[urlEnd - 1] == ',') urlEnd--;
                result.Append(ModuleFileCompilerResourceUrl.Normalize(relativePath, value[urlStart..urlEnd]));
                result.Append(value, urlEnd, position - urlEnd);
                while (position < value.Length && value[position] != ',') result.Append(value[position++]);
            }
            return result.ToString();
        }
        private static string EscapeAttributeValue(string value, char quote) {
            // escape only the surrounding delimiter when compiled CSS could otherwise close the HTML attribute
            return quote == '"' ? value.Replace("\"", "&quot;", StringComparison.Ordinal) :
                quote == '\'' ? value.Replace("'", "&#39;", StringComparison.Ordinal) : value;
        }
        private static string DecodeAttributeDelimiter(string value, char quote) {
            // expose encoded surrounding-quote characters to the CSS scanner without normalizing unrelated entities
            if (quote == '"') {
                return value.Replace("&quot;", "\"", StringComparison.Ordinal).Replace("&#34;", "\"", StringComparison.Ordinal)
                    .Replace("&#x22;", "\"", StringComparison.OrdinalIgnoreCase);
            }
            if (quote == '\'') {
                return value.Replace("&apos;", "'", StringComparison.Ordinal).Replace("&#39;", "'", StringComparison.Ordinal)
                    .Replace("&#x27;", "'", StringComparison.OrdinalIgnoreCase);
            }
            return value;
        }
        private static int FindRawTextElementEnd(string html, int position, ReadOnlySpan<char> name) {
            // skip raw text without interpreting tags or URL-looking strings in its body
            while (position < html.Length) {
                var closeStart = html.IndexOf("</", position, StringComparison.Ordinal);
                if (closeStart < 0) return html.Length;
                var nameStart = closeStart + 2;
                var nameEnd = nameStart;
                while (nameEnd < html.Length && IsTagNameCharacter(html[nameEnd])) nameEnd++;
                if (html.AsSpan(nameStart, nameEnd - nameStart).Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    var closeEnd = FindTagEnd(html, nameEnd);
                    return closeEnd < 0 ? html.Length : closeEnd + 1;
                }
                position = Math.Max(nameEnd, closeStart + 2);
            }
            return html.Length;
        }
        private static bool TryReadStartTag(string html, int tagStart, out int nameStart, out int nameEnd, out int tagEnd) {
            // recognize a start tag and find its end while honoring quoted greater-than characters
            nameStart = tagStart + 1;
            nameEnd = nameStart;
            tagEnd = -1;
            if (nameStart >= html.Length || html[nameStart] is '/' or '!' or '?') return false;
            while (nameStart < html.Length && char.IsWhiteSpace(html[nameStart])) nameStart++;
            nameEnd = nameStart;
            while (nameEnd < html.Length && IsTagNameCharacter(html[nameEnd])) nameEnd++;
            if (nameEnd == nameStart) return false;
            tagEnd = FindTagEnd(html, nameEnd);
            return tagEnd >= 0;
        }
        private static int FindTagEnd(string html, int position) {
            // ignore greater-than characters enclosed by attribute quotes
            char quote = '\0';
            while (position < html.Length) {
                var character = html[position++];
                if (quote != '\0') {
                    if (character == quote) quote = '\0';
                } else if (character is '\'' or '"') {
                    quote = character;
                } else if (character == '>') {
                    return position - 1;
                }
            }
            return -1;
        }
        private static bool IsUrlAttribute(ReadOnlySpan<char> elementName, string attributeName, bool isImageInput) {
            // match only the element/attribute combinations owned by the runtime template URL layer
            foreach (var (selector, attribute) in UrlRules) {
                if (!attributeName.Equals(attribute, StringComparison.OrdinalIgnoreCase)) continue;
                if (selector == "input[type=image]") {
                    if (isImageInput) return true;
                } else if (selector == "button[formaction]") {
                    if (elementName.Equals("button", StringComparison.OrdinalIgnoreCase)) return true;
                } else if (elementName.Equals(selector, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }
        private static bool IsTagNameCharacter(char character) {
            return char.IsLetterOrDigit(character) || character is '-' or ':';
        }
        private static bool IsAttributeNameCharacter(char character) {
            return !char.IsWhiteSpace(character) && character is not '=' and not '>' and not '/';
        }
    }
}
