using System.Collections;
using System.Globalization;

namespace DProjects.XShell.Services.XTemplate {

    public abstract record XTemplateExpression(int Offset);
    public sealed record LiteralExpression(object? Value, int Offset) : XTemplateExpression(Offset);
    public sealed record IdentifierExpression(string Name, int Offset) : XTemplateExpression(Offset);
    public sealed record MemberAccessExpression(XTemplateExpression Target, string MemberName, int Offset) : XTemplateExpression(Offset);
    public sealed record IndexAccessExpression(XTemplateExpression Target, XTemplateExpression Index, int Offset) : XTemplateExpression(Offset);
    public sealed record UnaryExpression(string Operator, XTemplateExpression Operand, int Offset) : XTemplateExpression(Offset);
    public sealed record BinaryExpression(string Operator, XTemplateExpression Left, XTemplateExpression Right, int Offset) : XTemplateExpression(Offset);
    public sealed record ConditionalExpression(XTemplateExpression Condition, XTemplateExpression WhenTrue, XTemplateExpression WhenFalse, int Offset) : XTemplateExpression(Offset);
    public sealed record FormatterStage(string Name, IReadOnlyList<XTemplateExpression> Arguments, int Offset);
    public sealed record FormatExpression(XTemplateExpression Source, IReadOnlyList<FormatterStage> Formatters, int Offset) : XTemplateExpression(Offset);

    public abstract class XTemplateExpressionException : Exception {

        // props
        public int Offset { get; }

        // ctor
        protected XTemplateExpressionException(string message, int offset) : base($"{message} (at offset {offset}).") {
            Offset = offset;
        }
    }

    public sealed class XTemplateExpressionSyntaxException : XTemplateExpressionException {

        // ctor
        public XTemplateExpressionSyntaxException(string message, int offset) : base(message, offset) { }
    }

    public sealed class XTemplateExpressionEvaluationException : XTemplateExpressionException {

        // ctor
        public XTemplateExpressionEvaluationException(string message, int offset) : base(message, offset) { }
    }

    public sealed class XTemplateExpressionContext {

        // vars
        private readonly IReadOnlyDictionary<string, object?> _identifiers;
        private readonly XTemplateObjectAccess _objectAccess;

        // props
        public string? Locale { get; }

        // ctor
        public XTemplateExpressionContext(IReadOnlyDictionary<string, object?> identifiers, IEnumerable<IXTemplateObjectAdapter>? objectAdapters = null, string? locale = null) {
            if (identifiers == null) throw new ArgumentNullException(nameof(identifiers));
            _identifiers = new Dictionary<string, object?>(identifiers, StringComparer.Ordinal);
            _objectAccess = new XTemplateObjectAccess(objectAdapters);
            Locale = locale;
        }

        // methods
        public bool TryGetValue(string name, out object? value) => _identifiers.TryGetValue(name, out value);
        internal XTemplateObjectAccess ObjectAccess => _objectAccess;
        public XTemplateExpressionContext With(IReadOnlyDictionary<string, object?> identifiers) {
            if (identifiers == null) throw new ArgumentNullException(nameof(identifiers));
            var values = new Dictionary<string, object?>(_identifiers, StringComparer.Ordinal);
            foreach (var identifier in identifiers) values[identifier.Key] = identifier.Value;
            return new XTemplateExpressionContext(values, _objectAccess.Adapters, Locale);
        }
    }

    public static class XTemplateExpressions {

        // methods
        public static XTemplateExpression Parse(string source) => new ExpressionParser(source).Parse();

        public static object? Evaluate(string source, XTemplateExpressionContext context) => Evaluate(Parse(source), context);

        public static object? Evaluate(XTemplateExpression expression, XTemplateExpressionContext context) {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new ExpressionEvaluator(context).Evaluate(expression);
        }

        public static bool IsAssignable(XTemplateExpression expression) {
            if (expression is not MemberAccessExpression and not IndexAccessExpression) return false;
            while (expression is MemberAccessExpression or IndexAccessExpression) {
                expression = expression switch { MemberAccessExpression member => member.Target, IndexAccessExpression index => index.Target, _ => expression };
            }
            return expression is IdentifierExpression;
        }
    }

    internal enum ExpressionTokenKind { End, Identifier, Number, String, Null, True, False, Dot, OpenBracket, CloseBracket, OpenParenthesis, CloseParenthesis, Question, Colon, Comma, Pipe, Plus, Minus, Star, Slash, Percent, Bang, Less, LessOrEqual, Greater, GreaterOrEqual, EqualEqual, BangEqual, AndAnd, OrOr, QuestionQuestion }
    internal readonly record struct ExpressionToken(ExpressionTokenKind Kind, string Text, object? Value, int Offset);

