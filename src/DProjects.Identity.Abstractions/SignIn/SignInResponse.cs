using System.Collections.Generic;
using System.Security.Claims;

using DProjects.Utils;

namespace DProjects.Identity.SignIn {


    public class SignInResponse(SignInStatus status, SignInField[] fields, ClaimsPrincipal? claimsPrincipal) {

        
        //props
        public SignInStatus Status { get; } = status;
        public Dictionary<string, string> Headers { get; } = new();
        public SignInField[] Fields { get; } = fields;
        public ClaimsPrincipal? ClaimsPrincipal { get; } = claimsPrincipal;

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
        public static SignInResponse DataRequired(SignInField[] fields) {
            return new SignInResponse(SignInStatus.DataRequired, fields, null);
        }
        public static SignInResponse Failure(SignInField[]? fields = null) {
            return new SignInResponse(SignInStatus.Failure, fields ?? [], null);
        }
        public static SignInResponse Success(ClaimsPrincipal claimsPrincipal, SignInField[]? fields = null) {
            return new SignInResponse(SignInStatus.Success, fields ?? [], claimsPrincipal);
        }
    }

}