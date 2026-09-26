namespace DProjects.XShell.Services.XTemplate {

    internal sealed record XTemplateStyleDeclaration(string Name, string Value, string Priority);

    internal static class XTemplateStyleNames {

        // methods
        public static bool IsValid(string name) {
            if (name.Length == 0 || name.Any(character => char.IsWhiteSpace(character) || char.IsControl(character) || character is ':' or ';')) return false;
            if (name.StartsWith("--", StringComparison.Ordinal)) return name.Length > 2;
            if (!char.IsLetter(name[0]) && name[0] != '-' && name[0] != '_') return false;
            return name.Skip(1).All(character => char.IsLetterOrDigit(character) || character is '-' or '_');
        }
        public static string Normalize(string name) => name.StartsWith("--", StringComparison.Ordinal) ? name : name.ToLowerInvariant();
    }

    internal static class XTemplateStyleDeclarations {

        // methods
        public static IReadOnlyList<XTemplateStyleDeclaration> Parse(string source, int offset) {
            var declarations = new Dictionary<string, XTemplateStyleDeclaration>(StringComparer.Ordinal);
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
                if (!XTemplateStyleNames.IsValid(name)) throw new XTemplateException($"Invalid style declaration property name '{name}'", offset + start);
                if (value.Length == 0) throw new XTemplateException($"Invalid style declaration for '{name}': value is empty", offset + colon + 1);
                if (!name.StartsWith("--", StringComparison.Ordinal)) name = name.ToLowerInvariant();
                var (normalizedValue, priority) = ExtractPriority(value, offset + colon + 1);
                declarations[name] = new(name, normalizedValue, priority);
            }
            return declarations.Values.ToArray();
        }

        public static string Serialize(IEnumerable<XTemplateStyleDeclaration> declarations) {
            return string.Join(';', declarations.Select(declaration => declaration.Name + ":" + declaration.Value + (declaration.Priority.Length == 0 ? string.Empty : " !" + declaration.Priority)));
        }

        // methods (private)
        private static (string Value, string Priority) ExtractPriority(string value, int offset) {
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
    }
}
