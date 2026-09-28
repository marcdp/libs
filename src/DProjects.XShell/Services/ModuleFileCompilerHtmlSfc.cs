using System.Text;

using DProjects.XShell.Services.XTemplate;

namespace DProjects.XShell.Services {

    public class ModuleFileCompilerHtmlSfc {

        // inner classes
        private readonly record struct HtmlTag(string Name, int End, string Source);
        private readonly record struct HtmlCloseTag(int Start, int End);
        private sealed record Sections(string? Style, string? Template, string? ModuleScript);

        // methods
        public ModuleFileCompiler.FileContent Compile(ModuleFileCompilerContext context, string html) {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(html);

            // extract the developer-facing HTML sections
            var sections = ReadSections(html);
            if (sections.Template == null) throw new InvalidOperationException("An HTML X Template must contain exactly one <template> element.");
            var moduleScript = sections.ModuleScript ?? "export default {};";
            moduleScript = moduleScript.Trim().Replace("\n    ", "\n", StringComparison.Ordinal);

            // insert static source properties into the canonical JavaScript definition
            var properties = new List<KeyValuePair<string, string>>();
            if (sections.Style != null) properties.Add(new KeyValuePair<string, string>("style", EncodeTemplateLiteral(sections.Style)));
            properties.Add(new KeyValuePair<string, string>("template", EncodeTemplateLiteral(sections.Template)));
            var js = XTemplateJavaScriptCompiler.InsertDefaultExportProperties(moduleScript, properties, ["style", "template"]);

            // delegate canonical JavaScript processing to the existing compiler pipeline
            return new ModuleFileCompilerJs().Compile(context, js);
        }

