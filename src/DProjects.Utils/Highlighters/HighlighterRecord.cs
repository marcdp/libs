using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DProjects.Utils.Highlighters { 

    public static class HighlighterRecord {


        // constant
        // regex constants
        private static readonly Regex DateRegex = new(@"^\s*(?<date>\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z)(?=\s|$)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex ErrorRegex = new(@"\bError\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex WarningRegex = new(@"\bWarning\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        // default colors
        private static readonly string DefaultColor = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF");
        private static readonly string TypeColor = ConsoleUtils.ForegroundColorFromRgb("#CCCCCC"); // for the record type name
        private static readonly string HeaderColor = ConsoleUtils.ForegroundColorFromRgb("#7EE787"); // for property names (keys)
        private static readonly string StringColor = ConsoleUtils.ForegroundColorFromRgb("#A5D6FF"); // for strings
        private static readonly string NumericColor = ConsoleUtils.ForegroundColorFromRgb("#B5CEA8"); // for numbers
        private static readonly string DateColor = ConsoleUtils.ForegroundColorFromRgb("#8B949E"); // for an initial UTC timestamp
        // error colors
        private static readonly string ErrorDefaultColor = ConsoleUtils.ForegroundColorFromRgb("#FFB3B3");
        private static readonly string ErrorTypeColor = ConsoleUtils.ForegroundColorFromRgb("#FFE0E0");
        private static readonly string ErrorHeaderColor = ConsoleUtils.ForegroundColorFromRgb("#FF6B6B");
        private static readonly string ErrorStringColor = ConsoleUtils.ForegroundColorFromRgb("#FFC1C1");
        private static readonly string ErrorNumericColor = ConsoleUtils.ForegroundColorFromRgb("#FF8F8F");
        private static readonly string ErrorDateColor = ConsoleUtils.ForegroundColorFromRgb("#D98C8C");

        // warning colors
        private static readonly string WarningDefaultColor = ConsoleUtils.ForegroundColorFromRgb("#FFE0A3");
        private static readonly string WarningTypeColor = ConsoleUtils.ForegroundColorFromRgb("#FFF3D6");
        private static readonly string WarningHeaderColor = ConsoleUtils.ForegroundColorFromRgb("#FFD166");
        private static readonly string WarningStringColor = ConsoleUtils.ForegroundColorFromRgb("#FFE8B6");
        private static readonly string WarningNumericColor = ConsoleUtils.ForegroundColorFromRgb("#FFCA80");
        private static readonly string WarningDateColor = ConsoleUtils.ForegroundColorFromRgb("#C9A96E");

        // background colors
        private static readonly string BackgroundError = ConsoleUtils.BackgroundColorFromRgb("#220000");
        private static readonly string BackgroundWarning = ConsoleUtils.BackgroundColorFromRgb("#332211");
        // methods
        public static string Highlight(string input) {
            // Colorizes lines of the form: Log { Message = SUBSCRIBED INSTRUMENTS: [EUR/USD], Level = Information, StackTrace =  }
            if (string.IsNullOrEmpty(input)) return input;
            var lines = input.Split('\n');
            var result = new StringBuilder(input.Length * 2);
            for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++) {
                var line = lines[lineIndex];
                var isError = ErrorRegex.IsMatch(line);
                var isWarning = !isError && WarningRegex.IsMatch(line);
                var highlightedLine = HighlightLine(line, isError, isWarning);
                if (isError) {
                    AppendWithBackground(result, highlightedLine, BackgroundError);
                } else if (isWarning) {
                    AppendWithBackground(result, highlightedLine, BackgroundWarning);
                } else {
                    result.Append(highlightedLine);
                }
                if (lineIndex < lines.Length - 1) result.Append('\n');
            }
            return result.ToString();
        }

        private static void AppendWithBackground(StringBuilder result, string highlightedLine, string background) {
            result.Append(background)
                  .Append(highlightedLine.Replace(
                      ConsoleUtils.ATTRIBUTES_NONE,
                      ConsoleUtils.ATTRIBUTES_NONE + background))
                  .Append(ConsoleUtils.ATTRIBUTES_NONE);
        }

        private static string HighlightLine(string input, bool isError, bool isWarning) {
            var result = new System.Text.StringBuilder(input.Length * 2);
            var i = 0;
            var length = input.Length;
            var defaultColor = isError ? ErrorDefaultColor : isWarning ? WarningDefaultColor : DefaultColor;
            var typeColor = isError ? ErrorTypeColor : isWarning ? WarningTypeColor : TypeColor;
            var headerColor = isError ? ErrorHeaderColor : isWarning ? WarningHeaderColor : HeaderColor;
            var stringColor = isError ? ErrorStringColor : isWarning ? WarningStringColor : StringColor;
            var numericColor = isError ? ErrorNumericColor : isWarning ? WarningNumericColor : NumericColor;
            var dateColor = isError ? ErrorDateColor : isWarning ? WarningDateColor : DateColor;
            var dateMatch = DateRegex.Match(input);
            if (dateMatch.Success) {
                var date = dateMatch.Groups["date"];
                result.Append(input, 0, date.Index)
                      .Append(dateColor)
                      .Append(date.Value)
                      .Append(ConsoleUtils.ATTRIBUTES_NONE);
                i = date.Index + date.Length;
            }
            while (i < length) {
                var ch = input[i];
                // Record type names (before '{')
                if (char.IsUpper(ch) && (i == 0 || char.IsWhiteSpace(input[i - 1]) || input[i - 1] == '=' || input[i - 1] == ',')) {
                    var typeStart = i;
                    i++;
                    while (i < length && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) {
                        i++;
                    }
                    // Check if followed by whitespace and '{'
                    var lookAhead = i;
                    while (lookAhead < length && char.IsWhiteSpace(input[lookAhead])) {
                        lookAhead++;
                    }
                    if (lookAhead < length && input[lookAhead] == '{') {
                        result.Append(typeColor).Append(input.Substring(typeStart, i - typeStart)).Append(ConsoleUtils.ATTRIBUTES_NONE);
                        continue;
                    }
                    // Not a record type, backtrack
                    i = typeStart;
                }
                // Property names (before '=')
                if (char.IsLetter(ch) || ch == '_') {
                    var propStart = i;
                    i++;
                    while (i < length && (char.IsLetterOrDigit(input[i]) || input[i] == '_')) {
                        i++;
                    }
                    // Check if followed by whitespace and '='
                    var lookAhead = i;
                    while (lookAhead < length && char.IsWhiteSpace(input[lookAhead])) {
                        lookAhead++;
                    }
                    if (lookAhead < length && input[lookAhead] == '=') {
                        result.Append(headerColor).Append(input.Substring(propStart, i - propStart)).Append(ConsoleUtils.ATTRIBUTES_NONE);
                        continue;
                    }
                    // Not a property name, could be a string value or other identifier
                    var identifierValue = input.Substring(propStart, i - propStart);
                    result.Append(stringColor).Append(identifierValue).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    continue;
                }
                // Numbers (including negative, decimal, and scientific notation with comma as decimal separator)
                if (char.IsDigit(ch) || (ch == '-' && i + 1 < length && char.IsDigit(input[i + 1]))) {
                    var numStart = i;
                    i++;
                    while (i < length && (char.IsDigit(input[i]) || input[i] == '.' || input[i] == ',' || input[i] == 'e' || input[i] == 'E' || input[i] == '+' || input[i] == '-')) {
                        i++;
                    }
                    result.Append(numericColor).Append(input.Substring(numStart, i - numStart)).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    continue;
                }
                // Structural characters: {}
                if (ch == '{' || ch == '}') {
                    result.Append(defaultColor).Append(ch).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Separators: =, comma
                if (ch == '=' || ch == ',') {
                    result.Append(defaultColor).Append(ch).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Other characters (whitespace, etc.)
                result.Append(ch);
                i++;
            }
            return result.ToString();
        }

    }

}
