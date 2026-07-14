using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DProjects.Utils.Highlighters { 

    public static class HighlighterYaml {


        // consts
        private static readonly Regex NumberRegex = new(@"[+-]?(?:0[xX][0-9a-fA-F_]+|0[oO][0-7_]+|0[bB][01_]+|(?:[0-9][0-9_]*\.[0-9_]*|\.[0-9][0-9_]*)(?:[eE][+-]?[0-9][0-9_]*)?|[0-9][0-9_]*(?:[eE][+-]?[0-9][0-9_]*)?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly string DefaultColor = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF");
        private static readonly string KeyColor = ConsoleUtils.ForegroundColorFromRgb("#7EE787");
        private static readonly string StringColor = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF");
        private static readonly string StringBackslashColor = ConsoleUtils.ForegroundColorFromRgb("#CABA7D");
        private static readonly string BooleanColor = ConsoleUtils.ForegroundColorFromRgb("#569CD6");
        private static readonly string NumericColor = ConsoleUtils.ForegroundColorFromRgb("#B5CEA8");
        private static readonly string CommentsColor = ConsoleUtils.ForegroundColorFromRgb("#8B949E");
        private static readonly string EncryptedColor = ConsoleUtils.ForegroundColorFromRgb("#FF6B6B");
        private static readonly string ListItemColor = ConsoleUtils.ForegroundColorFromRgb("#BBBEBF");
        private static readonly string BracketColor = ConsoleUtils.ForegroundColorFromRgb("#FFD703");
        private static readonly string SeparatorColor = ConsoleUtils.ForegroundColorFromRgb("#D4D4D4");
        private static readonly string NullColor = ConsoleUtils.ForegroundColorFromRgb("#569CD6");
        private static readonly string TagColor = ConsoleUtils.ForegroundColorFromRgb("#C586C0");
        private static readonly string UrlPartsSeparatorsColor = ConsoleUtils.ForegroundColorFromRgb("#569CFF");


        // methods
        public static string Highlight(string yaml) {
            if (string.IsNullOrEmpty(yaml)) return yaml;
            var result = new List<string>();
            foreach (var line in yaml.Split('\n')) {
                result.Add(HighlightLine(line));
            }
            if (result.Count > 0) {
                result[result.Count - 1] = result[result.Count - 1] + ConsoleUtils.ATTRIBUTES_NONE;
            }
            return string.Join("\n", result);
        }
        public static string HighlightLine(string line) {
            if (line.StartsWith("---", StringComparison.Ordinal))
                return SeparatorColor + line + DefaultColor;

            var keyColon = FindKeyColon(line);
            var keyStart = 0;
            while (keyStart < line.Length && char.IsWhiteSpace(line[keyStart]))
                keyStart++;

            var listItemIndex = -1;
            if (keyStart < line.Length && line[keyStart] == '-' &&
                (keyStart + 1 == line.Length || char.IsWhiteSpace(line[keyStart + 1]))) {
                listItemIndex = keyStart++;
                while (keyStart < line.Length && char.IsWhiteSpace(line[keyStart]))
                    keyStart++;
            }

            var valueIsString = false;
            var valueIsUrl = false;
            var valueStart = line.Length;
            if (keyColon != -1) {
                valueStart = keyColon + 1;
                while (valueStart < line.Length && char.IsWhiteSpace(line[valueStart]))
                    valueStart++;

                if (valueStart < line.Length && line[valueStart] != '#') {
                    var valueEnd = valueStart;
                    while (valueEnd < line.Length && !char.IsWhiteSpace(line[valueEnd]) &&
                           !",]}#".Contains(line[valueEnd]))
                        valueEnd++;

                    var firstValue = line.Substring(valueStart, valueEnd - valueStart);
                    var numberMatch = NumberRegex.Match(firstValue);
                    var isNumber = numberMatch.Success && numberMatch.Index == 0 &&
                                   numberMatch.Length == firstValue.Length;
                    valueIsString = line[valueStart] is '\'' or '"' ||
                        (line[valueStart] is not '[' and not '{' &&
                         firstValue is not ("false" or "true" or "null") && !isNumber);

                    valueIsUrl = LooksLikeUrl(ValueWithoutComment(line.Substring(valueStart)));
                }
            } else if (listItemIndex != -1 ||
                       (keyStart < line.Length && line[keyStart] == '[')) {
                valueStart = keyStart;
                valueIsUrl = LooksLikeUrl(ValueWithoutComment(line.Substring(valueStart)));
            }

            var result = new StringBuilder(line.Length + 64);
            char? quote = null;
            var escaped = false;
            var i = 0;
            while (i < line.Length) {
                var current = line[i];
                var openedQuote = false;

                if (quote is null && current == '#' &&
                    (i == 0 || char.IsWhiteSpace(line[i - 1]))) {
                    result.Append(CommentsColor).Append(line, i, line.Length - i);
                    break;
                }

                if (!valueIsString && quote is null && (keyColon == -1 || i > keyColon) &&
                    StartsWithAt(line, "null", i) && IsScalarToken(line, i, 4)) {
                    result.Append(NullColor).Append("null").Append(DefaultColor);
                    i += 4;
                    continue;
                }

                string? boolean = null;
                if (!valueIsString && quote is null && (keyColon == -1 || i > keyColon)) {
                    foreach (var candidate in new[] { "false", "true" }) {
                        if (StartsWithAt(line, candidate, i) && IsScalarToken(line, i, candidate.Length)) {
                            boolean = candidate;
                            break;
                        }
                    }
                }

                if (boolean is not null) {
                    result.Append(BooleanColor).Append(boolean).Append(DefaultColor);
                    i += boolean.Length;
                    continue;
                }

                if (!valueIsString && quote is null && (keyColon == -1 || i > keyColon)) {
                    var numberMatch = NumberRegex.Match(line, i);
                    if (numberMatch.Success && numberMatch.Index == i &&
                        IsScalarToken(line, i, numberMatch.Length)) {
                        result.Append(NumericColor).Append(numberMatch.Value).Append(DefaultColor);
                        i = numberMatch.Index + numberMatch.Length;
                        continue;
                    }
                }

                var bracedEncrypted = StartsWithAt(line, "${enc:", i);
                var plainEncrypted = StartsWithAt(line, "enc:", i);
                if (bracedEncrypted || plainEncrypted) {
                    var tokenInKey = keyStart <= i && i < keyColon;
                    var marker = bracedEncrypted ? "${enc:" : "enc:";
                    result.Append(marker).Append(EncryptedColor);
                    i += marker.Length;
                    while (i < line.Length) {
                        if (bracedEncrypted && line[i] == '}' || quote is not null && line[i] == quote)
                            break;
                        if (quote is null && (char.IsWhiteSpace(line[i]) || line[i] == '#' ||
                                              ",]}".Contains(line[i])))
                            break;
                        result.Append(line[i++]);
                    }

                    result.Append(quote is not null ? StringColor : tokenInKey ? KeyColor : DefaultColor);
                    if (bracedEncrypted && i < line.Length && line[i] == '}')
                        result.Append(line[i++]);
                    continue;
                }

                if (quote is null && i == keyStart && keyStart < keyColon)
                    result.Append(KeyColor);
                if (quote is null && i == listItemIndex)
                    result.Append(ListItemColor);
                if (quote is null && "[]".Contains(current))
                    result.Append(BracketColor);
                if (quote is null && "<>".Contains(current))
                    result.Append(TagColor);
                if (valueIsUrl && i >= valueStart && ":/?&=".Contains(current))
                    result.Append(UrlPartsSeparatorsColor);

                if (quote == '"' && current == '\\') {
                    result.Append(StringBackslashColor).Append(current);
                    if (i + 1 < line.Length)
                        result.Append(line[++i]);
                    result.Append(StringColor);
                    i++;
                    continue;
                }

                if (quote is null && current is '\'' or '"') {
                    quote = current;
                    openedQuote = true;
                    result.Append(StringColor);
                }

                result.Append(current);
                if (valueIsUrl && i >= valueStart && ":/?&=".Contains(current))
                    result.Append(quote is not null ? StringColor : DefaultColor);

                if (quote == '"' && !openedQuote) {
                    if (escaped)
                        escaped = false;
                    else if (current == '\\')
                        escaped = true;
                    else if (current == '"') {
                        quote = null;
                        result.Append(i < keyColon ? KeyColor : DefaultColor);
                    }
                } else if (quote == '\'' && current == '\'' && !openedQuote) {
                    if (i + 1 < line.Length && line[i + 1] == '\'')
                        result.Append(line[++i]);
                    else {
                        quote = null;
                        result.Append(i < keyColon ? KeyColor : DefaultColor);
                    }
                } else if (quote is null && i == keyColon)
                    result.Append(DefaultColor);
                else if (quote is null && i == listItemIndex)
                    result.Append(DefaultColor);
                else if (quote is null && "[]".Contains(current))
                    result.Append(DefaultColor);
                else if (quote is null && "<>".Contains(current))
                    result.Append(DefaultColor);

                i++;
            }

            return result.ToString();
        }

        private static int FindKeyColon(string line) {
            char? quote = null;
            var escaped = false;
            var skipQuoteAt = -1;
            for (var i = 0; i < line.Length; i++) {
                var current = line[i];
                if (quote == '"') {
                    if (escaped) escaped = false;
                    else if (current == '\\') escaped = true;
                    else if (current == '"') quote = null;
                    continue;
                }

                if (quote == '\'') {
                    if (i == skipQuoteAt) continue;
                    if (current == '\'') {
                        if (i + 1 < line.Length && line[i + 1] == '\'') {
                            skipQuoteAt = i + 1;
                            continue;
                        }
                        quote = null;
                    }
                    continue;
                }

                if (current is '\'' or '"') quote = current;
                else if (current == '#' && (i == 0 || char.IsWhiteSpace(line[i - 1]))) break;
                else if (current == ':' && line.Substring(0,i).Replace("-", "").Trim().IndexOf(" ") != -1) break;
                else if (current == ':' && (i + 1 == line.Length ||
                         char.IsWhiteSpace(line[i + 1]) || "[]{},".Contains(line[i + 1]))) return i;
            }
            return -1;
        }

        private static bool IsScalarToken(string line, int start, int length) {
            var end = start + length;
            var startsScalar = start == 0 || char.IsWhiteSpace(line[start - 1]) ||
                               "[,{".Contains(line[start - 1]);
            var endsScalar = end == line.Length || char.IsWhiteSpace(line[end]) ||
                             ",]}".Contains(line[end]);
            return startsScalar && endsScalar;
        }

        private static bool StartsWithAt(string value, string candidate, int index) =>
            index >= 0 && index + candidate.Length <= value.Length &&
            value.AsSpan(index, candidate.Length).SequenceEqual(candidate.AsSpan());

        private static string ValueWithoutComment(string value) {
            var commentStart = value.IndexOf(" #", StringComparison.Ordinal);
            return commentStart == -1 ? value : value.Substring(0, commentStart);
        }

        private static bool LooksLikeUrl(string value) =>
            value.Contains("://", StringComparison.Ordinal) ||
            value.Contains(":?", StringComparison.Ordinal);

    }

}