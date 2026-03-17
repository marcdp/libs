

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

    }

}
