using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;


namespace DProjects.Utils {


    public static class HtmlUtils {

        //constants
        public const string DOCTYPE_HTML401Strict = "<!DOCTYPE HTML Public \"-//W3C//DTD HTML 4.01//EN\" \"http://www.w3.org/TR/html4/strict.dtd\">";
        public const string DOCTYPE_HTML401Transitional = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Transitional//EN\" \"http://www.w3.org/TR/html4/loose.dtd\">";
        public const string DOCTYPE_HTML401Frameset = "<!DOCTYPE HTML PUBLIC \"-//W3C//DTD HTML 4.01 Frameset//EN\" \"http://www.w3.org/TR/html4/frameset.dtd\">";
        public const string DOCTYPE_XHTML10Strict = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">";
        public const string DOCTYPE_XHTML10Transitional = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">";
        public const string DOCTYPE_XHTML10Frameset = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Frameset//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-frameset.dtd\">";
        public const string DOCTYPE_XHTML11 = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.1//EN\" \"http://www.w3.org/TR/xhtml11/DTD/xhtml11.dtd\">";
        public const string DOCTYPE_XHTMLBasic11 = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML Basic 1.1//EN\" \"http://www.w3.org/TR/xhtml-basic/xhtml-basic11.dtd\">";
        public const string DOCTYPE_XHTML5 = "<!DOCTYPE html>";

        //inner class
        private class CharReader {
            private char[] mValue;
            private int mOffset;
            public CharReader(string value) {
                mValue = value.ToCharArray();
                mOffset = -1;
            }
            public char PrevPrev => (mOffset > 0 + 1 ? mValue[mOffset - 1 - 1] : default(char));
            public char Prev => (mOffset > 0 ? mValue[mOffset - 1] : default(char));
            public char Current => (mOffset >= 0 && mOffset < mValue.Length ? mValue[mOffset] : default(char));
            public char Next => (mOffset < mValue.Length - 1 ? mValue[mOffset + 1] : default(char));
            public char NextNext => (mOffset < mValue.Length - 1 - 1 ? mValue[mOffset + 1 + 1] : default(char));
            public char NextNextNext => (mOffset < mValue.Length - 1 - 1 - 1 ? mValue[mOffset + 1 + 1 + 1] : default(char));
            public char Read() {
                if (mOffset == mValue.Length - 1) return default(char);
                mOffset++;
                return mValue[mOffset];
            }
        }

        private class CharWriter {
            private StringBuilder mValue;
            public CharWriter() {
                mValue = new StringBuilder();
            }
            public void Write(char c) {
                mValue.Append(c);
            }
            public void TrimRightSpaces() {
                do {
                    var c = mValue.ToString(mValue.Length - 1, 1);
                    if (!c.Equals(" ")) break;
                    mValue.Remove(mValue.Length - 1, 1);
                } while (true);

            }
            public override string ToString() {
                return mValue.ToString();
            }
        }

