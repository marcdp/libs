using System;

using DProjects.Utils;

namespace DProjects.Config {


    public class Factory {

        public static T CreateFromUrl<T>(string src) {
            var uri = new Uri(src);
            var query = UrlUtils.ParseQueryString(src);
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
                        argument = query.Get(parameterInfo.Name);
                    }
                    arguments[i] = argument;
                }
                var instance = constructorInfo.Invoke(arguments);
                return (T) instance; 
            }
            // throw exception 
            throw new Exception("Unable to create config instance from url: no constructor found.");
        }

    }

}