    internal sealed class ExpressionTokenizer {

        // vars
        private readonly string _source;
        private int _position;

        // ctor
        public ExpressionTokenizer(string source) {
            _source = source ?? throw new ArgumentNullException(nameof(source));
        }

        // methods
        public ExpressionToken Next() {
            while (_position < _source.Length && _source[_position] is ' ' or '\t' or '\r' or '\n') _position++;
            if (_position == _source.Length) return new(ExpressionTokenKind.End, string.Empty, null, _position);
            var offset = _position;
            var character = _source[_position++];
            if (IsIdentifierStart(character)) return ReadIdentifier(offset);
            if (character is >= '0' and <= '9') return ReadNumber(offset);
            if (character is '\'' or '"') return ReadString(character, offset);
            return character switch {
                '.' => new(ExpressionTokenKind.Dot, ".", null, offset), '[' => new(ExpressionTokenKind.OpenBracket, "[", null, offset), ']' => new(ExpressionTokenKind.CloseBracket, "]", null, offset),
                '(' => new(ExpressionTokenKind.OpenParenthesis, "(", null, offset), ')' => new(ExpressionTokenKind.CloseParenthesis, ")", null, offset), ',' => new(ExpressionTokenKind.Comma, ",", null, offset), '|' when Match('|') => new(ExpressionTokenKind.OrOr, "||", null, offset), '|' => new(ExpressionTokenKind.Pipe, "|", null, offset), '?' when Match('?') => new(ExpressionTokenKind.QuestionQuestion, "??", null, offset),
                '?' => new(ExpressionTokenKind.Question, "?", null, offset), ':' => new(ExpressionTokenKind.Colon, ":", null, offset), '+' when Match('+') => Error("Update operator '++' is not valid in XTemplate expressions", offset),
                '+' => new(ExpressionTokenKind.Plus, "+", null, offset), '-' when Match('-') => Error("Update operator '--' is not valid in XTemplate expressions", offset), '-' => new(ExpressionTokenKind.Minus, "-", null, offset),
                '*' => new(ExpressionTokenKind.Star, "*", null, offset), '/' => new(ExpressionTokenKind.Slash, "/", null, offset), '%' => new(ExpressionTokenKind.Percent, "%", null, offset), '!' when Match('=') => new(ExpressionTokenKind.BangEqual, "!=", null, offset),
                '!' => new(ExpressionTokenKind.Bang, "!", null, offset), '<' when Match('=') => new(ExpressionTokenKind.LessOrEqual, "<=", null, offset), '<' => new(ExpressionTokenKind.Less, "<", null, offset),
                '>' when Match('=') => new(ExpressionTokenKind.GreaterOrEqual, ">=", null, offset), '>' => new(ExpressionTokenKind.Greater, ">", null, offset), '=' when Match('=') => new(ExpressionTokenKind.EqualEqual, "==", null, offset),
                '=' => Error("Assignment operator '=' is not valid in XTemplate expressions", offset), '&' when Match('&') => new(ExpressionTokenKind.AndAnd, "&&", null, offset), '&' => Error("Unexpected '&'", offset),
                _ => Error($"Unexpected character '{character}'", offset)
            };
        }

