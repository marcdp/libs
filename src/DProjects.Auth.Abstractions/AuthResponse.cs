using System.Collections.Generic;

using DProjects.Utils;

namespace DProjects.Auth {


    public class AuthResponse(AuthStatus status, AuthField[] fields, AuthUser? user) {

        
        //props
        public AuthStatus Status { get; } = status;
        public Dictionary<string, string> Headers { get; } = new();
        public AuthField[] Fields { get; } = fields;
        public AuthUser? User { get; } = user;

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
        public static AuthResponse DataRequired(AuthField[] fields) {
            return new AuthResponse(AuthStatus.DataRequired, fields, null);
        }
        public static AuthResponse Failure(AuthField[]? fields = null) {
            return new AuthResponse(AuthStatus.Failure, fields ?? [], null);
        }
        public static AuthResponse Success(AuthUser user, AuthField[]? fields = null) {
            return new AuthResponse(AuthStatus.Success, fields ?? [], user);
        }
    }

}