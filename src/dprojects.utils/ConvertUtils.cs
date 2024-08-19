
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace DProjects.Utils {


    public static class ConvertUtils {


        //methods
        public class ToSimpleStringSettings {
            public bool AssumeMinus1IsNull = true;
            public int MaxLength = 0;
            public string DateTimeFormat = DateTimeUtils.DATETIME_ISO8601;
            public bool DateTimeUtc;
        }
        public static string ToSimpleString(object? aObject, ToSimpleStringSettings? settings = null) {
            var result = "";
            settings ??= new ToSimpleStringSettings();
            if (aObject is null) {
                result = "";
            } else if (aObject is bool) {
                result = (System.Convert.ToBoolean(aObject)) ? "Y" : "N";
            } else if (aObject is Single) {
                result = (System.Convert.ToSingle(aObject)).ToString().Replace(",", ".");
            } else if (aObject is double) {
                result = (System.Convert.ToSingle(aObject)).ToString().Replace(",", ".");
            } else if (aObject is short || aObject is int || aObject is long) {
                long aValue = System.Convert.ToInt64(aObject);
                if (settings.AssumeMinus1IsNull && aValue == -1) {
                    return "";
                }
                result = aValue.ToString();
            } else if (aObject is DateTime) {
                DateTime value = System.Convert.ToDateTime(aObject);
                if (value == default) {
                    return "";
                }
                if (settings.DateTimeUtc) value = value.ToUniversalTime();
                result = value.ToString(settings.DateTimeFormat);
            } else if (aObject is string[]) {
                string[] value = (string[])aObject;
                result = string.Join(",", value);
            } else if (aObject is object[]) {
                var aux = new StringBuilder();
                foreach (var item in (object[])aObject) {
                    if (aux.Length > 0) aux.Append(",");
                    aux.Append((item == null ? "null" : item.ToString()));
                }
                result = aux.ToString();
            } else if (aObject is char[]) {
                char[] value = (char[])aObject;
                result = string.Join(",", value);
            } else if (aObject is IDictionary) {
                var dict = (IDictionary)aObject;
                var aux = new StringBuilder();
                foreach (var key in dict.Keys) {
                    var value = dict[key];
                    if (aux.Length > 0) aux.Append("&");
                    aux.Append(UrlUtils.UrlEncode((key??"").ToString())).Append("=").Append(UrlUtils.UrlEncode((value??"").ToString()));
                }
                result = aux.ToString();
            } else if (aObject is Color) {
                var color = ((Color)aObject);
                result = $"#{color.R:X2}{color.G:X2}{color.B:X2}";
            } else if (aObject != null) {
                result = aObject.ToString() ?? "";
            }
            if (result != null && settings.MaxLength > 0 && result.Length > settings.MaxLength) {
                result = StringUtils.GetTextCutted(result, settings.MaxLength, true);
            }
            return result ?? "";
        }
        public static Type? ToSimpleType(string typeName) {
            if (typeName.Equals("bool", StringComparison.OrdinalIgnoreCase) || typeName.Equals("boolean", StringComparison.OrdinalIgnoreCase)) {
                return typeof(bool);
            } else if (typeName.Equals("bool[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(bool[]);
            } else if (typeName.Equals("short", StringComparison.OrdinalIgnoreCase) || typeName.Equals("int16", StringComparison.OrdinalIgnoreCase)) {
                return typeof(short);
            } else if (typeName.Equals("short[]", StringComparison.OrdinalIgnoreCase) || typeName.Equals("int16[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(short[]);
            } else if (typeName.Equals("int", StringComparison.OrdinalIgnoreCase) || typeName.Equals("int32", StringComparison.OrdinalIgnoreCase)) {
                return typeof(int);
            } else if (typeName.Equals("int[]", StringComparison.OrdinalIgnoreCase) || typeName.Equals("int32[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(int[]);
            } else if (typeName.Equals("long", StringComparison.OrdinalIgnoreCase) || typeName.Equals("int64", StringComparison.OrdinalIgnoreCase)) {
                return typeof(long);
            } else if (typeName.Equals("long[]", StringComparison.OrdinalIgnoreCase) || typeName.Equals("int64[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(long[]);
            } else if (typeName.Equals("float", StringComparison.OrdinalIgnoreCase)) {
                return typeof(float);
            } else if (typeName.Equals("float[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(float[]);
            } else if (typeName.Equals("double", StringComparison.OrdinalIgnoreCase)) {
                return typeof(double);
            } else if (typeName.Equals("double[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(double[]);
            } else if (typeName.Equals("string", StringComparison.OrdinalIgnoreCase)) {
                return typeof(string);
            } else if (typeName.Equals("string[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(string[]);
            } else if (typeName.Equals("date", StringComparison.OrdinalIgnoreCase) || typeName.Equals("datetime", StringComparison.OrdinalIgnoreCase)) {
                return typeof(DateTime);
            } else if (typeName.Equals("date[]", StringComparison.OrdinalIgnoreCase) || typeName.Equals("datetime[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(DateTime[]);
            } else if (typeName.Equals("time", StringComparison.OrdinalIgnoreCase)) {
                return typeof(TimeSpan);
            } else if (typeName.Equals("time[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(TimeSpan[]);
            } else if (typeName.Equals("guid", StringComparison.OrdinalIgnoreCase)) {
                return typeof(Guid);
            } else if (typeName.Equals("guid[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(Guid[]);
            } else if (typeName.Equals("byte", StringComparison.OrdinalIgnoreCase)) {
                return typeof(Byte);
            } else if (typeName.Equals("byte[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(Byte[]);
            } else if (typeName.Equals("char", StringComparison.OrdinalIgnoreCase)) {
                return typeof(Char);
            } else if (typeName.Equals("char[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(Char[]);
            } else if (typeName.Equals("decimal", StringComparison.OrdinalIgnoreCase)) {
                return typeof(decimal);
            } else if (typeName.Equals("decimal[]", StringComparison.OrdinalIgnoreCase)) {
                return typeof(decimal[]);
            }
            return null;
        }
        public static string FromSimpleType(Type aType) {
            if (aType == typeof(short)) {
                return "short";
            } else if (aType == typeof(short[])) {
                return "short[]";
            } else if (aType == typeof(int)) {
                return "int";
            } else if (aType == typeof(int[])) {
                return "int[]";
            } else if (aType == typeof(long)) {
                return "long";
            } else if (aType == typeof(long[])) {
                return "long[]";
            } else if (aType == typeof(float)) {
                return "float";
            } else if (aType == typeof(float[])) {
                return "float[]";
            } else if (aType == typeof(double)) {
                return "double";
            } else if (aType == typeof(double[])) {
                return "double[]";
            } else if (aType == typeof(decimal)) {
                return "decimal";
            } else if (aType == typeof(decimal[])) {
                return "decimal[]";
            } else if (aType == typeof(bool)) {
                return "bool";
            } else if (aType == typeof(bool[])) {
                return "bool[]";
            } else if (aType == typeof(DateTime)) {
                return "datetime";
            } else if (aType == typeof(DateTime[])) {
                return "datetime[]";
            } else if (aType == typeof(string)) {
                return "string";
            } else if (aType == typeof(string[])) {
                return "string[]";
            } else if (aType == typeof(byte)) {
                return "byte";
            } else if (aType == typeof(byte[])) {
                return "byte[]";
            } else if (aType == typeof(char)) {
                return "char";
            } else if (aType == typeof(char[])) {
                return "char[]";
            } else if (aType == typeof(TimeSpan)) {
                return "time";
            } else if (aType == typeof(TimeSpan[])) {
                return "time[]";
            } else if (aType == typeof(Guid)) {
                return "guid";
            } else if (aType == typeof(Guid[])) {
                return "guid[]";
            } else {
                return aType.FullName + ", " + aType.GetTypeInfo().Assembly.GetName().Name;
            }
        }
        public static string ToHexString(byte[] value) {
            if (value == null) return "";
            var hexNumbers = new StringBuilder();
            for (int i = 0; i <= value.Length - 1; i++) {
                string hexNumber = value[i].ToString("X");
                if (hexNumber.Length == 1) {
                    hexNumbers.Append("0");
                }
                hexNumbers.Append(hexNumber);
            }
            return hexNumbers.ToString();
        }
        public static byte[] FromHexString(string hexString) {
            if (hexString.Length % 2 != 0) {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
            }
            var HexAsBytes = new byte[(int)((double)hexString.Length / 2 - 1) + 1];
            for (int index = 0; index <= HexAsBytes.Length - 1; index++) {
                string byteValue = hexString.Substring(index * 2, 2);
                HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            return HexAsBytes;
        }
        public static DateTime FromEpochSeconds(long epoch) {
            var d = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return d.AddSeconds(epoch);
        }
        public static DateTime FromEpochMilliSeconds(long epoch) {
            var d = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return d.AddMilliseconds(epoch);
        }
        public static long ToEpochSeconds(DateTime aDate) {
            return System.Convert.ToInt64((aDate.ToUniversalTime().Ticks - 621355968000000000) / 10000000);
        }
        public static long ToEpoch(DateTime aDate) {
            return System.Convert.ToInt64((aDate.ToUniversalTime().Ticks - 621355968000000000) / 10000);
        }
        public static bool ToBoolean(object? aObject) {
            bool result = false;
            if (aObject is null) return false;
            if (aObject is string) {
                string str = (aObject.ToString() ?? "").ToLower();
                if (str.Equals("0") || str.Equals("false", StringComparison.InvariantCultureIgnoreCase) || str.Equals("N", StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrEmpty(str)) {
                    result = false;
                } else {
                    result = true;
                }
            } else if (aObject is bool) {
                result = System.Convert.ToBoolean(aObject);
            } else if (aObject is int) {
                result = System.Convert.ToInt32(aObject) != 0;
            } else if (aObject is long) {
                result = System.Convert.ToInt64(aObject) != 0;
            } else if (aObject is string) {
                result = (aObject.ToString() ?? "").Length > 0;
            } else if (aObject is DateTime) {
                result = !(System.Convert.ToDateTime(aObject) == System.Convert.ToDateTime(null));
            } else if (aObject is double) {
                result = System.Convert.ToDouble(aObject) > 0;
            } else {
                try {
                    result = System.Convert.ToBoolean(aObject);
                } catch {
                }
            }
            return result;
        }
        public static System.Drawing.Color ToColor(string text) {
            if (text.Equals("empty")) {
                return System.Drawing.Color.Empty;
            } else if (text.StartsWith("#") & text.Length == 7) {
                text = text.Substring(1);
                var parts = new string[] { text.Substring(0, 2), text.Substring(2, 2), text.Substring(4, 2) };
                return System.Drawing.Color.FromArgb(255, int.Parse(parts[0], System.Globalization.NumberStyles.HexNumber), int.Parse(parts[1], System.Globalization.NumberStyles.HexNumber), int.Parse(parts[2], System.Globalization.NumberStyles.HexNumber));
            } else if (text.StartsWith("#") & text.Length == 4) {
                text = text.Substring(1);
                var parts = new string[] { text.Substring(0, 1), text.Substring(1, 1), text.Substring(2, 1) };
                return System.Drawing.Color.FromArgb(255, int.Parse(parts[0], System.Globalization.NumberStyles.HexNumber) * 16, int.Parse(parts[1], System.Globalization.NumberStyles.HexNumber) * 16, int.Parse(parts[2], System.Globalization.NumberStyles.HexNumber) * 16);
            } else if (text.StartsWith("#") & text.Length == 9) {
                text = text.Substring(1);
                var parts = new string[] { text.Substring(0, 2), text.Substring(2, 2), text.Substring(4, 2), text.Substring(6, 2) };
                return System.Drawing.Color.FromArgb(int.Parse(parts[0], System.Globalization.NumberStyles.HexNumber), int.Parse(parts[1], System.Globalization.NumberStyles.HexNumber) * 16, int.Parse(parts[2], System.Globalization.NumberStyles.HexNumber) * 16, int.Parse(parts[3], System.Globalization.NumberStyles.HexNumber) * 16);
            } else if (text.StartsWith("rgb")) {
                text = text.Substring(3).Replace("(", "").Replace(")", "");
                var textParts = text.Split(',');
                if (textParts.Length == 3) {
                    int.TryParse(textParts[0], out int r);
                    int.TryParse(textParts[1], out int g);
                    int.TryParse(textParts[2], out int b);
                    return System.Drawing.Color.FromArgb(r, g, b);
                } else if (textParts.Length == 4) {
                    int.TryParse(textParts[0], out int a);
                    int.TryParse(textParts[1], out int r);
                    int.TryParse(textParts[2], out int g);
                    int.TryParse(textParts[3], out int b);
                    return System.Drawing.Color.FromArgb(a, r, g, b);
                } else
                    return System.Drawing.Color.FromName(text);
            } else {
                return System.Drawing.Color.FromName(text);
            }
        }

        public static int ToInteger(object? aObject) {
            if (aObject is null) return (int)0;
            if (aObject is string && aObject.Equals("")) {
                return 0;
            }
            return System.Convert.ToInt32(aObject);
        }
        public static long ToLong(object? aObject) {
            if (aObject is null) return (long)0;
            if (aObject is string && aObject.Equals("")) {
                return 0;
            }
            return System.Convert.ToInt64(aObject);
        }
        public static double ToDouble(object? aObject) {
            if (aObject is null) return 0.0;
            if (aObject is string && aObject.Equals("")) return 0;
            return double.Parse(aObject.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        public static decimal ToDecimal(object? aObject) {
            if (aObject is null) return 0.0m;
            if (aObject is string && aObject.Equals("")) return 0;
            return decimal.Parse(aObject.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        public static string ToString(object? aObject) {
            if (aObject is null) return "";
            if (aObject is string && aObject.Equals("")) return "";
            return aObject.ToString() ?? "";
        }
        public static float ToSingle(object? aObject) {
            if (aObject is null) return (float)0.0;
            if (aObject is string && aObject.Equals("")) return 0;
            return Single.Parse(aObject.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        public static string[] ToStringA(string text, char separator) {
            return ToStringA(text, separator, false);
        }
        public static string[] ToStringA(string text, char separator, bool trimValues) {
            var result = new List<string>();
            if (text != null && text.Length > 0) {
                int index1 = 0;
                int index2 = text.IndexOf(separator);
                while (index2 >= 0) {
                    string token = text.Substring(index1, index2 - index1);
                    result.Add(token);
                    index1 = index2 + 1;
                    index2 = text.IndexOf(separator, index1);
                }
                if (index1 < text.Length) {
                    string part = text.Substring(index1);
                    if (trimValues && (part.StartsWith(" ", StringComparison.Ordinal) || part.EndsWith(" ", StringComparison.Ordinal))) {
                        part = part.Trim();
                    }
                    result.Add(part);
                }
            }
            return [.. result];
        }
        public static string[] ToStringA(object? aObject) {
            if (aObject == null) {
                return [];
            }
            if (aObject is string) {
                return [aObject.ToString() ?? ""];
            }
            if (aObject is string[]) {
                return ((string[])aObject);
            }
            if (aObject is object[]) {
                object[] objectA = (object[])aObject;
                string[] result = new string[objectA.Length];
                for (int i = 0; i <= objectA.Length - 1; i++) {
                    if (objectA[i] is string) {
                        result[i] = (string)(objectA[i]);
                    } else {
                        result[i] = objectA[i].ToString() ?? "";
                    }
                }
                return result;
            }
            if (aObject is System.Collections.IList) {
                var objectA = (System.Collections.IList)aObject;
                string[] result = new string[objectA.Count];
                for (int i = 0; i <= objectA.Count - 1; i++) {
                    object? o = objectA[i];
                    if (o == null) {
                        result[i] = "";
                    } else if (o is string) {
                        result[i] = (string)o;
                    } else {
                        result[i] = o.ToString() ?? "";
                    }
                }
                return result;
            }
            return [aObject.ToString() ?? ""];
        }
        //public static object[] ToObjectA(object? aObject) {
        //    var result = new List<object>();
        //    if (aObject == null) {
        //        return [];
        //    } else if (aObject is object[]) {
        //        result.AddRange((object[])aObject);
        //    } else if (aObject is System.Array) {
        //        foreach (object? o in ((System.Array)aObject)) {
        //            if (o != null) result.Add(o);
        //        }
        //    } else {
        //        throw new NotImplementedException("ConvertUtils.ToObjectA not implemented: " + aObject.GetType().FullName);
        //    }
        //    return result.ToArray();
        //}
        //public static object?[] ToObjectA(object?[] source, Type type) {
        //    var result = (object?[])Array.CreateInstance(type, source.Length);
        //    for (var i = 0; i < source.Length; i++) {
        //        result[i] = source[i];
        //    }
        //    return result;
        //}
        //public static object? ToEnum(object? aObject, Type type) {
        //    if (aObject == null) {
        //        return null;
        //    } else if (int.TryParse((aObject).ToString(), out int aux)) {
        //        return Convert.ToInt32(aObject);
        //    } else {
        //        return System.Enum.Parse(type, aObject.ToString() ?? "", true);
        //    }
        //}
        public static T To<T>(object? aObject) {
            var type = typeof(T);
            var result = To(aObject, type, true);
            return (T)result!;
        }
        public static object? To(object? aObject, Type type, bool throwExceptionIfUnableToConvert, Func<Type, string, object>? getService = null) {
            if (aObject == null) {
            } else if (aObject.GetType() != type && !aObject.GetType().GetTypeInfo().IsSubclassOf(type)) {             
                if (type == typeof(DateTime) || type == typeof(DateTime?)) {
                    if (aObject is string && (aObject).ToString() == "") {
                        aObject = default(DateTime);
                    } else if (aObject is string && "now".Equals(aObject.ToString(), StringComparison.CurrentCultureIgnoreCase)) {
                        aObject = DateTime.Now;
                    } else if (aObject is string) {
                        if (aObject is string && aObject.ToString().IndexOf("- ")!=-1) {
                            aObject = aObject.ToString().Replace("- ", "-").Replace(".", ":");
                        }
                        try {
                            aObject = Convert.ToDateTime(aObject);
                        } catch (System.FormatException) {
                            aObject = DateTimeUtils.Parse((string)aObject);
                        }
                    } else {
                        aObject = Convert.ToDateTime(aObject);
                    }
                } else if (type == typeof(TimeSpan)) {
                    if (aObject is string) {
                        aObject = TimeSpan.Parse((string)aObject);
                    } else if (aObject == null) {
                        aObject = null;
                    } else {
                        aObject = TimeSpan.Parse(aObject.ToString() ?? "");
                    }
                } else if (type == typeof(int) || type == typeof(int?)) {
                    if (aObject == null) {
                        aObject = 0;
                    } else if (aObject.GetType() == typeof(string) && (aObject).ToString() == "") {
                        aObject = 0;
                    } else {
                        aObject = Convert.ToInt32(aObject);
                    }
                } else if (type == typeof(short) || type == typeof(short?)) {
                    if (aObject == null) {
                        aObject = (short)0;
                    } else if (aObject.GetType() == typeof(string) && (aObject).ToString() == "") {
                        aObject = (short)0;
                    } else {
                        aObject = Convert.ToInt16(aObject);
                    }
                } else if (type == typeof(long) || type == typeof(long?)) {
                    if (aObject == null) {
                        aObject = (long)0;
                    } else if (aObject.GetType() == typeof(string) && (aObject).ToString() == "") {
                        aObject = (long)0;
                    } else {
                        aObject = Convert.ToInt64(aObject);
                    }
                } else if (type == typeof(bool) || type == typeof(bool?)) {
                    if (aObject == null) {
                        aObject = false;
                    } else if ("1".Equals(aObject) || string.Equals("true", aObject.ToString(), StringComparison.OrdinalIgnoreCase) || string.Equals("yes", aObject.ToString(), StringComparison.OrdinalIgnoreCase) || string.Equals("y", aObject.ToString(), StringComparison.OrdinalIgnoreCase)) {
                        aObject = true;
                    } else if ("0".Equals(aObject) || string.Equals("false", aObject.ToString(), StringComparison.OrdinalIgnoreCase) || string.Equals("no", aObject.ToString(), StringComparison.OrdinalIgnoreCase) || string.Equals("n", aObject.ToString(), StringComparison.OrdinalIgnoreCase)) {
                        aObject = false;
                    } else {
                        aObject = Convert.ToBoolean(aObject);
                    }
                } else if (type == typeof(double) || type == typeof(double?)) {
                    if (aObject == null) {
                        aObject = 0.0;
                    } else if (aObject is string) {
                        if (string.IsNullOrEmpty((string)aObject)) return 0.0;
                        aObject = double.Parse((string)aObject, CultureInfo.InvariantCulture);
                    } else if (aObject is DateTime) {
                        aObject = (double)((DateTime)aObject).Ticks / TimeSpan.TicksPerMillisecond;
                    } else {
                        aObject = Convert.ToDouble(aObject);
                    }
                } else if (type == typeof(byte) || type == typeof(byte?)) {
                    aObject = Convert.ToByte(aObject);
                } else if (type == typeof(char) || type == typeof(char?)) {
                    aObject = Convert.ToChar(aObject);
                } else if (type == typeof(decimal) || type == typeof(decimal?)) {
                    if (aObject == null) {
                        aObject = 0.0m;
                    } else if (aObject is string) {
                        aObject = decimal.Parse((string)aObject, CultureInfo.InvariantCulture);
                    } else {
                        aObject = Convert.ToDecimal(aObject);
                    }
                } else if (type == typeof(string)) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (aObject is Type) {
                        aObject = FromSimpleType((Type)aObject);
                    } else if (aObject is DateTime) {
                        aObject = ((DateTime)aObject).ToString(DateTimeUtils.DATETIME_ISO8601);
                    } else if (aObject is bool) {
                        aObject = ((bool)aObject) ? "true" : "false";
                    } else if (aObject is decimal) {
                        aObject = ((decimal)aObject).ToString("0.0", CultureInfo.InvariantCulture);
                    } else if (aObject is double) {
                        aObject = ((double)aObject).ToString("0.0", CultureInfo.InvariantCulture);
                    } else if (aObject is float) {
                        aObject = ((float)aObject).ToString("0.0", CultureInfo.InvariantCulture);
                    } else if (aObject is byte[]) {
                        aObject = Convert.ToBase64String((byte[])aObject);
                    } else if (aObject.GetType().IsArray) {
                        var sb = new StringBuilder();
                        foreach (var o in (Array)aObject) {
                            if (o != null) sb.Append((sb.Length > 0 ? "," : "")).Append(To<string>(o));
                        }
                        aObject = sb.ToString();
                    } else {
                        aObject = Convert.ToString(aObject);
                    }
                } else if (type == typeof(Single)) {
                    if (aObject is string) {
                        aObject = float.Parse((string)aObject, CultureInfo.InvariantCulture);
                    } else {
                        aObject = Convert.ToDouble(aObject);
                    }
                } else if (type == typeof(byte[])) {
                    if (aObject == null) {
                        aObject = null;
                    } else {
                        aObject = Convert.FromBase64String(aObject.ToString() ?? "");
                    }
                } else if (type == typeof(string[])) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (aObject is Array) {
                        var o = new List<string>();
                        foreach (object? oo in ((Array)aObject)) {
                            if (oo != null) o.Add(oo.ToString() ?? "");
                        }
                        aObject = o.ToArray();
                    } else if (aObject is List<object>) {
                        var o = new List<string>();
                        foreach (object? oo in ((List<object>)aObject)) {
                            if (oo != null) o.Add(oo.ToString() ?? "");
                        }
                        aObject = o.ToArray();
                    } else if (aObject is string) {
                        var aux = (string)aObject;
                        if (aux.Length == 0) {
                            aObject = new string[] { };
                        } else {
                            aObject = (aObject.ToString() ?? "").Split(',');
                        }
                    }
                } else if (type == typeof(IList<string>)) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (aObject is Array) {
                        var aux = new List<string>();
                        foreach (object? oo in ((Array)aObject)) {
                            if (oo != null) aux.Add(oo.ToString() ?? "");
                        }
                        aObject = aux.ToArray();
                    } else {
                        var aux = new List<string>();
                        aux.AddRange((aObject.ToString() ?? "").Split(','));
                        aObject = aux;
                    }
                } else if (type == typeof(int[])) {
                    if (aObject is Array) {
                        var o = new List<int>();
                        foreach (object? oo in ((Array)aObject)) {
                            if (oo != null) o.Add(Convert.ToInt32(oo));
                        }
                        aObject = o.ToArray();
                    } else if (aObject is int) {
                        aObject = new int[] { Convert.ToInt32(aObject) };
                    } else if (aObject is long) {
                        aObject = new int[] { Convert.ToInt32(aObject) };
                    } else if (aObject is string) {
                        var result = new List<int>();
                        foreach (var str in ((string)aObject).Split(',')) {
                            if (int.TryParse(str, out int number)) {
                                result.Add(number);
                            }
                        }
                        aObject = result.ToArray();
                    }
                } else if (type == typeof(bool[])) {
                    if (aObject is Array) {
                        var o = new List<bool>();
                        foreach (object? oo in ((Array)aObject)) {
                            if (oo != null) o.Add(Convert.ToBoolean(oo));
                        }
                        aObject = o.ToArray();
                    } else if (aObject is int) {
                        aObject = new bool[] { Convert.ToBoolean(aObject) };
                    } else if (aObject is long) {
                        aObject = new bool[] { Convert.ToBoolean(aObject) };
                    } else if (aObject is bool) {
                        aObject = new bool[] { Convert.ToBoolean(aObject) };
                    } else if (aObject is string) {
                        var result = new List<bool>();
                        foreach (var str in ((string)aObject).Split(',')) {
                            if (str.Length>0) result.Add(Convert.ToBoolean(str));
                        }
                        aObject = result.ToArray();
                    }
                } else if (type == typeof(object[])) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (aObject is Array) {
                        var o = new List<object>();
                        foreach (object? oo in ((Array)aObject)) {
                            if (oo != null) o.Add(oo);
                        }
                        aObject = o.ToArray();
                    } else if (aObject is IList<object>) {
                        var o = new List<object>();
                        foreach (object? oo in (IList<object>)aObject) {
                            if (oo != null) o.Add(oo);
                        }
                        aObject = o.ToArray();
                    } else if (aObject is string) {
                        var aux = (string)aObject;
                        if (aux.Length == 0) {
                            aObject = new string[] { };
                        } else {
                            aObject = (aObject).ToString().Split(',');
                        }
                    }
                } else if (type.GetTypeInfo().IsEnum) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (int.TryParse(aObject.ToString(), out _)) {
                        aObject = Convert.ToInt32(aObject);
                    } else {
                        aObject = Enum.Parse(type, aObject.ToString() ?? "", true);
                    }
                } else if (type == typeof(Dictionary<string, string>)) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (aObject.GetType() == typeof(object[]) && ((object[])aObject).Length == 0) {
                        aObject = new Dictionary<string, string>();
                    } else if (aObject.GetType() == typeof(Dictionary<string, object>)) {
                        var c = (Dictionary<string, object>)aObject;
                        var c2 = new Dictionary<string, string>();
                        foreach (string key in c.Keys) {
                            object v = c[key];
                            if (v == null) {
                                c2[key] = "";
                            } else {
                                c2[key] = v.ToString() ?? "";
                            }
                        }
                        aObject = c2;
                    } else {
                        //aObject = Serialization.JsonDeserializer.Deserialize<VO>(aObject.ToString());
                        throw new NotImplementedException();
                    }
                //} else if (type == typeof(VO)) {
                //    if (aObject == null) {
                //        aObject = null;
                //    } else if (aObject.GetType() == typeof(object[]) && ((object[])aObject).Length == 0) {
                //        aObject = new VO();
                //    } else if (aObject.GetType() == typeof(Dictionary<string, object>)) {
                //        var c = (Dictionary<string, object>)aObject;
                //        var c2 = new VO();
                //        foreach (string key in c.Keys) {
                //            object v = c[key];
                //            if (v == null) {
                //                c2[key] = "";
                //            } else {
                //                c2[key] = v.ToString() ?? "";
                //            }
                //        }
                //        aObject = c2;
                //    } else {
                //        aObject = Serialization.JsonDeserializer.Deserialize<VO>(aObject.ToString());
                //    }
                } else if (type.IsSubclassOf(typeof(Dictionary<string, object?>))) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (aObject.GetType() == typeof(object[]) && ((object[])aObject).Length == 0) {
                        aObject = new Dictionary<string, string>();
                    } else if (aObject.GetType() == typeof(Dictionary<string, object?>)) {
                        var vo = (Dictionary<string, object?>)aObject;
                        var dict = (Dictionary<string, object?>)Activator.CreateInstance(type);
                        foreach (string key in vo.Keys) {
                            dict[key] = vo[key];
                        }
                        aObject = dict;
                    } else {
                        //var vo = Serialization.JsonDeserializer.Deserialize<VO>(aObject.ToString());
                        //var dict = (Dictionary<string, object?>) Activator.CreateInstance(type);
                        //foreach (string key in vo.Keys) {
                        //    dict[key] = vo[key];
                        //}
                        //aObject = dict;
                        throw new NotImplementedException();
                    }
                } else if (type == typeof(Uri)) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (aObject.GetType() == typeof(string)) {
                        var uri = (string)aObject;
                        if (uri.Length == 0) {
                            aObject = null;
                        } else {
                            aObject = new Uri(uri);
                        }
                    } else {
                        aObject = new System.Uri(aObject.ToString() ?? "");
                    }
                } else if (type == typeof(System.Net.IPAddress)) {
                    if (aObject == null) {
                        aObject = null;
                    } else if (aObject.GetType() == typeof(string)) {
                        var str = (string)aObject;
                        if (str.Length == 0 || str.Equals("*")) {
                            aObject = System.Net.IPAddress.Any;
                        } else {
                            aObject = System.Net.IPAddress.Parse(str);
                        }
                    } else {
                        aObject = System.Net.IPAddress.Parse(aObject.ToString());
                    }
                } else if (type == typeof(CultureInfo)) {
                    if (aObject == null) {
                        aObject = null;
                    } else {
                        aObject = new CultureInfo(aObject.ToString() ?? "");
                    }
                } else if (type == typeof(Encoding)) {
                    if (aObject == null) {
                        aObject = null;
                    } else {
                        aObject = Encoding.GetEncoding(aObject.ToString() ?? "");
                    }
                } else if (type == typeof(Type)) {
                    if (aObject == null) {
                        aObject = null;
                    } else {
                        var typeName = aObject.ToString() ?? "";
                        aObject = ToSimpleType(typeName);
                        aObject ??= Type.GetType(typeName);
                        if (aObject == null && throwExceptionIfUnableToConvert) {
                            throw new ArgumentException("Unable to find type '" + typeName + "'");
                        }
                    }
                } else if (type == typeof(Guid)) {
                    if (aObject == null) {
                        aObject = null;
                    } else {
                        aObject = Guid.Parse(aObject.ToString() ?? "");
                    }
                } else if (type == typeof(System.Xml.XmlDocument) && aObject is string) {
                    var xmlDocument = new System.Xml.XmlDocument();
                    var xml = (string)aObject;
                    if (xml.Length > 0) xmlDocument.LoadXml((string)aObject);
                    aObject = xmlDocument;
                } else if (type == typeof(Version)) {
                    if (aObject == null) {
                        aObject = null;
                    } else {
                        aObject = System.Version.Parse(aObject.ToString() ?? "");
                    }
                } else if (type == typeof(System.Drawing.Color)) {
                    if (aObject == null) {
                        aObject = null;
                    } else {
                        aObject = ToColor(aObject.ToString() ?? "");
                    }
                } else {
                    var elementType = type.GetElementType();
                    if (type.IsArray && elementType != null && elementType.IsEnum) {
                        if (aObject == null) {
                            aObject = null;
                        } else if (int.TryParse(aObject.ToString(), out _)) {
                            var value = Convert.ToInt32(aObject);
                            var arr = (Array?)Activator.CreateInstance(type, 1);
                            arr?.SetValue(value, 0);
                            aObject = arr;
                        } else if (aObject.ToString().Length == 0) {
                            aObject = (Array?)Activator.CreateInstance(type, 0);
                        } else {
                            var auxx = (aObject.ToString() ?? "").Split(',');
                            var arr = (Array?)Activator.CreateInstance(type, auxx.Length);
                            if (arr != null) {
                                for (var i = 0; i < auxx.Length; i++) {
                                    var value = System.Enum.Parse(elementType, auxx[i], true);
                                    arr.SetValue(value, i);
                                }
                            }
                            aObject = arr;
                        }
                    } else {
                        if (throwExceptionIfUnableToConvert) {
                            if (aObject == null) {
                                aObject = null;
                            } else if (aObject is string && getService != null) {
                                var str = (string)aObject;
                                aObject = getService(type, str);
                            } else {
                                throw new ArgumentException("Unable to convert object from type '" + aObject.GetType().Name + "' to type '" + type.Name + "'.");
                            }
                        }
                    }
                }
            }
            return aObject;
        }


    }

}


;