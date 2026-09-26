using System.Collections;
using System.Net;
using System.Text;

namespace DProjects.XShell.Services.XTemplate {

    public sealed class XTemplateException : Exception {
        public int Offset { get; }
        public XTemplateException(string message, int offset) : base($"{message} (at template offset {offset}).") {
            Offset = offset;
        }
    }
    internal abstract record XTemplateNode(int Offset);
    internal sealed record XTemplateTextNode(string Text, int Offset) : XTemplateNode(Offset);
    internal sealed record XTemplateRawHtmlNode(string Html, int Offset) : XTemplateNode(Offset);
    internal sealed record XTemplateInterpolationNode(XTemplateExpression Expression, int Offset) : XTemplateNode(Offset);
    internal sealed record XTemplateCommentNode(string Text, int Offset) : XTemplateNode(Offset);
    internal sealed record XTemplateStaticAttribute(string Name, string Value, bool HasValue, int Offset) : XTemplateElementAttribute(Offset);
    internal sealed record XTemplateBoundAttribute(string Name, XTemplateExpression Expression, int Offset) : XTemplateElementAttribute(Offset);
    internal sealed record XTemplateDynamicAttribute(XTemplateExpression NameExpression, XTemplateExpression ValueExpression, int Offset) : XTemplateElementAttribute(Offset);
    internal sealed record XTemplateAttributeSpread(XTemplateExpression Expression, int Offset) : XTemplateElementAttribute(Offset);
    internal sealed record XTemplateClassAttribute(string Name, XTemplateExpression Expression, int Offset) : XTemplateElementAttribute(Offset);
    internal sealed record XTemplateShowAttribute(XTemplateExpression Expression, int Offset) : XTemplateElementAttribute(Offset);
    internal abstract record XTemplateElementAttribute(int Offset);
    internal sealed record XTemplateForDefinition(string ItemName, string IndexName, XTemplateExpression Collection, int Offset);
    internal sealed record XTemplateRecursiveDefinition(string ItemName, string IndexName, string AbsoluteIndexName, XTemplateExpression Collection, XTemplateExpression Children, string? WrapperName, int Offset);
    internal sealed record XTemplateSelectModel(object? Value, int Offset);
    internal sealed record XTemplateElementNode(string Name, List<XTemplateElementAttribute> Attributes, List<XTemplateNode> Children, int Offset, XTemplateExpression? If, XTemplateExpression? ElseIf, bool IsElse, XTemplateForDefinition? For, XTemplateRecursiveDefinition? Recursive, XTemplateExpression? Text, XTemplateExpression? Html, XTemplateExpression? Model, XTemplateExpression? ChildrenExpression) : XTemplateNode(Offset);

    internal sealed class OrderedAttributes {

        // vars
        private readonly List<KeyValuePair<string, string?>> _items;

        // props
        public List<KeyValuePair<string, string?>> Items => _items;

        // ctor
        public OrderedAttributes() { _items = new(); }
        public OrderedAttributes(IEnumerable<KeyValuePair<string, string?>> items) { _items = new(items); }