        // methods (private)
        private ExpressionToken ReadIdentifier(int offset) {
            while (_position < _source.Length && IsIdentifierPart(_source[_position])) _position++;
            var text = _source[offset.._position];
            return text switch { "null" => new(ExpressionTokenKind.Null, text, null, offset), "true" => new(ExpressionTokenKind.True, text, true, offset), "false" => new(ExpressionTokenKind.False, text, false, offset), _ => new(ExpressionTokenKind.Identifier, text, null, offset) };
        }
        private ExpressionToken ReadNumber(int offset) {
            while (_position < _source.Length && char.IsAsciiDigit(_source[_position])) _position++;
            if (_position < _source.Length && _source[_position] == '.') {
                var decimalOffset = _position++;
                if (_position == _source.Length || !char.IsAsciiDigit(_source[_position])) throw new XTemplateExpressionSyntaxException("A decimal point must be followed by digits", decimalOffset);
                while (_position < _source.Length && char.IsAsciiDigit(_source[_position])) _position++;
            }
            var text = _source[offset.._position];
            var value = double.Parse(text, CultureInfo.InvariantCulture);
            if (!double.IsFinite(value)) throw new XTemplateExpressionSyntaxException("Numeric literals must be finite binary64 values", offset);
            return new(ExpressionTokenKind.Number, text, value, offset);
        }
        private ExpressionToken ReadString(char quote, int offset) {
            var result = new System.Text.StringBuilder();
            while (_position < _source.Length) {
                var character = _source[_position++];
                if (character == quote) return new(ExpressionTokenKind.String, _source[offset.._position], result.ToString(), offset);
                if (character is '\r' or '\n') throw new XTemplateExpressionSyntaxException("A string cannot contain a raw line break", _position - 1);
                if (character != '\\') {
                    if (char.IsLowSurrogate(character)) throw new XTemplateExpressionSyntaxException("String literals must not contain unpaired surrogates", _position - 1);
                    if (char.IsHighSurrogate(character)) {
                        if (_position == _source.Length || !char.IsLowSurrogate(_source[_position])) throw new XTemplateExpressionSyntaxException("String literals must not contain unpaired surrogates", _position - 1);
                        result.Append(character);
                        result.Append(_source[_position++]);
                        continue;
                    }
                    result.Append(character);
                    continue;
                }
                if (_position == _source.Length) throw new XTemplateExpressionSyntaxException("Incomplete string escape", _position - 1);
                character = _source[_position++];
                if (character == 'u') { result.Append(ReadUnicodeEscape()); continue; }
                result.Append(character switch { '\\' => '\\', '\'' => '\'', '"' => '"', 'n' => '\n', 'r' => '\r', 't' => '\t', 'b' => '\b', 'f' => '\f', _ => throw new XTemplateExpressionSyntaxException($"Unknown escape '\\{character}'", _position - 1) });
            }
            throw new XTemplateExpressionSyntaxException("Unterminated string", offset);
        }
        private string ReadUnicodeEscape() {
            var offset = _position - 2;
            if (_position + 4 > _source.Length || !ushort.TryParse(_source.AsSpan(_position, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var code)) throw new XTemplateExpressionSyntaxException("Malformed Unicode escape", offset);
            _position += 4;
            var character = (char)code;
            if (!char.IsHighSurrogate(character)) {
                if (char.IsLowSurrogate(character)) throw new XTemplateExpressionSyntaxException("Unicode escapes must not contain unpaired surrogates", offset);
                return character.ToString();
            }
            if (_position + 6 > _source.Length || _source[_position] != '\\' || _source[_position + 1] != 'u' || !ushort.TryParse(_source.AsSpan(_position + 2, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var lowCode) || !char.IsLowSurrogate((char)lowCode)) throw new XTemplateExpressionSyntaxException("Unicode escapes must not contain unpaired surrogates", offset);
            _position += 6;
            return new string(new[] { character, (char)lowCode });
        }
        private bool Match(char character) { if (_position < _source.Length && _source[_position] == character) { _position++; return true; } return false; }
        private static bool IsIdentifierStart(char character) => character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or '_';
        private static bool IsIdentifierPart(char character) => IsIdentifierStart(character) || character is >= '0' and <= '9';
        private static ExpressionToken Error(string message, int offset) => throw new XTemplateExpressionSyntaxException(message, offset);
    }

    internal sealed class ExpressionParser {

        // vars
        private readonly ExpressionTokenizer _tokenizer;
        private ExpressionToken _current;

        // ctor
        public ExpressionParser(string source) {
            _tokenizer = new ExpressionTokenizer(source);
            _current = _tokenizer.Next();
        }

        // methods
        public XTemplateExpression Parse() {
            if (_current.Kind == ExpressionTokenKind.End) throw Error("Expected an expression");
            var expression = ParsePipeline();
            if (_current.Kind != ExpressionTokenKind.End) throw Error($"Unexpected token '{_current.Text}'");
            return expression;
        }

        // methods (private)
        private XTemplateExpression ParseConditional() {
            var condition = ParseCoalesce();
            if (!Take(ExpressionTokenKind.Question)) return condition;
            var offset = _current.Offset - 1;
            var whenTrue = ParseConditional();
            Require(ExpressionTokenKind.Colon, "Expected ':' in conditional expression");
            return new ConditionalExpression(condition, whenTrue, ParseConditional(), offset);
        }
        private XTemplateExpression ParsePipeline() {
            var expression = ParseConditional();
            if (!Take(ExpressionTokenKind.Pipe)) return expression;
            var formatters = new List<FormatterStage>();
            do {
                var name = _current;
                Require(ExpressionTokenKind.Identifier, "Expected a formatter identifier after '|'");
                var arguments = new List<XTemplateExpression>();
                if (Take(ExpressionTokenKind.OpenParenthesis) && !Take(ExpressionTokenKind.CloseParenthesis)) {
                    do { arguments.Add(ParsePipeline()); } while (Take(ExpressionTokenKind.Comma));
                    Require(ExpressionTokenKind.CloseParenthesis, "Missing closing ')' in formatter arguments");
                }
                formatters.Add(new FormatterStage(name.Text, arguments, name.Offset));
            } while (Take(ExpressionTokenKind.Pipe));
            return new FormatExpression(expression, formatters, formatters[0].Offset);
        }
        private XTemplateExpression ParseCoalesce() => ParseBinary(ParseLogicalOr, ExpressionTokenKind.QuestionQuestion);
        private XTemplateExpression ParseLogicalOr() => ParseBinary(ParseLogicalAnd, ExpressionTokenKind.OrOr);
        private XTemplateExpression ParseLogicalAnd() => ParseBinary(ParseEquality, ExpressionTokenKind.AndAnd);
        private XTemplateExpression ParseEquality() => ParseBinary(ParseComparison, ExpressionTokenKind.EqualEqual, ExpressionTokenKind.BangEqual);
        private XTemplateExpression ParseComparison() => ParseBinary(ParseAdditive, ExpressionTokenKind.Less, ExpressionTokenKind.LessOrEqual, ExpressionTokenKind.Greater, ExpressionTokenKind.GreaterOrEqual);
        private XTemplateExpression ParseAdditive() => ParseBinary(ParseMultiplicative, ExpressionTokenKind.Plus, ExpressionTokenKind.Minus);
        private XTemplateExpression ParseMultiplicative() => ParseBinary(ParseUnary, ExpressionTokenKind.Star, ExpressionTokenKind.Slash, ExpressionTokenKind.Percent);
        private XTemplateExpression ParseBinary(Func<XTemplateExpression> parseOperand, params ExpressionTokenKind[] kinds) {
            var expression = parseOperand();
            while (kinds.Contains(_current.Kind)) { var token = _current; Next(); expression = new BinaryExpression(token.Text, expression, parseOperand(), token.Offset); }
            return expression;
        }
        private XTemplateExpression ParseUnary() {
            if (_current.Kind is ExpressionTokenKind.Bang or ExpressionTokenKind.Plus or ExpressionTokenKind.Minus) { var token = _current; Next(); return new UnaryExpression(token.Text, ParseUnary(), token.Offset); }
            return ParseMember();
        }
        private XTemplateExpression ParseMember() {
            var expression = ParsePrimary();
            while (true) {
                if (Take(ExpressionTokenKind.Dot)) { var token = _current; Require(ExpressionTokenKind.Identifier, "Expected a member identifier after '.'"); expression = new MemberAccessExpression(expression, token.Text, token.Offset); }
                else if (Take(ExpressionTokenKind.OpenBracket)) { var offset = _current.Offset - 1; var index = ParsePipeline(); Require(ExpressionTokenKind.CloseBracket, "Missing closing ']'"); expression = new IndexAccessExpression(expression, index, offset); }
                else return expression;
            }
        }
        private XTemplateExpression ParsePrimary() {
            var token = _current; Next();
            return token.Kind switch {
                ExpressionTokenKind.Null or ExpressionTokenKind.True or ExpressionTokenKind.False or ExpressionTokenKind.Number or ExpressionTokenKind.String => new LiteralExpression(token.Value, token.Offset),
                ExpressionTokenKind.Identifier => new IdentifierExpression(token.Text, token.Offset),
                ExpressionTokenKind.OpenParenthesis => ParseParenthesized(token.Offset),
                _ => throw new XTemplateExpressionSyntaxException($"Expected an expression, found '{token.Text}'", token.Offset)
            };
        }
        private XTemplateExpression ParseParenthesized(int offset) { var expression = ParsePipeline(); Require(ExpressionTokenKind.CloseParenthesis, "Missing closing ')'"); return expression; }
        private bool Take(ExpressionTokenKind kind) { if (_current.Kind != kind) return false; Next(); return true; }
        private void Require(ExpressionTokenKind kind, string message) { if (_current.Kind != kind) throw Error(message); Next(); }
        private void Next() => _current = _tokenizer.Next();
        private XTemplateExpressionSyntaxException Error(string message) => new(message, _current.Offset);
    }
}
