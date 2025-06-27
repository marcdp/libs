using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;


namespace DProjects.Utils {


    public static class StringUtils {


        //equals + compare
        public static bool Equals(string a, string b) {
            return string.Equals(a, b, StringComparison.CurrentCultureIgnoreCase);
        }
        public static int Compare(string a, string b) {
            return string.Compare(a, b, StringComparison.CurrentCultureIgnoreCase);
        }

        ////connectionString
        //public static NameValueCollection GetConnectionStringNameValueCollection(string connectionString) {
        //    var result = new NameValueCollection();
        //    var insideQuotes = false;
        //    for (int i = 0; i< connectionString.Length; i++) {
        //        char c = connectionString[i];
        //        if (c == '"') {
        //            insideQuotes = !insideQuotes;
        //        } else { 
        //            if (!insideQuotes) { 
        //                if (c == ';') {
        //                    var keyValue = connectionString.Substring(0, i).Trim();
        //                    if (keyValue.Length > 0) {
        //                        var index = keyValue.IndexOf('=');
        //                        if (index > 0 && index < keyValue.Length - 1) {
        //                            var key = keyValue.Substring(0, index).Trim();
        //                            var value = keyValue.Substring(index + 1).Trim();
        //                            result[key] = value;
        //                        }
        //                    }
        //                    connectionString = connectionString.Substring(i + 1);
        //                    i = -1; // Reset index to start parsing the next pair
        //                }
        //            }
        //        }
        //    }
        //    return result;
        //}
        //public static NameValueCollection GetConnectionStringNameValueCollectionOld(string connectionString) {
        //    var result = new NameValueCollection();

        //    if (string.IsNullOrWhiteSpace(connectionString))
        //        return result;

        //    var pairs = connectionString.Split(';');

        //    foreach (var pair in pairs) {
        //        if (string.IsNullOrWhiteSpace(pair))
        //            continue;

        //        var index = pair.IndexOf('=');
        //        if (index <= 0 || index == pair.Length - 1)
        //            continue; // Skip invalid pairs

        //        var key = pair.Substring(0, index).Trim();
        //        var value = pair.Substring(index + 1).Trim();

        //        result[key] = value;
        //    }

        //    return result;
        //}
        public static NameValueCollection GetConnectionStringNameValueCollection(string text) {
            var result = new NameValueCollection();
            if (string.IsNullOrEmpty(text))
                return result;

            int i = 0;

            while (i < text.Length) {
                // Parse the key
                int keyStart = i;
                while (i < text.Length) {
                    if (text[i] == '=') break;
                    if (text[i] == ';') {
                        break;
                        //var aux = text.Substring(keyStart, i - keyStart).Trim();
                        //result.Add(aux, "");
                        //continue;
                    }
                    i++;
                }

                if (i == text.Length)
                    throw new FormatException("Invalid connection string format: missing '=' for a key.");

                string key = text.Substring(keyStart, i - keyStart).Trim();
                if (text[i]==';') {
                    result.Add(key, "");
                    i++;
                    continue;
                }

                i++; // Skip '='

                // Parse the value
                string value;
                if (i < text.Length && text[i] == '"') {
                    i++; // Skip opening quote
                    int valueStart = i;

                    while (i < text.Length && text[i] != '"')
                        i++;

                    if (i == text.Length)
                        throw new FormatException("Invalid connection string format: unmatched quote in value.");

                    value = text.Substring(valueStart, i - valueStart);
                    i++; // Skip closing quote
                } else {
                    int valueStart = i;

                    while (i < text.Length && text[i] != ';')
                        i++;

                    value = text.Substring(valueStart, i - valueStart).Trim();
                }

                result.Add(key, value);

                // Skip the delimiter ';'
                if (i < text.Length && text[i] == ';')
                    i++;
            }

            return result;
        }
        public static string ReplaceConnectionStringVariable(string text, string variable, string newvariable) {
            //if (text == null) return text;
            string loweredText = text.ToLower() + ";";
            int i = loweredText.IndexOf(variable.ToLower() + "=");
            if (i > -1) {
                var k = i + variable.Length + 1;
                int j = loweredText.IndexOf(";", k);
                if (j > -1) {
                    return text.Substring(0, i) + newvariable + text.Substring(j);
                } else {
                    return text;
                }
            } else {
                return text;
            }
        }
        //public static NameValueCollection GetConnectionStringNameValueCollection(string text) {
        //    var result = new NameValueCollection();
        //    int start = 0;
        //    while (start < text.Length) {
        //        int equalsIndex = text.IndexOf('=', start);
        //        if (equalsIndex == -1) throw new FormatException("Invalid connection string format: missing '=' character.");
        //        string key = text.Substring(start, equalsIndex - start).Trim();
        //        int valueStart = equalsIndex + 1;
        //        string value;
        //        if (valueStart < text.Length && text[valueStart] == '"') {
        //            // Value is quoted
        //            int endQuoteIndex = text.IndexOf('"', valueStart + 1);
        //            if (endQuoteIndex == -1)
        //                throw new FormatException("Invalid connection string format: missing closing quote.");