        // methods
        public void Set(string name, string? value) { var index = _items.FindIndex(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase)); if (index < 0) _items.Add(new(name, value)); else _items[index] = new(name, value); }
        public void Remove(string name) { var index = _items.FindIndex(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase)); if (index >= 0) _items.RemoveAt(index); }
        public void AddClass(string name) { var current = _items.FirstOrDefault(item => string.Equals(item.Key, "class", StringComparison.OrdinalIgnoreCase)); var classes = (current.Value ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList(); if (!classes.Contains(name, StringComparer.Ordinal)) classes.Add(name); Set("class", string.Join(' ', classes)); }
        public bool Contains(string name) => _items.Any(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase));
        public string? GetValue(string name) => _items.FirstOrDefault(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    }

    // class
    public sealed class XTemplateRenderer {

        // consts
        private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase) { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };

        // vars
        private readonly XTemplateObjectAccess mObjectAccess;
        private readonly string? mLocale;

        // ctor
        public XTemplateRenderer(IEnumerable<IXTemplateObjectAdapter>? objectAdapters = null, string? locale = null) {
            mObjectAccess = new XTemplateObjectAccess(objectAdapters);
            mLocale = locale;
        }

        // methods
        public string Render(string template, object? state) {
            var root = new XTemplateParser(template.Trim()).Parse();
            var context = new XTemplateExpressionContext(new Dictionary<string, object?> { ["state"] = state }, mObjectAccess.Adapters, mLocale);
            var result = new StringBuilder();
            RenderChildren(root.Children, context, result, null);
            return result.ToString();
        }

        // methods (private)
        private void RenderChildren(IReadOnlyList<XTemplateNode> children, XTemplateExpressionContext context, StringBuilder result, XTemplateSelectModel? selectModel) {
            for (var index = 0; index < children.Count; index++) {
                if (children[index] is XTemplateElementNode element && element.If != null) {
                    var end = FindConditionalEnd(children, index);
                    var selected = FindSelectedConditionalBranch(children, index, end, context);
                    for (var branchIndex = index; branchIndex <= end; branchIndex++) {
                        var node = children[branchIndex];
                        if (node is XTemplateElementNode branch && (branch.If != null || branch.ElseIf != null || branch.IsElse)) {
                            if (ReferenceEquals(branch, selected)) RenderElement(branch, context, result, selectModel);
                        } else RenderNode(node, context, result, selectModel);
                    }
                    index = end;
                    continue;
                }
                if (children[index] is XTemplateElementNode { ElseIf: not null } or XTemplateElementNode { IsElse: true }) throw new XTemplateException("Conditional branch has no preceding x-if", children[index].Offset);
                RenderNode(children[index], context, result, selectModel);
            }
        }
        private static int FindConditionalEnd(IReadOnlyList<XTemplateNode> children, int start) {
            var end = start;
            for (var index = start + 1; index < children.Count; index++) {
                if (children[index] is XTemplateTextNode { Text: var text } && string.IsNullOrWhiteSpace(text)) { end = index; continue; }
                if (children[index] is XTemplateElementNode { ElseIf: not null }) { end = index; continue; }
                if (children[index] is XTemplateElementNode { IsElse: true }) { end = index; break; }
                break;
            }
            return end;
        }
        private static XTemplateElementNode? FindSelectedConditionalBranch(IReadOnlyList<XTemplateNode> children, int start, int end, XTemplateExpressionContext context) {
            for (var index = start; index <= end; index++) {
                if (children[index] is not XTemplateElementNode element) continue;
                if (element.IsElse) return element;
                var condition = element.If ?? element.ElseIf;
                if (condition != null && IsTruthy(XTemplateExpressions.Evaluate(condition, context), element.Offset)) return element;
            }
            return null;
        }
        private void RenderNode(XTemplateNode node, XTemplateExpressionContext context, StringBuilder result, XTemplateSelectModel? selectModel) {
            switch (node) {
                case XTemplateTextNode text: result.Append(HtmlText(text.Text)); break;
                case XTemplateInterpolationNode interpolation: result.Append(HtmlText(ScalarString(XTemplateExpressions.Evaluate(interpolation.Expression, context), interpolation.Offset))); break;
                case XTemplateCommentNode comment: result.Append("<!--").Append(comment.Text).Append("-->"); break;
                case XTemplateRawHtmlNode raw: result.Append(raw.Html); break;
                case XTemplateElementNode element: RenderElement(element, context, result, selectModel); break;
                default: throw new XTemplateException("Unsupported template node", node.Offset);
            }
        }
        private void RenderElement(XTemplateElementNode element, XTemplateExpressionContext context, StringBuilder result, XTemplateSelectModel? selectModel) {
            if (element.ChildrenExpression != null) throw new XTemplateException("x-children is not supported by server rendering because it requires browser DOM nodes", element.Offset);
            if (element.Recursive != null) {
                RenderRecursiveItems(element, NormalizeCollection(XTemplateExpressions.Evaluate(element.Recursive.Collection, context), element.Recursive.Offset), context, 0, 0, result, selectModel);
                return;
            }
            if (element.For != null) {
                var collection = XTemplateExpressions.Evaluate(element.For.Collection, context);
                var itemIndex = 0;
                foreach (var item in NormalizeCollection(collection, element.For.Offset)) {
                    var locals = new Dictionary<string, object?> { [element.For.ItemName] = item, [element.For.IndexName] = itemIndex };
                    RenderElementOnce(element, context.With(locals), result, null, selectModel);
                    itemIndex++;
                }
                return;
            }
            RenderElementOnce(element, context, result, null, selectModel);
        }
        private int RenderRecursiveItems(XTemplateElementNode element, IEnumerable<object?> items, XTemplateExpressionContext context, int indexAbsolute, int indent, StringBuilder result, XTemplateSelectModel? selectModel) {
            var definition = element.Recursive!;
            var siblingIndex = 0;
            foreach (var item in items) {
                var currentAbsolute = indexAbsolute++;
                var locals = new Dictionary<string, object?> { [definition.ItemName] = item, [definition.IndexName] = siblingIndex, [definition.AbsoluteIndexName] = currentAbsolute, ["indent"] = indent };
                RenderElementOnce(element, context.With(locals), result, () => {
                    var childItems = NormalizeCollectionOrEmpty(XTemplateExpressions.Evaluate(definition.Children, context.With(locals)), definition.Offset);
                    var descendants = new StringBuilder();
                    indexAbsolute = RenderRecursiveItems(element, childItems, context.With(locals), indexAbsolute, indent + 1, descendants, selectModel);
                    if (descendants.Length == 0) return;
                    if (definition.WrapperName == null) result.Append(descendants);
                    else result.Append('<').Append(definition.WrapperName).Append('>').Append(descendants).Append("</").Append(definition.WrapperName).Append('>');
                }, selectModel);
                siblingIndex++;
            }
            return indexAbsolute;
        }
        private void RenderElementOnce(XTemplateElementNode element, XTemplateExpressionContext context, StringBuilder result, Action? renderRecursiveChildren, XTemplateSelectModel? selectModel) {
            var attributes = BuildAttributes(element, context);
            var childSelectModel = ApplyModel(element, context, attributes) ?? selectModel;
            if (element.Name == "option" && selectModel != null) {
                var content = new StringBuilder();
                RenderElementContent(element, context, content, renderRecursiveChildren, null);
                ApplyOptionSelection(attributes, selectModel, HtmlTextContent(content.ToString()));
                AppendElementStart(element, attributes, result);
                result.Append(content).Append("</").Append(element.Name).Append('>');
                return;
            }
            AppendElementStart(element, attributes, result);
            if (VoidElements.Contains(element.Name)) return;
            RenderElementContent(element, context, result, renderRecursiveChildren, childSelectModel);
            result.Append("</").Append(element.Name).Append('>');
        }
        private void RenderElementContent(XTemplateElementNode element, XTemplateExpressionContext context, StringBuilder result, Action? renderRecursiveChildren, XTemplateSelectModel? selectModel) {
            if (element.Text != null) result.Append(HtmlText(ScalarString(XTemplateExpressions.Evaluate(element.Text, context), element.Offset)));
            else if (element.Html != null) result.Append(ScalarString(XTemplateExpressions.Evaluate(element.Html, context), element.Offset));
            else RenderChildren(element.Children, context, result, selectModel);
            renderRecursiveChildren?.Invoke();
        }
        private static void AppendElementStart(XTemplateElementNode element, IEnumerable<KeyValuePair<string, string?>> attributes, StringBuilder result) {
            result.Append('<').Append(element.Name);
            foreach (var attribute in attributes) {
                if (string.Equals(attribute.Key, "style", StringComparison.OrdinalIgnoreCase)) throw new XTemplateException("Inline style attributes are not supported by the server XTemplate renderer", element.Offset);
                result.Append(' ').Append(attribute.Key);
                if (attribute.Value != null) result.Append("=\"").Append(HtmlAttribute(attribute.Value)).Append('"');
            }
            result.Append('>');
        }
        private List<KeyValuePair<string, string?>> BuildAttributes(XTemplateElementNode element, XTemplateExpressionContext context) {
            var attributes = new OrderedAttributes();
            var hasShow = false;
            var isHidden = false;
            foreach (var attribute in element.Attributes) {
                switch (attribute) {
                    case XTemplateStaticAttribute staticAttribute: attributes.Set(staticAttribute.Name, staticAttribute.HasValue ? staticAttribute.Value : null); break;
                    case XTemplateAttributeSpread spread: AddSpread(attributes, XTemplateExpressions.Evaluate(spread.Expression, context), spread.Offset); break;
                    case XTemplateBoundAttribute bound: SetBoundAttribute(attributes, bound.Name, XTemplateExpressions.Evaluate(bound.Expression, context), bound.Offset); break;
                    case XTemplateDynamicAttribute dynamicAttribute:
                        var name = ScalarString(XTemplateExpressions.Evaluate(dynamicAttribute.NameExpression, context), dynamicAttribute.Offset);
                        if (!IsValidAttributeName(name)) throw new XTemplateException($"Invalid dynamic attribute name '{name}'", dynamicAttribute.Offset);
                        SetBoundAttribute(attributes, name, XTemplateExpressions.Evaluate(dynamicAttribute.ValueExpression, context), dynamicAttribute.Offset);
                        break;
                    case XTemplateClassAttribute classAttribute:
                        if (IsTruthy(XTemplateExpressions.Evaluate(classAttribute.Expression, context), classAttribute.Offset)) attributes.AddClass(classAttribute.Name);
                        break;
                    case XTemplateShowAttribute showAttribute:
                        hasShow = true;
                        isHidden |= !IsTruthy(XTemplateExpressions.Evaluate(showAttribute.Expression, context), showAttribute.Offset);
                        break;
                }
            }
            if (hasShow) {
                if (isHidden) attributes.Set("hidden", null);
                else attributes.Remove("hidden");
            }
            return attributes.Items;
        }
        private static XTemplateSelectModel? ApplyModel(XTemplateElementNode element, XTemplateExpressionContext context, List<KeyValuePair<string, string?>> attributes) {
            if (element.Model == null) return null;
            var modelValue = XTemplateExpressions.Evaluate(element.Model, context);
            var values = new OrderedAttributes(attributes);
            if (element.Name == "select") {
                if (values.Contains("multiple")) throw new XTemplateException("x-model on select[multiple] is not supported by server rendering", element.Offset);
                return new XTemplateSelectModel(modelValue, element.Offset);
            }
            if (element.Name != "input") return null;
            var type = values.GetValue("type")?.ToLowerInvariant() ?? "text";
            if (type == "checkbox") { if (IsTruthy(modelValue, element.Offset)) values.Set("checked", null); else values.Remove("checked"); }
            else if (type == "radio") {
                var value = values.GetValue("value") ?? string.Empty;
                if (modelValue != null && ScalarString(modelValue, element.Offset) == value) values.Set("checked", null); else values.Remove("checked");
            } else if (modelValue == null) values.Remove("value");
            else values.Set("value", ScalarString(modelValue, element.Offset));
            attributes.Clear();
            attributes.AddRange(values.Items);
            return null;
        }
        private static void ApplyOptionSelection(List<KeyValuePair<string, string?>> attributes, XTemplateSelectModel selectModel, string textContent) {
            var values = new OrderedAttributes(attributes);
            var optionValue = values.Contains("value") ? values.GetValue("value") ?? string.Empty : textContent;
            if (selectModel.Value != null && ScalarString(selectModel.Value, selectModel.Offset) == optionValue) values.Set("selected", null);
            else values.Remove("selected");
            attributes.Clear();
            attributes.AddRange(values.Items);
        }
        private void AddSpread(OrderedAttributes attributes, object? value, int offset) {
            if (value == null) return;
            foreach (var member in ObjectMembers(value, offset)) {
                EnsureNotStyleAttribute(member.Key, offset);
                if (member.Value is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal) SetBoundAttribute(attributes, member.Key, member.Value, offset);
            }
        }
        private void SetBoundAttribute(OrderedAttributes attributes, string name, object? value, int offset) {
            if (!IsValidAttributeName(name)) throw new XTemplateException($"Invalid attribute name '{name}'", offset);
            EnsureNotStyleAttribute(name, offset);
            if (value == null || value is false) { attributes.Remove(name); return; }
            if (value is true) { attributes.Set(name, null); return; }
            if (value is string text) { attributes.Set(name, text); return; }
            if (IsNumeric(value)) { attributes.Set(name, ScalarString(NormalizeNumber(value, offset), offset)); return; }
            var keys = ObjectMembers(value, offset).Where(member => IsTruthy(member.Value, offset)).Select(member => member.Key);
            attributes.Set(name, string.Join(' ', keys));
        }
        private IEnumerable<object?> NormalizeCollection(object? value, int offset) {
            if (value == null || value is bool) throw new XTemplateException("x-for requires a collection, string, object, or non-negative integer number", offset);
            if (IsNumeric(value)) {
                var number = NormalizeNumber(value, offset);
                if (!double.IsFinite(number) || number < 0 || number != Math.Truncate(number)) throw new XTemplateException("x-for numeric sources must be finite non-negative integers", offset);
                if (number > int.MaxValue) throw new XTemplateException("x-for numeric source is too large", offset);
                return Enumerable.Range(1, (int)number).Cast<object?>();
            }
            if (value is string text) return text.EnumerateRunes().Select(rune => (object?)rune.ToString()).ToArray();
            if (mObjectAccess.CanAdapt(value)) return ObjectMembers(value, offset).Select(member => (object?)member.Key).ToArray();
            if (value is IEnumerable enumerable) return enumerable.Cast<object?>().ToArray();
            return ObjectMembers(value, offset).Select(member => (object?)member.Key).ToArray();
        }
        private IEnumerable<object?> NormalizeCollectionOrEmpty(object? value, int offset) => value == null ? Array.Empty<object?>() : NormalizeCollection(value, offset);
        private IEnumerable<KeyValuePair<string, object?>> ObjectMembers(object value, int offset) {
            try { return mObjectAccess.GetMembers(value).ToArray(); }
            catch (XTemplateObjectAccessException exception) { throw new XTemplateException(exception.Message, offset); }
            catch (Exception exception) { throw new XTemplateException($"Unable to enumerate exposed object members: {exception.Message}", offset); }
        }
        private static bool IsNumeric(object? value) => XTemplateValues.IsNumeric(value);
        private static double NormalizeNumber(object value, int offset) {
            try { return XTemplateValues.NormalizeNumber(value); }
            catch (XTemplateValueException exception) { throw new XTemplateException(exception.Message, offset); }
        }
        private static bool IsTruthy(object? value, int offset) {
            try { return XTemplateValues.IsTruthy(value); }
            catch (XTemplateValueException exception) { throw new XTemplateException(exception.Message, offset); }
        }
        private static string ScalarString(object? value, int offset) {
            try { return XTemplateValues.ToScalarString(value); }
            catch (XTemplateValueException exception) { throw new XTemplateException(exception.Message, offset); }
        }
        private static string HtmlText(string value) => value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
        private static string HtmlTextContent(string html) {
            var text = new StringBuilder();
            var inTag = false;
            char quote = '\0';
            foreach (var character in html) {
                if (!inTag) {
                    if (character == '<') inTag = true;
                    else text.Append(character);
                } else if (quote != '\0') {
                    if (character == quote) quote = '\0';
                } else if (character is '\'' or '"') quote = character;
                else if (character == '>') inTag = false;
            }
            return WebUtility.HtmlDecode(text.ToString());
        }
        private static string HtmlAttribute(string value) => HtmlText(value).Replace("\"", "&quot;", StringComparison.Ordinal).Replace("'", "&#39;", StringComparison.Ordinal);
        private static void EnsureNotStyleAttribute(string name, int offset) { if (string.Equals(name, "style", StringComparison.OrdinalIgnoreCase)) throw new XTemplateException("Inline style attributes are not supported by the server XTemplate renderer", offset); }
        private static bool IsValidAttributeName(string name) => XTemplateAttributeNames.IsValid(name);
    }


}
