using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web;

namespace DProjects.Utils {


    public static class UrlUtils {

        //methods
        public static string UrlDecode(string text) {
            return System.Net.WebUtility.UrlDecode(text);
        }
        public static string UrlEncode(string text) {
            return System.Net.WebUtility.UrlEncode(text);
        }
        public static string UrlEncodePart(string text) {
            if (text.IndexOf("%") != -1) {
                text = text.Replace("%", "%25");
            }
            if (text.IndexOf("<") != -1) {
                text = text.Replace("<", "%3C");
            }
            if (text.IndexOf(">") != -1) {
                text = text.Replace(">", "%3E");
            }
            if (text.IndexOf("#") != -1) {
                text = text.Replace("#", "%23");
            }
            if (text.IndexOf("{") != -1) {
                text = text.Replace("{", "%7B");
            }
            if (text.IndexOf("}") != -1) {
                text = text.Replace("}", "%7D");
            }
            if (text.IndexOf("|") != -1) {
                text = text.Replace("|", "%7C");
            }
            if (text.IndexOf("\\") != -1) {
                text = text.Replace("\\", "%5C");
            }
            if (text.IndexOf("^") != -1) {
                text = text.Replace("^", "%5E");
            }
            if (text.IndexOf("~") != -1) {
                text = text.Replace("~", "%7E");
            }
            if (text.IndexOf("[") != -1) {
                text = text.Replace("[", "%5B");
            }
            if (text.IndexOf("]") != -1) {
                text = text.Replace("]", "%5D");
            }
            if (text.IndexOf("`") != -1) {
                text = text.Replace("`", "%60");
            }
            if (text.IndexOf(";") != -1) {
                text = text.Replace(";", "%3B");
            }
            if (text.IndexOf("?") != -1) {
                text = text.Replace("?", "%3F");
            }
            if (text.IndexOf(":") != -1) {
                text = text.Replace(":", "%3A");
            }
            if (text.IndexOf("@") != -1) {
                text = text.Replace("@", "%40");
            }
            if (text.IndexOf("=") != -1) {
                text = text.Replace("=", "%3D");
            }
            if (text.IndexOf("&") != -1) {
                text = text.Replace("&", "%26");
            }
            if (text.IndexOf("$") != -1) {
                text = text.Replace("$", "%24");
            }
            return text;
        }        

        //wrap/unwrap
        public static string WrapUrl(string innerUrl, string schema, string path, NameValueCollection query) {
            var result = new StringBuilder();
            result.Append(schema);
            result.Append(":");
            result.Append(innerUrl);
            result.Append("!");
            result.Append(path);
            var qsCounter = 0;
            foreach (var key in query.Keys) {
                result.Append(qsCounter == 0 ? "?" : "&");
                result.Append(UrlUtils.UrlEncode(query[key.ToString()]));
                qsCounter++;
            }
            return result.ToString();
        }
        public static (string, string) UnwrapUrl(string url) {
            var i = url.IndexOf(":");
            var k = url.LastIndexOf("!");
            var innerUrl = (k==-1 ? url.Substring(i + 1) : url.Substring(i + 1, k - i - 1));
            var outerUrl = url.Substring(0, i) + ":" + (k==-1 ? "" : url.Substring(k+1));            
            return (outerUrl, innerUrl);
        }


        //query
        public static NameValueCollection ParseQueryString(string query) {
            return HttpUtility.ParseQueryString(query);
        }
        public static NameValueCollection ParseQueryString(string query, Encoding encoding) {
            return HttpUtility.ParseQueryString(query, encoding);
        }
        public static T? GetQueryValue<T>(string query, string key) {
            var nameValueCollection = HttpUtility.ParseQueryString(query);
            var values = nameValueCollection.GetValues(key);
            if (values == null || values.Length == 0) return default;
            if (values.Length == 1) return ConvertUtils.To<T>(values[0]);
            return ConvertUtils.To<T>(values);
        }
        public static T GetQueryValue<T>(string query, string key, T defaultValue) {
            var nameValueCollection = HttpUtility.ParseQueryString(query);
            var values = nameValueCollection.GetValues(key);
            if (values == null || values.Length == 0) return defaultValue;
            if (values.Length == 1) return ConvertUtils.To<T>(values[0]);
            return ConvertUtils.To<T>(values);
        }
        public static object? GetQueryValue(Type type, string query, string key, object? defaultValue = null, bool throwExceptionIfUnableToConvert = false) {
            var nameValueCollection = HttpUtility.ParseQueryString(query);
            var values = nameValueCollection.GetValues(key);
            if (values == null || values.Length == 0) return defaultValue;
            if (values.Length == 1) return ConvertUtils.To(values[0], type, throwExceptionIfUnableToConvert);
            return ConvertUtils.To(values, type, throwExceptionIfUnableToConvert);
        }


