using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DProjects.Utils.Highlighters { 

    public static class HighlighterCsv {

        // consts
        private static readonly Regex NumberRegex = new Regex(@"[+-]?(?:0[xX][0-9a-fA-F_]+|0[oO][0-7_]+|0[bB][01_]+|(?:[0-9][0-9_]*\.[0-9_]*|\.[0-9][0-9_]*)(?:[eE][+-]?[0-9][0-9_]*)?|[0-9][0-9_]*(?:[eE][+-]?[0-9][0-9_]*)?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly string DefaultColor              = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF");
        private static readonly string StringColor               = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF");
        private static readonly string StringBackslashColor      = ConsoleUtils.ForegroundColorFromRgb("#CABA7D");
        private static readonly string BooleanColor              = ConsoleUtils.ForegroundColorFromRgb("#569CD6");
        private static readonly string NumericColor              = ConsoleUtils.ForegroundColorFromRgb("#B5CEA8");
        private static readonly string CommentsColor             = ConsoleUtils.ForegroundColorFromRgb("#8B949E");
        private static readonly string EncryptedColor            = ConsoleUtils.ForegroundColorFromRgb("#FF6B6B");
        private static readonly string SeparatorColor            = ConsoleUtils.ForegroundColorFromRgb("#D4D4D4");
        private static readonly string NullColor                 = ConsoleUtils.ForegroundColorFromRgb("#569CD6");
        private static readonly string UrlPartsSeparatorsColor   = ConsoleUtils.ForegroundColorFromRgb("#569CFF");

        // methods
        public static string Highlight(string csv) {
            if (string.IsNullOrEmpty(csv)) return csv;
            var result = new List<string>();
            foreach (var line in csv.Split('\n')) {
                result.Add(HighlightLine(line));
            }
            if (result.Count > 0) {
                result[result.Count - 1] = result[result.Count - 1] + ConsoleUtils.ATTRIBUTES_NONE;
            }
            return string.Join("\n", result);
        }
        public static string HighlightLine(string line) {
            if (string.IsNullOrEmpty(line)) return line;

            // Comment line
            if (line[0] == '#')
                return CommentsColor + line + ConsoleUtils.ATTRIBUTES_NONE;

            var result = new StringBuilder(line.Length + 64);
            var i = 0;

            while (i <= line.Length) {
                // Parse one field
                bool quoted = i < line.Length && line[i] == '"';
                string rawField;
                string fieldValue;

                if (quoted) {
                    var j = i + 1;
                    var inner = new StringBuilder();
                    while (j < line.Length) {
                        if (line[j] == '"') {
                            j++;
                            if (j < line.Length && line[j] == '"') { inner.Append('"'); j++; }
                            else break;
                        } else {
                            inner.Append(line[j++]);
                        }
                    }
                    rawField = line.Substring(i, j - i);
                    fieldValue = inner.ToString();
                    i = j;
                } else {
                    var j = i;
                    while (j < line.Length && line[j] != ',') j++;
                    rawField = line.Substring(i, j - i);
                    fieldValue = rawField.Trim();
                    i = j;
                }

                EmitField(result, rawField, fieldValue, quoted);

                if (i < line.Length && line[i] == ',') {
                    result.Append(SeparatorColor).Append(',').Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i++;
                } else {
                    break;
                }
            }

            return result.ToString();
        }

        // helpers
        private static void EmitField(StringBuilder result, string rawField, string fieldValue, bool quoted) {
            // enc: marker
            if (rawField.StartsWith("enc:", StringComparison.Ordinal)) {
                result.Append(DefaultColor).Append("enc:")
                      .Append(EncryptedColor).Append(rawField.Substring(4))
                      .Append(ConsoleUtils.ATTRIBUTES_NONE);
                return;
            }
            if (rawField.StartsWith("${enc:", StringComparison.Ordinal) && rawField.EndsWith("}")) {
                result.Append(DefaultColor).Append("${enc:")
                      .Append(EncryptedColor).Append(rawField.Substring(6, rawField.Length - 7))
                      .Append(DefaultColor).Append('}')
                      .Append(ConsoleUtils.ATTRIBUTES_NONE);
                return;
            }

            var color = GetFieldColor(fieldValue, quoted);

            if (quoted) {
                // Opening quote
                result.Append(StringBackslashColor).Append('"').Append(color);
                // Inner content: highlight escaped double-quotes
                var k = 1;
                while (k < rawField.Length) {
                    if (rawField[k] == '"') {
                        if (k + 1 < rawField.Length && rawField[k + 1] == '"') {
                            result.Append(StringBackslashColor).Append("\"\"").Append(color);
                            k += 2;
                        } else {
                            result.Append(StringBackslashColor).Append('"');
                            k++;
                            break;
                        }
                    } else {
                        result.Append(rawField[k++]);
                    }
                }
                result.Append(ConsoleUtils.ATTRIBUTES_NONE);
            } else if (LooksLikeUrl(fieldValue)) {
                result.Append(color);
                for (var k = 0; k < rawField.Length; k++) {
                    var c = rawField[k];
                    if (":/?&=".IndexOf(c) >= 0) {
                        result.Append(UrlPartsSeparatorsColor).Append(c).Append(color);
                    } else {
                        result.Append(c);
                    }
                }
                result.Append(ConsoleUtils.ATTRIBUTES_NONE);
            } else {
                result.Append(color).Append(rawField).Append(ConsoleUtils.ATTRIBUTES_NONE);
            }
        }

        private static string GetFieldColor(string value, bool quoted) {
            if (quoted) return StringColor;
            if (string.IsNullOrEmpty(value) ||
                string.Equals(value, "null", StringComparison.OrdinalIgnoreCase))
                return NullColor;
            if (string.Equals(value, "true",  StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "yes",   StringComparison.OrdinalIgnoreCase) ||
                string.Equals(value, "no",    StringComparison.OrdinalIgnoreCase))
                return BooleanColor;
            var m = NumberRegex.Match(value);
            if (m.Success && m.Index == 0 && m.Length == value.Length)
                return NumericColor;
            return DefaultColor;
        }

        private static bool LooksLikeUrl(string value) =>
            value.Contains("://") || value.Contains(":?");

    }

}