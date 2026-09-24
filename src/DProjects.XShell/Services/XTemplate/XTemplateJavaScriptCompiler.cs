using System.Globalization;
using System.Text;

namespace DProjects.XShell.Services.XTemplate {

    internal sealed class XTemplateJavaScriptCompiler {

        private readonly XTemplateCompiler _templateCompiler;

        public XTemplateJavaScriptCompiler(XTemplateCompiler templateCompiler) {
            _templateCompiler = templateCompiler;
        }

        // methods
        public string Transform(string source) {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // locate the exported definition without interpreting strings or comments as source structure
            var tokens = JavaScriptLexer.Tokenize(source);
            var export = FindDefaultExportObject(tokens);
            if (export == null) return source;
            var properties = ReadTopLevelProperties(tokens, export.Value.OpenBraceIndex, export.Value.CloseBraceIndex);
            var template = properties.FirstOrDefault(property => property.Name == "template");
            if (template == null) throw new InvalidOperationException("The exported X component definition does not declare a static 'template' property.");
            var valueToken = tokens[template.ValueStartTokenIndex];
            if (valueToken.Kind != TokenKind.Template) {
                throw new InvalidOperationException($"The exported X component 'template' at JavaScript offset {valueToken.Start} must be a static template literal.");
            }
            var templateText = DecodeStaticTemplateLiteral(source[valueToken.Start..valueToken.End], valueToken.Start);
            var renderer = _templateCompiler.Compile(templateText);
            var existingRenderer = properties.FirstOrDefault(property => property.Name == "templateRenderer");
            if (existingRenderer != null) {
                var start = tokens[existingRenderer.ValueStartTokenIndex].Start;
                var end = tokens[existingRenderer.ValueEndTokenIndex].End;
                return source[..start] + renderer + source[end..];
            }

            // insert directly after template while retaining all original module text
            var newline = source.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
            var indentation = GetLineIndentation(source, tokens[template.KeyTokenIndex].Start);
            var separator = template.SeparatorTokenIndex >= 0;
            var insertionOffset = separator ? tokens[template.SeparatorTokenIndex].End : tokens[template.ValueEndTokenIndex].End;
            var prefix = separator ? "" : ",";
            return source[..insertionOffset] + prefix + newline + indentation + "templateRenderer: " + renderer + "," + source[insertionOffset..];
        }

        // methods (private)
        private static ExportObject? FindDefaultExportObject(IReadOnlyList<Token> tokens) {
            for (var index = 0; index + 2 < tokens.Count; index++) {
                if (!tokens[index].Is("export") || !tokens[index + 1].Is("default")) continue;
                if (!tokens[index + 2].Is("{")) return null;
                var closeBrace = FindMatchingToken(tokens, index + 2, "{", "}");
                if (closeBrace < 0) throw new InvalidOperationException($"Malformed JavaScript: exported component object at offset {tokens[index + 2].Start} is not closed.");
                return new ExportObject(index + 2, closeBrace);
            }
            return null;
        }

        private static List<ObjectProperty> ReadTopLevelProperties(IReadOnlyList<Token> tokens, int openBraceIndex, int closeBraceIndex) {
            var result = new List<ObjectProperty>();
            var index = openBraceIndex + 1;
            while (index < closeBraceIndex) {
                if (tokens[index].Is(",")) { index++; continue; }
                var keyIndex = index;
                var name = GetPropertyName(tokens[keyIndex]);
                if (name == null || keyIndex + 1 >= closeBraceIndex || !tokens[keyIndex + 1].Is(":")) {
                    index = SkipProperty(tokens, index, closeBraceIndex) + 1;
                    continue;
                }
                var valueStart = keyIndex + 2;
                if (valueStart >= closeBraceIndex) throw new InvalidOperationException($"Malformed JavaScript: property '{name}' has no value.");
                var separator = FindPropertySeparator(tokens, valueStart, closeBraceIndex);
                var valueEnd = (separator >= 0 ? separator : closeBraceIndex) - 1;
                if (valueEnd < valueStart) throw new InvalidOperationException($"Malformed JavaScript: property '{name}' has no value.");
                result.Add(new ObjectProperty(name, keyIndex, valueStart, valueEnd, separator));
                index = separator >= 0 ? separator + 1 : closeBraceIndex;
            }
            return result;
        }

        private static int SkipProperty(IReadOnlyList<Token> tokens, int start, int closeBraceIndex) {
            var separator = FindPropertySeparator(tokens, start, closeBraceIndex);
            return separator >= 0 ? separator : closeBraceIndex - 1;
        }

