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
        private static readonly string[] PrimaryStructuralDirectiveNames = ["x-if", "x-elseif", "x-else", "x-for", "x-recursive", "x-once"];

        private enum StructuralDirectiveKind {
            None,
            If,
            ElseIf,
            Else,
            For,
            Recursive,
            Once
        }

        private enum ConditionalChainState {
            None,
            Open,
            ClosedByElse
        }

        // vars
        private readonly XTemplateExpressionJavaScriptCompiler _expressionCompiler = new();

        // methods
        public string Compile(string template) {
            return CompileArtifact(template).RenderJavaScript;
        }
        public XTemplateCompileResult CompileArtifact(string template) {
            if (template == null) throw new ArgumentNullException(nameof(template));
            var root = new HtmlParser(NormalizeLineEndings(template).Trim()).Parse();
            var indent = "    ";
            ValidateStructuralDirectives(root);
            ValidateConditionalChains(root);
            var dependencies = new Dictionary<string, List<IReadOnlyList<string>>>(StringComparer.Ordinal);
            var slots = new List<string>();
            CollectMetadata(root, [], dependencies, slots, new HashSet<string>(StringComparer.Ordinal));
            var body = new List<string> {
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
            var javascript = "(state, handler, invalidate, utils, i18n, renderCount) => {\n" + indent + string.Join("\n" + indent, body) + "\n" + indent + "}";
            var compiledDependencies = dependencies.Select(item => new XTemplateDependency(item.Key, item.Value)).ToArray();
            return new XTemplateCompileResult(javascript, compiledDependencies, slots);
        }

        // methods (private)
        private static void CollectMetadata(
            ElementNode element,
            IReadOnlyList<string> ancestors,
            Dictionary<string, List<IReadOnlyList<string>>> dependencies,
            List<string> slots,
            HashSet<string> slotNames) {
            foreach (var child in element.Children.OfType<ElementNode>()) {
                if (child.Name.Contains('-', StringComparison.Ordinal)) {
                    var resource = "component:" + child.Name;
                    if (!dependencies.TryGetValue(resource, out var paths)) {
                        paths = [];
                        dependencies.Add(resource, paths);
                    }
                    if (!paths.Any(path => path.SequenceEqual(ancestors))) paths.Add(ancestors.ToArray());
                }
                if (child.Name == "slot") {
                    ValidateStaticSlotName(child);
                    var slotName = child.GetAttribute("name") ?? "";
                    if (slotNames.Add(slotName)) slots.Add(slotName);
                }
                if (child.HasAttribute("x-pre")) continue;
                var childAncestors = new string[ancestors.Count + 1];
                for (var index = 0; index < ancestors.Count; index++) childAncestors[index] = ancestors[index];
                childAncestors[^1] = child.Name;
                CollectMetadata(child, childAncestors, dependencies, slots, slotNames);
            }
        }
        private static void ValidateStaticSlotName(ElementNode element) {
            foreach (var attribute in element.Attributes) {
                if (attribute.Name == "x-attr" || attribute.Name.StartsWith("x-attr:[", StringComparison.Ordinal) || attribute.Name == "x-attr:name") {
                    throw TemplateError("Slot names must be declared statically with the 'name' attribute.", element);
                }
            }
        }
        private int CompileNode(TemplateNode node, int index, List<string> javascript, int level, XTemplateExpressionJavaScriptScope scope) {
            var indent = new string(' ', (level + 1) * 4);
            if (node is TextNode textNode) {
                javascript.Add($"{indent}utils.createVDOM(\"#text\", null, null, null, null, {{index: {index}}}, {ToJavaScriptString(textNode.Text)}),");
                return 1;
            }
            if (node is ExpressionNode expressionNode) {
                var expression = CompileExpression(expressionNode.Expression, "interpolation", expressionNode.SourceOffset, scope);
                javascript.Add($"{indent}utils.createVDOM(\"#text\", null, null, null, null, {{index: {index}}}, utils.expr.scalar({expression})),");
                return 1;
            }
            if (node is CommentNode commentNode) {
                javascript.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, null, {{index: {index}}}, {ToJavaScriptString(commentNode.Text)}),");
                return 1;
            }

            var element = (ElementNode)node;
            if (element.Name.StartsWith("x:", StringComparison.Ordinal)) throw TemplateError($"Invalid X template node <{element.Name}>.", element);
            var line = new StringBuilder(indent).Append("utils.createVDOM(").Append(ToJavaScriptString(element.Name));
            var postLine = new List<string>();
            var post = new List<string>();
            var attributes = new List<string>();
            var properties = new List<string>();
            string? styles = null;
            var events = new List<string>();
            var options = new List<string> { $"index:{index}" };
            var classes = new List<string>();
            var childScope = scope;
            var expressionScope = scope;
            var structuralDirective = GetStructuralDirective(element);
            var scopedLoop = structuralDirective.Kind is StructuralDirectiveKind.For or StructuralDirectiveKind.Recursive ? structuralDirective.Attribute : null;
            if (scopedLoop != null) {
                var loop = ParseLoop(scopedLoop.Value, scopedLoop.Name, scopedLoop.Name == "x-recursive", element);
                expressionScope = scopedLoop.Name == "x-recursive"
                    ? scope.With(loop.Item, loop.Index, loop.AbsoluteIndex, loop.Indent)
                    : scope.With(loop.Item, loop.Index);
                childScope = expressionScope;
            }
            string? text = null;
            string? childrenToAppend = null;
            var styleAttribute = element.Attributes.FirstOrDefault(attribute => attribute.Name == "style");
            if (styleAttribute != null) styles = CompileStyles(styleAttribute);
            var staticAttributes = element.Attributes.Where(attribute => IsStaticAttribute(attribute.Name) && attribute.Name != "style").ToArray();
            var staticAttributesJavascript = "{" + string.Join(',', staticAttributes.Select(attribute => $"{ToJavaScriptString(attribute.Name)}:{ToJavaScriptString(attribute.Value)}")) + "}";

            // merge static classes into the conditional class binding
            if (element.Attributes.Any(attribute => attribute.Name.StartsWith("x-class:", StringComparison.Ordinal))) {
                var classAttribute = element.GetAttribute("class");
                if (!string.IsNullOrEmpty(classAttribute)) {
                    foreach (var className in classAttribute.Split(' ', StringSplitOptions.RemoveEmptyEntries)) classes.Add(ToJavaScriptString(className));
                }
            }

            // apply the element structural wrapper once, independently of attribute source order
            switch (structuralDirective.Kind) {
                case StructuralDirectiveKind.If:
                    line.Clear().Append(indent).Append($"...((_ifs.c{level} = utils.expr.truthy({CompileExpression(structuralDirective.Attribute!.Value, structuralDirective.Attribute.Name, element.SourceOffset, expressionScope)})) ? [utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"] : [utils.createVDOM(\"#comment\", null, null, null, null, {{index: {index}}}, 'x-if')]),");
                    break;
                case StructuralDirectiveKind.ElseIf:
                    line.Clear().Append(indent).Append($"...(_ifs.c{level} ? [] : (_ifs.c{level} = utils.expr.truthy({CompileExpression(structuralDirective.Attribute!.Value, structuralDirective.Attribute.Name, element.SourceOffset, expressionScope)})) ? [utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"] : [utils.createVDOM(\"#comment\", null, null, null, null, {{index: {index}}}, 'x-elseif')]),");
                    break;
                case StructuralDirectiveKind.Else:
                    if (!string.IsNullOrEmpty(structuralDirective.Attribute!.Value)) throw TemplateError("Directive 'x-else' cannot have a value.", element);
                    line.Clear().Append(indent).Append($"...(!_ifs.c{level} ? [utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"] : [utils.createVDOM(\"#comment\", null, null, null, null, {{index: {index}}}, 'x-else')]),");
                    break;
                case StructuralDirectiveKind.For:
                    var forLoop = ParseLoop(structuralDirective.Attribute!.Value, structuralDirective.Attribute.Name, false, element);
                    var forKeyName = element.GetAttribute("x-key");
                    var forType = string.IsNullOrWhiteSpace(forKeyName) ? "position" : "key";
                    var forCollection = CompileExpression(forLoop.Collection, structuralDirective.Attribute.Name, element.SourceOffset, scope);
                    javascript.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, null, {{index: {index}, forType:'{forType}'}}, 'x-for-start'),");
                    line.Clear().Append(indent).Append($"...(utils.expr.collection({forCollection}).map(({forLoop.Item}, {forLoop.Index}) => utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add(")),");
                    if (!string.IsNullOrWhiteSpace(forKeyName)) options.Add($"\"key\":utils.expr.member({forLoop.Item}, {ToJavaScriptString(ValidateKeyName(forKeyName, element))})");
                    post.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, null, {{index: {index}, forType:'{forType}'}}, 'x-for-end'),");
                    break;
                case StructuralDirectiveKind.Recursive:
                    var recursiveLoop = ParseLoop(structuralDirective.Attribute!.Value, structuralDirective.Attribute.Name, true, element);
                    var recursiveKeyName = element.GetAttribute("x-key");
                    var recursiveForType = string.IsNullOrWhiteSpace(recursiveKeyName) ? "position" : "key";
                    var recursiveCollection = CompileExpression(recursiveLoop.Collection, structuralDirective.Attribute.Name, element.SourceOffset, scope);
                    javascript.Add($"{indent}...(func = (_items, {recursiveLoop.AbsoluteIndex}, {recursiveLoop.Indent}, wrapper) => {{ let _itemsArray = utils.expr.collection(_items); let _result = [");
                    indent += "    ";
                    level++;
                    javascript.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, null, {{index: {index - 1}, forType:'{recursiveForType}'}}, 'x-for-start'),");
                    line.Clear().Append(indent).Append($"...(_itemsArray.map(({recursiveLoop.Item}, {recursiveLoop.Index}) => {{ let _result = utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"; {recursiveLoop.AbsoluteIndex}++; return _result;}})),");
                    if (!string.IsNullOrWhiteSpace(recursiveKeyName)) options.Add($"\"key\":utils.expr.member({recursiveLoop.Item}, {ToJavaScriptString(ValidateKeyName(recursiveKeyName, element))})");
                    post.Add($"{indent}utils.createVDOM(\"#comment\", null, null, null, null, {{index: {index - 1}, forType:'{recursiveForType}'}}, 'x-for-end'),");
                    var wrapper = element.GetAttribute("x-recursive-wrapper") ?? "";
                    post.Add($"\n    {indent[..^4]}]; if (wrapper) _result = [utils.createVDOM(wrapper, null, null, null, null, {{index:1}}, _result)]; return _result;}})({recursiveCollection}, 0, 0),");
                    childrenToAppend = $"func(utils.expr.member({recursiveLoop.Item}, \"children\"), {recursiveLoop.AbsoluteIndex} + 1, {recursiveLoop.Indent} + 1, {ToJavaScriptString(wrapper)})";
                    break;
                case StructuralDirectiveKind.Once:
                    if (!string.IsNullOrEmpty(structuralDirective.Attribute!.Value)) throw TemplateError("Directive 'x-once' cannot have a value.", element);
                    line.Clear().Append(indent).Append($"...((renderCount==0) ? [utils.createVDOM({ToJavaScriptString(element.Name)}");
                    postLine.Add($"] : [utils.createVDOM({ToJavaScriptString(element.Name)}, null, null, null, null, {{once:true}})]),");
                    break;
            }

            foreach (var attribute in element.Attributes) {
                var name = attribute.Name;
                var value = attribute.Value;
                if (IsPrimaryStructuralDirective(name)) {
                    continue;
                } else if (name == "style") {
                    continue;
                } else if (name == "class" && classes.Count > 0) {
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
                    else {
                        if (string.Equals(attributeName, "style", StringComparison.OrdinalIgnoreCase)) throw new XTemplateException("Generic attribute binding cannot target 'style'", attribute.Offset);
                        attributes.Add($"{ToJavaScriptString(attributeName)}:{expression}");
                    }
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
                } else if (name == "x-key") {
                    if (!element.HasAttribute("x-for") && !element.HasAttribute("x-recursive")) throw TemplateError("Directive 'x-key' requires 'x-for' or 'x-recursive'.", element);
                    if (string.IsNullOrWhiteSpace(value)) throw TemplateError("Directive 'x-key' requires a property name.", element);
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
                } else if (name == "x-pre") {
                    if (!string.IsNullOrEmpty(value)) throw TemplateError("Directive 'x-pre' cannot have a value.", element);
                    EnsureRawContentHasNoInlineStyles(element);
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
            line.Append(", ").Append(styles ?? "null");
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

        private static void ValidateStructuralDirectives(ElementNode parent) {
            foreach (var child in parent.Children) {
                if (child is ElementNode element) {
                    var primaryDirectives = PrimaryStructuralDirectiveNames.Where(element.HasAttribute).ToArray();
                    if (primaryDirectives.Length > 1) {
                        var directives = string.Join(", ", primaryDirectives.Select(directive => $"'{directive}'"));
                        throw TemplateError($"Element <{element.Name}> cannot contain more than one primary structural directive: {directives}.", element);
                    }
                    if (element.HasAttribute("x-key") && !element.HasAttribute("x-for") && !element.HasAttribute("x-recursive")) throw TemplateError("Directive 'x-key' requires 'x-for' or 'x-recursive'.", element);
                    if (element.HasAttribute("x-recursive-wrapper") && !element.HasAttribute("x-recursive")) throw TemplateError("Directive 'x-recursive-wrapper' requires 'x-recursive'.", element);
                    ValidateStructuralDirectives(element);
                }
            }
        }

        private static void ValidateConditionalChains(ElementNode parent) {
            var chainState = ConditionalChainState.None;
            foreach (var child in parent.Children) {
                if (child is ElementNode element) {
                    switch (GetStructuralDirective(element).Kind) {
                        case StructuralDirectiveKind.If:
                            chainState = ConditionalChainState.Open;
                            break;
                        case StructuralDirectiveKind.ElseIf:
                            if (chainState != ConditionalChainState.Open) {
                                throw TemplateError("Directive 'x-elseif' requires a preceding 'x-if' or 'x-elseif' conditional chain.", element);
                            }
                            break;
                        case StructuralDirectiveKind.Else:
                            if (chainState != ConditionalChainState.Open) {
                                throw TemplateError("Directive 'x-else' requires a preceding conditional chain.", element);
                            }
                            chainState = ConditionalChainState.ClosedByElse;
                            break;
                        default:
                            chainState = ConditionalChainState.None;
                            break;
                    }
                    ValidateConditionalChains(element);
                } else if (child is not CommentNode && (child is not TextNode text || !string.IsNullOrWhiteSpace(text.Text))) {
                    chainState = ConditionalChainState.None;
                }
            }
        }

        private static StructuralDirectiveInfo GetStructuralDirective(ElementNode element) {
            var attribute = element.Attributes.FirstOrDefault(attribute => IsPrimaryStructuralDirective(attribute.Name));
            return attribute == null ? new(StructuralDirectiveKind.None, null) : new(GetStructuralDirectiveKind(attribute.Name), attribute);
        }

        private static bool IsPrimaryStructuralDirective(string name) => GetStructuralDirectiveKind(name) != StructuralDirectiveKind.None;
        private static StructuralDirectiveKind GetStructuralDirectiveKind(string name) => name switch {
            "x-if" => StructuralDirectiveKind.If,
            "x-elseif" => StructuralDirectiveKind.ElseIf,
            "x-else" => StructuralDirectiveKind.Else,
            "x-for" => StructuralDirectiveKind.For,
            "x-recursive" => StructuralDirectiveKind.Recursive,
            "x-once" => StructuralDirectiveKind.Once,
            _ => StructuralDirectiveKind.None
        };

        private static void EnsureEmptyElement(ElementNode element, string directive) {
            if (element.Children.Count > 0) throw TemplateError($"Directive '{directive}' requires an empty element.", element);
        }
        private static void EnsureRawContentHasNoInlineStyles(ElementNode element) {
            foreach (var child in element.Children.OfType<ElementNode>()) {
                var styleAttribute = child.Attributes.FirstOrDefault(attribute => attribute.Name == "style");
                if (styleAttribute != null) throw new XTemplateException("Inline style attributes are not supported inside x-pre raw content", styleAttribute.Offset);
                EnsureRawContentHasNoInlineStyles(child);
            }
        }
        private static string CompileStyles(TemplateAttribute attribute) {
            var declarations = ParseStyleDeclarations(attribute.Value, attribute.Offset);
            return "{" + string.Join(',', declarations.Select(declaration => $"[{ToJavaScriptString(declaration.Name)}]:{{value:{ToJavaScriptString(declaration.Value)},priority:{ToJavaScriptString(declaration.Priority)}}}")) + "}";
        }
        private static IReadOnlyList<StyleDeclaration> ParseStyleDeclarations(string source, int offset) {
            var declarations = new Dictionary<string, StyleDeclaration>(StringComparer.Ordinal);
            var position = 0;
            while (position < source.Length) {
                while (position < source.Length && (char.IsWhiteSpace(source[position]) || source[position] == ';')) position++;
                if (position >= source.Length) break;
                var start = position;
                var colon = -1;
                var quote = '\0';
                var parentheses = 0;
                var brackets = 0;
                var braces = 0;
                while (position < source.Length) {
                    var character = source[position];
                    if (character == '\\') {
                        if (position + 1 >= source.Length) throw new XTemplateException("Invalid style declaration: incomplete escape", offset + position);
                        position += 2;
                        continue;
                    }
                    if (quote != '\0') {
                        if (character == quote) quote = '\0';
                        position++;
                        continue;
                    }
                    if (character is '\'' or '"') { quote = character; position++; continue; }
                    if (character == '(') parentheses++;
                    else if (character == ')' && --parentheses < 0) throw new XTemplateException("Invalid style declaration: unmatched ')'", offset + position);
                    else if (character == '[') brackets++;
                    else if (character == ']' && --brackets < 0) throw new XTemplateException("Invalid style declaration: unmatched ']'", offset + position);
                    else if (character == '{') braces++;
                    else if (character == '}' && --braces < 0) throw new XTemplateException("Invalid style declaration: unmatched '}'", offset + position);
                    else if (character == ':' && colon < 0 && parentheses == 0 && brackets == 0 && braces == 0) colon = position;
                    else if (character == ';' && parentheses == 0 && brackets == 0 && braces == 0) break;
                    position++;
                }
                if (quote != '\0' || parentheses != 0 || brackets != 0 || braces != 0) throw new XTemplateException("Invalid style declaration: unclosed string or function", offset + start);
                var end = position;
                if (position < source.Length && source[position] == ';') position++;
                if (colon < start || colon >= end) throw new XTemplateException("Invalid style declaration: expected a property name followed by ':'", offset + start);
                var name = source[start..colon].Trim();
                var value = source[(colon + 1)..end].Trim();
                if (!IsCssPropertyName(name)) throw new XTemplateException($"Invalid style declaration property name '{name}'", offset + start);
                if (value.Length == 0) throw new XTemplateException($"Invalid style declaration for '{name}': value is empty", offset + colon + 1);
                if (!name.StartsWith("--", StringComparison.Ordinal)) name = name.ToLowerInvariant();
                var (normalizedValue, priority) = ExtractStylePriority(value, offset + colon + 1);
                declarations[name] = new(name, normalizedValue, priority);
            }
            return declarations.Values.ToArray();
        }
        private static (string Value, string Priority) ExtractStylePriority(string value, int offset) {
            var quote = '\0';
            var parentheses = 0;
            var brackets = 0;
            var braces = 0;
            var importantMarker = -1;
            for (var position = 0; position < value.Length; position++) {
                var character = value[position];
                if (character == '\\') { position++; continue; }
                if (quote != '\0') { if (character == quote) quote = '\0'; continue; }
                if (character is '\'' or '"') { quote = character; continue; }
                if (character == '(') parentheses++;
                else if (character == ')') parentheses--;
                else if (character == '[') brackets++;
                else if (character == ']') brackets--;
                else if (character == '{') braces++;
                else if (character == '}') braces--;
                else if (character == '!' && parentheses == 0 && brackets == 0 && braces == 0) importantMarker = position;
            }
            if (importantMarker < 0 || !string.Equals(value[(importantMarker + 1)..].Trim(), "important", StringComparison.OrdinalIgnoreCase)) return (value, string.Empty);
            var normalizedValue = value[..importantMarker].TrimEnd();
            if (normalizedValue.Length == 0) throw new XTemplateException("Invalid style declaration: '!important' requires a value", offset + importantMarker);
            return (normalizedValue, "important");
        }
        private static bool IsCssPropertyName(string name) {
            if (name.Length == 0 || name.Any(character => char.IsWhiteSpace(character) || char.IsControl(character) || character is ':' or ';')) return false;
            if (name.StartsWith("--", StringComparison.Ordinal)) return name.Length > 2;
            if (!char.IsLetter(name[0]) && name[0] != '-' && name[0] != '_') return false;
            return name.Skip(1).All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
        }
        private string CompileExpression(string source, string directive, int offset, XTemplateExpressionJavaScriptScope scope) => _expressionCompiler.Compile(ParseExpression(source, directive, offset), scope);
        private static XTemplateExpression ParseExpression(string source, string directive, int offset) {
            if (string.IsNullOrWhiteSpace(source)) throw new InvalidOperationException($"Directive '{directive}' requires an XTemplate expression at template offset {offset}.");
            try { return XTemplateExpressions.Parse(source); }
            catch (XTemplateExpressionException exception) { throw new InvalidOperationException($"Invalid XTemplate expression '{source}' for directive '{directive}' at template offset {offset}: {exception.Message}", exception); }
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
        private sealed record TemplateAttribute(string Name, string Value, bool HasValue, int Offset);
        private sealed record StyleDeclaration(string Name, string Value, string Priority);
        private sealed record StructuralDirectiveInfo(StructuralDirectiveKind Kind, TemplateAttribute? Attribute);
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
                    var attributeOffset = mPosition;
                    var attributeName = ReadAttributeName().ToLowerInvariant();
                    if (attributeName.Length == 0) throw new InvalidOperationException($"Malformed X template attribute at offset {mPosition}.");
                    if (attributes.Any(attribute => attribute.Name == attributeName)) throw new InvalidOperationException($"Malformed X template: duplicate attribute '{attributeName}' on <{name}>.");
                    SkipWhitespace();
                    var hasValue = mPosition < source.Length && source[mPosition] == '=';
                    var value = "";
                    if (hasValue) { mPosition++; SkipWhitespace(); value = WebUtility.HtmlDecode(ReadAttributeValue()); }
                    attributes.Add(new TemplateAttribute(attributeName, value, hasValue, attributeOffset));
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
