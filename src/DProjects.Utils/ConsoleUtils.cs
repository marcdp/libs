

namespace DProjects.Utils {


    public static class ConsoleUtils {

        //http://invisible-island.net/xterm/ctlseqs/ctlseqs.html#h2-Controls-beginning-with-ESC
        //http://ascii-table.com/ansi-escape-sequences-vt-100.php

        public const string ESC = "\u001B";
        public const string CSI = ESC + "[";

        public const string RESET = ESC + "c";

        public const string CHARSET_USA_G0 = ESC + "(B";       //Set United States G0 character set

        public const string CURSOR_UP = ESC + "[A";
        public const string CURSOR_UP_LINES = ESC + "[*A";
        public const string CURSOR_DOWN = ESC + "[B";
        public const string CURSOR_DOWN_LINES = ESC + "[*B";
        public const string CURSOR_LEFT = ESC + "[D";
        public const string CURSOR_LEFT_COLUMNS = ESC + "[*D";
        public const string CURSOR_RIGHT = ESC + "[C";
        public const string CURSOR_RIGHT_COLUMNS = ESC + "[*C";

        public const string CURSOR_SAVE = ESC + "[s";
        public const string CURSOR_RESTORE = ESC + "[u";

        public const string CURSOR_SHOW = ESC + "[?25h";
        public const string CURSOR_HIDE = ESC + "[?25l";

        public const string CURSOR_START_BLINK = ESC + "[?12h";
        public const string CURSOR_STOP_BLINK = ESC + "[?12l";

        public const string CURSOR_MOVETO = ESC + "[*;*H";
        public const string CURSOR_MOVETO_1_1 = ESC + "[H";

        public const string BUFFER_ALTERNATE = ESC + "[?1049h";
        public const string BUFFER_MAIN = ESC + "[?1049l";


        public const string ERASE_DISPLAY_FROM_CURSOR_DOWN = ESC + "[J";
        public const string ERASE_DISPLAY_FROM_CURSOR_UP = ESC + "[1J";
        public const string ERASE_DISPLAY = ESC + "[2J";

        public const string ERASE_END_OF_LINE = ESC + "[0K";
        public const string ERASE_END_OF_LINE_ALIAS = ESC + "[K";
        public const string ERASE_START_OF_LINE = ESC + "[1K";
        public const string ERASE_LINE = ESC + "[2K";

        public const string ATTRIBUTES_NONE = ESC + "[0m";
        public const string ATTRIBUTES_BOLD = ESC + "[1m";
        public const string ATTRIBUTES_UNDERSCORE = ESC + "[4m";
        public const string ATTRIBUTES_BLINK = ESC + "[5m";
        public const string ATTRIBUTES_REVERSE = ESC + "[7m";

        public const string REQUEST_REPORT_CURSOR_POSITION = ESC + "[6n";

        public const string COLOR_BLACK = ESC + "[0;30m";
        public const string COLOR_RED = ESC + "[0;31m";
        public const string COLOR_GREEN = ESC + "[0;32m";
        public const string COLOR_YELLOW = ESC + "[0;33m";
        public const string COLOR_BLUE = ESC + "[0;34m";
        public const string COLOR_MAGENTA = ESC + "[0;35m";
        public const string COLOR_CYAN = ESC + "[0;36m";
        public const string COLOR_WHITE = ESC + "[0;37m";

        public const string COLOR_BRBLACK = ESC + "[01;30m";
        public const string COLOR_BRRED = ESC + "[01;31m";
        public const string COLOR_BRGREEN = ESC + "[01;32m";
        public const string COLOR_BRYELLOW = ESC + "[01;33m";
        public const string COLOR_BRBLUE = ESC + "[01;34m";
        public const string COLOR_BRMAGENTA = ESC + "[01;35m";
        public const string COLOR_BRCYAN = ESC + "[01;36m";
        public const string COLOR_BRWHITE = ESC + "[01;37m";

        public const string BGCOLOR_BLACK = ESC + "[40m";
        public const string BGCOLOR_RED = ESC + "[41m";
        public const string BGCOLOR_GREEN = ESC + "[42m";
        public const string BGCOLOR_YELLOW = ESC + "[43m";
        public const string BGCOLOR_BLUE = ESC + "[44m";
        public const string BGCOLOR_MAGENTA = ESC + "[45m";
        public const string BGCOLOR_CYAN = ESC + "[46m";
        public const string BGCOLOR_WHITE = ESC + "[47m";

        public const string BGCOLOR_BRBLACK = ESC + "[1;40m";
        public const string BGCOLOR_BRRED = ESC + "[1;41m";
        public const string BGCOLOR_BRGREEN = ESC + "[1;42m";
        public const string BGCOLOR_BRYELLOW = ESC + "[1;43m";
        public const string BGCOLOR_BRBLUE = ESC + "[1;44m";
        public const string BGCOLOR_BRMAGENTA = ESC + "[1;45m";
        public const string BGCOLOR_BRCYAN = ESC + "[1;46m";
        public const string BGCOLOR_BRWHITE = ESC + "[1;47m";


