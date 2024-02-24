using System;
using System.IO;
using System.Text;


namespace DProjects.Utils {


    public static class JavascriptUtils {

        //methods
        public static string RemoveComments(string js) {
            StringBuilder result = new StringBuilder();
            bool insideDoubleQuotes = false;
            bool insideSingleQuotes = false;
            bool insideLineComment = false;
            bool insideRegularExpression = false;
            for (int i = 0; i <= js.Length - 1; i++) {
                char cPrev = i > 0 ? (js[i - 1]) : ' ';
                char c = js[i];
                char cNext = i < js.Length - 1 ? (js[i + 1]) : ' ';
                if (c == CharUtils.CHAR_LF) {
                    insideLineComment = false;
                } else if (c == StringUtils.AscW('\'')) {
                    if (!insideSingleQuotes && !insideRegularExpression && !insideLineComment) {
                        if (!insideDoubleQuotes) {
                            if (cPrev == StringUtils.AscW('\\')) {
                            } else {
                                insideDoubleQuotes = true;
                            }
                        } else {
                            insideDoubleQuotes = false;
                        }
                    }
                } else if (c == StringUtils.AscW('\'')) {
                    if (!insideDoubleQuotes && !insideRegularExpression && !insideLineComment) {
                        if (!insideSingleQuotes) {
                            if (cPrev == StringUtils.AscW('\\')) {
                            } else {
                                insideSingleQuotes = true;
                            }
                        } else {
                            insideSingleQuotes = false;
                        }
                    }
                } else if (cPrev != StringUtils.AscW('\\') && c == StringUtils.AscW('/') && cNext != StringUtils.AscW('/') && !insideSingleQuotes && !insideDoubleQuotes && !insideLineComment) {
                    insideRegularExpression = !insideRegularExpression;
                } else if (cPrev != StringUtils.AscW('\\') && c == StringUtils.AscW('/') && cNext == StringUtils.AscW('/') && !insideSingleQuotes && !insideDoubleQuotes) {
                    insideLineComment = true;
                }
                if (!insideLineComment) {
                    result.Append(c);
                }
            }
            return result.ToString();
        }
        public static string RemoveEmptyLines(string text) {
            StringBuilder result = new StringBuilder();
            foreach (string line in text.Replace(CharUtils.CHAR_CR.ToString(), "").Split(CharUtils.CHAR_LF)) {
                if (line.Trim().Length == 0) {
                } else {
                    result.AppendLine(line);
                }
            }
            return result.ToString();
        }
        public static string Minify(string js) {
            var jsMin = new JSMin();
            var aa = new MemoryStream();
            var destination = new StreamWriter(aa);
            jsMin.Minify(new StreamReader(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(js))), destination);
            destination.Dispose();
            var result = System.Text.Encoding.UTF8.GetString(aa.ToArray());
            return result;
        }


        //inner class
        class JSMin {
            const int EOF = -1;

            StreamReader? sr;
            StreamWriter? sw;

            int theA;
            int theB;
            int theLookahead = EOF;

            public void Minify(StreamReader[] readers, string dst) {
                sw = new StreamWriter(dst);
                for (int i = 0; i < readers.Length; i++) {
                    using (sr = readers[i]) {
                        jsmin();
                    }
                }
                sw.Close();
            }

            public void Minify(StreamReader reader, StreamWriter writer) {
                using (sr = reader) {
                    using (sw = writer) {
                        jsmin();
                    }
                }
            }

            public void Minify(string instance, string dst) {
                this.Minify(new StreamReader(instance), new StreamWriter(dst));
            }