        private static int FindPropertySeparator(IReadOnlyList<Token> tokens, int start, int closeBraceIndex) {
            var braces = 0;
            var brackets = 0;
            var parentheses = 0;
            for (var index = start; index < closeBraceIndex; index++) {
                if (tokens[index].Is("{")) braces++;
                else if (tokens[index].Is("}")) braces--;
                else if (tokens[index].Is("[")) brackets++;
                else if (tokens[index].Is("]")) brackets--;
                else if (tokens[index].Is("(")) parentheses++;
                else if (tokens[index].Is(")")) parentheses--;
                else if (tokens[index].Is(",") && braces == 0 && brackets == 0 && parentheses == 0) return index;
                if (braces < 0 || brackets < 0 || parentheses < 0) throw new InvalidOperationException($"Malformed JavaScript structure near offset {tokens[index].Start}.");
            }
            if (braces != 0 || brackets != 0 || parentheses != 0) throw new InvalidOperationException("Malformed JavaScript structure in exported component definition.");
            return -1;
        }

        private static int FindMatchingToken(IReadOnlyList<Token> tokens, int openIndex, string open, string close) {
            var depth = 0;
            for (var index = openIndex; index < tokens.Count; index++) {
                if (tokens[index].Is(open)) depth++;
                else if (tokens[index].Is(close) && --depth == 0) return index;
            }
            return -1;
        }

        private static string? GetPropertyName(Token token) {
            if (token.Kind == TokenKind.Identifier) return token.Text;
            if (token.Kind == TokenKind.String) return DecodeQuotedString(token.Text);
            return null;
        }

        private static string GetLineIndentation(string source, int offset) {
            var lineStart = source.LastIndexOf('\n', Math.Max(0, offset - 1));
            lineStart = lineStart < 0 ? 0 : lineStart + 1;
            var position = lineStart;
            while (position < offset && source[position] is ' ' or '\t') position++;
            return source[lineStart..position];
        }

        private static string DecodeStaticTemplateLiteral(string literal, int sourceOffset) {
            var result = new StringBuilder(literal.Length);
            for (var index = 1; index < literal.Length - 1; index++) {
                var character = literal[index];
                if (character == '$' && index + 1 < literal.Length - 1 && literal[index + 1] == '{') {
                    throw new InvalidOperationException($"The X component template at JavaScript offset {sourceOffset} must be static; template substitutions are not supported.");
                }
                if (character != '\\') { result.Append(character); continue; }
                if (++index >= literal.Length - 1) throw new InvalidOperationException($"Invalid escape sequence in template literal at JavaScript offset {sourceOffset + index}.");
                character = literal[index];
                if (character == '\r' || character == '\n') {
                    if (character == '\r' && index + 1 < literal.Length - 1 && literal[index + 1] == '\n') index++;
                    continue;
                }
                result.Append(character switch {
                    'b' => '\b', 'f' => '\f', 'n' => '\n', 'r' => '\r', 't' => '\t', 'v' => '\v', '0' => '\0',
                    'x' => DecodeFixedEscape(literal, ref index, 2, sourceOffset),
                    'u' => DecodeUnicodeEscape(literal, ref index, sourceOffset),
                    _ => character
                });
            }
            return result.ToString();
        }

        private static string DecodeFixedEscape(string literal, ref int index, int digits, int sourceOffset) {
            if (index + digits >= literal.Length - 1) throw new InvalidOperationException($"Incomplete escape sequence at JavaScript offset {sourceOffset + index}.");
            var hex = literal.Substring(index + 1, digits);
            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value)) throw new InvalidOperationException($"Invalid escape sequence at JavaScript offset {sourceOffset + index}.");
            index += digits;
            return char.ConvertFromUtf32(value);
        }

        private static string DecodeUnicodeEscape(string literal, ref int index, int sourceOffset) {
            if (index + 1 < literal.Length - 1 && literal[index + 1] == '{') {
                var end = literal.IndexOf('}', index + 2);
                if (end < 0 || end >= literal.Length - 1) throw new InvalidOperationException($"Incomplete Unicode escape at JavaScript offset {sourceOffset + index}.");
                var hex = literal[(index + 2)..end];
                if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value) || value > 0x10FFFF) throw new InvalidOperationException($"Invalid Unicode escape at JavaScript offset {sourceOffset + index}.");
                index = end;
                return char.ConvertFromUtf32(value);
            }
            return DecodeFixedEscape(literal, ref index, 4, sourceOffset);
        }

        private static string DecodeQuotedString(string literal) {
            var result = new StringBuilder(literal.Length - 2);
            for (var index = 1; index < literal.Length - 1; index++) {
                if (literal[index] == '\\' && index + 1 < literal.Length - 1) result.Append(literal[++index]);
                else result.Append(literal[index]);
            }
            return result.ToString();
        }

        private sealed record ObjectProperty(string Name, int KeyTokenIndex, int ValueStartTokenIndex, int ValueEndTokenIndex, int SeparatorTokenIndex);
        private readonly record struct ExportObject(int OpenBraceIndex, int CloseBraceIndex);
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
