using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
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
        public static NameValueCollection ParseQueryString(string query) {
            return HttpUtility.ParseQueryString(query);
        }
        public static NameValueCollection ParseQueryString(string query, Encoding encoding) {
            return HttpUtility.ParseQueryString(query, encoding);
        }
        public static T? ParseQueryString<T>(string query, string key) {
            var nameValueCollection = HttpUtility.ParseQueryString(query);
            var values = nameValueCollection.GetValues(key);
            if (values == null || values.Length == 0) return default;
            if (values.Length == 1) return ConvertUtils.To<T>(values[0]);
            return ConvertUtils.To<T>(values);
        }
        public static T ParseQueryString<T>(string query, string key, T defaultValue) {
            var nameValueCollection = HttpUtility.ParseQueryString(query);
            var values = nameValueCollection.GetValues(key);
            if (values == null || values.Length == 0) return defaultValue;
            if (values.Length == 1) return ConvertUtils.To<T>(values[0]);
            return ConvertUtils.To<T>(values);
        }



        //deserialize
        public class DeserializeSettings {
            public string PropertyNameScheme { get; set; } = "";
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
            if (!string.IsNullOrEmpty(uri.Host)) {
                var host = uri.Host;
                var port = "";
                if (uri.Host.IndexOf(":") != -1) {
                    host = uri.Host.Split(':')[0];
                    port = uri.Host.Split(':')[1];
                }
                if (!string.IsNullOrEmpty(host) && !string.IsNullOrEmpty(settings.PropertyNameHost)) {
                    dict[settings.PropertyNameHost] = host;
                }
                if (!string.IsNullOrEmpty(port) && !string.IsNullOrEmpty(settings.PropertyNamePort)) {
                    dict[settings.PropertyNamePort] = port;
                }
            }
            if (!string.IsNullOrEmpty(uri.AbsolutePath) && !string.IsNullOrEmpty(settings.PropertyNamePath)) {
                dict[settings.PropertyNamePath] = uri.AbsolutePath;
            }
            var query = ParseQueryString(uri.Query);
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
                        if (settings.ThrowExceptionIfPropertyNotFound) throw new Exception("Unable to deserialize: property not found: " + pair.Key);
                    } else {
                        pInfo.SetValue(instance, ConvertUtils.To(pair.Value, pInfo.PropertyType, false), new object[] { });
                    }
                }
            }
            return instance;
        }


    }

}