            /* jsmin -- Copy the input to the output, deleting the characters which are
                    insignificant to JavaScript. Comments will be removed. Tabs will be
                    replaced with spaces. Carriage returns will be replaced with linefeeds.
                    Most spaces and linefeeds will be removed.
            */
            void jsmin() {
                theA = '\n';
                action(3);
                while (theA != EOF) {
                    switch (theA) {
                        case ' ': {
                                if (isAlphaNumeric(theB)) {
                                    action(1);
                                } else {
                                    action(2);
                                }
                                break;
                            }
                        case '\n': {
                                switch (theB) {
                                    case '{':
                                    case '[':
                                    case '(':
                                    case '+':
                                    case '-': {
                                            action(1);
                                            break;
                                        }
                                    case ' ': {
                                            action(3);
                                            break;
                                        }
                                    default: {
                                            if (isAlphaNumeric(theB)) {
                                                action(1);
                                            } else {
                                                action(2);
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                        default: {
                                switch (theB) {
                                    case ' ': {
                                            if (isAlphaNumeric(theA)) {
                                                action(1);
                                                break;
                                            }
                                            action(3);
                                            break;
                                        }
                                    case '\n': {
                                            switch (theA) {
                                                case '}':
                                                case ']':
                                                case ')':
                                                case '+':
                                                case '-':
                                                case '"':
                                                case '\'': {
                                                        action(1);
                                                        break;
                                                    }
                                                default: {
                                                        if (isAlphaNumeric(theA)) {
                                                            action(1);
                                                        } else {
                                                            action(3);
                                                        }
                                                        break;
                                                    }
                                            }
                                            break;
                                        }
                                    default: {
                                            action(1);
                                            break;
                                        }
                                }
                                break;
                            }
                    }
                }
            }
            /* action -- do something! What you do is determined by the argument:
                    1   Output A. Copy B to A. Get the next B.
                    2   Copy B to A. Get the next B. (Delete A).
                    3   Get the next B. (Delete B).
               action treats a string as a single character. Wow!
               action recognizes a regular expression if it is preceded by ( or , or =.
            */
            void action(int d) {
                if (d <= 1) {
                    put(theA);
                }
                if (d <= 2) {
                    theA = theB;
                    if (theA == '\'' || theA == '"') {
                        for (; ; )
                        {
                            put(theA);
                            theA = get();
                            if (theA == theB) {
                                break;
                            }
                            if (theA <= '\n') {
                                throw new Exception(string.Format("Error: JSMIN unterminated string literal: {0}\n", theA));
                            }
                            if (theA == '\\') {
                                put(theA);
                                theA = get();
                            }
                        }
                    }
                }
                if (d <= 3) {
                    theB = next();
                    if (theB == '/' && (theA == '(' || theA == ',' || theA == '=' ||
                                        theA == '[' || theA == '!' || theA == ':' ||
                                        theA == '&' || theA == '|' || theA == '?' ||
                                        theA == '{' || theA == '}' || theA == ';' ||
                                        theA == '\n')) {
                        put(theA);
                        put(theB);
                        for (; ; )
                        {
                            theA = get();
                            if (theA == '/') {
                                break;
                            } else if (theA == '\\') {
                                put(theA);
                                theA = get();
                            } else if (theA <= '\n') {
                                throw new Exception(string.Format("Error: JSMIN unterminated Regular Expression literal : {0}.\n", theA));
                            }
                            put(theA);
                        }
                        theB = next();
                    }
                }
            }
            /* next -- get the next character, excluding comments. peek() is used to see
                    if a '/' is followed by a '/' or '*'.
            */
            int next() {
                int c = get();

                if (c == '/') {
                    switch (peek()) {
                        case '/': {
                                for (; ; )
                                {
                                    c = get();
                                    if (c <= '\n') {
                                        return c;
                                    }
                                }
                            }
                        case '*': {
                                get();
                                for (; ; )
                                {
                                    switch (get()) {
                                        case '*': {
                                                if (peek() == '/') {
                                                    get();
                                                    return ' ';
                                                }
                                                break;
                                            }
                                        case EOF: {
                                                throw new Exception("Error: JSMIN Unterminated comment.\n");
                                            }
                                    }
                                }
                            }
                        default: {
                                return c;
                            }
                    }
                }

                return c;
            }
            /* peek -- get the next character without getting it.
            */
            int peek() {
                theLookahead = get();

                return theLookahead;
            }
            /* get -- return the next character from stdin. Watch out for lookahead. If
                    the character is a control character, translate it to a space or
                    linefeed.
            */
            int get() {
                int c = theLookahead;
                theLookahead = EOF;

                if (c == EOF && sr != null) {
                    c = sr.Read();
                }
                if (c >= ' ' || c == '\n' || c == EOF) {
                    return c;
                }
                if (c == '\r') {
                    return '\n';
                }

                return ' ';
            }
            void put(int c) {
                if (sw != null) sw.Write((char)c);
            }
            /* isAlphaNumeric -- return true if the character is a letter, digit, underscore,
                    dollar sign, or non-ASCII character.
            */
            bool isAlphaNumeric(int c) {
                return ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') ||
                    (c >= 'A' && c <= 'Z') || c == '_' || c == '$' || c == '\\' ||
                    c > 126);
            }
        }

    }


}