        //            value = text.Substring(valueStart + 1, endQuoteIndex - valueStart - 1);
        //            start = endQuoteIndex + 1;
        //        } else {
        //            // Value is unquoted
        //            int semiColonIndex = text.IndexOf(';', valueStart);
        //            if (semiColonIndex == -1) {
        //                value = text.Substring(valueStart).Trim();
        //                start = text.Length;
        //            } else {
        //                value = text.Substring(valueStart, semiColonIndex - valueStart).Trim();
        //                start = semiColonIndex + 1;
        //            }
        //        }
        //        result.Add(key, value);
        //        // Skip trailing semicolon
        //        if (start < text.Length && text[start] == ';') {
        //            start++;
        //        }
        //    }
        //    return result;
        //}
        public static T GetConnectionStringVariable<T>(string text, string variable, T defaultValue) {
            string? value = GetConnectionStringVariable(text, variable, null!);
            if (value == null) return defaultValue!;
            return ConvertUtils.To<T>(value);
        }
        public static string? GetConnectionStringVariable(string text, string variable) {
            return GetConnectionStringVariable(text, variable, null!);
        }
        public static string GetConnectionStringVariable(string text, string variable, string defaultValue = "") {
            if (text == null) return defaultValue;
            var values = GetConnectionStringNameValueCollection(text);
            if (values.AllKeys.Contains(variable)) {
                return values[variable];
            }
            return defaultValue;
        }
        public static string GetConnectionStringVariableOld(string text, string variable, string defaultValue = "") {
            if (text == null) return defaultValue;
            string loweredText = text.ToLower() + ";";
            int i = loweredText.IndexOf(variable.ToLower() + "=");
            if (i > -1) {
                i += variable.Length + 1;
                int j = loweredText.IndexOf(";", i);
                if (j > -1) {
                    string res = text.Substring(i, j - i);
                    if (string.IsNullOrEmpty(res)) {
                        return defaultValue;
                    }
                    return res;
                } else {
                    return defaultValue;
                }
            } else {
                return defaultValue;
            }
        }
        public static string RemoveConnectionStringVariable(string text, string variable) {
            var values = GetConnectionStringNameValueCollection(text);
            var result = new StringBuilder();
            values.Remove(variable);
            foreach (string key in values.Keys) {
                var value = values[key];
                if (result.Length > 0) result.Append(";");
                result.Append(key);
                result.Append("=");
                if (value.IndexOf(";") != -1) result.Append('"');
                result.Append(value);
                if (value.IndexOf(";") != -1) result.Append('"');
            }
            return result.ToString();
        }
        public static string[] GetConnectionStringVariableNames(string text, string[]? excludeVariableNames = null) {
            var aux = new List<string>();
            if (excludeVariableNames == null) excludeVariableNames = [];
            var result = new List<string>();
            var values = GetConnectionStringNameValueCollection(text);
            foreach(string key in values.Keys) {
                if (System.Array.IndexOf(excludeVariableNames, key) == -1) {
                    result.Add(key);
                }
            }
            return result.ToArray();
        }
        public static bool SeemsConnectionString(string text) {
            if (text.IndexOf(";")!=-1 && text.IndexOf("=") != -1) {
                return true;
            }
            return false;
        }


