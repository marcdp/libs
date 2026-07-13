using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DProjects.Utils.Highlighters { 

    public static class HighlighterRecord {
       
        // methods
        public static string Highlight(string input) {
            // Colorizes lines of the form: 2026-05-26T10:45:45Z [Level] Message ...
            if (string.IsNullOrEmpty(input)) return input;
            var result = new System.Text.StringBuilder(input.Length * 2);
            var i = 0;
            var length = input.Length;
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
                        result.Append(ConsoleUtils.COLOR_BRMAGENTA).Append(ConsoleUtils.ATTRIBUTES_BOLD).Append(input.Substring(typeStart, i - typeStart)).Append(ConsoleUtils.ATTRIBUTES_NONE);
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
                        result.Append(ConsoleUtils.COLOR_BRCYAN).Append(input.Substring(propStart, i - propStart)).Append(ConsoleUtils.ATTRIBUTES_NONE);
                        continue;
                    }
                    // Not a property name, could be a string value or other identifier
                    var identifierValue = input.Substring(propStart, i - propStart);
                    result.Append(ConsoleUtils.COLOR_GREEN).Append(identifierValue).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    continue;
                }
                // Numbers (including negative, decimal, and scientific notation with comma as decimal separator)
                if (char.IsDigit(ch) || (ch == '-' && i + 1 < length && char.IsDigit(input[i + 1]))) {
                    var numStart = i;
                    i++;
                    while (i < length && (char.IsDigit(input[i]) || input[i] == '.' || input[i] == ',' || input[i] == 'e' || input[i] == 'E' || input[i] == '+' || input[i] == '-')) {
                        i++;
                    }
                    result.Append(ConsoleUtils.COLOR_BRYELLOW).Append(input.Substring(numStart, i - numStart)).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    continue;
                }
                // Structural characters: {}
                if (ch == '{' || ch == '}') {
                    result.Append(ConsoleUtils.COLOR_BRWHITE).Append(ch).Append(ConsoleUtils.ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Separators: =, comma
                if (ch == '=' || ch == ',') {
                    result.Append(ConsoleUtils.COLOR_WHITE).Append(ch).Append(ConsoleUtils.ATTRIBUTES_NONE);
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