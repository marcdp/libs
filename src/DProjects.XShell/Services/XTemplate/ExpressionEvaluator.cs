using System.Collections;
using System.Globalization;
using System.Reflection;

namespace DProjects.XShell.Services.XTemplate {

    internal sealed class ExpressionEvaluator {

        // vars
        private readonly XTemplateExpressionContext _context;

        // ctor
        public ExpressionEvaluator(XTemplateExpressionContext context) {
            _context = context;
        }

        // methods
        public object? Evaluate(XTemplateExpression expression) {
            return expression switch {
                LiteralExpression literal => literal.Value,
                IdentifierExpression identifier => ResolveIdentifier(identifier),
                MemberAccessExpression member => GetMember(Evaluate(member.Target), member.MemberName, member.Offset),
                IndexAccessExpression index => GetIndex(Evaluate(index.Target), Evaluate(index.Index), index.Offset),
                UnaryExpression unary => EvaluateUnary(unary),
                BinaryExpression binary => EvaluateBinary(binary),
                ConditionalExpression conditional => IsTruthy(Evaluate(conditional.Condition)) ? Evaluate(conditional.WhenTrue) : Evaluate(conditional.WhenFalse),
                _ => throw new XTemplateExpressionEvaluationException("Unsupported expression node", expression.Offset)
            };
        }