        //format
        public static string FormatSize(long size, bool minimum1KB = false, bool returnEmptyFor0Bytes = false, bool useDotAsDecimalSeparator = true, bool useNoSpaces = false) {
            var result = "";
            var space = (useNoSpaces ? "" : " ");
            if (size == 0 && returnEmptyFor0Bytes) {
                result = "";
            } else if (size < 1024) {
                var bytes = size;
                if (minimum1KB) {
                    result = "1" + space + "KB";
                } else {
                    result = bytes + space + "bytes";
                }
            } else if (size < 1024 * 1024) {
                var kbytes = ((double)size / 1024);
                result = string.Format("{0:#}" + space + "KB", kbytes);
            } else if (size < 1024 * 1024 * 1024) {
                var mbytes = ((double)size / (1024 * 1024));
                result = string.Format("{0:#.0}" + space + "MB", mbytes);
            } else {
                var gbytes = ((double)size / (1024 * 1024 * 1024));
                result = string.Format("{0:#.0}" + space + "GB", gbytes);
            }
            if (useDotAsDecimalSeparator && result.IndexOf(",") != -1) result = result.Replace(",", ".");
            return result;
        }
        public static long UnFormatSize(string text) {
            text = text.ToUpper().Trim();
            if (text.EndsWith("YB")) { //Yottabyte
                return System.Convert.ToInt64(decimal.Parse(text.Substring(0, text.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture) * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024);
            } else if (text.EndsWith("ZB")) { //Zettabyte
                return System.Convert.ToInt64(decimal.Parse(text.Substring(0, text.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture) * 1024 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024);
            } else if (text.EndsWith("EB")) { //exabyte
                return System.Convert.ToInt64(decimal.Parse(text.Substring(0, text.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture) * 1024 * 1024 * 1024 * 1024 * 1024 * 1024);
            } else if (text.EndsWith("PB")) { //petabyte
                return System.Convert.ToInt64(decimal.Parse(text.Substring(0, text.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture) * 1024 * 1024 * 1024 * 1024 * 1024);
            } else if (text.EndsWith("TB")) { //terabyte
                return System.Convert.ToInt64(decimal.Parse(text.Substring(0, text.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture) * 1024 * 1024 * 1024 * 1024);
            } else if (text.EndsWith("GB")) { //megabyte
                return System.Convert.ToInt64(decimal.Parse(text.Substring(0, text.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture) * 1024 * 1024 * 1024);
            } else if (text.EndsWith("MB")) { //megabyte
                return System.Convert.ToInt64(decimal.Parse(text.Substring(0, text.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture) * 1024 * 1024);
            } else if (text.EndsWith("KB")) { //kilobyte
                return System.Convert.ToInt64((decimal.Parse(text.Substring(0, text.Length - 2), NumberStyles.Any, CultureInfo.InvariantCulture) * 1024));
            } else if (text.EndsWith("BYTES")) { //kilobyte
                return System.Convert.ToInt64((decimal.Parse(text.Substring(0, text.Length - 5), NumberStyles.Any, CultureInfo.InvariantCulture)));
            } else {
                return long.Parse(text);
            }
        }


        //decode
        public static string DecodeMimeEncodedString(string text) {
            //info: https://en.wikipedia.org/wiki/MIME#Encoded-Word
            //ex: =?iso-8859-1?Q?=A1Hola,_se=F1or!?=
            //ex: =?utf-8?B?2LPZhNin2YU=?=
            if (text.StartsWith("=?") && text.EndsWith("?=")) {
                var aux = text.Substring(2, text.Length - 4);
                if (aux.IndexOf("?") != -1) {
                    var charset = aux.Substring(0, aux.IndexOf("?"));
                    aux = aux.Substring(aux.IndexOf("?") + 1);
                    if (aux.IndexOf("?") != -1) {
                        var encoding = aux.Substring(0, aux.IndexOf("?"));
                        aux = aux.Substring(aux.IndexOf("?") + 1);
                        if (encoding.Equals("B")) {
                            //base64
                            var buffer = Base64Utils.FromBase64(aux);
                            var charsetEncoding = Encoding.GetEncoding(charset);
                            return charsetEncoding.GetString(buffer);
                        } else if (encoding.Equals("Q")) {
                            //Q-encoding
                            var charsetEncoding = Encoding.GetEncoding(charset);
                            return DecodeQuotedPrintable(aux, charsetEncoding);
                        }
                    }
                }
            }
            return text;
        }
        public static string DecodeQuotedPrintable(string text, Encoding encoding) {
            MemoryStream ms = new MemoryStream();
            for (int i = 0; i <= text.Length - 1; i++) {
                char c = text[i];
                if (c == '=' && i < text.Length - 2) {
                    string hex = text.Substring(i + 1, 2);
                    if (hex == Environment.NewLine) {
                        i += 2;
                    } else {
                        int ascii = int.Parse(hex, System.Globalization.NumberStyles.HexNumber);
                        ms.WriteByte(Convert.ToByte(ascii));
                        i += 2;
                    }
                } else if (c == ' ') {
                    ms.WriteByte((byte)c);
                } else {
                    ms.WriteByte(System.Convert.ToByte(System.Text.Encoding.ASCII.GetBytes(c.ToString().ToCharArray())[0]));
                }
            }
            byte[] buffer = ms.ToArray();
            ms.Dispose();
            return encoding.GetString(buffer);
        }

        //other
        public static string[] SplitByColumns(string text, int columns) {
            var result = new List<string>();
            var i = 0;
            do {
                var item =text.Substring(i, Math.Min(columns, text.Length-i));
                if (item.Length == 0) break;
                result.Add(item);
                i += columns;
            } while (i < text.Length);
            return result.ToArray();
        }
        public static string SplitByColumnsAndFold(string text, int columns) {
            var lines = SplitByColumns(text, columns);
            return string.Join(System.Environment.NewLine, lines);
        }
        public static string GetTextCutted(string text, int maxlength, bool addDotsIfRequired) {
            string result = "";
            if (text == null) {
                result = "";
            } else if (text.Length < maxlength) {
                result = text;
            } else {
                if (addDotsIfRequired) {
                    result = text.Substring(0, maxlength - 3) + "...";
                } else {
                    result = text.Substring(0, maxlength);
                }
            }
            return result;
        }
        public static string TranslateXmlEntitiesToString(string text) {
            return System.Net.WebUtility.HtmlDecode(text);
        }
        public static string TranslateStringToXmlEntities(string text) {
            return System.Net.WebUtility.HtmlEncode(text);
        }
        public static string ConvertFullNameToInitials(string name) {
            if (string.IsNullOrEmpty(name)) return "";
            if (name.IndexOf(",") != -1) {
                var aux = name.Substring(name.IndexOf(",")+1).Trim();
                if (aux.Length > 0) aux = aux.Substring(0, 1);
                name = aux + " " + name;
                if (name.IndexOf(",")!=-1) name = name.Substring(0, name.IndexOf(",") + 1).Trim();
            }
            while (name.IndexOf("  ") != -1) name = name.Replace("  ", " ");
            name = name.Trim();
            var nameParts = name.Split(' ');
            if (nameParts.Length == 0) {
                return "";
            } else if (nameParts.Length == 1) {
                return nameParts[0].Substring(0, 1).ToUpper();
            } else if (nameParts.Length == 2) {
                return nameParts[0].Substring(0, 1).ToUpper() + nameParts[1].Substring(0, 1).ToUpper();
            } else {
                return nameParts[0].Substring(0, 1).ToUpper() + nameParts[1].Substring(0, 1).ToUpper() + nameParts[2].Substring(0, 1).ToUpper();
            }
        }
        
        public static string ReplaceASCIICharToAlphaNumeric(string s) {
            StringBuilder result = new StringBuilder(s.Length);
            s = ReplaceASCIICharToASCI(TranslateXmlEntitiesToString(s.ToLower()));
            for (int i = 0; i <= s.Length - 1; i++) {
                char c = s[i];
                int ci = Convert.ToInt32(c);
                if ((48 <= ci && ci <= 57) ||
                    (65 <= ci && ci <= 90) ||
                    (97 <= ci && ci <= 122) ||
                        c == '_' ) {
                    result.Append(c);
                }
            }
            return result.ToString().Replace("--", "-").Replace("--", "-");
        }
        public static string ReplaceDiacritics(this string str) {
            var chars =
                from c in str.Normalize(NormalizationForm.FormD).ToCharArray()
                let uc = CharUnicodeInfo.GetUnicodeCategory(c)
                where uc != UnicodeCategory.NonSpacingMark
                select c;

            var cleanStr = new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
            return cleanStr;
        }
        public static string ReplaceASCIICharToASCI(string s) {
            s = s.Replace("á", "a");
            s = s.Replace("à", "a");
            s = s.Replace("Á", "a");
            s = s.Replace("À", "a");
            s = s.Replace("A", "a");
            s = s.Replace("é", "e");
            s = s.Replace("è", "e");
            s = s.Replace("È", "e");
            s = s.Replace("É", "e");
            s = s.Replace("E", "e");
            s = s.Replace("í", "i");
            s = s.Replace("ï", "i");
            s = s.Replace("Í", "i");
            s = s.Replace("Ï", "i");
            s = s.Replace("I", "i");
            s = s.Replace("ó", "o");
            s = s.Replace("ò", "o");
            s = s.Replace("Ó", "o");
            s = s.Replace("Ò", "o");
            s = s.Replace("O", "o");
            s = s.Replace("ú", "u");
            s = s.Replace("ü", "u");
            s = s.Replace("Ú", "u");
            s = s.Replace("Ú", "u");
            s = s.Replace("Ü", "u");
            s = s.Replace("Ç", "C");
            s = s.Replace("ç", "c");
            s = s.Replace("Ñ", "N");
            s = s.Replace("ñ", "n");
            return s.ToLower();
        }
        public static string ReplaceASCIICharToASCICaseSensitive(string s) {
            s = s.Replace("á", "a");
            s = s.Replace("à", "a");
            s = s.Replace("Á", "A");
            s = s.Replace("À", "A");
            s = s.Replace("é", "e");
            s = s.Replace("è", "e");
            s = s.Replace("È", "E");
            s = s.Replace("É", "E");
            s = s.Replace("í", "i");
            s = s.Replace("ï", "i");
            s = s.Replace("Í", "I");
            s = s.Replace("Ï", "I");
            s = s.Replace("ó", "o");
            s = s.Replace("ò", "o");
            s = s.Replace("Ó", "O");
            s = s.Replace("Ò", "O");
            s = s.Replace("ú", "u");
            s = s.Replace("ü", "u");
            s = s.Replace("Ú", "U");
            s = s.Replace("Ú", "U");
            s = s.Replace("Ü", "U");
            s = s.Replace("Ç", "C");
            s = s.Replace("Ç", "c");
            s = s.Replace("Ñ", "N");
            s = s.Replace("ñ", "n");
            return s;
        }
        public static int GetStringIndent(string text, char c = ' ') {
            var arr = text.ToCharArray();
            for (var i = 0; i < text.Length; i++) {
                if (arr[i] != c) return i;
            }
            return text.Length;
        }
        public static int CountCharactersInString(string text, char character) {
            int i = 0;
            int counter = 0;
            do {
                i = text.IndexOf(character, i);
                if (i > -1) {
                    counter++;
                    i++;
                }
            } while (!(i == -1));
            return counter;
        }
        public static string Capitalize(string text) {
            //convierte cadenas del tipo hola_que_talestas a HolaQueTalEstas
            if (text == null || text.Length == 0) {
                return "";
            }
            if (text.Length == 1) {
                return text.ToUpper();
            }
            string s = "";
            s = text.Substring(0, 1).ToUpper() + text.Substring(1).ToLower();
            while (s.IndexOf("_") > -1) {
                int i = 0;
                i = s.IndexOf("_");
                s = s.Remove(i, 1);
                if (i < s.Length) {
                    s = s.Substring(0, i) + char.ToUpper(s[i]) + s.Substring(i + 1);
                }
            }
            return s;
        }
        public static string CapitalizeFirstChar(string text) {
            //convierte cadenas del tipo holaQueTal a HolaQueTal
            string s = "";
            if (text.Length == 0) {
                return text;
            }
            s = text.Substring(0, 1).ToUpper() + text.Substring(1);
            while (s.IndexOf("_") > -1) {
                int i = 0;
                i = s.IndexOf("_");
                s = s.Remove(i, 1);
                if (i < s.Length) {
                    s = s.Substring(0, i) + char.ToUpper(s[i]) + s.Substring(i + 1);
                }
            }
            return s;
        }
        public static string CapitalizeAndSpace(string text) {
            //convierte cadenas del tipo holaQueTal a Hola que tal
            string s = UnCapitalize(text);
            return CapitalizeFirstChar(s.Replace("_", " ").Replace("  ", " "));
        }
        public static string UnCapitalize(string text, bool ignoreConsecutiveCapitalLetters = false) {
            //convierte cadenas del tipo HolaQueTalEstas a hola_que_talestas
            var s = new StringBuilder();
            var prevWhatUpper = false;
            for (int i = 0; i <= text.Length - 1; i++) {
                char c = text[i];
                if (char.IsUpper(c) && i > 0 && !prevWhatUpper) {
                    s.Append("_");
                }
                s.Append(c);
                prevWhatUpper = (ignoreConsecutiveCapitalLetters ? char.IsUpper(c) : false);
            }
            return s.ToString();
        }
        public static string KebabCase(string name) {
            return SnakeCase(name).Replace("_", "-");
        }
        public static string SnakeCase(string name) {
            if (string.IsNullOrEmpty(name)) return name;
            var builder = new StringBuilder(name.Length + Math.Min(2, name.Length / 5));
            var previousCategory = default(UnicodeCategory?);
            for (var currentIndex = 0; currentIndex < name.Length; currentIndex++) {
                var currentChar = name[currentIndex];
                if (currentChar == '_') {
                    builder.Append('_');
                    previousCategory = null;
                    continue;
                }
                var currentCategory = char.GetUnicodeCategory(currentChar);
                switch (currentCategory) {
                    case UnicodeCategory.UppercaseLetter:
                    case UnicodeCategory.TitlecaseLetter:
                        if (previousCategory == UnicodeCategory.SpaceSeparator ||
                            previousCategory == UnicodeCategory.LowercaseLetter ||
                            previousCategory != UnicodeCategory.DecimalDigitNumber &&
                            previousCategory != null &&
                            currentIndex > 0 &&
                            currentIndex + 1 < name.Length &&
                            char.IsLower(name[currentIndex + 1])) {
                            builder.Append('_');
                        }

                        currentChar = char.ToLower(currentChar);
                        break;

                    case UnicodeCategory.LowercaseLetter:
                    case UnicodeCategory.DecimalDigitNumber:
                        if (previousCategory == UnicodeCategory.SpaceSeparator)
                            builder.Append('_');
                        break;

                    default:
                        if (previousCategory != null)
                            previousCategory = UnicodeCategory.SpaceSeparator;
                        continue;
                }

                builder.Append(currentChar);
                previousCategory = currentCategory;
            }
            return builder.ToString();
        } 
        public static string UnCapitalizeFirstChar(string text) {
            //convierte cadenas del tipo HolaQueTalEstas a hola_que_talestas
            if (text.Length > 0) {
                text = text.Substring(0, 1).ToLower() + text.Substring(1);
            }
            return text;
        }
        
        public static string CamelCase(string text) {
            //convierte cadenas del tipo hola-que-talestas a holaQueTalEstas
            if (text == null || text.Length == 0) return "";
            var result = new StringBuilder(text.Length);
            var index = 0;
            var cPrevSpace = false;
            foreach (char c in text.ToCharArray()) {
                if (c.Equals(' ') || c.Equals('-')) {
                    cPrevSpace = true;
                    continue;
                }
                if (cPrevSpace) {
                    result.Append(c.ToString().ToUpper());
                    cPrevSpace = false;
                } else {
                    result.Append(c);
                }
                index++;
            }
            return result.ToString();
        }
        public static string CamelToCapitalizeCase(string text) {
            //convierte cadenas del tipo holaQueTal a Hola que tal
            var ignoreConsecutiveCapitalLetters = false;
            var s = new StringBuilder();
            var prevWhatUpper = false;
            for (int i = 0; i <= text.Length - 1; i++) {
                char c = text[i];
                if (char.IsUpper(c) && i > 0 && !prevWhatUpper) {
                    s.Append("_");
                }
                s.Append(c);
                prevWhatUpper = (ignoreConsecutiveCapitalLetters ? char.IsUpper(c) : false);
            }
            return CapitalizeFirstChar(s.ToString().Replace("_", " ").Replace("  ", " "));
        }
        public static string CamelToSnakeCase(string text, bool ignoreConsecutiveCapitalLetters = false) {
            //convierte cadenas del tipo HolaQueTalEstas a hola_que_talestas
            var s = new StringBuilder();
            var prevWhatUpper = false;
            for (int i = 0; i <= text.Length - 1; i++) {
                char c = text[i];
                if (char.IsUpper(c) && i > 0 && !prevWhatUpper) {
                    s.Append("_");
                }
                s.Append(c);
                prevWhatUpper = (ignoreConsecutiveCapitalLetters ? char.IsUpper(c) : false);
            }
            return s.ToString().ToLower();
        }
        public static string CamelToKebabCase(string text, bool ignoreConsecutiveCapitalLetters = false) {
            //convierte cadenas del tipo HolaQueTalEstas a hola_que_talestas
            var s = new StringBuilder();
            var prevWhatUpper = false;
            for (int i = 0; i <= text.Length - 1; i++) {
                char c = text[i];
                if (char.IsUpper(c) && i > 0 && !prevWhatUpper) {
                    s.Append("-");
                }
                s.Append(c);
                prevWhatUpper = (ignoreConsecutiveCapitalLetters ? char.IsUpper(c) : false);
            }
            return s.ToString().ToLower();
        }
        public static string CamelToNormalCase(string text) {
            //convierte cadenas del tipo HolaQueTalEstas a hola que tal estas
            bool allCharactersAreUpperCase = true;
            StringBuilder s = new StringBuilder();
            char cPrev = 'a';
            for (int i = 0; i <= text.Length - 1; i++) {
                char c = text[i];
                if (!char.IsUpper(c)) {
                    allCharactersAreUpperCase = false;
                }
                if (char.IsUpper(c) && i > 0 && !char.IsUpper(cPrev)) {
                    s.Append("_");
                }
                s.Append(c);
                cPrev = c;
            }
            if (allCharactersAreUpperCase) {
                return CapitalizeFirstChar(s.ToString().Replace("_", " "));
            } else {
                return CapitalizeFirstChar(s.ToString().Replace("_", " ").ToLower());
            }
        }
        public static bool IsAllTextUppercase(string text) {
            foreach (char c in text) {
                if (char.IsLetterOrDigit(c)) {
                    if (char.IsLower(c)) {
                        return false;
                    }
                }
            }
            return true;
        }
        public static string GetStringRightPaddedWithSpaces(string s, int length) {
            var sb = new StringBuilder(s);
            while (sb.Length < length) {
                sb.Append(" ");
            }
            return sb.ToString();
        }
        public static string Space(int number, char character = ' ') {
            if (number == 0) return "";
            var sb = new StringBuilder(number);
            for (int i = 0; i < number; i++) {
                sb.Append(character);
            }
            return sb.ToString();
        }
        public static string Space(int number, string str) {
            if (number == 0) return "";
            var sb = new StringBuilder(number);
            for (int i = 0; i < number; i++) {
                sb.Append(str);
            }
            return sb.ToString();
        }
        public static bool Like(string text, string? pattern, bool ignoreCase = false) {
            if (pattern == null) return true;
            if (pattern.Length == 0) return false;
            while (pattern.IndexOf("**") != -1) pattern = pattern.Replace("**", "*");
            if (ignoreCase) {
                text = text.ToLower();
                pattern = pattern.ToLower();
            }
            var patternRegExp = new StringBuilder();
            patternRegExp.Append("^"); //start of text
            for (int i = 0; i <= pattern.Length - 1; i++) {
                char c = pattern[i];
                if (c == '*') { //zero or more characters
                    patternRegExp.Append(".*");
                } else if (c == '?') { // any single character
                    patternRegExp.Append(".");
                } else if (c == '#') { // any single digit
                    patternRegExp.Append("\\d");
                } else if (c == '[') { // character ranges
                    int j = pattern.IndexOf(']', i);
                    if (j < 0) j = text.Length;
                    bool exclude = (i+1 < pattern.Length && pattern[i+1] == '!');
                    if (exclude) i++;
                    patternRegExp.Append("[" + (exclude ? "^" : "") + pattern.Substring(i+1, j - i - 1) + "]");
                    i = j;
                } else if (c == '.' || c == '+' || c == '^' || c == '$' || c == '|' || c == '\\' || c == '(') { //special characters
                    patternRegExp.Append("\\" + c);
                } else {
                    patternRegExp.Append(c); //normal character
                }
            }
            patternRegExp.Append("\\z"); //end of text
            var patternRegExpString = patternRegExp.ToString();
            var regExp = new System.Text.RegularExpressions.Regex(patternRegExpString);
            var result = regExp.Match(text).Success;
            return result;
        }
        //public static bool Like2(string text, string pattern, bool ignoreCase = false) {
        //    int matched = 0;
        //    while (pattern.IndexOf("**") != -1) pattern = pattern.Replace("**", "*");
        //    if (ignoreCase) {
        //        text = text.ToLower();
        //        pattern = pattern.ToLower();
        //    }
        //    for (int i = 0; i < pattern.Length;) {
        //        if (matched > text.Length) return false;
        //        char c = pattern[i++];
        //        if (c == '[')  {
        //            bool exclude = (i < pattern.Length && pattern[i] == '!');
        //            if (exclude) i++;
        //            int j = pattern.IndexOf(']', i);
        //            if (j < 0) j = text.Length;
        //            var charList = CharListToSet(pattern.Substring(i, j - i));
        //            i = j + 1;
        //            if (charList.Contains(text[matched]) == exclude) return false;
        //            matched++;
        //        } else if (c == '?')  {
        //            matched++;
        //        } else if (c == '#') {
        //            if (!Char.IsDigit(text[matched])) return false;
        //            matched++;
        //        } else if (c == '*') {
        //            if (i < pattern.Length) {
        //                char next = pattern[i];
        //                int j = text.IndexOf(next, matched);
        //                if (j < 0) return false;
        //                matched = j;
        //            } else {
        //                matched = text.Length;
        //                break;
        //            }
        //        } else  {
        //            if (matched >= text.Length || c != text[matched]) return false;
        //            matched++;
        //        }
        //    }
        //    return (matched == text.Length);
        //}
        private static HashSet<char> CharListToSet(string charList) {
            var set = new HashSet<char>();
            for (int i = 0; i < charList.Length; i++) {
                if ((i + 1) < charList.Length && charList[i + 1] == '-') {
                    // Character range
                    char startChar = charList[i++];
                    i++; // Hyphen
                    char endChar = (char)0;
                    if (i < charList.Length) endChar = charList[i++];
                    for (int j = startChar; j <= endChar; j++) {
                        set.Add((char)j);
                    }
                } else {
                    set.Add(charList[i]);
                }
            }
            return set;
        }
        public static string ReplaceCaseInsensitive(string str, string oldValue, string newValue) {
            return Replace(str, oldValue, newValue, StringComparison.OrdinalIgnoreCase);
        }
        public static string Replace(string str, string oldValue, string newValue, StringComparison comparisonType) {
            newValue = newValue ?? string.Empty;
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(oldValue) || oldValue.Equals(newValue, comparisonType)) {
                return str;
            }
            int foundAt;
            while ((foundAt = str.IndexOf(oldValue, 0, comparisonType)) != -1) {
                str = str.Remove(foundAt, oldValue.Length).Insert(foundAt, newValue);
            }
            return str;
        }
        public static int Asc(char c) {
            return EncodingUtils.GetDefault().GetBytes(c.ToString().ToCharArray())[0];
        }
        public static int AscW(char c) {
            return c;
        }
        public static int AscW(string c) {
            if ((c == null) || (c.Length == 0)) {
                throw new ArgumentException("Argument_LengthGTZero1");
            }
            return c[0];
        }
        public static char Chr(int charCode) {
            char ch = '\0';
            if ((charCode < -32768) || (charCode > 0xFFFF)) {
                throw new Exception("Invalid char code");
            }
            if ((charCode >= 0) && (charCode <= 0x7F)) {
                return Convert.ToChar(charCode);
            }
            try {
                Encoding encoding__1 = EncodingUtils.GetDefault();
                if (encoding__1.IsSingleByte && ((charCode < 0) || (charCode > 0xFF))) {
                    throw new Exception("Invalid char code");
                }
                char[] chars = new char[2];
                byte[] bytes = new byte[2];
                Decoder decoder = encoding__1.GetDecoder();
                if ((charCode >= 0) && (charCode <= 0xFF)) {
                    bytes[0] = (byte)(charCode & 0xFF);
                    decoder.GetChars(bytes, 0, 1, (chars.ToString() ?? "").ToCharArray(), 0);
                } else {
                    bytes[0] = System.Convert.ToByte((charCode & 0xFF00) >> 8);
                    bytes[1] = (byte)(charCode & 0xFF);
                    decoder.GetChars(bytes, 0, 2, (chars.ToString() ?? "").ToCharArray(), 0);
                }
                ch = chars[0];
            } catch {
                throw;
            }
            return ch;
        }
        public static char ChrW(int charCode) {
            if ((charCode < -32768) || (charCode > 0xffff)) {
                throw new ArgumentException("Argument_RangeTwoBytes1");
            }
            return Convert.ToChar((int)(charCode & 0xffff));
        }
        public static bool IsNumeric(object text) {
            return IsNumeric(text.ToString());
        }
        public static bool IsNumeric(string text) {
            double retNum;
            return double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out retNum);
        }
        public static bool IsInteger(object text) {
            return int.TryParse(text.ToString(), out int retNum);
        }
        public static bool IsInteger(string text) {
            return int.TryParse(text, out int retNum);
        }
        public static bool IsLong(object text) {
            return long.TryParse(text.ToString(), out long retNum);
        }
        public static bool IsLong(string text) {
            return long.TryParse(text, out long retNum);
        }
        public static bool IsHexadecimalInt(string text) {
            return text.StartsWith("0x") && int.TryParse(text.Substring(2), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier, null, out int retNum);
        }
        public static bool IsHexadecimalLong(string text) {
            return text.StartsWith("0x") && long.TryParse(text.Substring(2), NumberStyles.HexNumber | NumberStyles.AllowHexSpecifier, null, out long retNum);
        }
        public static bool IsDate(string text) {
            DateTime retDate;
            return System.DateTime.TryParse(Convert.ToString(text), out retDate);
        }
        public static bool IsEmail(string text) {
            if (text == null) return false;
            if (text.StartsWith("@")) return false;
            string validEmailPattern = @"^(?!\.)(""([^""\r\\]|\\[""\r\\])*""|([-a-z0-9!#$%&'*+/=?^_`{|}~]|(?<!\.)\.)*)(?<!\.)@[a-z0-9][\w\.-]*[a-z0-9]\.[a-z][a-z\.]*[a-z]$";
            var regex = new System.Text.RegularExpressions.Regex(validEmailPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return regex.IsMatch(text.ToString());
        }
        public static bool IsPhone(string text) {
            if (text == null) return false;
            return IsNumeric(text);
        }
        public static string CleanPhone(string phone, string prefix) {
            if (prefix != "") phone = prefix + phone;
            phone = phone.Replace("(", "").Replace(")", "").Replace("-", "").Replace(".", "").Replace(" ", "").Replace("+", "").Trim();
            if (phone.Length > 15) phone = phone.Substring(phone.Length - 15);
            return phone;
        }
        public static bool IsIBAN(string bankAccount) {
            bankAccount = bankAccount.ToUpper();
            // IN ORDER TO COPE WITH THE REGEX BELOW
            if (String.IsNullOrEmpty(bankAccount))
                return false;
            else if (bankAccount.Length < 10)
                return false;
            else if (System.Text.RegularExpressions.Regex.IsMatch(bankAccount, "^[A-Z0-9]")) {
                bankAccount = bankAccount.Replace(" ", String.Empty);
                string bank = bankAccount.Substring(4, bankAccount.Length - 4) + bankAccount.Substring(0, 4);
                int asciiShift = 55;
                StringBuilder sb = new StringBuilder();
                foreach (char c in bank) {
                    if (Char.IsLetter(c)) {
                        int v = Asc(c) - asciiShift;
                        sb.Append(v);
                    } else if (int.TryParse(c.ToString(), out int v)) {
                        sb.Append(v);
                    } else {
                        return false;
                    }

                }
                string checkSumString = sb.ToString();
                int checksum = int.Parse(checkSumString.Substring(0, 1));
                for (int i = 1; i <= checkSumString.Length - 1; i++) {
                    int v = int.Parse(checkSumString.Substring(i, 1));
                    checksum *= 10;
                    checksum += v;
                    checksum = checksum % 97;
                }
                return checksum == 1;
            }
            return false;
        }
        //public static bool IsToken(object? expression) {
        //    if (expression == null) return false;
        //    var text = (expression.ToString() ?? "").ToCharArray();
        //    if (text.Length == 0) return false;
        //    int index = 0;
        //    foreach (var c in text) {
        //        if (index == 0 && !Char.IsLetter(c)) return false;
        //        if (!Char.IsLetterOrDigit(c) && c != '_') return false;
        //        index++;
        //    }
        //    return true;
        //}

        public static Type[] InferDataType(string? text) {
            var result = new List<Type>();
            if (text == null) return new Type[] { };
            if (text.Length == 0 || int.TryParse(text, out int resultInteger) || text.ToLower().Equals("nan") || text.ToLower().Equals("inf") || text.ToLower().Equals("-inf")) result.Add(typeof(int));
            if (text.Length == 0 || long.TryParse(text, out long resultLong) || text.ToLower().Equals("nan") || text.ToLower().Equals("inf") || text.ToLower().Equals("-inf")) result.Add(typeof(long));
            if (text.Length == 0 || double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo, out double resultDouble)) result.Add(typeof(double));
            if (text.Length == 0 || text.Equals("true") || text.Equals("True") || text.Equals("TRUE") || text.Equals("1") || text.Equals("false") || text.Equals("False") || text.Equals("FALSE") || text.Equals("0")) result.Add(typeof(bool));
            if (text.Length == 0 || DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_TIME, null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime resultTime)) result.Add(typeof(TimeSpan));
            if (text.Length == 0 || DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_TIME_MS, null, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime resultTimeMs)) result.Add(typeof(TimeSpan));
            if (text.Length == 0 || DateTimeUtils.Parse(text, true) != default(DateTime)) result.Add(typeof(DateTime));
            if (text.StartsWith("\"") && text.EndsWith("\"") && text.Length > 1) result.AddRange(InferDataType(text.Substring(1, text.Length - 2)));
            if (text.Length == 0 || result.Count == 0) result.Add(typeof(String));
            return result.ToArray();
        }
        public static string ReplaceUnicodeSpacingMark(string stringToReplace) {
            StringBuilder filenameStringBuilder = new StringBuilder();
            // Dins unicode existeixen diferents representacions per un mateix caràcter.
            // Dins d'aquestes representacions existeixen 2 maneres de representar els caràcters: Canónica (conserva informació) i Compatible (perd informació)
            // Canónica representa un caràcter tal com és
            // Compatible representa un caràcter visualment igual però internament pot ser que no ho sigui (nivell de bits)
            // Es normalitza l'string a una representació coneguda (descomposició canónica)
            stringToReplace = stringToReplace.Normalize(NormalizationForm.FormC);
            if (!stringToReplace.IsNormalized(NormalizationForm.FormC)) {
                foreach (char c in stringToReplace.ToCharArray()) {
                    if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark) {
                        filenameStringBuilder.Append(c);
                    }
                }
                return filenameStringBuilder.ToString().Normalize(NormalizationForm.FormC);
            } else {
                return stringToReplace;
            }
        }
        //public static string ReplaceUnicodeSpacingMark(string stringToReplace) {
        //    var filenameStringBuilder = new StringBuilder();
        //    // Dins unicode existeixen diferents representacions per un mateix caràcter.
        //    // Dins d'aquestes representacions existeixen 2 maneres de representar els caràcters: Canónica (conserva informació) i Compatible (perd informació)
        //    // Canónica representa un caràcter tal com és
        //    // Compatible representa un caràcter visualment igual però internament pot ser que no ho sigui (nivell de bits)
        //    // Es normalitza l'string a una representació coneguda (descomposició canónica)
        //    stringToReplace = stringToReplace.Normalize(NormalizationForm.FormC);
        //    if (!stringToReplace.IsNormalized(NormalizationForm.FormC)) {
        //        foreach (char c in stringToReplace.ToCharArray()) {
        //            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark) {
        //                filenameStringBuilder.Append(c);
        //            }
        //        }
        //        return filenameStringBuilder.ToString().Normalize(NormalizationForm.FormC);
        //    } else {
        //        return stringToReplace;
        //    }
        //}
        //public static string ReplaceASCIICharToASCIPrintable(string s, bool caseSensitive = false) {
        //    if (caseSensitive) {
        //        s = ReplaceASCIICharToASCICaseSensitive(s);
        //    } else {
        //        s = ReplaceASCIICharToASCI(s);
        //    }
        //    for (int i = 0; i <= s.Length - 1; i++) {
        //        char c = s[i];
        //        int ci = Convert.ToInt32(c);
        //        bool cValid = false;
        //        if ((48 <= ci && ci <= 57) ||
        //                (65 <= ci && ci <= 90) ||
        //                (97 <= ci && ci <= 122) ||
        //                c == '_') {
        //            cValid = true;
        //        }
        //        if (!cValid) {
        //            s = s.Replace(c, ' ');
        //        }
        //    }
        //    return s;
        //}





    }


}


