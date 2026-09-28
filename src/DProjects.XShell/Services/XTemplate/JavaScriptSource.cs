using System.Globalization;
using System.Text;

namespace DProjects.XShell.Services.XTemplate {

    internal sealed class JavaScriptSource {

        // vars
        private readonly IReadOnlyList<Token> _tokens;

        // props
        public string Source { get; }

        // ctor
        private JavaScriptSource(string source, IReadOnlyList<Token> tokens) {
            Source = source;
            _tokens = tokens;
        }

        // methods
        public static JavaScriptSource Parse(string source) {
            ArgumentNullException.ThrowIfNull(source);
            return new JavaScriptSource(source, JavaScriptLexer.Tokenize(source));
        }
        public static string EncodeStaticTemplateLiteral(string value) {
            ArgumentNullException.ThrowIfNull(value);
            var result = new StringBuilder(value.Length + 2).Append('`');
            for (var index = 0; index < value.Length; index++) {
                if (value[index] == '\\') result.Append("\\\\");
                else if (value[index] == '`') result.Append("\\`");
                else if (value[index] == '$' && index + 1 < value.Length && value[index + 1] == '{') {
                    result.Append("\\${");
                    index++;
                } else result.Append(value[index]);
            }
            return result.Append('`').ToString();
        }
        public JavaScriptObject? FindDefaultExportObject() {
            var exportIndex = FindDefaultExportIndex();
            if (exportIndex < 0 || exportIndex + 2 >= _tokens.Count || !_tokens[exportIndex + 2].Is("{")) return null;
            return ReadObject(exportIndex + 2, "exported component object");
        }
        public JavaScriptObject RequireDefaultExportObject(string missingMessage, string nonObjectMessage) {
            var exportIndex = FindDefaultExportIndex();
            if (exportIndex < 0) throw new InvalidOperationException(missingMessage);
            if (exportIndex + 2 >= _tokens.Count || !_tokens[exportIndex + 2].Is("{")) throw new InvalidOperationException(nonObjectMessage);
            return ReadObject(exportIndex + 2, "exported component object");
        }
        public string GetLineIndentation(int offset) {
            var lineStart = Source.LastIndexOf('\n', Math.Max(0, offset - 1));
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            var position = lineStart;
            while (position < offset && Source[position] is ' ' or '\t') position++;
            return Source[lineStart..position];
        }

