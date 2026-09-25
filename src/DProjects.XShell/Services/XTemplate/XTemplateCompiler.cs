using System.Net;
using System.Text;
using System.Text.Json;

namespace DProjects.XShell.Services.XTemplate {

    public sealed class XTemplateCompiler {

        // consts
        private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase) {
            "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
        };
        private static readonly HashSet<string> RawTextElements = new(StringComparer.OrdinalIgnoreCase) { "script", "style" };

        // vars
        private readonly XTemplateExpressionJavaScriptCompiler _expressionCompiler = new();

        // methods
        public string Compile(string template) {
            if (template == null) throw new ArgumentNullException(nameof(template));
            var root = new HtmlParser(NormalizeLineEndings(template).Trim()).Parse();
            var indent = "    ";
            ValidateConditionalChains(root);
            var body = new List<string> {
                //indent + "debugger;",
                indent + "let _ifs = {};",
                indent + "let func;",
                indent + "return ["
            };
            var index = 0;
            var scope = new XTemplateExpressionJavaScriptScope(["state"]);
            foreach (var node in root.Children) {
                index += CompileNode(node, index, body, 1, scope);
            }
            body.Add(indent + "];");
            return "(state, handler, invalidate, utils, i18n, renderCount) => {\n" + indent + string.Join("\n" + indent, body) + "\n" + indent + "}";
        }

        // methods (private)
        private int CompileNode(TemplateNode node, int index, List<string> javascript, int level, XTemplateExpressionJavaScriptScope scope) {
            var indent = new string(' ', (level + 1) * 4);
            if (node is TextNode textNode) {
                javascript.Add($"{indent}utils.createVDOM(\"#text\", null, null, null, {{index: {index}}}, {ToJavaScriptString(textNode.Text)}),");
                return 1;
            }
            if (node is ExpressionNode expressionNode) {
                var expression = CompileExpression(expressionNode.Expression, "interpolation", expressionNode.SourceOffset, scope);
                javascript.Add($"{indent}utils.createVDOM(\"#text\", null, null, null, {{index: {index}}}, utils.expr.scalar({expression})),");
                return 1;
            }
            if (node is CommentNode commentNode) {
                javascript.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, {{index: {index}}}, {ToJavaScriptString(commentNode.Text)}),");
                return 1;
            }

            var element = (ElementNode)node;
            if (element.Name.StartsWith("x:", StringComparison.Ordinal)) throw TemplateError($"Invalid X template node <{element.Name}>.", element);
            var line = new StringBuilder(indent).Append("utils.createVDOM(").Append(ToJavaScriptString(element.Name));
            var postLine = new List<string>();
            var post = new List<string>();
            var attributes = new List<string>();
            var properties = new List<string>();
            var events = new List<string>();
            var options = new List<string> { $"index:{index}" };
            var classes = new List<string>();
            var childScope = scope;
            var expressionScope = scope;
            var scopedLoop = element.Attributes.FirstOrDefault(attribute => attribute.Name is "x-for" or "x-recursive");
            if (scopedLoop != null) {
                var loop = ParseLoop(scopedLoop.Value, scopedLoop.Name, scopedLoop.Name == "x-recursive", element);
                expressionScope = scopedLoop.Name == "x-recursive"
                    ? scope.With(loop.Item, loop.Index, loop.AbsoluteIndex, loop.Indent)
                    : scope.With(loop.Item, loop.Index);
                childScope = expressionScope;
            }
            string? text = null;
            string? childrenToAppend = null;
            var staticAttributes = element.Attributes.Where(attribute => IsStaticAttribute(attribute.Name)).ToArray();
            var staticAttributesJavascript = "{" + string.Join(',', staticAttributes.Select(attribute => $"{ToJavaScriptString(attribute.Name)}:{ToJavaScriptString(attribute.Value)}")) + "}";

            // merge static classes into the conditional class binding
            if (element.Attributes.Any(attribute => attribute.Name.StartsWith("x-class:", StringComparison.Ordinal))) {
                var classAttribute = element.GetAttribute("class");
                if (!string.IsNullOrEmpty(classAttribute)) {
                    foreach (var className in classAttribute.Split(' ', StringSplitOptions.RemoveEmptyEntries)) classes.Add(ToJavaScriptString(className));
                }
            }