        // methods (private)
        private object? ResolveIdentifier(IdentifierExpression expression) {
            if (!_context.TryGetValue(expression.Name, out var value)) throw new XTemplateExpressionEvaluationException($"Unknown identifier '{expression.Name}'", expression.Offset);
            return Normalize(value, expression.Offset);
        }
        private object? EvaluateUnary(UnaryExpression expression) {
            var value = Evaluate(expression.Operand);
            return expression.Operator switch { "!" => !IsTruthy(value), "+" => Number(value, expression.Offset), "-" => CheckedNumber(-Number(value, expression.Offset), expression.Offset), _ => throw new XTemplateExpressionEvaluationException($"Unsupported unary operator '{expression.Operator}'", expression.Offset) };
        }
        private object? EvaluateBinary(BinaryExpression expression) {
            var left = Evaluate(expression.Left);
            if (expression.Operator == "&&") return IsTruthy(left) ? Evaluate(expression.Right) : left;
            if (expression.Operator == "||") return IsTruthy(left) ? left : Evaluate(expression.Right);
            if (expression.Operator == "??") return left ?? Evaluate(expression.Right);
            var right = Evaluate(expression.Right);
            return expression.Operator switch {
                "+" => Add(left, right, expression.Offset), "-" => Arithmetic(left, right, expression.Offset, (a, b) => a - b), "*" => Arithmetic(left, right, expression.Offset, (a, b) => a * b),
                "/" => Divide(left, right, expression.Offset), "%" => Modulo(left, right, expression.Offset), "==" => Equal(left, right), "!=" => !Equal(left, right),
                "<" => Compare(left, right, expression.Offset) < 0, "<=" => Compare(left, right, expression.Offset) <= 0, ">" => Compare(left, right, expression.Offset) > 0, ">=" => Compare(left, right, expression.Offset) >= 0,
                _ => throw new XTemplateExpressionEvaluationException($"Unsupported binary operator '{expression.Operator}'", expression.Offset)
            };
        }
        private static object Add(object? left, object? right, int offset) {
            if (left is double leftNumber && right is double rightNumber) return CheckedNumber(leftNumber + rightNumber, offset);
            if (left is string || right is string) return ScalarString(left, offset) + ScalarString(right, offset);
            throw new XTemplateExpressionEvaluationException("Operator '+' requires two numbers or a string operand", offset);
        }
        private static object Arithmetic(object? left, object? right, int offset, Func<double, double, double> operation) => CheckedNumber(operation(Number(left, offset), Number(right, offset)), offset);
        private static object Divide(object? left, object? right, int offset) { var divisor = Number(right, offset); if (divisor == 0) throw new XTemplateExpressionEvaluationException("Division by zero", offset); return CheckedNumber(Number(left, offset) / divisor, offset); }
        private static object Modulo(object? left, object? right, int offset) { var divisor = Number(right, offset); if (divisor == 0) throw new XTemplateExpressionEvaluationException("Modulo by zero", offset); return CheckedNumber(Number(left, offset) % divisor, offset); }
        private static int Compare(object? left, object? right, int offset) {
            if (left is double leftNumber && right is double rightNumber) return leftNumber.CompareTo(rightNumber);
            if (left is string leftString && right is string rightString) return string.CompareOrdinal(leftString, rightString);
            throw new XTemplateExpressionEvaluationException("Comparison requires two numbers or two strings", offset);
        }
        private static bool Equal(object? left, object? right) {
            if (left == null || right == null) return left == null && right == null;
            if (left.GetType() != right.GetType()) return false;
            return left is double or bool or string ? left.Equals(right) : ReferenceEquals(left, right);
        }
        private static object? GetMember(object? target, string memberName, int offset) {
            if (target == null) return null;
            if (target is string text) return memberName == "length" ? (double)text.Length : null;
            if (IsCollection(target)) return memberName == "length" ? (double)CollectionLength(target) : null;
            if (TryGetDictionaryValue(target, memberName, out var value)) return Normalize(value, offset);
            if (IsDictionary(target)) return null;
            var type = target.GetType();
            var property = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public);
            if (property?.CanRead == true && property.GetIndexParameters().Length == 0 && property.GetMethod?.IsPublic == true) return Normalize(property.GetValue(target), offset);
            var field = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public);
            return field == null ? null : Normalize(field.GetValue(target), offset);
        }
        private static object? GetIndex(object? target, object? index, int offset) {
            if (target == null) return null;
            if (index is string memberName) return GetMember(target, memberName, offset);
            if (index is not double number || number < 0 || number != Math.Truncate(number)) throw new XTemplateExpressionEvaluationException("A collection index must be a non-negative integer number", offset);
            if (!IsCollection(target)) throw new XTemplateExpressionEvaluationException("Indexed access requires an object string index or collection numeric index", offset);
            if (number > int.MaxValue) return null;
            var position = (int)number;
            if (target is string text) return position < text.Length ? text[position].ToString() : null;
            if (target is IList list) return position < list.Count ? Normalize(list[position], offset) : null;
            return ((IEnumerable)target).Cast<object?>().Skip(position).Select(value => Normalize(value, offset)).FirstOrDefault();
        }
        private static bool TryGetDictionaryValue(object target, string name, out object? value) {
            if (target is IDictionary dictionary && dictionary.Contains(name)) { value = dictionary[name]; return true; }
            value = null;
            return false;
        }
        private static bool IsCollection(object value) => value is IEnumerable && value is not string && !IsDictionary(value);
        private static bool IsDictionary(object value) => value is IDictionary;
        private static int CollectionLength(object collection) => collection is ICollection collectionValue ? collectionValue.Count : ((IEnumerable)collection).Cast<object?>().Count();
        private static object? Normalize(object? value, int offset) {
            if (value == null || value is bool or string or double) return value;
            if (value is byte or sbyte or short or ushort or int or uint or long or ulong or float or decimal) return CheckedNumber(Convert.ToDouble(value, CultureInfo.InvariantCulture), offset);
            if (value is char character) return character.ToString();
            return value;
        }
        private static double Number(object? value, int offset) => value is double number ? number : throw new XTemplateExpressionEvaluationException("Numeric operands are required", offset);
        private static double CheckedNumber(double value, int offset) => double.IsFinite(value) ? value : throw new XTemplateExpressionEvaluationException("Arithmetic result must be a finite number", offset);
        private static bool IsTruthy(object? value) => value switch { null => false, bool boolean => boolean, double number => number != 0, string text => text.Length != 0, _ => true };
        private static string ScalarString(object? value, int offset) => value switch { null => string.Empty, bool boolean => boolean ? "true" : "false", double number when number == 0 => "0", double number => number.ToString("R", CultureInfo.InvariantCulture), string text => text, _ => throw new XTemplateExpressionEvaluationException("Objects and collections cannot be converted to strings", offset) };
    }
}