        // methods (private)
        private int FindDefaultExportIndex() {
            for (var index = 0; index + 1 < _tokens.Count; index++) {
                if (_tokens[index].Is("export") && _tokens[index + 1].Is("default")) return index;
            }
            return -1;
        }
        private JavaScriptObject ReadObject(int openBraceIndex, string description) {
            var closeBraceIndex = FindMatchingToken(openBraceIndex, "{", "}");
            if (closeBraceIndex < 0) throw new InvalidOperationException($"Malformed JavaScript: {description} at offset {_tokens[openBraceIndex].Start} is not closed.");
            return new JavaScriptObject(this, openBraceIndex, closeBraceIndex);
        }
        private int FindMatchingToken(int openIndex, string open, string close) {
            var depth = 0;
            for (var index = openIndex; index < _tokens.Count; index++) {
                if (_tokens[index].Is(open)) depth++;
                else if (_tokens[index].Is(close) && --depth == 0) return index;
            }
            return -1;
        }
        private IReadOnlyList<JavaScriptProperty> ReadProperties(int openBraceIndex, int closeBraceIndex) {
            var result = new List<JavaScriptProperty>();
            var index = openBraceIndex + 1;
            while (index < closeBraceIndex) {
                if (_tokens[index].Is(",")) { index++; continue; }
                var keyIndex = index;
                var name = GetPropertyName(_tokens[keyIndex]);
                if (name == null || keyIndex + 1 >= closeBraceIndex || !_tokens[keyIndex + 1].Is(":")) {
                    index = SkipProperty(index, closeBraceIndex) + 1;
                    continue;
                }
                var valueStart = keyIndex + 2;
                if (valueStart >= closeBraceIndex) throw new InvalidOperationException($"Malformed JavaScript: property '{name}' has no value.");
                var separator = FindPropertySeparator(valueStart, closeBraceIndex);
                var valueEnd = (separator >= 0 ? separator : closeBraceIndex) - 1;
                if (valueEnd < valueStart) throw new InvalidOperationException($"Malformed JavaScript: property '{name}' has no value.");
                result.Add(new JavaScriptProperty(this, name, keyIndex, valueStart, valueEnd, separator));
                index = separator >= 0 ? separator + 1 : closeBraceIndex;
            }
            return result;
        }
        private bool HasExplicitProperty(int openBraceIndex, int closeBraceIndex, string propertyName) {
            var index = openBraceIndex + 1;
            while (index < closeBraceIndex) {
                if (_tokens[index].Is(",")) { index++; continue; }
                var separator = FindPropertySeparator(index, closeBraceIndex);
                var end = separator >= 0 ? separator : closeBraceIndex;
                if (GetExplicitPropertyName(index, end) == propertyName) return true;
                index = separator >= 0 ? separator + 1 : closeBraceIndex;
            }
            return false;
        }
        private string? GetExplicitPropertyName(int start, int end) {
            if (start >= end) return null;
            if (_tokens[start].Is("get") || _tokens[start].Is("set") || _tokens[start].Is("async")) start++;
            if (start < end && _tokens[start].Is("*")) start++;
            if (start >= end) return null;
            if (_tokens[start].Is("[") && start + 2 < end && _tokens[start + 2].Is("]") && _tokens[start + 1].Kind == TokenKind.String) {
                return DecodeQuotedString(_tokens[start + 1].Text, _tokens[start + 1].Start);
            }
            return GetPropertyName(_tokens[start]);
        }
        private int SkipProperty(int start, int closeBraceIndex) {
            var separator = FindPropertySeparator(start, closeBraceIndex);
            return separator >= 0 ? separator : closeBraceIndex - 1;
        }
        private int FindPropertySeparator(int start, int closeBraceIndex) {
            var braces = 0;
            var brackets = 0;
            var parentheses = 0;
            for (var index = start; index < closeBraceIndex; index++) {
                if (_tokens[index].Is("{")) braces++;
                else if (_tokens[index].Is("}")) braces--;
                else if (_tokens[index].Is("[")) brackets++;
                else if (_tokens[index].Is("]")) brackets--;
                else if (_tokens[index].Is("(")) parentheses++;
                else if (_tokens[index].Is(")")) parentheses--;
                else if (_tokens[index].Is(",") && braces == 0 && brackets == 0 && parentheses == 0) return index;
                if (braces < 0 || brackets < 0 || parentheses < 0) throw new InvalidOperationException($"Malformed JavaScript structure near offset {_tokens[index].Start}.");
            }
            if (braces != 0 || brackets != 0 || parentheses != 0) throw new InvalidOperationException("Malformed JavaScript structure in exported object.");
            return -1;
        }
        private string? GetPropertyName(Token token) {
            if (token.Kind == TokenKind.Identifier) return token.Text;
            if (token.Kind == TokenKind.String) return DecodeQuotedString(token.Text, token.Start);
            return null;
        }
        private JavaScriptObject? AsObject(int valueStartTokenIndex, int valueEndTokenIndex) {
            if (!_tokens[valueStartTokenIndex].Is("{")) return null;
            var closeBraceIndex = FindMatchingToken(valueStartTokenIndex, "{", "}");
            return closeBraceIndex == valueEndTokenIndex ? new JavaScriptObject(this, valueStartTokenIndex, closeBraceIndex) : null;
        }
        private string? GetStaticString(int valueStartTokenIndex, int valueEndTokenIndex) {
            if (valueStartTokenIndex != valueEndTokenIndex) return null;
            var token = _tokens[valueStartTokenIndex];
            if (token.Kind == TokenKind.Template && HasTemplateSubstitution(token.Text)) return null;
            if (token.Kind is not TokenKind.String and not TokenKind.Template) return null;
            return DecodeQuotedString(token.Text, token.Start);
        }
        private static bool HasTemplateSubstitution(string literal) {
            for (var index = 1; index < literal.Length - 1; index++) {
                if (literal[index] == '\\') {
                    index++;
                } else if (literal[index] == '$' && index + 1 < literal.Length - 1 && literal[index + 1] == '{') {
                    return true;
                }
            }
            return false;
        }
        private static string DecodeQuotedString(string literal, int sourceOffset) {
            var result = new StringBuilder(literal.Length - 2);
            for (var index = 1; index < literal.Length - 1; index++) {
                var character = literal[index];
                if (character != '\\') { result.Append(character); continue; }
                if (++index >= literal.Length - 1) throw new InvalidOperationException($"Invalid escape sequence in JavaScript string at offset {sourceOffset + index}.");
                character = literal[index];
                if (character == '\r' || character == '\n') {
                    if (character == '\r' && index + 1 < literal.Length - 1 && literal[index + 1] == '\n') index++;
                    continue;
                }
                if (character == 'x') {
                    result.Append((char)DecodeHexEscape(literal, ref index, 2, sourceOffset));
                } else if (character == 'u' && index + 1 < literal.Length - 1 && literal[index + 1] == '{') {
                    var end = literal.IndexOf('}', index + 2);
                    if (end < 0 || end >= literal.Length - 1) throw new InvalidOperationException($"Incomplete Unicode escape in JavaScript string at offset {sourceOffset + index}.");
                    var hex = literal[(index + 2)..end];
                    if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) || value > 0x10FFFF) {
                        throw new InvalidOperationException($"Invalid Unicode escape in JavaScript string at offset {sourceOffset + index}.");
                    }
                    result.Append(char.ConvertFromUtf32(value));
                    index = end;
                } else if (character == 'u') {
                    result.Append((char)DecodeHexEscape(literal, ref index, 4, sourceOffset));
                } else {
                    result.Append(character switch { 'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', 'v' => '\v', '0' => '\0', _ => character });
                }
            }
            return result.ToString();
        }
        private static int DecodeHexEscape(string literal, ref int index, int digits, int sourceOffset) {
            if (index + digits >= literal.Length - 1) throw new InvalidOperationException($"Incomplete escape sequence in JavaScript string at offset {sourceOffset + index}.");
            var hex = literal.Substring(index + 1, digits);
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) {
                throw new InvalidOperationException($"Invalid escape sequence in JavaScript string at offset {sourceOffset + index}.");
            }
            index += digits;
            return value;
        }

        internal sealed class JavaScriptObject {

            // vars
            private readonly JavaScriptSource _document;
            private IReadOnlyList<JavaScriptProperty>? _properties;

            // props
            public int Start => _document._tokens[OpenBraceTokenIndex].Start;
            public int ContentStart => _document._tokens[OpenBraceTokenIndex].End;
            private int OpenBraceTokenIndex { get; }
            private int CloseBraceTokenIndex { get; }
            private IReadOnlyList<JavaScriptProperty> Properties => _properties ??= _document.ReadProperties(OpenBraceTokenIndex, CloseBraceTokenIndex);

            // ctor
            public JavaScriptObject(JavaScriptSource document, int openBraceTokenIndex, int closeBraceTokenIndex) {
                _document = document;
                OpenBraceTokenIndex = openBraceTokenIndex;
                CloseBraceTokenIndex = closeBraceTokenIndex;
            }

            // methods
            public JavaScriptProperty? FindProperty(string name) => Properties.FirstOrDefault(property => property.Name == name);
            public bool HasExplicitProperty(string name) => _document.HasExplicitProperty(OpenBraceTokenIndex, CloseBraceTokenIndex, name);
        }

        internal sealed class JavaScriptProperty {

            // vars
            private readonly JavaScriptSource _document;
            private readonly int _keyTokenIndex;
            private readonly int _separatorTokenIndex;

            // props
            public string Name { get; }
            public int KeyStart => _document._tokens[_keyTokenIndex].Start;
            public int? SeparatorEnd => _separatorTokenIndex >= 0 ? _document._tokens[_separatorTokenIndex].End : null;
            public JavaScriptValue Value { get; }

            // ctor
            public JavaScriptProperty(JavaScriptSource document, string name, int keyTokenIndex, int valueStartTokenIndex, int valueEndTokenIndex, int separatorTokenIndex) {
                _document = document;
                _keyTokenIndex = keyTokenIndex;
                _separatorTokenIndex = separatorTokenIndex;
                Name = name;
                Value = new JavaScriptValue(document, valueStartTokenIndex, valueEndTokenIndex);
            }
        }

        internal readonly struct JavaScriptValue {

            // vars
            private readonly JavaScriptSource _document;
            private readonly int _startTokenIndex;
            private readonly int _endTokenIndex;

            // props
            public int Start => _document._tokens[_startTokenIndex].Start;
            public int End => _document._tokens[_endTokenIndex].End;
            public string Text => _document.Source[Start..End];
            public bool IsTemplateLiteral => _startTokenIndex == _endTokenIndex && _document._tokens[_startTokenIndex].Kind == TokenKind.Template;

            // ctor
            public JavaScriptValue(JavaScriptSource document, int startTokenIndex, int endTokenIndex) {
                _document = document;
                _startTokenIndex = startTokenIndex;
                _endTokenIndex = endTokenIndex;
            }

            // methods
            public JavaScriptObject? AsObject() => _document.AsObject(_startTokenIndex, _endTokenIndex);
            public string? GetStaticString() => _document.GetStaticString(_startTokenIndex, _endTokenIndex);
        }

        private enum TokenKind { Identifier, String, Template, Punctuation, Number, Regex }
        private sealed record Token(TokenKind Kind, string Text, int Start, int End) { public bool Is(string value) => Text == value; }

        private static class JavaScriptLexer {

            // methods
            public static List<Token> Tokenize(string source) {
                var result = new List<Token>();
                var position = 0;
                while (position < source.Length) {
                    if (char.IsWhiteSpace(source[position])) { position++; continue; }
                    if (source[position] == '/' && position + 1 < source.Length && source[position + 1] == '/') { SkipLineComment(source, ref position); continue; }
                    if (source[position] == '/' && position + 1 < source.Length && source[position + 1] == '*') { SkipBlockComment(source, ref position); continue; }
                    var start = position;
                    if (source[position] is '\'' or '"') {
                        ScanQuotedString(source, ref position);
                        result.Add(new Token(TokenKind.String, source[start..position], start, position));
                    } else if (source[position] == '`') {
                        ScanTemplate(source, ref position);
                        result.Add(new Token(TokenKind.Template, source[start..position], start, position));
                    } else if (IsIdentifierStart(source[position])) {
                        position++;
                        while (position < source.Length && IsIdentifierPart(source[position])) position++;
                        result.Add(new Token(TokenKind.Identifier, source[start..position], start, position));
                    } else if (char.IsDigit(source[position])) {
                        position++;
                        while (position < source.Length && (char.IsLetterOrDigit(source[position]) || source[position] is '.' or '_')) position++;
                        result.Add(new Token(TokenKind.Number, source[start..position], start, position));
                    } else if (source[position] == '/' && CanStartRegex(result)) {
                        ScanRegex(source, ref position);
                        result.Add(new Token(TokenKind.Regex, source[start..position], start, position));
                    } else {
                        position++;
                        result.Add(new Token(TokenKind.Punctuation, source[start..position], start, position));
                    }
                }
                return result;
            }

            // methods (private)
            private static void SkipLineComment(string source, ref int position) { position += 2; while (position < source.Length && source[position] is not '\r' and not '\n') position++; }
            private static void SkipBlockComment(string source, ref int position) {
                var start = position;
                var end = source.IndexOf("*/", position + 2, StringComparison.Ordinal);
                if (end < 0) throw new InvalidOperationException($"Malformed JavaScript: block comment at offset {start} is not closed.");
                position = end + 2;
            }
            private static void ScanQuotedString(string source, ref int position) {
                var start = position;
                var quote = source[position++];
                while (position < source.Length) {
                    if (source[position] == '\\') { position += Math.Min(2, source.Length - position); continue; }
                    if (source[position++] == quote) return;
                }
                throw new InvalidOperationException($"Malformed JavaScript: string at offset {start} is not closed.");
            }
            private static void ScanTemplate(string source, ref int position) {
                var start = position++;
                while (position < source.Length) {
                    if (source[position] == '\\') { position += Math.Min(2, source.Length - position); continue; }
                    if (source[position] == '$' && position + 1 < source.Length && source[position + 1] == '{') {
                        position += 2;
                        ScanTemplateExpression(source, ref position, start);
                        continue;
                    }
                    if (source[position++] == '`') return;
                }
                throw new InvalidOperationException($"Malformed JavaScript: template literal at offset {start} is not closed.");
            }
            private static void ScanTemplateExpression(string source, ref int position, int templateStart) {
                var depth = 1;
                var canStartRegex = true;
                while (position < source.Length) {
                    if (char.IsWhiteSpace(source[position])) { position++; continue; }
                    if (source[position] == '/' && position + 1 < source.Length && source[position + 1] == '/') { SkipLineComment(source, ref position); continue; }
                    if (source[position] == '/' && position + 1 < source.Length && source[position + 1] == '*') { SkipBlockComment(source, ref position); continue; }
                    if (source[position] is '\'' or '"') { ScanQuotedString(source, ref position); canStartRegex = false; continue; }
                    if (source[position] == '`') { ScanTemplate(source, ref position); canStartRegex = false; continue; }
                    if (source[position] == '/' && canStartRegex) { ScanRegex(source, ref position); canStartRegex = false; continue; }
                    if (source[position] == '{') { depth++; position++; canStartRegex = true; continue; }
                    if (source[position] == '}') {
                        position++;
                        if (--depth == 0) return;
                        canStartRegex = false;
                        continue;
                    }
                    if (IsIdentifierStart(source[position])) {
                        var identifierStart = position++;
                        while (position < source.Length && IsIdentifierPart(source[position])) position++;
                        var identifier = source[identifierStart..position];
                        canStartRegex = identifier is "return" or "case" or "throw" or "typeof" or "void" or "delete" or "new" or "in" or "of";
                        continue;
                    }
                    if (char.IsDigit(source[position])) {
                        position++;
                        while (position < source.Length && (char.IsLetterOrDigit(source[position]) || source[position] is '.' or '_')) position++;
                        canStartRegex = false;
                        continue;
                    }
                    canStartRegex = source[position] is '(' or '[' or ',' or ':' or ';' or '=' or '!' or '?' or '+' or '-' or '*' or '%' or '&' or '|' or '^' or '~' or '<' or '>';
                    position++;
                }
                throw new InvalidOperationException($"Malformed JavaScript: template substitution in literal at offset {templateStart} is not closed.");
            }
            private static void ScanRegex(string source, ref int position) {
                var start = position++;
                var characterClass = false;
                while (position < source.Length) {
                    var character = source[position++];
                    if (character == '\\') { if (position < source.Length) position++; continue; }
                    if (character == '[') characterClass = true;
                    else if (character == ']') characterClass = false;
                    else if (character == '/' && !characterClass) { while (position < source.Length && char.IsLetter(source[position])) position++; return; }
                    else if (character is '\r' or '\n') break;
                }
                throw new InvalidOperationException($"Malformed JavaScript: regular expression at offset {start} is not closed.");
            }
            private static bool CanStartRegex(IReadOnlyList<Token> tokens) {
                if (tokens.Count == 0) return true;
                var previous = tokens[^1].Text;
                if (previous == ">" && tokens.Count > 1 && tokens[^2].Text == "=") return true;
                return previous is "(" or "[" or "{" or "," or ":" or ";" or "=" or "!" or "?" ||
                    previous is "return" or "case" or "throw" or "typeof" or "void" or "delete" or "new" or "in" or "of";
            }
            private static bool IsIdentifierStart(char character) => char.IsLetter(character) || character is '_' or '$';
            private static bool IsIdentifierPart(char character) => char.IsLetterOrDigit(character) || character is '_' or '$';
        }
    }
}