        //deserialize
        public class DeserializeSettings {
            public string PropertyNameScheme { get; set; } = "Scheme";
            public string PropertyNameHost { get; set; } = "Host";
            public string PropertyNamePort { get; set; } = "Port";
            public string PropertyNamePath { get; set; } = "Path";
            public string PropertyNameUser { get; set; } = "User";
            public string PropertyNamePassword { get; set; } = "Password";
            public string[] Excluded { get; set; } = [];
            public bool ThrowExceptionIfPropertyNotFound { get; set; } = true;
        }
        public static T Deserialize<T>(string input, DeserializeSettings? settings = null) {
            return Deserialize<T>(new StringReader(input), settings);
        }
        public static T Deserialize<T>(TextReader reader, DeserializeSettings? settings = null) {
            return (T)Deserialize(typeof(T), reader, settings);
        }
        public static object Deserialize(Type type, TextReader reader, DeserializeSettings? settings = null) {
            if (settings == null) settings = new DeserializeSettings();
            var instance = Activator.CreateInstance(type);
            var url = reader.ReadToEnd();
            var uri = new Uri(url);
            //dict
            var dict = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(settings.PropertyNameScheme)) {
                dict[settings.PropertyNameScheme] = uri.Scheme;
            }
            if (!string.IsNullOrEmpty(uri.UserInfo)) {
                var userInfoArray = uri.UserInfo.Split(':');
                var user = true ? UrlDecode(userInfoArray[0]) : userInfoArray[0];
                if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(settings.PropertyNameUser)) {
                    dict[settings.PropertyNameUser] = user;
                }
                if (userInfoArray.Length > 1) {
                    var password = true ? UrlDecode(userInfoArray[1]) : userInfoArray[1];
                    if (!string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(settings.PropertyNamePassword)) {
                        dict[settings.PropertyNamePassword] = password;
                    }
                }
            }
            if (!string.IsNullOrEmpty(uri.Scheme) && !string.IsNullOrEmpty(settings.PropertyNameScheme)) {
                dict[settings.PropertyNameScheme] = uri.Scheme;
            }
            if (!string.IsNullOrEmpty(uri.Host)) {
                var host = uri.Host;
                var port = uri.Port;
                if (uri.Host.IndexOf(":") != -1) {
                    host = uri.Host.Split(':')[0];
                    port = int.Parse(uri.Host.Split(':')[1]);
                }
                if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(settings.PropertyNameHost)) {
                    dict[settings.PropertyNameHost] = host;
                }
                if (port>0 && !string.IsNullOrEmpty(settings.PropertyNamePort)) {
                    dict[settings.PropertyNamePort] = port.ToString();
                }
            }
            if (!string.IsNullOrEmpty(uri.AbsolutePath) && !string.IsNullOrEmpty(settings.PropertyNamePath)) {
                dict[settings.PropertyNamePath] = uri.AbsolutePath;
            }
            var query = HttpUtility.ParseQueryString(uri.Query);
            foreach (var key in query.Keys) {
                var name = key.ToString();
                dict[name] = query.Get(name);
            }
            //set    
            foreach (var pair in dict) {
                var excluded = false;
                foreach (var exc in settings.Excluded) if (exc.Equals(pair.Key, StringComparison.OrdinalIgnoreCase)) excluded = true;
                if (!excluded) {
                    var pInfo = instance.GetType().GetProperty(pair.Key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                    if (pInfo == null) {
                        if (settings.ThrowExceptionIfPropertyNotFound && !settings.PropertyNameScheme.Equals(pair.Key)) {
                            throw new Exception("Unable to deserialize: property not found: " + pair.Key);
                        }
                    } else {
                        pInfo.SetValue(instance, ConvertUtils.To(pair.Value, pInfo.PropertyType, false), new object[] { });
                    }
                }
            }
            return instance;
        }


        //serialize
        public enum PasswordMode {
            None,
            InUserInfo,
            InQuery
        }

        public class SerializeSettings {
            public string PropertyNameScheme { get; set; } = "";
            public string PropertyNameHost { get; set; } = "Host";
            public string PropertyNamePort { get; set; } = "Port";
            public string PropertyNamePath { get; set; } = "Path";
            public string PropertyNameUser { get; set; } = "User";
            public string PropertyNamePassword { get; set; } = "Password";
            public PasswordMode PasswordMode { get; set; } = PasswordMode.None;
            public string[] Excluded { get; set; } = [];
            public SerializeSettings() {
            }
        }


        //methods
        public static string Serialize(string schema, object? instance, SerializeSettings? settings = null) {
            var sw = new StringWriter();
            Serialize(schema, instance, sw, settings);
            return sw.ToString();
        }
        public static void Serialize(string schema, object? instance, TextWriter writer, SerializeSettings? settings = null) {
            if (settings == null) settings = new SerializeSettings();
            writer.Write(schema + ":");
            if (instance != null) {
                var newInstance = Activator.CreateInstance(instance.GetType());
                var properties = instance.GetType().GetProperties();
                var host = "";
                int port = 0;
                var user = "";
                var password = "";
                var path = "";
                var query = new StringBuilder();
                foreach (var pInfo in properties) {
                    if (settings.Excluded.Contains<string>(pInfo.Name)) {
                    } else if (pInfo.Name.Equals(settings.PropertyNameHost, StringComparison.OrdinalIgnoreCase)) {
                        host = ConvertUtils.To<string>(pInfo.GetValue(instance));
                    } else if (pInfo.Name.Equals(settings.PropertyNamePort, StringComparison.OrdinalIgnoreCase)) {
                        port = ConvertUtils.To<int>(pInfo.GetValue(instance));
                    } else if (pInfo.Name.Equals(settings.PropertyNameUser, StringComparison.OrdinalIgnoreCase)) {
                        user = ConvertUtils.To<string>(pInfo.GetValue(instance));
                    } else if (pInfo.Name.Equals(settings.PropertyNamePassword, StringComparison.OrdinalIgnoreCase)) {
                        password = ConvertUtils.To<string>(pInfo.GetValue(instance));
                    } else if (pInfo.Name.Equals(settings.PropertyNamePath, StringComparison.OrdinalIgnoreCase)) {
                        path = ConvertUtils.To<string>(pInfo.GetValue(instance));
                    } else {
                        var pValue = ConvertUtils.To<string>(pInfo.GetValue(instance));
                        var pValueDefault = ConvertUtils.To<string>(pInfo.GetValue(newInstance));
                        if (pValue != null && pValue != pValueDefault) {
                            query.Append(query.Length == 0 ? "?" : "&");
                            query.Append(StringUtils.UnCapitalizeFirstChar(pInfo.Name));
                            query.Append("=");
                            query.Append(UrlUtils.UrlEncode(pValue));
                        }
                    }
                }
                if (!string.IsNullOrEmpty(host) || port != 0 || !string.IsNullOrEmpty(user) || (!string.IsNullOrEmpty(password) && settings.PasswordMode != PasswordMode.None)) {
                    writer.Write("//");
                    if (settings.PasswordMode == PasswordMode.InUserInfo) {
                        if (!string.IsNullOrEmpty(user) || !string.IsNullOrEmpty(password)) {
                            writer.Write(UrlUtils.UrlEncode(user) + ":" + UrlUtils.UrlEncode(password) + "@");
                        }
                    } else {
                        if (!string.IsNullOrEmpty(user)) {
                            writer.Write(UrlUtils.UrlEncode(user) + "@");
                        }
                    }
                    writer.Write(host);
                    if (port != 0) {
                        writer.Write(":" + port);
                    }
                }
                writer.Write(path);
                writer.Write(query.ToString());
            }
        }
        //pretty
        public static string ToPrettyUrl(string s) {
            StringBuilder result = new StringBuilder(s.Length);
            s = StringUtils.ReplaceASCIICharToASCI(StringUtils.TranslateXmlEntitiesToString(s.ToLower()));
            for (int i = 0; i <= s.Length - 1; i++) {
                char c = s[i];
                int ci = Convert.ToInt32(c);
                if (c == ' ' || c == '-' || c == '/' || c == '\\' || c == ';' || c == ':') {
                    result.Append("-");
                } else if ((48 <= ci && ci <= 57) ||
                        (65 <= ci && ci <= 90) ||
                        (97 <= ci && ci <= 122) ||
                        c == '_' || c == '.') {
                    result.Append(c);
                }
            }
            return result.ToString().Replace("--", "-").Replace("--", "-");
        }

    }

}


