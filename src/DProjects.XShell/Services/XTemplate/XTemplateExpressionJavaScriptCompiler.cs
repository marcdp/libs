using System.Globalization;
using System.Text.Json;

namespace DProjects.XShell.Services.XTemplate {

    internal sealed class XTemplateExpressionJavaScriptCompiler {

        // methods
        public string Compile(XTemplateExpression expression, XTemplateExpressionJavaScriptScope scope) {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (scope == null) throw new ArgumentNullException(nameof(scope));
            return CompileExpression(expression, scope);
        }

        public string CompileAssignment(XTemplateExpression expression, string valueExpression, XTemplateExpressionJavaScriptScope scope) {
            if (!XTemplateExpressions.IsAssignable(expression)) throw new XTemplateExpressionSyntaxException("x-model requires an assignable expression", expression.Offset);
            var (root, segments) = GetAssignmentPath(expression);
            var path = string.Join(", ", segments.Select(segment => segment switch {
                MemberAccessExpression member => $"{{kind:\"member\", name:{ToJavaScriptString(member.MemberName)}}}",
                IndexAccessExpression index => $"{{kind:\"index\", value:() => {CompileExpression(index.Index, scope)}}}",
                _ => throw new XTemplateExpressionSyntaxException("x-model requires an assignable expression", segment.Offset)
            }));
            return $"utils.expr.assign({CompileIdentifier(root, scope)}, [{path}], {valueExpression})";
        }

        // methods (private)
        private string CompileExpression(XTemplateExpression expression, XTemplateExpressionJavaScriptScope scope) {
            return expression switch {
                LiteralExpression literal => CompileLiteral(literal),
                IdentifierExpression identifier => CompileIdentifier(identifier, scope),
                MemberAccessExpression member => $"utils.expr.member({CompileExpression(member.Target, scope)}, {ToJavaScriptString(member.MemberName)})",
                IndexAccessExpression index => $"utils.expr.index({CompileExpression(index.Target, scope)}, {CompileExpression(index.Index, scope)})",
                UnaryExpression unary => CompileUnary(unary, scope),
                BinaryExpression binary => CompileBinary(binary, scope),
                ConditionalExpression conditional => $"utils.expr.conditional(() => {CompileExpression(conditional.Condition, scope)}, () => {CompileExpression(conditional.WhenTrue, scope)}, () => {CompileExpression(conditional.WhenFalse, scope)})",
                FormatExpression format => CompileFormat(format, scope),
                _ => throw new XTemplateExpressionSyntaxException("Unsupported XTemplate expression node", expression.Offset)
            };
        }

        private static string CompileLiteral(LiteralExpression expression) {
            return expression.Value switch {
                null => "null",
                true => "true",
                false => "false",
                double number when double.IsFinite(number) => number.ToString("R", CultureInfo.InvariantCulture),
                string text => ToJavaScriptString(text),
                _ => throw new XTemplateExpressionSyntaxException("Unsupported XTemplate literal", expression.Offset)
            };
        }

        private static string CompileIdentifier(IdentifierExpression expression, XTemplateExpressionJavaScriptScope scope) {
            if (!scope.Contains(expression.Name)) throw new XTemplateExpressionSyntaxException($"Unknown XTemplate identifier '{expression.Name}'", expression.Offset);
            return expression.Name;
        }

        private string CompileUnary(UnaryExpression expression, XTemplateExpressionJavaScriptScope scope) {
            var operand = CompileExpression(expression.Operand, scope);
            return expression.Operator switch {
                "!" => $"utils.expr.not({operand})",
                "+" => $"utils.expr.unaryPlus({operand})",
                "-" => $"utils.expr.unaryMinus({operand})",
                _ => throw new XTemplateExpressionSyntaxException($"Unsupported unary operator '{expression.Operator}'", expression.Offset)
            };
        }

        private string CompileBinary(BinaryExpression expression, XTemplateExpressionJavaScriptScope scope) {
            if (expression.Operator is "&&" or "||" or "??") {
                var shortCircuitMethod = expression.Operator switch { "&&" => "and", "||" => "or", _ => "coalesce" };
                return $"utils.expr.{shortCircuitMethod}(() => {CompileExpression(expression.Left, scope)}, () => {CompileExpression(expression.Right, scope)})";
            }
            var method = expression.Operator switch {
                "+" => "add", "-" => "subtract", "*" => "multiply", "/" => "divide", "%" => "modulo",
                "==" => "equal", "!=" => "notEqual", "<" => "less", "<=" => "lessOrEqual", ">" => "greater", ">=" => "greaterOrEqual",
                _ => throw new XTemplateExpressionSyntaxException($"Unsupported binary operator '{expression.Operator}'", expression.Offset)
            };
            return $"utils.expr.{method}({CompileExpression(expression.Left, scope)}, {CompileExpression(expression.Right, scope)})";
        }

        private string CompileFormat(FormatExpression expression, XTemplateExpressionJavaScriptScope scope) {
            var value = CompileExpression(expression.Source, scope);
            foreach (var formatter in expression.Formatters) {
                var arguments = string.Join(", ", formatter.Arguments.Select(argument => CompileExpression(argument, scope)));
                value = $"utils.expr.format({value}, {ToJavaScriptString(formatter.Name)}, () => [{arguments}], i18n)";
            }
            return value;
        }

        private static (IdentifierExpression Root, IReadOnlyList<XTemplateExpression> Segments) GetAssignmentPath(XTemplateExpression expression) {
            var segments = new List<XTemplateExpression>();
            while (expression is MemberAccessExpression or IndexAccessExpression) {
                switch (expression) {
                    case MemberAccessExpression member:
                        segments.Add(member);
                        expression = member.Target;
                        break;
                    case IndexAccessExpression index:
                        segments.Add(index);
                        expression = index.Target;
                        break;
                }
            }
            segments.Reverse();
            if (expression is not IdentifierExpression root) throw new XTemplateExpressionSyntaxException("x-model requires an assignable expression", expression.Offset);
            return (root, segments);
        }

        private static string ToJavaScriptString(string value) => JsonSerializer.Serialize(value);
    }

    internal sealed class XTemplateExpressionJavaScriptScope {

        // vars
        private readonly HashSet<string> _identifiers;

        // ctor
        public XTemplateExpressionJavaScriptScope(IEnumerable<string> identifiers) {
            _identifiers = new HashSet<string>(identifiers ?? throw new ArgumentNullException(nameof(identifiers)), StringComparer.Ordinal);
        }

        // methods
        public bool Contains(string name) => _identifiers.Contains(name);
        public XTemplateExpressionJavaScriptScope With(params string[] identifiers) => new(_identifiers.Concat(identifiers));
    }
}
