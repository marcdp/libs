using DProjects.Utils;
using System.Collections.Generic;


namespace DProjects.Identity.SignIn {

    public class SignInRequest {


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
         
    }

}