using System;
using System.Collections.Generic;
using System.Text;


namespace DProjects.Utils {


    public static class PathUtils {

        //constants
        public readonly static char[] PATH_INVALID_CHARS = new char[] { ':', '*', '?', '<', '>', '|', '*', '\"' };
        public readonly static char[] PATH_NAME_INVALID_CHARS = new char[] { ':', '*', '?', '<', '>', '|', '*', '\"', '/', '\\' };


        //read methods
        public static string Combine(string path, string name) {
            //get path name
            string result = "";
            if ((path.Equals("") || path.Equals("/")) && name.StartsWith("/")) {
                result = name;
                while (result.IndexOf("/./") != -1) {
                    result = result.Replace("/./", "/");
                }
                while (result.EndsWith("/.") && result.Length > 2) {
                    result = result.Substring(0, result.Length - 2);
                }
                if (result.EndsWith("/.") && result.Length == 2) {
                    result = "/";
                }
                return result;
            } else if (name.Equals("/")) {
                name = "";
            }
            if (!path.EndsWith("/")) {
                path += "/";
            }
            if (name.StartsWith("/")) {
                name = name.Substring(1);
            }
            if (path != "/" && name == "" && path.EndsWith("/")) {
                path = path.Substring(0, path.Length - 1);
            }
            result = path + name;
            if (result.IndexOf("..") != -1) {
                var tokens = new List<string>(result.Split('/'));
                int i = 0;
                while (i < tokens.Count) {
                    string token = tokens[i];
                    if (token == "..") {
                        tokens.RemoveAt(i);
                        if (i > 0) {
                            string pretoken = tokens[i - 1];
                            if (pretoken != "..") {
                                tokens.RemoveAt(i - 1);
                            }
                            i--;
                        }
                    } else if (token == ".") {
                        tokens.RemoveAt(i);
                    } else {
                        i++;
                    }
                }
                result = string.Join("/", tokens.ToArray());
                if (string.IsNullOrEmpty(result)) {
                    result = "/";
                }
            }
            while (result.IndexOf("/./") != -1) {
                result = result.Replace("/./","/");
            }
            while (result.EndsWith("/.") && result.Length > 2) {
                result = result.Substring(0, result.Length - 2);
            }
            if (result.EndsWith("/.") && result.Length == 2) {
                result = "/";
            }
            return result;
        }
        public static string Combine(string path, string name, string name2) {
            return Combine(Combine(path, name), name2);
        }
        public static string Combine(string path, string name, string name2, string name3) {
            return Combine(Combine(path, name), name2, name3);
        }
        public static string Combine(string path, string name, string name2, string name3, string name4) {
            return Combine(Combine(path, name), name2, name3, name4);
        }
        public static string Normalize(string path) {
            return Combine(path, "");
        }
        public static string NormalizeIfRequired(string path) {
            if (path.IndexOf("/.") != -1) {
                return Combine(path, "");
            }
            return path;
        }
        public static string Uncombine(string prefix, string path) {
            //get path unprefixed
            if (prefix != "/") {
                if (path.StartsWith(prefix)) {
                    path = path.Substring(prefix.Length);
                    if (path == "") {
                        path = "/";
                    }
                }
            }
            return path;
        }
        public static string Create(string pwd, string path) {
            //create path
            return (path.StartsWith("/")) ? path : (PathUtils.Combine(pwd, path));
        }
        public static string GetPathParent(string path) {
            //get parent path of a path
            path = NormalizeIfRequired(path);
            int i = path.LastIndexOf("/");
            if (i == -1) {
                return "";
            }
            string parentPath = path.Substring(0, i);
            if (string.IsNullOrEmpty(parentPath)) {
                parentPath = "/";
            }
            return parentPath;
        }
        public static string GetPathGrandParent(string path) {
            //get grand parent path of a path
            return GetPathParent(GetPathParent(path));
        }
        public static string GetPathAncestor(string path, int levels) {
            string aux = path;
            for (int i = 1; i <= levels; i++) {
                aux = GetPathParent(aux);
            }
            return aux;
        }
        public static string GetPathExtension(string path) {
            //get path extension
            path = NormalizeIfRequired(path);
            int i = path.LastIndexOf(".");
            if (i == -1) {
                return "";
            }
            var result = path.Substring(i);
            if (result.IndexOf("/") != -1) return "";
            return result;
        }
        public static string GetPathName(string path) {
            //get path name
            path = NormalizeIfRequired(path);
            if (path.LastIndexOf("/") == -1) return path;
            string name = path.Substring(path.LastIndexOf("/") + 1);
            return name;
        }
        public static string GetPathNameWithoutExtension(string path) {
            //get path name without extension
            string name = GetPathName(path);
            int i = name.LastIndexOf(".");
            if (i == -1) {
                return name;
            }
            return name.Substring(0, i);
        }
        public static string GetPathFirstName(string path) {
            //get path first name
            path = NormalizeIfRequired(path);
            if (path.StartsWith("/")) {
                path = path.Substring(1);
            }
            if (path.IndexOf("/") != -1) {
                return path.Substring(0, path.IndexOf("/"));
            }
            return path;
        }
        public static int GetPathPartsCount(string path) {
            //get path count
            path = NormalizeIfRequired(path);
            int result = 0;
            if (path == "/") {
                result = 0;
            } else {
                result = path.Split('/').Length - 1;
            }
            return result;
        }
        public static string GetPathCuttedByLevel(string path, int level) {
            path = NormalizeIfRequired(path);
            var aux = new StringBuilder(path.Length);
            int counter = 0;
            foreach (string pathPart in path.Split('/')) {
                if (counter == 1) {
                } else {
                    aux.Append("/");
                }
                aux.Append(pathPart);
                counter++;
                if (counter > level) {
                    break;
                }
            }
            return aux.ToString();
        }
        public static string GetPathCuttedFromLevel(string path, int level) {
            path = NormalizeIfRequired(path);
            var aux = new StringBuilder(path.Length);
            int counter = 0;
            foreach (string pathPart in path.Split('/')) {
                if (counter > level) {
                    aux.Append("/");
                    aux.Append(pathPart);
                }
                counter++;
            }
            if (aux.Length == 0) aux.Append("/");
            return aux.ToString();
        }
        public static string GetPathInvalidCharsReplaced(string path) {
            char[] invalidChars = new char[] { '\\', ':', '*', '?', '<', '>', '|', '\"' };
            if (path.IndexOfAny(invalidChars) != -1) {
                foreach (char aChar in invalidChars) {
                    if (path.IndexOf(aChar) != -1) {
                        path = path.Replace(aChar, '_');
                    }
                }
            }
            return path;
        }
        public static string GetPathInvalidCharsReplacedStrong(string path) {
            char[] invalidChars = new char[] { '\\',  ':', '*', '?', '<', '>', '|', '\"', ',', '/' };
            if (path.IndexOfAny(invalidChars) != -1) {
                foreach (char aChar in invalidChars) {
                    if (path.IndexOf(aChar) != -1) {
                        path = path.Replace(aChar.ToString(), "");
                    }
                }
            }
            return path;
        }
        public static string GetPathInvalidCharsReplacedStrongStrong(string path) {
            string result = "";
            for (int i = 0; i <= path.Length - 1; i++) {
                char aChar = path[i];
                int iCharCode = StringUtils.Asc(aChar);
                if (48 <= iCharCode && iCharCode <= 57) {
                    result += aChar.ToString();
                } else if (65 <= iCharCode && iCharCode <= 90) {
                    result += aChar.ToString();
                } else if (97 <= iCharCode && iCharCode <= 122) {
                    result += aChar.ToString();
                } else if (aChar == '-') {
                    result += aChar.ToString();
                }
            }
            if (string.IsNullOrEmpty(result)) {
                result = "_";
            }
            return result.ToLower();
        }
        public static void Validate(string path) {
            if (path == null) throw new ArgumentNullException();
            if (path.Length == 0) throw new ArgumentException("Path is not valid: " + path);
            if (!path.StartsWith("/")) throw new ArgumentException("Path is not valid: " + path);
            if (path.Length > 1 && path.EndsWith("/")) throw new ArgumentException("Path is not valid: " + path);
            int i = 0; 
            foreach (char c in path) {
                if (c < ' ' || System.Array.IndexOf(PathUtils.PATH_INVALID_CHARS, c) != -1) {
                    throw new ArgumentNullException("Invalid character \'0x" + Convert.ToInt32(c).ToString("x") + "\' in path \'" + path + "\', position " + i);
                }
                i++;
            }
        }
        public static string GetPathURLEncoded(string path) {
            var result = new StringBuilder();
            foreach (var part in path.Split('/')) {
                if (part.Length > 0) {
                    var partEncoded = System.Uri.EscapeUriString(part);
                    if (partEncoded.IndexOf("#") != -1) partEncoded = partEncoded.Replace("#", "%23");
                    if (partEncoded.IndexOf("[") != -1) partEncoded = partEncoded.Replace("[", "%5B");
                    if (partEncoded.IndexOf("]") != -1) partEncoded = partEncoded.Replace("]", "%5D");
                    result.Append("/").Append(partEncoded);
                }
            }
            if (result.Length == 0) result.Append("/");
            if (path.Length > 1 && path.EndsWith("/") && !result.ToString().EndsWith("/")) result.Append("/");
            return result.ToString();
        }
        public static int CompareName(string nameA, string nameB) {
            return String.Compare(nameA, nameB, StringComparison.Ordinal);
        }
        public static int ComparePath(string pathA, string pathB) {
            int result = 0;
            pathA = NormalizeIfRequired(pathA);
            pathB = NormalizeIfRequired(pathB);
            var parentPathA = PathUtils.GetPathParent(pathA);
            var parentPathB = PathUtils.GetPathParent(pathB);
            if (parentPathA.Equals(parentPathB, StringComparison.Ordinal)) {
                result = CompareName(PathUtils.GetPathName(pathA), PathUtils.GetPathName(pathB));
            } else {
                var a = pathA.Split('/');
                var b = pathB.Split('/');
                for (var i = 0; i < System.Math.Max(a.Length, b.Length); i++) {
                    var v1part = (i < a.Length ? a[i] : "");
                    var v2part = (i < b.Length ? b[i] : "");
                    int k = String.Compare(v1part, v2part, StringComparison.Ordinal);
                    if (k != 0) {
                        result = k;
                        break;
                    }
                }
            }
            return result;
        }

    }


}