        // Colorize JSON string with VT sequence codes
        public static string ColorizeJson(string json) {
            if (string.IsNullOrEmpty(json)) return json;
            var result = new System.Text.StringBuilder(json.Length * 2);
            var i = 0;
            var length = json.Length;
            while (i < length) {
                var ch = json[i];
                // String values (keys and string values)
                if (ch == '"') {
                    var stringStart = i;
                    i++;
                    var escaped = false;
                    // Find the end of the string
                    while (i < length) {
                        if (json[i] == '\\' && !escaped) {
                            escaped = true;
                            i++;
                            continue;
                        }
                        if (json[i] == '"' && !escaped) {
                            i++;
                            break;
                        }
                        escaped = false;
                        i++;
                    }
                    // Determine if it's a key or value by looking ahead for ':'
                    var isKey = false;
                    var lookAhead = i;
                    while (lookAhead < length && char.IsWhiteSpace(json[lookAhead])) {
                        lookAhead++;
                    }
                    if (lookAhead < length && json[lookAhead] == ':') {
                        isKey = true;
                    }
                    var stringValue = json.Substring(stringStart, i - stringStart);
                    if (isKey) {
                        result.Append(COLOR_BRCYAN).Append(stringValue).Append(ATTRIBUTES_NONE);
                    } else {
                        result.Append(COLOR_GREEN).Append(stringValue).Append(ATTRIBUTES_NONE);
                    }
                    continue;
                }
                // Numbers (including negative, decimal, and scientific notation)
                if (char.IsDigit(ch) || (ch == '-' && i + 1 < length && char.IsDigit(json[i + 1]))) {
                    var numStart = i;
                    i++;
                    while (i < length && (char.IsDigit(json[i]) || json[i] == '.' || json[i] == 'e' || json[i] == 'E' || json[i] == '+' || json[i] == '-')) {
                        i++;
                    }
                    result.Append(COLOR_BRYELLOW).Append(json.Substring(numStart, i - numStart)).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // Boolean values: true/false
                if ((ch == 't' && i + 3 < length && json.Substring(i, 4) == "true") || (ch == 'f' && i + 4 < length && json.Substring(i, 5) == "false")) {
                    var boolLength = ch == 't' ? 4 : 5;
                    result.Append(COLOR_MAGENTA).Append(json.Substring(i, boolLength)).Append(ATTRIBUTES_NONE);
                    i += boolLength;
                    continue;
                }
                // Null value
                if (ch == 'n' && i + 3 < length && json.Substring(i, 4) == "null") {
                    result.Append(COLOR_RED).Append("null").Append(ATTRIBUTES_NONE);
                    i += 4;
                    continue;
                }
                // Structural characters: {}[],:
                if (ch == '{' || ch == '}' || ch == '[' || ch == ']') {
                    result.Append(COLOR_BRWHITE).Append(ch).Append(ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                if (ch == ':' || ch == ',') {
                    result.Append(COLOR_WHITE).Append(ch).Append(ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Whitespace and other characters
                result.Append(ch);
                i++;
            }
            return result.ToString();
        }
        // Colorize YAML string with VT sequence codes
        public static string ColorizeYaml(string yaml) {
            if (string.IsNullOrEmpty(yaml)) return yaml;
            var result = new System.Text.StringBuilder(yaml.Length * 2);
            var i = 0;
            var length = yaml.Length;
            while (i < length) {
                var ch = yaml[i];
                // Comments: # to end of line
                if (ch == '#') {
                    var commentStart = i;
                    while (i < length && yaml[i] != '\n') {
                        i++;
                    }
                    result.Append(COLOR_BRBLACK).Append(yaml.Substring(commentStart, i - commentStart)).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // Document markers: --- and ...
                if ((ch == '-' || ch == '.') && i + 2 < length && yaml[i + 1] == ch && yaml[i + 2] == ch && (i + 3 >= length || yaml[i + 3] == '\n' || yaml[i + 3] == '\r' || yaml[i + 3] == ' ')) {
                    result.Append(COLOR_BRWHITE).Append(ATTRIBUTES_BOLD).Append(yaml.Substring(i, 3)).Append(ATTRIBUTES_NONE);
                    i += 3;
                    continue;
                }
                // Quoted strings (single or double)
                if (ch == '"' || ch == '\'') {
                    var quote = ch;
                    var stringStart = i;
                    i++;
                    while (i < length) {
                        if (yaml[i] == '\\' && quote == '"' && i + 1 < length) {
                            i += 2;
                            continue;
                        }
                        if (yaml[i] == quote) {
                            i++;
                            break;
                        }
                        i++;
                    }
                    result.Append(COLOR_GREEN).Append(yaml.Substring(stringStart, i - stringStart)).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // Keys: identifier or quoted text followed by ':'
                if (char.IsLetter(ch) || ch == '_') {
                    var keyStart = i;
                    i++;
                    while (i < length && (char.IsLetterOrDigit(yaml[i]) || yaml[i] == '_' || yaml[i] == '-' || yaml[i] == '.')) {
                        i++;
                    }
                    var lookAhead = i;
                    while (lookAhead < length && yaml[lookAhead] == ' ') {
                        lookAhead++;
                    }
                    var token = yaml.Substring(keyStart, i - keyStart);
                    if (lookAhead < length && yaml[lookAhead] == ':') {
                        // YAML key
                        result.Append(COLOR_BRCYAN).Append(token).Append(ATTRIBUTES_NONE);
                        continue;
                    }
                    // Boolean values: true/false/yes/no/on/off
                    var lower = token.ToLower();
                    if (lower == "true" || lower == "false" || lower == "yes" || lower == "no" || lower == "on" || lower == "off") {
                        result.Append(COLOR_MAGENTA).Append(token).Append(ATTRIBUTES_NONE);
                        continue;
                    }
                    // Null values: null/~
                    if (lower == "null") {
                        result.Append(COLOR_RED).Append(token).Append(ATTRIBUTES_NONE);
                        continue;
                    }
                    // Plain string value
                    result.Append(COLOR_GREEN).Append(token).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // Null shorthand: ~
                if (ch == '~' && (i + 1 >= length || char.IsWhiteSpace(yaml[i + 1]) || yaml[i + 1] == '\n')) {
                    result.Append(COLOR_RED).Append('~').Append(ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Numbers (including negative, decimal, scientific notation)
                if (char.IsDigit(ch) || (ch == '-' && i + 1 < length && char.IsDigit(yaml[i + 1]))) {
                    var numStart = i;
                    i++;
                    while (i < length && (char.IsDigit(yaml[i]) || yaml[i] == '.' || yaml[i] == 'e' || yaml[i] == 'E' || yaml[i] == '+' || yaml[i] == '-' || yaml[i] == '_')) {
                        i++;
                    }
                    result.Append(COLOR_BRYELLOW).Append(yaml.Substring(numStart, i - numStart)).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // List item marker: '- '
                if (ch == '-' && i + 1 < length && yaml[i + 1] == ' ') {
                    result.Append(COLOR_BRWHITE).Append('-').Append(ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Structural characters: {}[]
                if (ch == '{' || ch == '}' || ch == '[' || ch == ']') {
                    result.Append(COLOR_BRWHITE).Append(ch).Append(ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Separators: :, ,
                if (ch == ':' || ch == ',') {
                    result.Append(COLOR_WHITE).Append(ch).Append(ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Anchors: &name
                if (ch == '&') {
                    var anchorStart = i;
                    i++;
                    while (i < length && !char.IsWhiteSpace(yaml[i])) {
                        i++;
                    }
                    result.Append(COLOR_BRBLUE).Append(yaml.Substring(anchorStart, i - anchorStart)).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // Aliases: *name
                if (ch == '*') {
                    var aliasStart = i;
                    i++;
                    while (i < length && !char.IsWhiteSpace(yaml[i])) {
                        i++;
                    }
                    result.Append(COLOR_BRBLUE).Append(yaml.Substring(aliasStart, i - aliasStart)).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // Other characters (whitespace, newlines, pipes, etc.)
                result.Append(ch);
                i++;
            }
            return result.ToString();
        }
        public static string ColorizeRecord(string input) {
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
                        result.Append(COLOR_BRMAGENTA).Append(ATTRIBUTES_BOLD).Append(input.Substring(typeStart, i - typeStart)).Append(ATTRIBUTES_NONE);
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
                        result.Append(COLOR_BRCYAN).Append(input.Substring(propStart, i - propStart)).Append(ATTRIBUTES_NONE);
                        continue;
                    }
                    // Not a property name, could be a string value or other identifier
                    var identifierValue = input.Substring(propStart, i - propStart);
                    result.Append(COLOR_GREEN).Append(identifierValue).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // Numbers (including negative, decimal, and scientific notation with comma as decimal separator)
                if (char.IsDigit(ch) || (ch == '-' && i + 1 < length && char.IsDigit(input[i + 1]))) {
                    var numStart = i;
                    i++;
                    while (i < length && (char.IsDigit(input[i]) || input[i] == '.' || input[i] == ',' || input[i] == 'e' || input[i] == 'E' || input[i] == '+' || input[i] == '-')) {
                        i++;
                    }
                    result.Append(COLOR_BRYELLOW).Append(input.Substring(numStart, i - numStart)).Append(ATTRIBUTES_NONE);
                    continue;
                }
                // Structural characters: {}
                if (ch == '{' || ch == '}') {
                    result.Append(COLOR_BRWHITE).Append(ch).Append(ATTRIBUTES_NONE);
                    i++;
                    continue;
                }
                // Separators: =, comma
                if (ch == '=' || ch == ',') {
                    result.Append(COLOR_WHITE).Append(ch).Append(ATTRIBUTES_NONE);
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
