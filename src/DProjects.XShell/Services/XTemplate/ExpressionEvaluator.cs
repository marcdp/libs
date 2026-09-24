using System.Collections;

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
                ConditionalExpression conditional => XTemplateValues.IsTruthy(Evaluate(conditional.Condition)) ? Evaluate(conditional.WhenTrue) : Evaluate(conditional.WhenFalse),
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
            return expression.Operator switch { "!" => !XTemplateValues.IsTruthy(value), "+" => Number(value, expression.Offset), "-" => CheckedNumber(-Number(value, expression.Offset), expression.Offset), _ => throw new XTemplateExpressionEvaluationException($"Unsupported unary operator '{expression.Operator}'", expression.Offset) };
        }
        private object? EvaluateBinary(BinaryExpression expression) {
            var left = Evaluate(expression.Left);
            if (expression.Operator == "&&") return XTemplateValues.IsTruthy(left) ? Evaluate(expression.Right) : left;
            if (expression.Operator == "||") return XTemplateValues.IsTruthy(left) ? left : Evaluate(expression.Right);
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
        private object? GetMember(object? target, string memberName, int offset) {
            if (target == null) return null;
            if (target is string text) return memberName == "length" ? (double)text.Length : null;
            if (IsCollection(target)) return memberName == "length" ? (double)CollectionLength(target) : null;
            try { return _context.ObjectAccess.TryGetMember(target, memberName, out var value) ? Normalize(value, offset) : null; }
            catch (XTemplateObjectAccessException exception) { throw new XTemplateExpressionEvaluationException(exception.Message, offset); }
        }
        private object? GetIndex(object? target, object? index, int offset) {
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
        private static bool IsCollection(object value) => value is IEnumerable && value is not string && !IsDictionary(value);
        private static bool IsDictionary(object value) => value is IDictionary;
        private static int CollectionLength(object collection) => collection is ICollection collectionValue ? collectionValue.Count : ((IEnumerable)collection).Cast<object?>().Count();
        private static object? Normalize(object? value, int offset) {
            if (value == null || value is bool or string) return value;
            if (XTemplateValues.IsNumeric(value)) {
                try { return XTemplateValues.NormalizeNumber(value); }
                catch (XTemplateValueException exception) { throw new XTemplateExpressionEvaluationException(exception.Message, offset); }
            }
            if (value is char character) return character.ToString();
            return value;
        }
        private static double Number(object? value, int offset) {
            try { return value is double number ? XTemplateValues.NormalizeNumber(number) : XTemplateValues.ToNumber(value); }
            catch (XTemplateValueException exception) { throw new XTemplateExpressionEvaluationException(exception.Message, offset); }
        }
        private static double CheckedNumber(double value, int offset) => double.IsFinite(value) ? value : throw new XTemplateExpressionEvaluationException("Arithmetic result must be a finite number", offset);
        private static string ScalarString(object? value, int offset) {
            try { return XTemplateValues.ToScalarString(value); }
            catch (XTemplateValueException exception) { throw new XTemplateExpressionEvaluationException(exception.Message, offset); }
        }
    }
}