            foreach (var attribute in element.Attributes) {
                var name = attribute.Name;
                var value = attribute.Value;
                if (name == "class" && classes.Count > 0) {
                    continue;
                } else if (name == "x-text") {
                    EnsureEmptyElement(element, name);
                    text = $"utils.expr.scalar({CompileExpression(value, name, element.SourceOffset, expressionScope)})";
                } else if (name == "x-html") {
                    EnsureEmptyElement(element, name);
                    var expression = CompileExpression(value, name, element.SourceOffset, expressionScope);
                    options.Add("format:\"html\"");
                    text = $"utils.expr.scalar({expression})";
                } else if (name == "x-children") {
                    text = CompileExpression(value, name, element.SourceOffset, expressionScope);
                    options.Add("format:\"node\"");
                } else if (name == "x-attr") {
                    attributes.Add($"...utils.expr.attributes({CompileExpression(value, name, element.SourceOffset, expressionScope)})");
                } else if (name.StartsWith("x-attr:", StringComparison.Ordinal)) {
                    var expression = CompileExpression(value, name, element.SourceOffset, expressionScope);
                    var attributeName = name[(name.IndexOf(':') + 1)..];
                    if (attributeName.StartsWith('[') && attributeName.EndsWith(']')) attributes.Add($"...utils.expr.dynamicArgument({CompileExpression(attributeName[1..^1], name, element.SourceOffset, expressionScope)}, {expression})");
                    else attributes.Add($"{ToJavaScriptString(attributeName)}:{expression}");
                } else if (name == "x-prop") {
                    properties.Add($"...utils.expr.properties({CompileExpression(value, name, element.SourceOffset, expressionScope)})");
                } else if (name.StartsWith("x-prop:", StringComparison.Ordinal)) {
                    var expression = CompileExpression(value, name, element.SourceOffset, expressionScope);
                    var propertyName = KebabToCamel(name[(name.IndexOf(':') + 1)..]);
                    if (propertyName.StartsWith('[') && propertyName.EndsWith(']')) properties.Add($"...utils.expr.dynamicProperty({CompileExpression(propertyName[1..^1], name, element.SourceOffset, expressionScope)}, {expression})");
                    else properties.Add($"{propertyName}:{expression}");
                } else if (name.StartsWith("x-on:", StringComparison.Ordinal)) {
                    if (string.IsNullOrWhiteSpace(value)) throw TemplateError($"Directive '{name}' requires an event handler name.", element);
                    var eventName = name[(name.IndexOf(':') + 1)..];
                    if (string.IsNullOrWhiteSpace(eventName)) throw TemplateError("An event binding requires an event name.", element);
                    events.Add($"{ToJavaScriptString(eventName)}: (event) => handler({ToJavaScriptString(value)}, event)");
                } else if (name == "x-if") {
                    line.Clear().Append(indent).Append($"...((_ifs.c{level} = utils.expr.truthy({CompileExpression(value, name, element.SourceOffset, expressionScope)})) ? [utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"] : [utils.createVDOM(\"#comment\", null, null, null, {{index: {index}}}, 'x-if')]),");
                } else if (name == "x-elseif") {
                    line.Clear().Append(indent).Append($"...(_ifs.c{level} ? [] : (_ifs.c{level} = utils.expr.truthy({CompileExpression(value, name, element.SourceOffset, expressionScope)})) ? [utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"] : [utils.createVDOM(\"#comment\", null, null, null, {{index: {index}}}, 'x-elseif')]),");
                } else if (name == "x-else") {
                    if (!string.IsNullOrEmpty(value)) throw TemplateError("Directive 'x-else' cannot have a value.", element);
                    line.Clear().Append(indent).Append($"...(!_ifs.c{level} ? [utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"] : [utils.createVDOM(\"#comment\", null, null, null, {{index: {index}}}, 'x-else')]),");
                } else if (name == "x-for") {
                    var loop = ParseLoop(value, name, false, element);
                    var keyName = element.GetAttribute("x-key");
                    var forType = string.IsNullOrWhiteSpace(keyName) ? "position" : "key";
                    var collection = CompileExpression(loop.Collection, name, element.SourceOffset, scope);
                    javascript.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, {{index: {index}, forType:'{forType}'}}, 'x-for-start'),");
                    line.Clear().Append(indent).Append($"...(utils.expr.collection({collection}).map(({loop.Item}, {loop.Index}) => utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add(")),");
                    if (!string.IsNullOrWhiteSpace(keyName)) options.Add($"\"key\":utils.expr.member({loop.Item}, {ToJavaScriptString(ValidateKeyName(keyName, element))})");
                    childScope = expressionScope;
                    post.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, {{index: {index}, forType:'{forType}'}}, 'x-for-end'),");
                } else if (name == "x-key") {
                    if (!element.HasAttribute("x-for") && !element.HasAttribute("x-recursive")) throw TemplateError("Directive 'x-key' requires 'x-for' or 'x-recursive'.", element);
                    if (string.IsNullOrWhiteSpace(value)) throw TemplateError("Directive 'x-key' requires a property name.", element);
                } else if (name == "x-recursive") {
                    var loop = ParseLoop(value, name, true, element);
                    var keyName = element.GetAttribute("x-key");
                    var forType = string.IsNullOrWhiteSpace(keyName) ? "position" : "key";
                    var collection = CompileExpression(loop.Collection, name, element.SourceOffset, scope);
                    javascript.Add($"{indent}...(func = (_items, {loop.AbsoluteIndex}, {loop.Indent}, wrapper) => {{ let _itemsArray = utils.expr.collection(_items); let _result = [");
                    indent += "    ";
                    level++;
                    javascript.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, {{index: {index - 1}, forType:'{forType}'}}, 'x-for-start'),");
                    line.Clear().Append(indent).Append($"...(_itemsArray.map(({loop.Item}, {loop.Index}) => {{ let _result = utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"; {loop.AbsoluteIndex}++; return _result;}})),");
                    if (!string.IsNullOrWhiteSpace(keyName)) options.Add($"\"key\":utils.expr.member({loop.Item}, {ToJavaScriptString(ValidateKeyName(keyName, element))})");
                    post.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, {{index: {index - 1}, forType:'{forType}'}}, 'x-for-end'),");
                    var wrapper = element.GetAttribute("x-recursive-wrapper") ?? "";
                    post.Add($"\n    {indent[..^4]}]; if (wrapper) _result = [utils.createVDOM(wrapper, null, null, null, {{index:1}}, _result)]; return _result;}})({collection}, 0, 0),");
                    childrenToAppend = $"func(utils.expr.member({loop.Item}, \"children\"), {loop.AbsoluteIndex} + 1, {loop.Indent} + 1, {ToJavaScriptString(wrapper)})";
                    childScope = expressionScope;
                } else if (name == "x-recursive-wrapper") {
                    if (!element.HasAttribute("x-recursive")) throw TemplateError("Directive 'x-recursive-wrapper' requires 'x-recursive'.", element);
                } else if (name == "x-show") {
                    attributes.Add($"hidden:utils.expr.truthy({CompileExpression(value, name, element.SourceOffset, expressionScope)}) ? null : true");
                } else if (name.StartsWith("x-class:", StringComparison.Ordinal)) {
                    var className = name[(name.IndexOf(':') + 1)..];
                    if (string.IsNullOrWhiteSpace(className)) throw TemplateError("Directive 'x-class' requires a class name.", element);
                    classes.Add($"(utils.expr.truthy({CompileExpression(value, name, element.SourceOffset, expressionScope)}) ? {ToJavaScriptString(className)} : null)");
                } else if (name == "x-model") {
                    CompileModel(element, value, properties, events, expressionScope);
                } else if (name == "x-once") {
                    if (!string.IsNullOrEmpty(value)) throw TemplateError("Directive 'x-once' cannot have a value.", element);
                    line.Clear().Append(indent).Append($"...((renderCount==0) ? [utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"] : [utils.createVDOM({ToJavaScriptString(element.Name)}, null, null, null, {{once:true}})]),");
                } else if (name == "x-pre") {
                    if (!string.IsNullOrEmpty(value)) throw TemplateError("Directive 'x-pre' cannot have a value.", element);
                    options.Add("format:\"html\"");
                    text = ToJavaScriptString(SerializeChildren(element));
                } else if (name.StartsWith("x-", StringComparison.Ordinal)) {
                    throw TemplateError($"Invalid X template directive '{name}'.", element);
                } else {
                    attributes.Add($"{ToJavaScriptString(name)}:utils.rewriteAttribute({ToJavaScriptString(element.Name)}, {staticAttributesJavascript}, {ToJavaScriptString(name)}, {ToJavaScriptString(value)})");
                }
            }

            if (classes.Count > 0) attributes.Add($"...{{class:[{string.Join(',', classes)}].filter(c => c).join(' ')}}");
            line.Append(", ").Append(attributes.Count > 0 ? $"{{{string.Join(',', attributes)}}}" : "null");
            line.Append(", ").Append(properties.Count > 0 ? $"{{{string.Join(',', properties)}}}" : "null");
            line.Append(", ").Append(events.Count > 0 ? $"{{{string.Join(',', events)}}}" : "null");
            line.Append(", ").Append(options.Count > 0 ? $"{{{string.Join(',', options)}}}" : "null");
            if (text != null) {
                line.Append(", ").Append(text).Append(')').Append(postLine.Count > 0 ? string.Join("", postLine) : ",");
                javascript.Add(line.ToString());
            } else if (element.Children.Count > 0) {
                line.Append(", [");
                javascript.Add(line.ToString());

                for (var childIndex = 0; childIndex < element.Children.Count; childIndex++) {
                    CompileNode(
                        element.Children[childIndex],
                        childIndex,
                        javascript,
                        level + 1,
                        childScope
                    );
                }

                var closing = new StringBuilder(indent);
                closing.Append(']');

                if (childrenToAppend != null) {
                    closing.Append(", ");
                    closing.Append(childrenToAppend);
                }

                closing.Append(')');

                if (postLine.Count > 0) {
                    closing.Append(string.Join("", postLine));
                } else {
                    closing.Append(',');
                }

                javascript.Add(closing.ToString());
            } else {
                line.Append(')').Append(postLine.Count > 0 ? string.Join("", postLine) : ",");
                javascript.Add(line.ToString());
            }
            if (post.Count > 0) javascript.Add(string.Join("", post));
            return 1;
        }

        private void CompileModel(ElementNode element, string expression, List<string> properties, List<string> events, XTemplateExpressionJavaScriptScope scope) {
            var model = ParseExpression(expression, "x-model", element.SourceOffset);
            if (!XTemplateExpressions.IsAssignable(model)) throw TemplateError("Directive 'x-model' requires an assignable XTemplate expression.", element);
            var value = _expressionCompiler.Compile(model, scope);
            var assignment = _expressionCompiler.CompileAssignment(model, "value", scope);
            var propertyName = "value";
            var propertyValue = value;
            if (element.Name == "input") {
                var type = element.GetAttribute("type");
                if (type == "range") propertyName = "valueAsNumber";
                else if (type == "checkbox") propertyName = "checked";
                else if (type == "radio") {
                    propertyName = "checked";
                    propertyValue = $"function() {{ return utils.expr.equal({value}, utils.expr.member(this.attrs, \"value\")); }}";
                }
            } else if (element.Name == "select" && element.HasAttribute("multiple")) throw TemplateError("x-model on select[multiple] is not supported.", element);
            properties.Add($"{propertyName}:{propertyValue}");
            events.Add($"'change.stop': (event) => {{ let value = utils.getInputValue(event.target); {assignment}; invalidate(); }}");
        }

        private static LoopDefinition ParseLoop(string value, string directive, bool recursive, ElementNode element) {
            var separator = value.IndexOf(" in ", StringComparison.Ordinal);
            if (separator <= 0 || value.IndexOf(" in ", separator + 4, StringComparison.Ordinal) >= 0) {
                throw TemplateError($"Invalid {directive} expression '{value}'. Expected 'item in collection' or '(item,index) in collection'.", element);
            }
            var variablesText = value[..separator].Trim();
            var collection = value[(separator + 4)..].Trim();
            if (collection.Length == 0) throw TemplateError($"Invalid {directive} expression '{value}': the collection is missing.", element);
            var variables = variablesText.StartsWith('(') && variablesText.EndsWith(')')
                ? variablesText[1..^1].Split(',').Select(item => item.Trim()).ToArray()
                : new[] { variablesText };
            if (variables.Length == 0 || variables.Length > (recursive ? 3 : 2) || variables.Any(item => !IsJavaScriptIdentifier(item))) {
                throw TemplateError($"Invalid {directive} variables '{variablesText}'.", element);
            }
            return new LoopDefinition(variables[0], variables.Length > 1 ? variables[1] : "index", variables.Length > 2 ? variables[2] : "indexAbsolute", "indent", collection);
        }

        private static void ValidateConditionalChains(ElementNode parent) {
            foreach (var child in parent.Children) {
                if (child is ElementNode element) {
                    var structuralCount = new[] { "x-if", "x-elseif", "x-else", "x-for", "x-recursive", "x-once" }.Count(element.HasAttribute);
                    if (structuralCount > 1) throw TemplateError("An element cannot contain more than one structural directive.", element);
                    ValidateConditionalChains(element);
                }
            }
        }

        private static void EnsureEmptyElement(ElementNode element, string directive) {
            if (element.Children.Count > 0) throw TemplateError($"Directive '{directive}' requires an empty element.", element);
        }
        private string CompileExpression(string source, string directive, int offset, XTemplateExpressionJavaScriptScope scope) => _expressionCompiler.Compile(ParseExpression(source, directive, offset), scope);
        private static XTemplateExpression ParseExpression(string source, string directive, int offset) {
            if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException($"Directive '{directive}' requires an XTemplate expression at template offset {offset}.");
            try { return XTemplateExpressions.Parse(source); }
            catch (XTemplateExpressionException exception) { throw new InvalidOperationException($"Invalid XTemplate expression for '{directive}' at template offset {offset}: {exception.Message}", exception); }
        }
        private static string ValidateKeyName(string? keyName, ElementNode element) {
            if (!IsXTemplateIdentifier(keyName)) throw TemplateError("Directive 'x-key' requires an XTemplate member name.", element);
            return keyName!;
        }
        private static InvalidOperationException TemplateError(string message, ElementNode element) => new($"{message} Near <{element.Name}> at template offset {element.Offset}.");
        private static bool IsStaticAttribute(string name) => !name.StartsWith("x-", StringComparison.Ordinal);
        private static bool IsJavaScriptIdentifier(string value) => IsXTemplateIdentifier(value);
        private static bool IsXTemplateIdentifier(string? value) => !string.IsNullOrEmpty(value) && (char.IsAsciiLetter(value[0]) || value[0] == '_') && value.Skip(1).All(character => char.IsAsciiLetterOrDigit(character) || character == '_');
        private static string NormalizeLineEndings(string value) => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
        private static string ToJavaScriptString(string value) => JsonSerializer.Serialize(value);

        private static string KebabToCamel(string value) {
            var result = new StringBuilder(value.Length);
            var upper = false;
            foreach (var character in value) {
                if (character == '-') upper = true;
                else {
                    result.Append(upper ? char.ToUpperInvariant(character) : character);
                    upper = false;
                }
            }
            return result.ToString();
        }

        private static string SerializeChildren(ElementNode element) {
            var result = new StringBuilder();
            foreach (var child in element.Children) SerializeNode(child, result);
            return result.ToString();
        }

        private static void SerializeNode(TemplateNode node, StringBuilder result) {
            if (node is TextNode text) result.Append(WebUtility.HtmlEncode(text.Text));
            else if (node is ExpressionNode expression) result.Append("{{").Append(expression.Expression).Append("}}");
            else if (node is CommentNode comment) result.Append("<!--").Append(comment.Text).Append("-->");
            else if (node is ElementNode element) {
                result.Append('<').Append(element.Name);
                foreach (var attribute in element.Attributes) {
                    result.Append(' ').Append(attribute.Name);
                    if (attribute.HasValue) result.Append("=\"").Append(WebUtility.HtmlEncode(attribute.Value)).Append('"');
                }
                result.Append('>');
                foreach (var child in element.Children) SerializeNode(child, result);
                if (!VoidElements.Contains(element.Name)) result.Append("</").Append(element.Name).Append('>');
            }
        }

        private abstract record TemplateNode(int Offset);
        private sealed record TextNode(string Text, int SourceOffset) : TemplateNode(SourceOffset);
        private sealed record ExpressionNode(string Expression, int SourceOffset) : TemplateNode(SourceOffset);
        private sealed record CommentNode(string Text, int SourceOffset) : TemplateNode(SourceOffset);
        private sealed record TemplateAttribute(string Name, string Value, bool HasValue);
        private sealed record LoopDefinition(string Item, string Index, string AbsoluteIndex, string Indent, string Collection);
        private sealed record ElementNode(string Name, List<TemplateAttribute> Attributes, List<TemplateNode> Children, int SourceOffset) : TemplateNode(SourceOffset) {
            public bool HasAttribute(string name) => Attributes.Any(attribute => attribute.Name == name);
            public string? GetAttribute(string name) => Attributes.FirstOrDefault(attribute => attribute.Name == name)?.Value;
        }

        private sealed class HtmlParser(string source) {

            // vars
            private int mPosition;

            // methods
            public ElementNode Parse() {
                var root = new ElementNode("#root", new(), new(), 0);
                var stack = new Stack<ElementNode>();
                stack.Push(root);
                while (mPosition < source.Length) {
                    if (source.AsSpan(mPosition).StartsWith("<!--", StringComparison.Ordinal)) ParseComment(stack.Peek());
                    else if (source[mPosition] == '<' && mPosition + 1 < source.Length && source[mPosition + 1] == '/') ParseClosingTag(stack);
                    else if (source[mPosition] == '<' && mPosition + 1 < source.Length && IsTagNameStart(source[mPosition + 1])) ParseElement(stack);
                    else ParseText(stack.Peek());
                }
                if (stack.Count != 1) throw new InvalidOperationException($"Malformed X template: element <{stack.Peek().Name}> is not closed.");
                return root;
            }

            // methods (private)
            private void ParseComment(ElementNode parent) {
                var offset = mPosition;
                var end = source.IndexOf("-->", mPosition + 4, StringComparison.Ordinal);
                if (end < 0) throw new InvalidOperationException($"Malformed X template: comment at offset {offset} is not closed.");
                parent.Children.Add(new CommentNode(source[(mPosition + 4)..end], offset));
                mPosition = end + 3;
            }

            private void ParseClosingTag(Stack<ElementNode> stack) {
                var offset = mPosition;
                mPosition += 2;
                SkipWhitespace();
                var name = ReadName().ToLowerInvariant();
                SkipWhitespace();
                if (mPosition >= source.Length || source[mPosition] != '>') throw new InvalidOperationException($"Malformed X template closing tag at offset {offset}.");
                mPosition++;
                if (stack.Count == 1 || stack.Peek().Name != name) throw new InvalidOperationException($"Malformed X template: unexpected closing tag </{name}> at offset {offset}.");
                stack.Pop();
            }

            private void ParseElement(Stack<ElementNode> stack) {
                var offset = mPosition++;
                var name = ReadName().ToLowerInvariant();
                var attributes = new List<TemplateAttribute>();
                var selfClosing = false;
                while (mPosition < source.Length) {
                    SkipWhitespace();
                    if (mPosition >= source.Length) break;
                    if (source[mPosition] == '>') { mPosition++; break; }
                    if (source[mPosition] == '/' && mPosition + 1 < source.Length && source[mPosition + 1] == '>') { selfClosing = true; mPosition += 2; break; }
                    var attributeName = ReadAttributeName().ToLowerInvariant();
                    if (attributeName.Length == 0) throw new InvalidOperationException($"Malformed X template attribute at offset {mPosition}.");
                    if (attributes.Any(attribute => attribute.Name == attributeName)) throw new InvalidOperationException($"Malformed X template: duplicate attribute '{attributeName}' on <{name}>.");
                    SkipWhitespace();
                    var hasValue = mPosition < source.Length && source[mPosition] == '=';
                    var value = "";
                    if (hasValue) { mPosition++; SkipWhitespace(); value = WebUtility.HtmlDecode(ReadAttributeValue()); }
                    attributes.Add(new TemplateAttribute(attributeName, value, hasValue));
                }
                var element = new ElementNode(name, attributes, new(), offset);
                stack.Peek().Children.Add(element);
                if (selfClosing || VoidElements.Contains(name)) return;
                if (RawTextElements.Contains(name)) {
                    var closing = $"</{name}>";
                    var end = source.IndexOf(closing, mPosition, StringComparison.OrdinalIgnoreCase);
                    if (end < 0) throw new InvalidOperationException($"Malformed X template: element <{name}> at offset {offset} is not closed.");
                    if (end > mPosition) element.Children.Add(new TextNode(source[mPosition..end], mPosition));
                    mPosition = end + closing.Length;
                    return;
                }
                stack.Push(element);
            }

            private void ParseText(ElementNode parent) {
                var offset = mPosition;
                var end = FindNextMarkupStart();
                var value = WebUtility.HtmlDecode(source[mPosition..end]);
                mPosition = end;
                AddInterpolatedText(parent, value, offset);
            }

            private int FindNextMarkupStart() {
                var position = mPosition;
                while (position < source.Length) {
                    position = source.IndexOf('<', position);
                    if (position < 0) return source.Length;
                    if (source.AsSpan(position).StartsWith("<!--", StringComparison.Ordinal) ||
                        position + 1 < source.Length && (source[position + 1] == '/' || IsTagNameStart(source[position + 1]))) return position;
                    position++;
                }
                return source.Length;
            }

            private static void AddInterpolatedText(ElementNode parent, string value, int offset) {
                var position = 0;
                while (position < value.Length) {
                    var start = value.IndexOf("{{", position, StringComparison.Ordinal);
                    if (start < 0) { if (position < value.Length) parent.Children.Add(new TextNode(value[position..], offset + position)); return; }
                    if (start > position) parent.Children.Add(new TextNode(value[position..start], offset + position));
                    var end = value.IndexOf("}}", start + 2, StringComparison.Ordinal);
                    if (end < 0) throw new InvalidOperationException($"Malformed X template interpolation at offset {offset + start}: missing '}}'.");
                    var expression = value[(start + 2)..end];
                    if (string.IsNullOrWhiteSpace(expression)) throw new InvalidOperationException($"Malformed X template interpolation at offset {offset + start}: expression is empty.");
                    parent.Children.Add(new ExpressionNode(expression, offset + start));
                    position = end + 2;
                }
            }

            private string ReadName() {
                var start = mPosition;
                while (mPosition < source.Length && (char.IsLetterOrDigit(source[mPosition]) || source[mPosition] is '-' or ':' or '_')) mPosition++;
                if (start == mPosition) throw new InvalidOperationException($"Malformed X template name at offset {start}.");
                return source[start..mPosition];
            }
            private string ReadAttributeName() {
                var start = mPosition;
                while (mPosition < source.Length && !char.IsWhiteSpace(source[mPosition]) && source[mPosition] is not '=' and not '>' && !(source[mPosition] == '/' && mPosition + 1 < source.Length && source[mPosition + 1] == '>')) mPosition++;
                return source[start..mPosition];
            }
            private string ReadAttributeValue() {
                if (mPosition >= source.Length) throw new InvalidOperationException("Malformed X template: attribute value is missing.");
                if (source[mPosition] is '\'' or '"') {
                    var quote = source[mPosition++];
                    var start = mPosition;
                    while (mPosition < source.Length && source[mPosition] != quote) mPosition++;
                    if (mPosition >= source.Length) throw new InvalidOperationException($"Malformed X template: quoted attribute at offset {start - 1} is not closed.");
                    var value = source[start..mPosition];
                    mPosition++;
                    return value;
                }
                var startUnquoted = mPosition;
                while (mPosition < source.Length && !char.IsWhiteSpace(source[mPosition]) && source[mPosition] != '>') mPosition++;
                return source[startUnquoted..mPosition];
            }
            private void SkipWhitespace() { while (mPosition < source.Length && char.IsWhiteSpace(source[mPosition])) mPosition++; }
            private static bool IsTagNameStart(char character) => char.IsLetter(character) || character is '_' or ':';
        }
    }
}