        //methods
        public static string MinifyHtml(string html) {
            var input = new CharReader(html);
            var output = new CharWriter();
            ProcessRaw(input, output, "");
            return output.ToString();
        }
        private static void ProcessRaw(CharReader input, CharWriter output, string tag) {
            var tagPre = (tag.Equals("pre"));
            var tagInline = HtmlUtils.IsInlineHtmlElement(tag);
            var tagBlock = !tagPre && !tagInline;
            do {
                var c = input.Read();
                if (c == default(char)) break;
                if (c == '<' && input.Next == '!' && input.NextNext == '-' && input.NextNextNext == '-') {
                    ProcessComment(input, output);
                } else if (c == '<' && input.Next == '!') {
                    ProcessDocType(input, output);
                } else if (c == '<' && input.Next == '/') {
                    ProcessNodeClose(input, output);
                } else if (c == '<') {
                    ProcessNodeOpen(input, output);
                } else {
                    if (tagPre) {
                        output.Write(c);
                    } else if (tagInline) {
                        if (char.IsWhiteSpace(c)) c = ' ';
                        if (char.IsWhiteSpace(c) && char.IsWhiteSpace(input.Prev)) continue;
                        output.Write(c);
                    } else {
                        if (char.IsWhiteSpace(c)) c = ' ';
                        if (char.IsWhiteSpace(c) && char.IsWhiteSpace(input.Prev)) continue;
                        output.Write(c);
                    }
                }
            } while (true);
        }
        private static void ProcessDocType(CharReader input, CharWriter output) {
            output.Write(input.Current);
            do {
                var c = input.Read();
                if (c == default(char)) break;
                if (char.IsWhiteSpace(c)) c = ' ';
                if (char.IsWhiteSpace(c) && char.IsWhiteSpace(input.Prev)) continue;
                if (c == '>') output.TrimRightSpaces();
                output.Write(c);
                if (c == '>') break;
            } while (true);
        }
        private static void ProcessComment(CharReader input, CharWriter output) {
            do {
                var c = input.Read();
                if (c == default(char)) break;
                if (input.PrevPrev == '-' && input.Prev == '-' && c == '>') {
                    break;
                }
            } while (true);
        }
        private static void ProcessNodeOpen(CharReader input, CharWriter output) {
            output.Write(input.Current);
            var nodeName = new StringBuilder();
            var insideNodeName = true;
            do {
                var c = input.Read();
                if (c == default(char)) break;
                if (char.IsWhiteSpace(c)) {
                    c = ' ';
                    insideNodeName = false;
                }
                if (char.IsWhiteSpace(c) && char.IsWhiteSpace(input.Prev)) continue;
                if (c == '\'' || c == '"') {
                    ProcessNodeAttribute(input, output);
                    continue;
                }
                if (c == '>') {
                    output.TrimRightSpaces();
                    insideNodeName = false;
                }
                output.Write(c);
                if (insideNodeName) nodeName.Append(c);
                if (c == '>') {
                    if (input.Prev == '/') {
                        return;
                    }
                    if (nodeName.ToString().StartsWith("/")) {
                        return;
                    }
                    break;
                }
            } while (true);
            ProcessRaw(input, output, nodeName.ToString());
        }
        private static void ProcessNodeAttribute(CharReader input, CharWriter output) {
            var openChar = input.Current;
            output.Write(input.Current);
            do {
                var c = input.Read();
                if (c == default(char)) break;
                if (c == '\\') {
                    output.Write(c);
                    c = input.Read();
                    output.Write(c);
                    continue;
                }
                output.Write(c);
                if (c == openChar) break;
            } while (true);
        }
        private static void ProcessNodeClose(CharReader input, CharWriter output) {
            output.Write(input.Current);
            do {
                var c = input.Read();
                if (c == default(char)) break;
                if (char.IsWhiteSpace(c)) continue;
                output.Write(c);
                if (c == '>') break;
            } while (true);
        }




