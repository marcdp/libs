using System.Net;

namespace DProjects.XShell.Services.XTemplate {

    internal sealed class XTemplateParser {

        // consts
        private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase) { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };
        private static readonly HashSet<string> RawTextElements = new(StringComparer.OrdinalIgnoreCase) { "script", "style" };

        // vars
        private readonly string _source;
        private int _position;

        // ctor
        public XTemplateParser(string source) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        // methods
        public XTemplateElementNode Parse() {
            var root = new MutableElement("#root", 0, new());
            var stack = new Stack<MutableElement>();
            stack.Push(root);
            while (_position < _source.Length) {
                if (_source.AsSpan(_position).StartsWith("<!--", StringComparison.Ordinal)) ParseComment(stack.Peek());
                else if (_source[_position] == '<' && _position + 1 < _source.Length && _source[_position + 1] == '/') ParseClosingTag(stack);
                else if (_source[_position] == '<' && _position + 1 < _source.Length && IsTagNameStart(_source[_position + 1])) ParseElement(stack);
                else ParseText(stack.Peek());
            }
            if (stack.Count != 1) throw Error($"Element <{stack.Peek().Name}> is not closed", stack.Peek().Offset);
            return Freeze(root);
        }

        // methods (private)
        private void ParseComment(MutableElement parent) {
            var offset = _position;
            var end = _source.IndexOf("-->", _position + 4, StringComparison.Ordinal);
            if (end < 0) throw Error("Comment is not closed", offset);
            parent.Children.Add(new XTemplateCommentNode(_source[(_position + 4)..end], offset));
            _position = end + 3;
        }
        private void ParseClosingTag(Stack<MutableElement> stack) {
            var offset = _position;
            _position += 2;
            SkipWhitespace();
            var name = ReadName().ToLowerInvariant();
            SkipWhitespace();
            if (_position >= _source.Length || _source[_position] != '>') throw Error("Malformed closing tag", offset);
            _position++;
            if (stack.Count == 1 || stack.Peek().Name != name) throw Error($"Unexpected closing tag </{name}>", offset);
            stack.Peek().ContentEnd = offset;
            stack.Pop();
        }
        private void ParseElement(Stack<MutableElement> stack) {
            var offset = _position++;
            var name = ReadName().ToLowerInvariant();
            var attributes = new List<RawAttribute>();
            var selfClosing = false;
            while (_position < _source.Length) {
                SkipWhitespace();
                if (_position >= _source.Length) break;
                if (_source[_position] == '>') { _position++; break; }
                if (_source[_position] == '/' && _position + 1 < _source.Length && _source[_position + 1] == '>') { _position += 2; selfClosing = true; break; }
                var attributeOffset = _position;
                var attributeName = ReadAttributeName();
                if (attributeName.Length == 0) throw Error("Malformed attribute", attributeOffset);
                if (attributes.Any(attribute => string.Equals(attribute.Name, attributeName, StringComparison.OrdinalIgnoreCase))) throw Error($"Duplicate attribute '{attributeName}'", attributeOffset);
                SkipWhitespace();
                var hasValue = _position < _source.Length && _source[_position] == '=';
                var value = string.Empty;
                if (hasValue) { _position++; SkipWhitespace(); value = WebUtility.HtmlDecode(ReadAttributeValue()); }
                attributes.Add(new(attributeName, value, hasValue, attributeOffset));
            }
            var element = new MutableElement(name, offset, attributes) { ContentStart = _position };
            stack.Peek().Children.Add(element);
            if (selfClosing || VoidElements.Contains(name)) return;
            if (RawTextElements.Contains(name)) {
                var closing = $"</{name}>";
                var end = _source.IndexOf(closing, _position, StringComparison.OrdinalIgnoreCase);
                if (end < 0) throw Error($"Element <{name}> is not closed", offset);
                if (end > _position) element.Children.Add(new XTemplateTextNode(_source[_position..end], _position));
                element.ContentEnd = end;
                _position = end + closing.Length;
                return;
            }
            stack.Push(element);
        }
        private void ParseText(MutableElement parent) {
            var offset = _position;
            var end = FindNextMarkupStart();
            AddInterpolatedText(parent.Children, WebUtility.HtmlDecode(_source[_position..end]), offset);
            _position = end;
        }
        private int FindNextMarkupStart() {
            var position = _position;
            while (position < _source.Length) {
                position = _source.IndexOf('<', position);
                if (position < 0) return _source.Length;
                if (_source.AsSpan(position).StartsWith("<!--", StringComparison.Ordinal) || position + 1 < _source.Length && (_source[position + 1] == '/' || IsTagNameStart(_source[position + 1]))) return position;
                position++;
            }
            return _source.Length;
        }
        private XTemplateElementNode Freeze(MutableElement element) {
            var preAttribute = element.Attributes.FirstOrDefault(attribute => string.Equals(attribute.Name, "x-pre", StringComparison.OrdinalIgnoreCase));
            if (preAttribute != null && preAttribute.HasValue && !string.IsNullOrEmpty(preAttribute.Value)) throw Error("Directive 'x-pre' cannot have a value", preAttribute.Offset);
            var children = preAttribute == null ? element.Children.Select(child => child is MutableElement childElement ? (XTemplateNode)Freeze(childElement) : (XTemplateNode)child).ToList() : new List<XTemplateNode> { new XTemplateRawHtmlNode(_source[element.ContentStart..element.ContentEnd], element.ContentStart) };
            var attributes = new List<XTemplateElementAttribute>();
            XTemplateExpression? condition = null;
            XTemplateExpression? elseIf = null;
            XTemplateExpression? text = null;
            XTemplateExpression? html = null;
            XTemplateExpression? model = null;
            XTemplateExpression? childrenExpression = null;
            XTemplateForDefinition? loop = null;
            XTemplateRecursiveDefinition? recursive = null;
            string? recursiveWrapper = null;
            var isElse = false;
            foreach (var attribute in element.Attributes) {
                var directive = attribute.Name;
                if (string.Equals(directive, "x-if", StringComparison.OrdinalIgnoreCase)) condition = ParseExpression(attribute, "x-if");
                else if (string.Equals(directive, "x-elseif", StringComparison.OrdinalIgnoreCase)) elseIf = ParseExpression(attribute, "x-elseif");
                else if (string.Equals(directive, "x-else", StringComparison.OrdinalIgnoreCase)) { if (attribute.HasValue && !string.IsNullOrEmpty(attribute.Value)) throw Error("Directive 'x-else' cannot have a value", attribute.Offset); isElse = true; }
                else if (string.Equals(directive, "x-for", StringComparison.OrdinalIgnoreCase)) loop = ParseFor(attribute);
                else if (string.Equals(directive, "x-recursive", StringComparison.OrdinalIgnoreCase)) recursive = ParseRecursive(attribute);
                else if (string.Equals(directive, "x-recursive-wrapper", StringComparison.OrdinalIgnoreCase)) { if (!attribute.HasValue || !IsTagName(attribute.Value)) throw Error("x-recursive-wrapper requires a tag name", attribute.Offset); recursiveWrapper = attribute.Value.ToLowerInvariant(); }
                else if (string.Equals(directive, "x-text", StringComparison.OrdinalIgnoreCase)) text = ParseExpression(attribute, "x-text");
                else if (string.Equals(directive, "x-html", StringComparison.OrdinalIgnoreCase)) html = ParseExpression(attribute, "x-html");
                else if (string.Equals(directive, "x-children", StringComparison.OrdinalIgnoreCase)) childrenExpression = ParseExpression(attribute, "x-children");
                else if (string.Equals(directive, "x-model", StringComparison.OrdinalIgnoreCase)) { model = ParseExpression(attribute, "x-model"); if (!XTemplateExpressions.IsAssignable(model)) throw Error("x-model requires an assignable expression", attribute.Offset); }
                else if (string.Equals(directive, "x-pre", StringComparison.OrdinalIgnoreCase)) { }
                else if (string.Equals(directive, "x-attr", StringComparison.OrdinalIgnoreCase)) attributes.Add(new XTemplateAttributeSpread(ParseExpression(attribute, "x-attr"), attribute.Offset));
                else if (directive.StartsWith("x-attr:[", StringComparison.OrdinalIgnoreCase) && directive.EndsWith(']')) {
                    var nameSource = directive[8..^1];
                    if (string.IsNullOrWhiteSpace(nameSource)) throw Error("Dynamic attribute name expression is empty", attribute.Offset);
                    attributes.Add(new XTemplateDynamicAttribute(ParseExpressionSource(nameSource, attribute.Offset), ParseExpression(attribute, "x-attr"), attribute.Offset));
                } else if (directive.StartsWith("x-attr:", StringComparison.OrdinalIgnoreCase)) {
                    var name = directive[7..];
                    if (!IsValidAttributeName(name)) throw Error($"Invalid attribute name '{name}'", attribute.Offset);
                    attributes.Add(new XTemplateBoundAttribute(name, ParseExpression(attribute, "x-attr"), attribute.Offset));
                } else if (directive.StartsWith("x-class:", StringComparison.OrdinalIgnoreCase)) {
                    var name = directive[8..];
                    if (string.IsNullOrWhiteSpace(name) || name.Any(char.IsWhiteSpace)) throw Error("x-class requires a class name", attribute.Offset);
                    attributes.Add(new XTemplateClassAttribute(name, ParseExpression(attribute, "x-class"), attribute.Offset));
                } else if (string.Equals(directive, "x-show", StringComparison.OrdinalIgnoreCase)) attributes.Add(new XTemplateShowAttribute(ParseExpression(attribute, "x-show"), attribute.Offset));
                else if (string.Equals(directive, "x-key", StringComparison.OrdinalIgnoreCase)) { if (!attribute.HasValue || !IsIdentifier(attribute.Value)) throw Error("x-key requires a property-name value", attribute.Offset); }
                else if (directive.StartsWith("x-on:", StringComparison.OrdinalIgnoreCase)) ValidateEvent(attribute);
                else if (string.Equals(directive, "x-prop", StringComparison.OrdinalIgnoreCase)) throw Error("Whole-object x-prop is not supported by XTemplate", attribute.Offset);
                else if (directive.StartsWith("x-prop:[", StringComparison.OrdinalIgnoreCase) && directive.EndsWith(']')) { var source = directive[8..^1]; if (string.IsNullOrWhiteSpace(source)) throw Error("Dynamic property name expression is empty", attribute.Offset); ParseExpressionSource(source, attribute.Offset); ParseExpression(attribute, "x-prop"); }
                else if (directive.StartsWith("x-prop:", StringComparison.OrdinalIgnoreCase)) { if (!IsValidAttributeName(directive[7..])) throw Error("Invalid property name", attribute.Offset); ParseExpression(attribute, "x-prop"); }
                else if (string.Equals(directive, "x-once", StringComparison.OrdinalIgnoreCase)) { if (attribute.HasValue && !string.IsNullOrEmpty(attribute.Value)) throw Error("Directive 'x-once' cannot have a value", attribute.Offset); }
                else if (directive.StartsWith("x-", StringComparison.OrdinalIgnoreCase)) throw Error($"Unsupported XTemplate directive '{directive}'", attribute.Offset);
                else attributes.Add(new XTemplateStaticAttribute(directive, attribute.Value, attribute.HasValue, attribute.Offset));
            }
            var structuralCount = (condition != null ? 1 : 0) + (elseIf != null ? 1 : 0) + (isElse ? 1 : 0) + (loop != null ? 1 : 0) + (recursive != null ? 1 : 0);
            if (structuralCount > 1) throw Error("An element cannot contain more than one structural directive", element.Offset);
            if (text != null && children.Count > 0) throw Error("Directive 'x-text' requires an empty element", element.Offset);
            if (html != null && children.Count > 0) throw Error("Directive 'x-html' requires an empty element", element.Offset);
            if (text != null && html != null) throw Error("An element cannot combine x-text and x-html", element.Offset);
            if (childrenExpression != null && element.Children.Count > 0) throw Error("Directive 'x-children' requires an empty element", element.Offset);
            if (recursiveWrapper != null && recursive == null) throw Error("Directive 'x-recursive-wrapper' requires x-recursive", element.Offset);
            if (recursive != null) recursive = recursive with { WrapperName = recursiveWrapper };
            if (element.Attributes.Any(attribute => string.Equals(attribute.Name, "x-key", StringComparison.OrdinalIgnoreCase)) && loop == null && recursive == null) throw Error("Directive 'x-key' requires x-for or x-recursive", element.Offset);
            return new(element.Name, attributes, children, element.Offset, condition, elseIf, isElse, loop, recursive, text, html, model, childrenExpression);
        }
        private XTemplateForDefinition ParseFor(RawAttribute attribute) {
            if (!attribute.HasValue) throw Error("x-for requires a value", attribute.Offset);
            var separator = attribute.Value.IndexOf(" in ", StringComparison.Ordinal);
            if (separator <= 0 || attribute.Value.IndexOf(" in ", separator + 4, StringComparison.Ordinal) >= 0) throw Error("x-for requires 'item in expression' or '(item,index) in expression'", attribute.Offset);
            var variables = attribute.Value[..separator].Trim();
            var collection = attribute.Value[(separator + 4)..].Trim();
            var names = variables.StartsWith('(') && variables.EndsWith(')') ? variables[1..^1].Split(',').Select(name => name.Trim()).ToArray() : new[] { variables };
            if (names.Length is < 1 or > 2 || names.Any(name => !IsIdentifier(name)) || collection.Length == 0) throw Error("Invalid x-for variables or collection", attribute.Offset);
            return new(names[0], names.Length == 2 ? names[1] : "index", ParseExpressionSource(collection, attribute.Offset), attribute.Offset);
        }
        private XTemplateRecursiveDefinition ParseRecursive(RawAttribute attribute) {
            if (!attribute.HasValue) throw Error("x-recursive requires a value", attribute.Offset);
            var separator = attribute.Value.IndexOf(" in ", StringComparison.Ordinal);
            if (separator <= 0 || attribute.Value.IndexOf(" in ", separator + 4, StringComparison.Ordinal) >= 0) throw Error("x-recursive requires 'item in expression' or '(item,index,indexAbsolute) in expression'", attribute.Offset);
            var variables = attribute.Value[..separator].Trim();
            var collection = attribute.Value[(separator + 4)..].Trim();
            var names = variables.StartsWith('(') && variables.EndsWith(')') ? variables[1..^1].Split(',').Select(name => name.Trim()).ToArray() : new[] { variables };
            if (names.Length is < 1 or > 3 || names.Any(name => !IsIdentifier(name)) || collection.Length == 0) throw Error("Invalid x-recursive variables or collection", attribute.Offset);
            var item = names[0];
            return new(item, names.Length > 1 ? names[1] : "index", names.Length > 2 ? names[2] : "indexAbsolute", ParseExpressionSource(collection, attribute.Offset), new MemberAccessExpression(new IdentifierExpression(item, attribute.Offset), "children", attribute.Offset), null, attribute.Offset);
        }
        private void ValidateEvent(RawAttribute attribute) {
            var eventName = attribute.Name[5..];
            if (string.IsNullOrWhiteSpace(eventName) || eventName.Split('.').Any(part => !IsIdentifier(part))) throw Error("Invalid x-on event name or modifier", attribute.Offset);
            if (!attribute.HasValue || string.IsNullOrWhiteSpace(attribute.Value)) throw Error("x-on requires a command name", attribute.Offset);
        }
        private XTemplateExpression ParseExpression(RawAttribute attribute, string directive) { if (!attribute.HasValue || string.IsNullOrWhiteSpace(attribute.Value)) throw Error($"Directive '{directive}' requires an expression", attribute.Offset); return ParseExpressionSource(attribute.Value, attribute.Offset); }
        private XTemplateExpression ParseExpressionSource(string value, int offset) { try { return XTemplateExpressions.Parse(value); } catch (XTemplateExpressionException exception) { throw Error(exception.Message, offset + exception.Offset); } }
        private static void AddInterpolatedText(List<object> children, string value, int offset) {
            var position = 0;
            while (position < value.Length) {
                var start = value.IndexOf("{{", position, StringComparison.Ordinal);
                if (start < 0) { if (position < value.Length) children.Add(new XTemplateTextNode(value[position..], offset + position)); return; }
                if (start > position) children.Add(new XTemplateTextNode(value[position..start], offset + position));
                var end = value.IndexOf("}}", start + 2, StringComparison.Ordinal);
                if (end < 0) throw new XTemplateException("Interpolation is missing '}}'", offset + start);
                var source = value[(start + 2)..end];
                if (string.IsNullOrWhiteSpace(source)) throw new XTemplateException("Interpolation expression is empty", offset + start);
                try { children.Add(new XTemplateInterpolationNode(XTemplateExpressions.Parse(source), offset + start)); } catch (XTemplateExpressionException exception) { throw new XTemplateException(exception.Message, offset + start + 2 + exception.Offset); }
                position = end + 2;
            }
        }
        private string ReadName() { var start = _position; while (_position < _source.Length && (char.IsLetterOrDigit(_source[_position]) || _source[_position] is '-' or ':' or '_')) _position++; if (start == _position) throw Error("Malformed name", start); return _source[start.._position]; }
        private string ReadAttributeName() { var start = _position; while (_position < _source.Length && !char.IsWhiteSpace(_source[_position]) && _source[_position] is not '=' and not '>' && !(_source[_position] == '/' && _position + 1 < _source.Length && _source[_position + 1] == '>')) _position++; return _source[start.._position]; }
        private string ReadAttributeValue() { if (_position >= _source.Length) throw Error("Attribute value is missing", _position); if (_source[_position] is '\'' or '"') { var quote = _source[_position++]; var start = _position; while (_position < _source.Length && _source[_position] != quote) _position++; if (_position >= _source.Length) throw Error("Quoted attribute is not closed", start - 1); var value = _source[start.._position]; _position++; return value; } var unquoted = _position; while (_position < _source.Length && !char.IsWhiteSpace(_source[_position]) && _source[_position] != '>') _position++; return _source[unquoted.._position]; }
        private void SkipWhitespace() { while (_position < _source.Length && char.IsWhiteSpace(_source[_position])) _position++; }
        private static bool IsTagNameStart(char character) => char.IsLetter(character) || character is '_' or ':';
        private static bool IsTagName(string value) => value.Length > 0 && char.IsLetter(value[0]) && value.All(character => char.IsLetterOrDigit(character) || character is '-' or '_' or ':');
        private static bool IsIdentifier(string value) => value.Length > 0 && (value[0] is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_') && value.Skip(1).All(character => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '_');
        private static bool IsValidAttributeName(string name) => XTemplateAttributeNames.IsValid(name);
        private static XTemplateException Error(string message, int offset) => new(message, offset);
        private sealed record RawAttribute(string Name, string Value, bool HasValue, int Offset);
        private sealed class MutableElement(string name, int offset, List<RawAttribute> attributes) {
            public string Name { get; } = name;
            public int Offset { get; } = offset;
            public List<RawAttribute> Attributes { get; } = attributes;
            public List<object> Children { get; } = new();
            public int ContentStart { get; init; }
            public int ContentEnd { get; set; }
        }
    }
}