        // methods (private)
        private Sections ReadSections(string html) {
            string? style = null;
            string? template = null;
            string? moduleScript = null;
            var position = 0;
            while (TryReadNextStartTag(html, ref position, out var tag)) {
                if (tag.Name == "script") {
                    var close = FindRawTextCloseTag(html, tag.End, "script");
                    if (IsModuleScript(tag.Source)) {
                        if (moduleScript != null) throw new InvalidOperationException("An HTML X Template cannot contain multiple <script type=\"module\"> elements.");
                        moduleScript = html[tag.End..close.Start];
                    }
                    position = close.End;
                } else if (tag.Name == "style") {
                    var close = FindRawTextCloseTag(html, tag.End, "style");
                    if (style != null) throw new InvalidOperationException("An HTML X Template cannot contain multiple <style> elements.");
                    style = html[tag.End..close.Start];
                    style = style.Replace("\n", "\n    ", StringComparison.Ordinal);
                    position = close.End;
                } else if (tag.Name == "template") {
                    var close = FindTemplateCloseTag(html, tag.End);
                    if (template != null) throw new InvalidOperationException("An HTML X Template cannot contain multiple <template> elements.");
                    template = html[tag.End..close.Start];
                    template = template.Replace("\n", "\n    ", StringComparison.Ordinal);
                    position = close.End;
                }
            }
            return new Sections(style, template, moduleScript);
        }
        private bool TryReadNextStartTag(string html, ref int position, out HtmlTag tag) {
            while (position < html.Length) {
                var start = html.IndexOf('<', position);
                if (start < 0) break;
                if (html.AsSpan(start).StartsWith("<!--", StringComparison.Ordinal)) {
                    var commentEnd = html.IndexOf("-->", start + 4, StringComparison.Ordinal);
                    if (commentEnd < 0) throw new InvalidOperationException($"Malformed HTML: comment at offset {start} is not closed.");
                    position = commentEnd + 3;
                    continue;
                }
                if (start + 1 >= html.Length || html[start + 1] is '/' or '!' or '?') { position = start + 1; continue; }
                var nameStart = start + 1;
                while (nameStart < html.Length && char.IsWhiteSpace(html[nameStart])) nameStart++;
                var nameEnd = nameStart;
                while (nameEnd < html.Length && IsTagNameCharacter(html[nameEnd])) nameEnd++;
                if (nameEnd == nameStart) { position = start + 1; continue; }
                var end = FindTagEnd(html, nameEnd, start);
                var name = html[nameStart..nameEnd].ToLowerInvariant();
                tag = new HtmlTag(name, end + 1, html[start..(end + 1)]);
                position = tag.End;
                return true;
            }
            tag = default;
            return false;
        }
        private HtmlCloseTag FindRawTextCloseTag(string html, int position, string name) {
            while (position < html.Length) {
                var start = html.IndexOf("</", position, StringComparison.Ordinal);
                if (start < 0) break;
                var nameStart = start + 2;
                var nameEnd = nameStart;
                while (nameEnd < html.Length && IsTagNameCharacter(html[nameEnd])) nameEnd++;
                if (html.AsSpan(nameStart, nameEnd - nameStart).Equals(name, StringComparison.OrdinalIgnoreCase)) {
                    var end = FindTagEnd(html, nameEnd, start);
                    return new HtmlCloseTag(start, end + 1);
                }
                position = nameEnd;
            }
            throw new InvalidOperationException($"Malformed HTML: <{name}> element is not closed.");
        }
        private HtmlCloseTag FindTemplateCloseTag(string html, int position) {
            var depth = 1;
            while (position < html.Length) {
                var start = html.IndexOf('<', position);
                if (start < 0) break;
                if (html.AsSpan(start).StartsWith("<!--", StringComparison.Ordinal)) {
                    var commentEnd = html.IndexOf("-->", start + 4, StringComparison.Ordinal);
                    if (commentEnd < 0) throw new InvalidOperationException($"Malformed HTML: comment at offset {start} is not closed.");
                    position = commentEnd + 3;
                    continue;
                }
                var closing = start + 1 < html.Length && html[start + 1] == '/';
                var nameStart = start + (closing ? 2 : 1);
                while (nameStart < html.Length && char.IsWhiteSpace(html[nameStart])) nameStart++;
                var nameEnd = nameStart;
                while (nameEnd < html.Length && IsTagNameCharacter(html[nameEnd])) nameEnd++;
                if (nameEnd == nameStart) { position = start + 1; continue; }
                var end = FindTagEnd(html, nameEnd, start);
                var name = html[nameStart..nameEnd];
                if (!closing && (name.Equals("script", StringComparison.OrdinalIgnoreCase) || name.Equals("style", StringComparison.OrdinalIgnoreCase))) {
                    position = FindRawTextCloseTag(html, end + 1, name).End;
                    continue;
                }
                if (name.Equals("template", StringComparison.OrdinalIgnoreCase)) {
                    if (closing) {
                        if (--depth == 0) return new HtmlCloseTag(start, end + 1);
                    } else if (!IsSelfClosingTag(html, start, end)) {
                        depth++;
                    }
                }
                position = end + 1;
            }
            throw new InvalidOperationException("Malformed HTML: <template> element is not closed.");
        }
        private int FindTagEnd(string html, int position, int tagStart) {
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
            throw new InvalidOperationException($"Malformed HTML: tag at offset {tagStart} is not closed.");
        }
        private bool IsModuleScript(string tag) {
            var position = 1;
            while (position < tag.Length && !char.IsWhiteSpace(tag[position]) && tag[position] != '>') position++;
            while (position < tag.Length) {
                while (position < tag.Length && (char.IsWhiteSpace(tag[position]) || tag[position] == '/')) position++;
                if (position >= tag.Length || tag[position] == '>') break;
                var nameStart = position;
                while (position < tag.Length && IsAttributeNameCharacter(tag[position])) position++;
                var name = tag[nameStart..position];
                while (position < tag.Length && char.IsWhiteSpace(tag[position])) position++;
                string? value = null;
                if (position < tag.Length && tag[position] == '=') {
                    position++;
                    while (position < tag.Length && char.IsWhiteSpace(tag[position])) position++;
                    if (position < tag.Length && tag[position] is '\'' or '"') {
                        var quote = tag[position++];
                        var valueStart = position;
                        while (position < tag.Length && tag[position] != quote) position++;
                        value = tag[valueStart..position];
                        if (position < tag.Length) position++;
                    } else {
                        var valueStart = position;
                        while (position < tag.Length && !char.IsWhiteSpace(tag[position]) && tag[position] is not '>' and not '/') position++;
                        value = tag[valueStart..position];
                    }
                }
                if (name.Equals("type", StringComparison.OrdinalIgnoreCase) && value?.Equals("module", StringComparison.OrdinalIgnoreCase) == true) return true;
                if (position == nameStart) position++;
            }
            return false;
        }
        private string EncodeTemplateLiteral(string value) {
            var result = new StringBuilder(value.Length + 2).Append('`');
            for (var index = 0; index < value.Length; index++) {
                if (value[index] == '\\') result.Append("\\\\");
                else if (value[index] == '`') result.Append("\\`");
                else if (value[index] == '$' && index + 1 < value.Length && value[index + 1] == '{') { result.Append("\\${"); index++; }
                else result.Append(value[index]);
            }
            return result.Append('`').ToString();
        }
        private bool IsSelfClosingTag(string html, int start, int end) {
            var position = end - 1;
            while (position > start && char.IsWhiteSpace(html[position])) position--;
            return html[position] == '/';
        }
        private bool IsTagNameCharacter(char character) => char.IsLetterOrDigit(character) || character is '-' or ':';
        private bool IsAttributeNameCharacter(char character) => !char.IsWhiteSpace(character) && character is not '=' and not '>' and not '/';

    }
}
