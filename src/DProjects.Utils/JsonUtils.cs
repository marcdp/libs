using System;
using System.Collections.Generic;

namespace DProjects.Utils {


    public static class JsonUtils {


        //print
        public static string Beautify(string json, int indent = 2) {
            json = (json ?? "").Replace("{}", @"\{\}").Replace("[]", @"\[\]");
            var inserts = new List<int[]>();
            bool quoted = false, escape = false;
            int depth = 0/*-1*/;
            for (int i = 0, N = json.Length; i < N; i++) {
                var chr = json[i];
                if (!escape && !quoted)
                    switch (chr) {
                        case '{':
                        case '[':
                            inserts.Add(new[] { i, +1, 0, indent * ++depth });
                            //int n = (i == 0 || "{[,".Contains(str[i - 1])) ? 0 : -1;
                            //inserts.Add(new[] { i, n, INDENT_SIZE * ++depth * -n, INDENT_SIZE - 1 });
                            break;
                        case ',':
                            inserts.Add(new[] { i, +1, 0, indent * depth });
                            //inserts.Add(new[] { i, -1, INDENT_SIZE * depth, INDENT_SIZE - 1 });
                            break;
                        case '}':
                        case ']':
                            inserts.Add(new[] { i, -1, indent * --depth, 0 });
                            //inserts.Add(new[] { i, -1, INDENT_SIZE * depth--, 0 });
                            break;
                        case ':':
                            inserts.Add(new[] { i, 0, 0, 1 });
                            break;
                    }
                quoted = (chr == '"') ? !quoted : quoted;
                escape = (chr == '\\') ? !escape : false;
            }
            if (inserts.Count > 0) {
                var sb = new System.Text.StringBuilder(json.Length * 2);
                int lastIndex = 0;
                foreach (var insert in inserts) {
                    int index = insert[0], before = insert[2], after = insert[3];
                    bool nlBefore = (insert[1] == -1), nlAfter = (insert[1] == +1);

                    sb.Append(json.Substring(lastIndex, index - lastIndex));

                    if (nlBefore) sb.AppendLine();
                    if (before > 0) sb.Append(new String(' ', before));

                    sb.Append(json[index]);

                    if (nlAfter) sb.AppendLine();
                    if (after > 0) sb.Append(new String(' ', after));

                    lastIndex = index + 1;
                }
                json = sb.ToString();
            }
            return json.Replace(@"\{\}", "{}").Replace(@"\[\]", "[]");
        }
        public static Type InferDataType(string json) {
            if (json == null) {
                return typeof(void);
            } else if (json.StartsWith("{") && json.EndsWith("}")) {
                return typeof(object);
            } else if (json.StartsWith("[") && json.EndsWith("]")) {
                return typeof(object[]);
            } else if (StringUtils.IsInteger(json)) {
                return typeof(int);
            } else if (StringUtils.IsLong(json)) {
                return typeof(long);
            } else if (StringUtils.IsNumeric(json)) {
                return typeof(decimal);
            } else if (json.Equals("true") || json.Equals("false")) {
                return typeof(bool);
            } else if (json.Equals("null")) {
                return typeof(object);
            } else {
                return typeof(string);
            }
        }
        

    }
}



