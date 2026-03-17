

using System.Linq;
using System.Text;

namespace DProjects.Utils {


    public static class CommandLineUtils {

        public static string EscapeWindowsArgument(string arg) {
            if (string.IsNullOrEmpty(arg))
                return "\"\"";

            if (!arg.Any(c => char.IsWhiteSpace(c) || c == '"' || c == '\\'))
                return arg;

            var sb = new StringBuilder();
            sb.Append('"');

            int backslashes = 0;
            foreach (char c in arg) {
                if (c == '\\') {
                    backslashes++;
                } else if (c == '"') {
                    sb.Append('\\', backslashes * 2 + 1);
                    sb.Append('"');
                    backslashes = 0;
                } else {
                    sb.Append('\\', backslashes);
                    backslashes = 0;
                    sb.Append(c);
                }
            }

            sb.Append('\\', backslashes * 2);
            sb.Append('"');

            return sb.ToString();
        }

    }

}
