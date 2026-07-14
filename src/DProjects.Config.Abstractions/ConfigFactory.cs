using System;
using System.Collections.Generic;
using System.Text;

using DProjects.Utils;

namespace DProjects.Config {


    public class ConfigFactory {

        public static T CreateFromUrl<T>(string src) {
            // create a config object from URL definition
            var uri = new Uri(src);
            var query = UrlUtils.ParseQueryString(src.IndexOf("?") != -1 ? src.Substring(src.IndexOf("?") + 1) : "");
            var type = typeof(T);
            foreach (var constructorInfo in type.GetConstructors()) {
                var parameterInfos = constructorInfo.GetParameters();
                var arguments = new object[parameterInfos.Length];
                for (var i = 0; i < parameterInfos.Length; i++) {
                    var parameterInfo = parameterInfos[i];
                    var argument = parameterInfo.DefaultValue;
                    if (parameterInfo.Name.Equals("scheme", StringComparison.OrdinalIgnoreCase)) {
                        argument = uri.Scheme;
                    } else if (parameterInfo.Name.Equals("path", StringComparison.OrdinalIgnoreCase)) {
                        argument = uri.AbsolutePath;
                    } else if (parameterInfo.Name.Equals("host", StringComparison.OrdinalIgnoreCase)) {
                        argument = uri.Host;
                    } else if (parameterInfo.Name.Equals("port", StringComparison.OrdinalIgnoreCase)) {
                        argument = uri.Port;
                    } else if (parameterInfo.Name.Equals("user", StringComparison.OrdinalIgnoreCase)) {
                        argument = uri.UserInfo.Split(':')[0];
                    } else if (parameterInfo.Name.Equals("password", StringComparison.OrdinalIgnoreCase)) {
                        argument = uri.UserInfo.Split(':')[1];
                    } else if (uri.Query.IndexOf("?" + parameterInfo.Name + "=", StringComparison.OrdinalIgnoreCase) != -1 || uri.Query.IndexOf("&" + parameterInfo.Name + "=", StringComparison.OrdinalIgnoreCase) != -1) {
                        var value = query.Get(parameterInfo.Name);
                        argument = ConvertUtils.To(value, parameterInfo.ParameterType, true)!;
                    }
                    arguments[i] = argument;
                }
                var instance = constructorInfo.Invoke(arguments);
                return (T)instance;
            }
            // throw exception 
            throw new Exception("Unable to create config instance from url: no constructor found.");
        }
        public static string ToUrl(string protocol, object config) {
            // create a URL definition from config object
            var queryParams = new List<string>();
            string? host = null;
            string? path = null;
            int? port = null;
            string? user = null;
            string? password = null;
            foreach (var propertyInfo in config.GetType().GetProperties()) {
                var name = propertyInfo.Name;
                var value = propertyInfo.GetValue(config);
                if (value != null) {
                    if (name.Equals("host", StringComparison.OrdinalIgnoreCase)) {
                        host = value.ToString()!;
                    } else if (name.Equals("path", StringComparison.OrdinalIgnoreCase)) {
                        path = value.ToString()!;
                    } else if (name.Equals("port", StringComparison.OrdinalIgnoreCase)) {
                        port = Convert.ToInt32(value);
                    } else if (name.Equals("user", StringComparison.OrdinalIgnoreCase)) {
                        user = value.ToString()!;
                    } else if (name.Equals("password", StringComparison.OrdinalIgnoreCase)) {
                        password = value.ToString()!;
                    } else { 
                        queryParams.Add($"{StringUtils.UnCapitalizeFirstChar(propertyInfo.Name)}={Uri.EscapeDataString(value.ToString()!)}");
                    }
                }
            }
            
            var result = new StringBuilder();
            result.Append($"{protocol}:");
            if (host != null || path != null || user != null) result.Append("//");
            if (user != null) result.Append(UrlUtils.UrlEncode(user) + ":" + UrlUtils.UrlEncode(password ?? "") + "@");
            if (host != null) result.Append(host);
            if (port != null) result.Append(":" + port);
            if (path != null) result.Append(path);
            if (queryParams.Count > 0) result.Append("?" + string.Join("&", queryParams));
            return result.ToString();
        }
        public static string ToUrl<T>(string protocol, object config) {
            return ToUrl(protocol, typeof(T));
        }

    }
}