using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DProjects.Auth {

    public class AuthProviderLoginPassword : IAuthProvider {


        //const
        private const string HEADER_LOGIN = "login";
        private const string HEADER_PASSWORD = "password";
        private const string AUTH_TYPE = "login-password";  


        //methods
        public async Task<AuthResponse> AuthenticateAsync(AuthRequest request) {
            //try authenticate
            if (request.Headers.ContainsKey(HEADER_LOGIN) && request.Headers.ContainsKey(HEADER_PASSWORD)) {
                var login = request.Headers[HEADER_LOGIN];
                var password = request.Headers[HEADER_PASSWORD];
                if (login.Equals("demo") && password.Equals("1234")) {
                    var identity = new Identity("demo", AUTH_TYPE, true, new string[] { "admin" }, new Dictionary<string, string>());
                    return AuthResponse.Success(identity);
                }
                return AuthResponse.Failure();
            }
            //data fields
            return AuthResponse.DataRequired([
                new AuthField(HEADER_LOGIN, "Login", AuthFieldType.String) {
                    PlaceHolder = "Enter your login",
                    Required = true,
                },
                new AuthField(HEADER_PASSWORD, "Password", AuthFieldType.Password) { 
                    PlaceHolder = "Enter your password",
                    Required = true,
                }
            ]);
        }
        
    }

}