        //utils
        public static string Beautify(string html, string tab = "    ") {
            StringBuilder aux = new StringBuilder(html.Length);
            bool insideTag = false;
            bool insideTagQuotes = false;
            //split 
            for (int i = 0; i <= html.Length - 1; i++) {
                char c = html[i];
                char cPrev = ' ';
                if (i > 0) {
                    cPrev = html[i - 1];
                }
                if (c == '<') {
                    if (insideTag) {
                        if (insideTagQuotes) {
                            aux.Append("&lt;");
                        } else {
                            aux.Append(c);
                        }
                    } else {
                        insideTag = true;
                        aux.AppendLine();
                        aux.Append(c);
                    }
                } else if (c == '>') {
                    if (cPrev == ']') {
                        aux.Append(c);
                    } else if (insideTagQuotes) {
                        aux.Append("&gt;");
                    } else {
                        insideTag = false;
                        aux.Append(c);
                        aux.AppendLine();
                    }
                } else if (insideTag) {
                    if (c == CharUtils.CHAR_LF || c == CharUtils.CHAR_CR) {
                        aux.Append(' ');
                    } else if (!insideTagQuotes && (c == ' ' || c == CharUtils.CHAR_TAB) && (cPrev == ' ' || cPrev == CharUtils.CHAR_TAB)) {

                    } else if (c == '\'') {
                        insideTagQuotes = !insideTagQuotes;
                        aux.Append(c);
                    } else {
                        aux.Append(c);
                    }
                } else if (c == CharUtils.CHAR_CR) {
                } else {
                    aux.Append(c);
                }
            }
            //compress lines
            int level = 0;
            StringBuilder aux2 = new StringBuilder(html.Length);
            bool previousAvoidAppendLine = false;
            foreach (string lineToUse in aux.ToString().Split(CharUtils.CHAR_LF)) {
                string line = lineToUse.Trim();
                if (line.Length > 0) {
                    bool avoidAppendLine = false;
                    if (line.StartsWith("<textarea")) {
                        avoidAppendLine = true;
                    }
                    if (line.EndsWith("/>")) {
                        aux2.Append(StringUtils.Space(level, tab)).Append(line);
                    } else if (line.StartsWith("</")) {
                        level--;
                        if (level < 0) {
                            level = 0;
                        }
                        if (!previousAvoidAppendLine) {
                            aux2.Append(StringUtils.Space(level, tab));
                        }
                        aux2.Append(line);
                    } else if (line.StartsWith("<!--")) {
                        aux2.Append(StringUtils.Space(level, tab)).Append(line);
                    } else if (line.StartsWith("<!")) {
                        aux2.Append(StringUtils.Space(level, tab)).Append(line);
                    } else if (line.StartsWith("<")) {
                        aux2.Append(StringUtils.Space(level, tab)).Append(line);
                        level++;
                    } else {
                        aux2.Append(StringUtils.Space(level, tab)).Append(line);
                    }
                    if (!avoidAppendLine) {
                        aux2.AppendLine();
                    }
                    previousAvoidAppendLine = avoidAppendLine;
                }
            }
            //
            string result = aux2.ToString();
            result = result.Replace(" />", "/>");
            result = result.Replace(" >", ">");
            //return result
            return result;
        }
        public static string RemoveHTMLComments(string html) {
            int i = 0;
            i = html.IndexOf("<!--");
            while (i != -1 && i < html.Length) {
                int j = html.IndexOf("-->", i);
                if (i != -1 && j != -1 && j > i) {
                    html = html.Remove(i, j - i + 4 - 1);
                    i = html.IndexOf("<!--", i);
                } else {
                    i = -1;
                }
            }
            return html;
        }
        public static string RemoveHTMLTags(string html) {
            StringBuilder sb = new StringBuilder();
            if (html.IndexOf("<") != -1) {
                string[] parts = html.Split('<');
                foreach (string part in parts) {
                    if (part.IndexOf(">") != -1) {
                        sb.Append(part.Substring(part.IndexOf(">") + 1) + " ");
                    } else {
                        sb.Append(part + " ");
                    }
                }
                return sb.ToString();
            } else {
                return html;
            }
        }
        public static string? GetAttributeValueFromTag(string html, string attribute) {
            var i = html.IndexOf(" " + attribute + "=");
            if (i == -1) i = html.IndexOf("\t" + attribute + "=");
            if (i == -1) i = html.IndexOf("\n" + attribute + "=");
            if (i == -1) return null;
            var s = html.IndexOf('"', i);
            if (s == -1) return null;
            var e = html.IndexOf('"', s+1);
            var result = html.Substring(s+1, e - s-1);
            return result;
        }
        public static string? GetAttributeValue(string html, string tag, string attribute) {
            int i = html.IndexOf("<" + tag, 0, StringComparison.OrdinalIgnoreCase);
            if (i == -1) return null;
            int j = html.IndexOf(">", i);
            if (j == -1) return null;
            var aux = html.Substring(i, j-i+1);
            return GetAttributeValueFromTag(aux, attribute);
        }
        public static IDictionary<string, string> GetMetaNameContentTags(string html) {
            var result = new Dictionary<string, string>();
            int i = 0;
            do {
                i = html.IndexOf("<meta", i, StringComparison.OrdinalIgnoreCase);
                if (i == -1) break;
                var j = html.IndexOf(">", i);
                if (j == -1) break;
                var aux = html.Substring(i, j - i + 1);
                i = j;
                var name = GetAttributeValueFromTag(aux, "name");
                var content = GetAttributeValueFromTag(aux, "content");
                if (name != null && content != null) result[name] = content;

            } while (true);
            return result;
        }
        public static string GetHTMLTagContents(string html, string tag) {
            string result = "";
            if (html != null) {
                int i = html.IndexOf("<" + tag, 0, StringComparison.OrdinalIgnoreCase);
                if (i != -1) {
                    i = html.IndexOf(">", i) + 1;
                    int j = html.IndexOf("/" + tag, StringComparison.OrdinalIgnoreCase);
                    if (j == -1) {
                        result = html.Substring(i);
                    } else {
                        result = html.Substring(i, j - i - 1);
                    }
                }
            }
            return result;
        }
        public static string IncrementHeaders(string html) {
            for (var i = 5; i > 1; i--) {
                html = DProjects.Utils.StringUtils.ReplaceCaseInsensitive(html, "<h" + (i-1), "<h" + i);
                html = DProjects.Utils.StringUtils.ReplaceCaseInsensitive(html, "</h" + (i - 1), "</h" + i);
            }
            return html;
        }
        public static string ConvertHtmlToText(string html) {
            string text = html;
            if (text.IndexOf("<body") != -1) {
                text = GetHTMLTagContents(text, "body");
            }
            if (text.IndexOf("<main") != -1) {
                text = GetHTMLTagContents(text, "main");
            }
            text = text.Replace(CharUtils.CHAR_CR, ' ');
            text = text.Replace(CharUtils.CHAR_LF, ' ');
            text = text.Replace(Environment.NewLine, " ");
            text = text.Replace("<br/>", Environment.NewLine).Replace("</p>", Environment.NewLine + Environment.NewLine).Replace("<br>", Environment.NewLine).Replace("<br />", Environment.NewLine).Replace("<p />", Environment.NewLine + Environment.NewLine);
            text = text.Replace("<BR/>", Environment.NewLine).Replace("</P>", Environment.NewLine + Environment.NewLine).Replace("<BR>", Environment.NewLine).Replace("<BR />", Environment.NewLine).Replace("<P />", Environment.NewLine + Environment.NewLine);
            text = text.Replace("<h1", Environment.NewLine + "<h1").Replace("<h2", Environment.NewLine + "<h2").Replace("<h3", Environment.NewLine + "<h3");
            text = text.Replace("<H1", Environment.NewLine + "<H1").Replace("<H2", Environment.NewLine + "<H2").Replace("<H3", Environment.NewLine + "<H3");
            text = text.Replace("</h1>", Environment.NewLine).Replace("</h2>", Environment.NewLine).Replace("</h3>", Environment.NewLine).Replace("</h4>", Environment.NewLine).Replace("</h5>", Environment.NewLine);
            text = text.Replace("</H1>", Environment.NewLine).Replace("</H2>", Environment.NewLine).Replace("</H3>", Environment.NewLine).Replace("</H4>", Environment.NewLine).Replace("</H5>", Environment.NewLine);
            text = text.Replace("<ul>", "").Replace("<li>", Environment.NewLine + " - ");
            text = text.Replace("<UL>", "").Replace("<LI>", Environment.NewLine + " - ");
            text = Regex.Replace(text, "<li.?>", Environment.NewLine + " - ");
            text = Regex.Replace(text, "<LI.?>", Environment.NewLine + " - ");
            text = text.Replace("</tr>", Environment.NewLine);
            text = text.Replace("</TR>", Environment.NewLine);
            text = text.Replace(CharUtils.CHAR_TAB.ToString(), "");
            text = System.Text.RegularExpressions.Regex.Replace(text, "( )+", " ");
            int i = text.IndexOf("<script", System.StringComparison.OrdinalIgnoreCase);
            while (i != -1 && i < text.Length) {
                int j = text.IndexOf("</script>", i, System.StringComparison.OrdinalIgnoreCase);
                if (i != -1 && j != -1 && j > i) {
                    text = text.Remove(i, j - i + 8 + 1);
                    i = j + 8;
                    i = text.IndexOf("<script", System.StringComparison.OrdinalIgnoreCase);
                } else {
                    i = -1;
                }
            }
            i = text.IndexOf("<style", System.StringComparison.OrdinalIgnoreCase);
            while (i != -1 && i < text.Length) {
                int j = text.IndexOf("</style>", System.StringComparison.OrdinalIgnoreCase);
                if (i != -1 && j != -1 && j > i) {
                    text = text.Remove(i, j - i + 7 + 1);
                    i = j + 7;
                    i = text.IndexOf("<style", System.StringComparison.OrdinalIgnoreCase);
                } else {
                    i = -1;
                }
            }
            i = text.IndexOf("<!--");
            while (i != -1 && i < text.Length) {
                int j = text.IndexOf("-->", i);
                if (i != -1 && j != -1) {
                    text = text.Remove(i, j - i + 3);
                    i = text.IndexOf("<!--", i);
                } else {
                    i = -1;
                }
            }
            text = RemoveHTMLTags(text);
            text = text.Replace(CharUtils.CHAR_TAB, ' ');
            text = text.Replace("&nbsp;", " ");
            text = Regex.Replace(text, "( )+", " ");
            text = Regex.Replace(text, "\n ", "\n");
            text = Regex.Replace(text, "\n\n", "\n");
            return text;
        }
        public static string HtmlDecode(string html) {
            return System.Net.WebUtility.HtmlDecode(html);
        }
        public static string HtmlEncode(string html) {
            return System.Net.WebUtility.HtmlEncode(html);
        }
        public static string[] ExtractLinks(string html, string uri) {
            var links = new List<string>();
            var basepath = "";
            if (uri != null) {
                if (uri.EndsWith("/")) {
                    basepath = uri;
                } else {
                    basepath = PathUtils.GetPathParent(uri);
                }
            }
            //base
            Regex regEx = new Regex("<base.*href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            Match match = regEx.Match(html);
            while (match.Success) {
                basepath = match.Groups[1].ToString().Trim().Replace("\'", "");
                match = match.NextMatch();
            }
            //link
            regEx = new Regex("<link.*href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                links.Add(match.Groups[1].ToString().Trim().Replace("\'", ""));
                match = match.NextMatch();
            }
            //script
            regEx = new Regex("<script.*src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                if (match.Groups[0].ToString().IndexOf('>') != -1) {
                } else if (match.Groups[1].ToString().Trim() == "p") {
                } else {
                    var url = match.Groups[1].ToString().Trim();
                    links.Add(url.Replace("\'", ""));
                }
                match = match.NextMatch();
            }
            //a
            regEx = new Regex("<a.*href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                links.Add(match.Groups[1].ToString().Trim().Replace("\'", ""));
                match = match.NextMatch();
            }
            //href
            regEx = new Regex("href\\s*=\\s*(?:(?:\"(?<url>[^\\\"]*)\\\")|(?<url>[^\\s]* ))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                links.Add(match.Groups[1].ToString().Trim());
                match = match.NextMatch();
            }
            //area
            regEx = new Regex("<area.*href\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                links.Add(match.Groups[1].ToString().Trim().Replace("\'", ""));
                match = match.NextMatch();
            }
            //img
            regEx = new Regex("<img.*src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                links.Add(match.Groups[1].ToString().Trim().Replace("\'", ""));
                match = match.NextMatch();
            }
            //iframe
            regEx = new Regex("<iframe.*src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                links.Add(match.Groups[1].ToString().Trim().Replace("\'", ""));
                match = match.NextMatch();
            }
            //frame
            regEx = new Regex("<frame.*src\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                links.Add(match.Groups[1].ToString().Trim().Replace("\'", ""));
                match = match.NextMatch();
            }
            //object
            regEx = new Regex("<object.*data\\s*=\\s*(?:\"(?<1>[^\"]*)\"|(?<1>\\S+))", (System.Text.RegularExpressions.RegexOptions)(RegexOptions.IgnoreCase | RegexOptions.Compiled));
            match = regEx.Match(html);
            while (match.Success) {
                links.Add(match.Groups[1].ToString().Trim().Replace("\'", ""));
                match = match.NextMatch();
            }
            //extract temp links
            links.Sort();
            var tempLinks = new List<string>();
            foreach (var linksToUse in links) {
                var link = linksToUse;
                if (link.IndexOf("\\") != -1) {
                    if (link.StartsWith("\\\\")) {
                        link = link.Substring(2);
                    }
                    if (link.StartsWith("\\\"")) {
                        link = link.Substring(2);
                    }
                    if (link.EndsWith("\\\"")) {
                        link = link.Substring(0, link.Length - 2);
                    }
                    if (link.EndsWith("\\\\")) {
                        link = link.Substring(0, link.Length - 2);
                    }
                    if (!link.StartsWith("\"")) {
                        link = "\"" + link;
                    }
                    if (!link.EndsWith("\"")) {
                        link = link + "\"";
                    }
                    //try {
                    //    link = JsonDeserializer.Deserialize<string>(link);
                    //} catch (Exception) {
                    //    link = "";
                    //}
                    link = link.Replace("\\/", "/");
                }
                if (link.StartsWith("\'")) {
                    link = "";
                }
                if (link.IndexOf("#") != -1) {
                    link = link.Substring(0, link.IndexOf("#"));
                }
                if (link.StartsWith("\"")) {
                    link = link.Substring(1);
                }
                if (link.EndsWith("\"")) {
                    link = link.Substring(0, link.Length - 1);
                }
                if (link.IndexOf("&") != -1) {
                    link = HtmlUtils.HtmlDecode(link);
                }
                if (string.IsNullOrEmpty(link) || link.StartsWith("#")) {
                    link = "";
                } else if (link.StartsWith("javascript:")) {
                    link = "";
                } else if (link.StartsWith("mailto:")) {
                    link = "";
                } else if (link.EndsWith("mailto:")) {
                    link = "";
                } else if (link.StartsWith("ftp:")) {
                    link = "";
                } else if (link.StartsWith("telnet:")) {
                    link = "";
                } else if (link.StartsWith("data:")) {
                    link = "";
                } else if (link.StartsWith("callto:")) {
                    link = "";
                } else if (link.StartsWith("skype:")) {
                    link = "";
                } else if (link.StartsWith("tel:")) {
                    link = "";
                } else if (link.StartsWith("vfps:")) {
                    link = "";
                } else if (link.EndsWith("\'")) {
                    link = "";
                } else if (link.StartsWith("?")) {
                    //string aux = basepath;
                    //if (aux.IndexOf("?") != -1) {
                    //    aux = aux.Substring(0, aux.IndexOf("?"));
                    //}
                    link = basepath + link;
                } else if (link.IndexOf("://") == -1) {
                    if (link.StartsWith("/")) {
                    } else {
                        link = PathUtils.Combine(basepath, link);
                    }
                } else if (link.IndexOf("://") != -1) {
                    if (link.IndexOf("..") != -1) {
                        link = PathUtils.Combine(link, "");
                    }
                }
                if (link.IndexOf(">") != -1) {
                    link = link.Substring(0, link.IndexOf(">"));
                }
                if (link.EndsWith("?")) {
                    link = link.Substring(0, link.Length - 1);
                }
                if (link.EndsWith("//")) {
                    link = link.Substring(0, link.Length - 1);
                }
                if (link.EndsWith("//")) {
                    link = link.Substring(0, link.Length - 1);
                }
                if ((link.StartsWith("http://") || link.StartsWith("https://")) && link.LastIndexOf("//") > 10) {
                    string comodin = ":/______________";
                    link = link.Replace("://", comodin);
                    link = link.Replace("//", "/");
                    link = link.Replace("//", "/");
                    link = link.Replace(comodin, "://");
                }
                if (tempLinks.Contains(link)) {
                    link = "";
                }
                if (!string.IsNullOrEmpty(link)) {
                    tempLinks.Add(link);
                }
            }
            //make absolute
            if (uri != null) {
                var schema = "";
                if (uri.IndexOf(":") != -1) {
                    schema = uri.Substring(0, uri.IndexOf(":"));
                }
                var schemaHostPort = uri;
                var j = schemaHostPort.IndexOf("/", 9);
                if (j != -1) {
                    schemaHostPort = schemaHostPort.Substring(0, j);
                }
                for (int i = 0; i <= tempLinks.Count - 1; i++) {
                    var link = tempLinks[i];
                    if (link.StartsWith("//")) {
                        link = schema + ":" + link;
                    } else if (link.StartsWith("/")) {
                        link = schemaHostPort + link;
                    }
                    tempLinks[i] = link;
                }
            }
            //set
            tempLinks.Sort();
            links = tempLinks;
            //remove duplicates
            var aux = new HashSet<string>();
            links.RemoveAll(x => !aux.Add(x));
            //return
            return links.ToArray();
        }
        public static string AbsolutizeLinks(string html, string url) {
            var result = new StringBuilder(html);
            //raw replace
            var keywords = new string[] { "href=\"", "src=\"", "url(" };
            foreach(var keyword in keywords) {
                var c = keyword.Substring(keyword.Length - 1, 1);
                var cEnd = (c=="(" ? ")" : c);
                int i = html.IndexOf(keyword);
                while (i!=-1) {
                    var j = html.IndexOf(cEnd, i + keyword.Length);
                    if (j!=-1) {
                        var link = html.Substring(i + keyword.Length, j - i - keyword.Length);
                        link = AbsolutizeLink(link, url);
                        html = html.Substring(0, i + keyword.Length) + link + html.Substring(j);
                    }
                    i = html.IndexOf(keyword, i+1);
                }
            }
            //return 
            return html;
        }
        public static string AbsolutizeLink(string link, string url) {
            var linkDoubleQuoted = false;
            var linkSingleQuoted = false;
            //unquote if required
            if (link.StartsWith("\"") && link.EndsWith("\"")) {
                linkDoubleQuoted = true;
                link = link.Substring(1, link.Length - 2);
            } else if (link.StartsWith("'") && link.EndsWith("'")) {
                linkSingleQuoted = true;
                link = link.Substring(1, link.Length - 2);
            }
            //proces
            if (link.IndexOf(":") != -1) {
            } else if (link.StartsWith("//")) {
                // ex: //path/to/my/file
                var schemaAndServer = new Uri(url).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
                link = schemaAndServer + link.Substring(1);
            } else if (link.StartsWith("/")) {
                // ex: /path/to/my/file
                var linkUrl = new Uri(new Uri(url), link);
                link = linkUrl.ToString();
            } else if (link.StartsWith("?")) {
                // ex: ?a=1&b=2
                link = url + link;
            } else if (link.StartsWith("#")) {
                // ex: #hashtag
                link = url + link;
            } else {
                // ex: relative/path/to/my/file
                if (url.EndsWith("/")) {
                    link = url + link;
                } else {
                    link = PathUtils.GetPathParent(url) + "/" + link;
                }
            }
            //normalize ..
            if (link.IndexOf("..") != -1) {
                var path = link;
                var qs = "";
                if (path.IndexOf("?") != -1) {
                    qs = path.Substring(path.IndexOf("?"));
                    path = path.Substring(0, path.IndexOf("?"));
                }
                link = PathUtils.Combine(path, "") + qs;
            }
            //quote if required
            if (linkDoubleQuoted) link = "\"" + link + "\"";
            if (linkSingleQuoted) link = "'" + link + "'";
            //return
            return link;
        }

        public static bool IsInlineHtmlElement(string tag) {
            return (tag.Equals("span") || tag.Equals("b") || tag.Equals("big") || tag.Equals("i") || tag.Equals("a") || tag.Equals("em") || tag.Equals("sub") || tag.Equals("sup") || tag.Equals("input") || tag.Equals("button") || tag.Equals("label") || tag.Equals("select") || tag.Equals("textarea"));
        }

    }


}


