using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace DProjects.XShell.Services.XTemplate {

    public sealed class XTemplateException : Exception {

        // props
        public int Offset { get; }

        // ctor
        public XTemplateException(string message, int offset) : base($"{message} (at template offset {offset}).") {
            Offset = offset;
        }
    }

    public sealed class XTemplateRenderer {

        // consts
        private static readonly HashSet<string> VoidElements = new(StringComparer.OrdinalIgnoreCase) { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };

        // methods
        public string Render(string template, object? state) {
            var root = new XTemplateParser(template.Trim()).Parse();
            var context = new XTemplateExpressionContext(new Dictionary<string, object?> { ["state"] = state });
            var result = new StringBuilder();
            RenderChildren(root.Children, context, result);
            return result.ToString();
        }

        // methods (private)
        private void RenderChildren(IReadOnlyList<XTemplateNode> children, XTemplateExpressionContext context, StringBuilder result) {
            for (var index = 0; index < children.Count; index++) {
                if (children[index] is XTemplateElementNode element && element.If != null) {
                    var end = FindConditionalEnd(children, index);
                    var selected = FindSelectedConditionalBranch(children, index, end, context);
                    for (var branchIndex = index; branchIndex <= end; branchIndex++) {
                        var node = children[branchIndex];
                        if (node is XTemplateElementNode branch && (branch.If != null || branch.ElseIf != null || branch.IsElse)) {
                            if (ReferenceEquals(branch, selected)) RenderElement(branch, context, result);
                        } else RenderNode(node, context, result);
                    }
                    index = end;
                    continue;
                }
                if (children[index] is XTemplateElementNode { ElseIf: not null } or XTemplateElementNode { IsElse: true }) throw new XTemplateException("Conditional branch has no preceding x-if", children[index].Offset);
                RenderNode(children[index], context, result);
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
                if (condition != null && IsTruthy(XTemplateExpressions.Evaluate(condition, context))) return element;
            }
            return null;
        }
        private void RenderNode(XTemplateNode node, XTemplateExpressionContext context, StringBuilder result) {
            switch (node) {
                case XTemplateTextNode text: result.Append(HtmlText(text.Text)); break;
                case XTemplateInterpolationNode interpolation: result.Append(HtmlText(ScalarString(XTemplateExpressions.Evaluate(interpolation.Expression, context), interpolation.Offset))); break;
                case XTemplateCommentNode comment: result.Append("<!--").Append(comment.Text).Append("-->"); break;
                case XTemplateRawHtmlNode raw: result.Append(raw.Html); break;
                case XTemplateElementNode element: RenderElement(element, context, result); break;
                default: throw new XTemplateException("Unsupported template node", node.Offset);
            }
        }
        private void RenderElement(XTemplateElementNode element, XTemplateExpressionContext context, StringBuilder result) {
            if (element.ChildrenExpression != null) throw new XTemplateException("x-children is not supported by server rendering because it requires browser DOM nodes", element.Offset);
            if (element.Recursive != null) {
                RenderRecursiveItems(element, NormalizeCollection(XTemplateExpressions.Evaluate(element.Recursive.Collection, context), element.Recursive.Offset), context, 0, 0, result);
                return;
            }
            if (element.For != null) {
                var collection = XTemplateExpressions.Evaluate(element.For.Collection, context);
                var itemIndex = 0;
                foreach (var item in NormalizeCollection(collection, element.For.Offset)) {
                    var locals = new Dictionary<string, object?> { [element.For.ItemName] = item, [element.For.IndexName] = itemIndex };
                    RenderElementOnce(element, context.With(locals), result, null);
                    itemIndex++;
                }
                return;
            }
            RenderElementOnce(element, context, result, null);
        }
        private int RenderRecursiveItems(XTemplateElementNode element, IEnumerable<object?> items, XTemplateExpressionContext context, int indexAbsolute, int indent, StringBuilder result) {
            var definition = element.Recursive!;
            var siblingIndex = 0;
            foreach (var item in items) {
                var currentAbsolute = indexAbsolute++;
                var locals = new Dictionary<string, object?> { [definition.ItemName] = item, [definition.IndexName] = siblingIndex, [definition.AbsoluteIndexName] = currentAbsolute, ["indent"] = indent };
                RenderElementOnce(element, context.With(locals), result, () => {
                    var childItems = NormalizeCollectionOrEmpty(XTemplateExpressions.Evaluate(definition.Children, context.With(locals)), definition.Offset);
                    var descendants = new StringBuilder();
                    indexAbsolute = RenderRecursiveItems(element, childItems, context.With(locals), indexAbsolute, indent + 1, descendants);
                    if (descendants.Length == 0) return;
                    if (definition.WrapperName == null) result.Append(descendants);
                    else result.Append('<').Append(definition.WrapperName).Append('>').Append(descendants).Append("</").Append(definition.WrapperName).Append('>');
                });
                siblingIndex++;
            }
            return indexAbsolute;
        }
        private void RenderElementOnce(XTemplateElementNode element, XTemplateExpressionContext context, StringBuilder result, Action? renderRecursiveChildren) {
            var attributes = BuildAttributes(element, context);
            ApplyModel(element, context, attributes);
            result.Append('<').Append(element.Name);
            foreach (var attribute in attributes) {
                result.Append(' ').Append(attribute.Key);
                if (attribute.Value != null) result.Append("=\"").Append(HtmlAttribute(attribute.Value)).Append('"');
            }
            result.Append('>');
            if (VoidElements.Contains(element.Name)) return;
            if (element.Text != null) result.Append(HtmlText(ScalarString(XTemplateExpressions.Evaluate(element.Text, context), element.Offset)));
            else if (element.Html != null) result.Append(ScalarString(XTemplateExpressions.Evaluate(element.Html, context), element.Offset));
            else RenderChildren(element.Children, context, result);
            renderRecursiveChildren?.Invoke();
            result.Append("</").Append(element.Name).Append('>');
        }
        private static List<KeyValuePair<string, string?>> BuildAttributes(XTemplateElementNode element, XTemplateExpressionContext context) {
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
                        if (IsTruthy(XTemplateExpressions.Evaluate(classAttribute.Expression, context))) attributes.AddClass(classAttribute.Name);
                        break;
                    case XTemplateShowAttribute showAttribute:
                        hasShow = true;
                        isHidden |= !IsTruthy(XTemplateExpressions.Evaluate(showAttribute.Expression, context));
                        break;
                }
            }
            if (hasShow) {
                if (isHidden) attributes.Set("hidden", null);
                else attributes.Remove("hidden");
            }
            return attributes.Items;
        }
        private static void ApplyModel(XTemplateElementNode element, XTemplateExpressionContext context, List<KeyValuePair<string, string?>> attributes) {
            if (element.Model == null) return;
            var modelValue = XTemplateExpressions.Evaluate(element.Model, context);
            var values = new OrderedAttributes(attributes);
            if (element.Name == "select") throw new XTemplateException("x-model on select is not supported by server rendering because selected-option semantics are not specified", element.Offset);
            if (element.Name != "input") return;
            var type = values.GetValue("type")?.ToLowerInvariant() ?? "text";
            if (type == "checkbox") { if (IsTruthy(modelValue)) values.Set("checked", null); else values.Remove("checked"); }
            else if (type == "radio") {
                var value = values.GetValue("value") ?? string.Empty;
                if (modelValue != null && ScalarString(modelValue, element.Offset) == value) values.Set("checked", null); else values.Remove("checked");
            } else if (modelValue == null) values.Remove("value");
            else values.Set("value", ScalarString(modelValue, element.Offset));
            attributes.Clear();
            attributes.AddRange(values.Items);
        }
        private static void AddSpread(OrderedAttributes attributes, object? value, int offset) {
            if (value == null) return;
            foreach (var member in ObjectMembers(value, offset)) {
                if (member.Value is string or bool or byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal) SetBoundAttribute(attributes, member.Key, member.Value, offset);
            }
        }
        private static void SetBoundAttribute(OrderedAttributes attributes, string name, object? value, int offset) {
            if (!IsValidAttributeName(name)) throw new XTemplateException($"Invalid attribute name '{name}'", offset);
            if (value == null || value is false) { attributes.Remove(name); return; }
            if (value is true) { attributes.Set(name, null); return; }
            if (value is string text) { attributes.Set(name, text); return; }
            if (IsNumeric(value)) { attributes.Set(name, ScalarString(NormalizeNumber(value), offset)); return; }
            var keys = ObjectMembers(value, offset).Where(member => IsTruthy(member.Value)).Select(member => member.Key);
            attributes.Set(name, string.Join(' ', keys));
        }
        private static IEnumerable<object?> NormalizeCollection(object? value, int offset) {
            if (value == null || value is bool) throw new XTemplateException("x-for requires a collection, string, object, or non-negative integer number", offset);
            if (IsNumeric(value)) {
                var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (!double.IsFinite(number) || number < 0 || number != Math.Truncate(number)) throw new XTemplateException("x-for numeric sources must be finite non-negative integers", offset);
                if (number > int.MaxValue) throw new XTemplateException("x-for numeric source is too large", offset);
                return Enumerable.Range(1, (int)number).Cast<object?>();
            }
            if (value is string text) return text.EnumerateRunes().Select(rune => (object?)rune.ToString()).ToArray();
            if (value is IDictionary dictionary) return dictionary.Keys.Cast<object?>().Select(key => key?.ToString()).ToArray();
            if (value is IEnumerable enumerable) return enumerable.Cast<object?>().ToArray();
            return ObjectMembers(value, offset).Select(member => (object?)member.Key).ToArray();
        }
        private static IEnumerable<object?> NormalizeCollectionOrEmpty(object? value, int offset) => value == null ? Array.Empty<object?>() : NormalizeCollection(value, offset);
        private static IEnumerable<KeyValuePair<string, object?>> ObjectMembers(object value, int offset) {
            if (value is IDictionary dictionary) {
                foreach (DictionaryEntry entry in dictionary) if (entry.Key is string key) yield return new(key, entry.Value);
                yield break;
            }
            var type = value.GetType();
            foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(property => property.CanRead && property.GetIndexParameters().Length == 0 && property.GetMethod?.IsPublic == true).OrderBy(property => property.Name, StringComparer.Ordinal)) {
                object? memberValue;
                try { memberValue = property.GetValue(value); } catch (Exception exception) { throw new XTemplateException($"Unable to read object member '{property.Name}': {exception.Message}", offset); }
                yield return new(property.Name, memberValue);
            }
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public).OrderBy(field => field.Name, StringComparer.Ordinal)) yield return new(field.Name, field.GetValue(value));
        }
        private static bool IsNumeric(object value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
        private static double NormalizeNumber(object value) => Convert.ToDouble(value, CultureInfo.InvariantCulture);
        private static bool IsTruthy(object? value) => value switch { null => false, bool boolean => boolean, string text => text.Length != 0, double number => number != 0, float number => number != 0, byte number => number != 0, sbyte number => number != 0, short number => number != 0, ushort number => number != 0, int number => number != 0, uint number => number != 0, long number => number != 0, ulong number => number != 0, decimal number => number != 0, _ => true };
        private static string ScalarString(object? value, int offset) => value switch { null => string.Empty, bool boolean => boolean ? "true" : "false", string text => text, _ when IsNumeric(value) => NormalizeNumber(value).ToString("R", CultureInfo.InvariantCulture), _ => throw new XTemplateException("Objects and collections cannot be converted to text", offset) };
        private static string HtmlText(string value) => value.Replace("&", "&amp;", StringComparison.Ordinal).Replace("<", "&lt;", StringComparison.Ordinal).Replace(">", "&gt;", StringComparison.Ordinal);
        private static string HtmlAttribute(string value) => HtmlText(value).Replace("\"", "&quot;", StringComparison.Ordinal).Replace("'", "&#39;", StringComparison.Ordinal);
        private static bool IsValidAttributeName(string name) => name.Length > 0 && name.All(character => !char.IsWhiteSpace(character) && character is not '"' and not '\'' and not '<' and not '>' and not '=' and not '/' and not '\0');
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
        public string? GetValue(string name) => _items.FirstOrDefault(item => string.Equals(item.Key, name, StringComparison.OrdinalIgnoreCase)).Value;
    }
}
