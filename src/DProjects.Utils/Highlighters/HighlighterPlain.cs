using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DProjects.Utils.Highlighters { 

    public static class HighlighterPlain {

        //* Sample input data table:
        //*         
        //* field1    field2    field3
        //* --------- --------- ----------
        //* Adx       123       null
        //* Atr       66        http://server?key=${enc:key}
        //* Bollinger null
        //* Candle    hello 
        //* Candle    asdf


        // const
        private static readonly Regex NumberRegex = new(@"[+-]?(?:0[xX][0-9a-fA-F_]+|0[oO][0-7_]+|0[bB][01_]+|(?:[0-9][0-9_]*\.[0-9_]*|\.[0-9][0-9_]*)(?:[eE][+-]?[0-9][0-9_]*)?|[0-9][0-9_]*(?:[eE][+-]?[0-9][0-9_]*)?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly string DefaultColor = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF");
        private static readonly string HeaderColor = ConsoleUtils.ForegroundColorFromRgb("#7EE787"); // for property names (keys)
        private static readonly string StringColor = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF"); // for strings
        private static readonly string StringBackslashColor = ConsoleUtils.ForegroundColorFromRgb("#CABA7D"); // for backslash inside strings
        private static readonly string BooleanColor = ConsoleUtils.ForegroundColorFromRgb("#569CD6"); // for boolenas
        private static readonly string NumericColor = ConsoleUtils.ForegroundColorFromRgb("#B5CEA8"); // for numbers
        private static readonly string EncryptedColor = ConsoleUtils.ForegroundColorFromRgb("#FF6B6B"); // for values that are encrypted inside strings (e.g., "enc:123456", "http://server?key=${enc:key}")
        private static readonly string NullColor = ConsoleUtils.ForegroundColorFromRgb("#569CD6"); // for "null"
        private static readonly string UrlPartsSeparatorsColor = ConsoleUtils.ForegroundColorFromRgb("#569CFF"); // for URL parts separators

        // methods
        public static string Highlight(string input) {
            if (string.IsNullOrEmpty(input)) return input;
            var lines = input.Split('\n');
            var result = new StringBuilder(input.Length + lines.Length * 16);
            // Pre-scan: lines consisting only of '-' and spaces are separators;
            // the line immediately before each separator is the header.
            var separators = new System.Collections.Generic.HashSet<int>();
            var headers    = new System.Collections.Generic.HashSet<int>();
            for (var idx = 0; idx < lines.Length; idx++) {
                var trimmed = lines[idx].TrimEnd('\r');
                if (IsSeparatorLine(trimmed)) {
                    separators.Add(idx);
                    if (idx > 0) headers.Add(idx - 1);
                }
            }
            for (var idx = 0; idx < lines.Length; idx++) {
                var line = lines[idx].TrimEnd('\r');
                if (separators.Contains(idx))
                    result.Append(HighlightSeparator(line));
                else if (headers.Contains(idx))
                    result.Append(HighlightHeader(line));
                else
                    result.Append(HighlightRow(line));
                if (idx < lines.Length - 1) result.Append('\n');
            }
            return result.ToString();
        }

        public static string HighlightHeader(string header) {
            if (string.IsNullOrEmpty(header)) return header;
            var result = new StringBuilder(header.Length + 64);
            var i = 0;
            while (i < header.Length) {
                if (char.IsWhiteSpace(header[i])) { result.Append(header[i++]); continue; }
                var start = i;
                while (i < header.Length && !char.IsWhiteSpace(header[i])) i++;
                result.Append(HeaderColor)
                      .Append(header.Substring(start, i - start))
                      .Append(ConsoleUtils.ATTRIBUTES_NONE);
            }
            return result.ToString();
        }

        public static string HighlightSeparator(string line) {
            if (string.IsNullOrEmpty(line)) return line;
            return StringBackslashColor + line + ConsoleUtils.ATTRIBUTES_NONE;
        }

        public static string HighlightRow(string row) {
            if (string.IsNullOrEmpty(row)) return row;
            var result = new StringBuilder(row.Length + 64);
            var i = 0;
            while (i < row.Length) {
                if (char.IsWhiteSpace(row[i])) { result.Append(row[i++]); continue; }
                var start = i;
                while (i < row.Length && !char.IsWhiteSpace(row[i])) i++;
                EmitToken(result, row.Substring(start, i - start));
            }
            return result.ToString();
        }

        // helpers
        private static void EmitToken(StringBuilder result, string token) {
            if (string.Equals(token, "null", StringComparison.OrdinalIgnoreCase)) {
                result.Append(NullColor).Append(token).Append(ConsoleUtils.ATTRIBUTES_NONE);
                return;
            }
            if (string.Equals(token, "true",  StringComparison.OrdinalIgnoreCase) ||
                string.Equals(token, "false", StringComparison.OrdinalIgnoreCase)) {
                result.Append(BooleanColor).Append(token).Append(ConsoleUtils.ATTRIBUTES_NONE);
                return;
            }
            var m = NumberRegex.Match(token);
            if (m.Success && m.Index == 0 && m.Length == token.Length) {
                result.Append(NumericColor).Append(token).Append(ConsoleUtils.ATTRIBUTES_NONE);
                return;
            }
            if (token.StartsWith("enc:", StringComparison.Ordinal)) {
                result.Append(DefaultColor).Append("enc:")
                      .Append(EncryptedColor).Append(token.Substring(4))
                      .Append(ConsoleUtils.ATTRIBUTES_NONE);
                return;
            }
            if (token.Contains("://") || token.Contains(":?")) {
                EmitUrlToken(result, token);
                return;
            }
            result.Append(StringColor).Append(token).Append(ConsoleUtils.ATTRIBUTES_NONE);
        }

        private static void EmitUrlToken(StringBuilder result, string token) {
            result.Append(StringColor);
            var i = 0;
            while (i < token.Length) {
                // ${enc:...} inline encrypted segment
                if (i + 6 <= token.Length && token.IndexOf("${enc:", i, StringComparison.Ordinal) == i) {
                    var close = token.IndexOf('}', i + 6);
                    if (close != -1) {
                        result.Append(EncryptedColor)
                              .Append(token.Substring(i, close - i + 1))
                              .Append(StringColor);
                        i = close + 1;
                        continue;
                    }
                }
                var c = token[i];
                if (":/?&=".IndexOf(c) >= 0)
                    result.Append(UrlPartsSeparatorsColor).Append(c).Append(StringColor);
                else
                    result.Append(c);
                i++;
            }
            result.Append(ConsoleUtils.ATTRIBUTES_NONE);
        }

        private static bool IsSeparatorLine(string line) {
            if (string.IsNullOrWhiteSpace(line)) return false;
            foreach (var c in line)
                if (c != '-' && c != ' ') return false;
            return true;
        }

    }

}