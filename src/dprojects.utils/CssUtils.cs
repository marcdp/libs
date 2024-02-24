namespace DProjects.Utils {


    public static class CssUtils {

        //minify css
        public static string MinifyCss(string css) {
            do {
                int i = css.IndexOf("/*");
                if (i == -1) break;
                int j = css.IndexOf("*/");
                if (j == -1) break;
                css = css.Substring(0, i) + css.Substring(j + 2);
            } while (true);
            foreach (var c in new char[] { '\n', '\r', CharUtils.CHAR_TAB }) { 
                if (css.IndexOf(c) != -1) css = css.Replace(c, ' ');
            }
            foreach (var c in new char[] { '{', '}', ',', ';', ':', '[' }) {
                while (css.IndexOf(c + " ") != -1) css = css.Replace(c + " ", "" + c);
                while (css.IndexOf(" " + c) != -1) css = css.Replace(" " + c, "" + c);
            }
            css = css.Replace(")format(", ") format(");
            css = css.Replace("\xFEFF", "");
            css = css.Trim();
            return css;
        }

    }


}


