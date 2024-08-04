using DProjects.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;

namespace DProjects.Auth {

    public class AuthRequest {


        //props
        public Dictionary<string, string> Headers { get; } = new();


        //methods
        public T GetHeader<T>(string name, T defaultValue) {
           if (Headers.TryGetValue(name, out var value)) {
                return ConvertUtils.To<T>(value);
            }
            return defaultValue;
        }
        public void SetHeader(string name, object value) {
            Headers[name] = value.ToString();
        }

        //static methods
        public static AuthRequest FromHttpBasic(string login, string password) {
            var request = new AuthRequest();
            request.Headers[DProjects.Utils.HttpUtils.HEADER_AUTHORIZATION] = "Basic " + DProjects.Utils.Base64Utils.ToBase64(DProjects.Utils.UrlUtils.UrlEncode(login) + ":" + DProjects.Utils.UrlUtils.UrlEncode(password));
            return request;
        }
    }

}