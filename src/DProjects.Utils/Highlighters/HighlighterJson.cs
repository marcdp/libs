using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DProjects.Utils.Highlighters { 

    public static class HighlighterJson {


        // consts
        private static readonly Regex NumberRegex = new(@"[+-]?(?:0[xX][0-9a-fA-F_]+|0[oO][0-7_]+|0[bB][01_]+|(?:[0-9][0-9_]*\.[0-9_]*|\.[0-9][0-9_]*)(?:[eE][+-]?[0-9][0-9_]*)?|[0-9][0-9_]*(?:[eE][+-]?[0-9][0-9_]*)?)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly string DefaultColor = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF");
        private static readonly string KeyColor = ConsoleUtils.ForegroundColorFromRgb("#7EE787"); // for property names (keys)
        private static readonly string StringColor = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF"); // for strings
        private static readonly string StringBackslashColor = ConsoleUtils.ForegroundColorFromRgb("#CABA7D"); // for backslash inside strings
        private static readonly string BooleanColor = ConsoleUtils.ForegroundColorFromRgb("#569CD6"); // for boolenas
        private static readonly string NumericColor = ConsoleUtils.ForegroundColorFromRgb("#B5CEA8"); // for numbers
        private static readonly string CommentsColor = ConsoleUtils.ForegroundColorFromRgb("#8B949E"); // comments starting with "//"
        private static readonly string EncryptedColor = ConsoleUtils.ForegroundColorFromRgb("#FF6B6B"); // for values that are encrypted inside strings (e.g., "enc:123456", "http://server?key=${enc:key}")
        private static readonly string BracketColor = ConsoleUtils.ForegroundColorFromRgb("#FFD703"); // for brackets : { } [ ]
        private static readonly string SeparatorColor = ConsoleUtils.ForegroundColorFromRgb("#D4D4D4"); // for comma
        private static readonly string NullColor = ConsoleUtils.ForegroundColorFromRgb("#569CD6"); // for "null"
        private static readonly string UrlPartsSeparatorsColor = ConsoleUtils.ForegroundColorFromRgb("#569CFF"); // for URL parts separators

        // methods
        public static string Highlight(string json) {
            if (string.IsNullOrEmpty(json)) return json;
            var result = new StringBuilder(json.Length * 2);
            var i = 0;
            var length = json.Length;
            while (i < length) {
                var ch = json[i];
                // Comments: // to end of line
                if (ch == '/' && i + 1 < length && json[i + 1] == '/') {
                    var commentStart = i;
                    while (i < length && json[i] != '\n') i++;
                    result.Append(CommentsColor).Append(json.Substring(commentStart, i - commentStart)).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    continue;
                }
                // Strings: keys and string values
                if (ch == '"') {
                    // Determine if it's a key by looking ahead for ':' after the closing quote
                    var lookAhead = i + 1;
                    var escaped = false;
                    while (lookAhead < length) {
                        if (json[lookAhead] == '\\' && !escaped) { escaped = true; lookAhead++; continue; }
                        if (json[lookAhead] == '"' && !escaped) { lookAhead++; break; }
                        escaped = false;
                        lookAhead++;
                    }
                    var afterQuote = lookAhead;
                    while (afterQuote < length && char.IsWhiteSpace(json[afterQuote])) afterQuote++;
                    var isKey = afterQuote < length && json[afterQuote] == ':';
                    var stringColor = isKey ? KeyColor : StringColor;
                    // Emit opening quote
                    result.Append(stringColor).Append('"');
                    i++;
                    escaped = false;
                    while (i < length) {
                        if (json[i] == '\\' && !escaped) {
                            // Emit the backslash + escape char together in StringBackslashColor
                            result.Append(StringBackslashColor).Append('\\');
                            i++;
                            if (i < length) result.Append(json[i++]);
                            result.Append(stringColor);
                            escaped = false;
                            continue;
                        }
                        if (json[i] == '"' && !escaped) {
                            result.Append('"');
                            i++;
                            break;
                        }
                        // enc: marker inside a string value
                        if (!isKey && !escaped && StartsWithAt(json, "enc:", i)) {
                            result.Append(EncryptedColor);
                            while (i < length && json[i] != '"') result.Append(json[i++]);
                            result.Append(stringColor);
                            continue;
                        }
                        escaped = false;
                        result.Append(json[i++]);
                    }
                    result.Append(ConsoleUtils.ATTRIBUTES_NONE);
                    // Colorize URL separators inside non-key string values
                    continue;
                }
                // Numbers
                if (char.IsDigit(ch) || (ch == '-' && i + 1 < length && char.IsDigit(json[i + 1]))) {
                    var numMatch = NumberRegex.Match(json, i);
                    if (numMatch.Success && numMatch.Index == i) {
                        result.Append(NumericColor).Append(numMatch.Value).Append(ConsoleUtils.ATTRIBUTES_NONE);
                        i += numMatch.Length;
                        continue;
                    }
                }
                // Boolean values: true / false
                if (ch == 't' && StartsWithAt(json, "true", i)) {
                    result.Append(BooleanColor).Append("true").Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i += 4;
                    continue;
                }
                if (ch == 'f' && StartsWithAt(json, "false", i)) {
                    result.Append(BooleanColor).Append("false").Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i += 5;
                    continue;
                }
                // Null value
                if (ch == 'n' && StartsWithAt(json, "null", i)) {
                    result.Append(NullColor).Append("null").Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i += 4;
                    continue;
                }
                // Brackets: { } [ ]
                if (ch == '{' || ch == '}' || ch == '[' || ch == ']') {
                    result.Append(BracketColor).Append(ch).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Separators: : ,
                if (ch == ':' || ch == ',') {
                    result.Append(SeparatorColor).Append(ch).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Whitespace and other characters
                result.Append(ch);
                i++;
            }
            return result.ToString();
        }

        private static bool StartsWithAt(string s, string token, int index) =>
            index + token.Length <= s.Length &&
            s.IndexOf(token, index, token.Length, StringComparison.Ordinal) == index;

    }

}