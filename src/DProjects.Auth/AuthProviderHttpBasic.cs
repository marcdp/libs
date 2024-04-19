//using Microsoft.Extensions.Configuration;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;

//namespace DProjects.Auth {

//    public class AuthProviderLoginPassword : IAuthProvider {


//        //inner class
//        private class User(string name, string password, string[] roles, IReadOnlyDictionary<string,string> claims) {
//            public string Name { get; } = name;
//            public string Password { get; private set; } = password;
//            public string[] Roles { get; private set; } = roles;
//            public IReadOnlyDictionary<string, string> Claims { get; private set; } = claims;
//            public Identity ToIdentity() {
//                return new Identity(Name, "Basic", true, Roles, Claims);
//            }
//        }


//        //vars
//        private readonly Dictionary<string, User> mUsers = [];


//        //ctor
//        public AuthProviderLoginPassword() {
//        }
//        public AuthProviderLoginPassword(IConfiguration section) {
//            foreach (var userSection in section.GetChildren()) {
//                var name = userSection.Key;
//                var password = userSection.GetValue<string>("password")!;
//                var roles = (userSection.GetValue<string>("roles") != null ? userSection.GetValue<string>("roles")!.Split(',') : []);
//                var claims = new Dictionary<string, string>();
//                userSection.GetSection("claims").Bind(claims);
//                var user = new User(name, password, roles, claims);
//                mUsers[user.Name] = user;
//            }
//        }

//        //props
//        public AuthDefinition Config {
//            get { 
//                return new AuthDefinition("HttpBasic", "HTTP Basic", "Authenticate using HTTP Basic Authentication.");
//            }
//        }
    

//        //methods
//        public Task<AuthResponse> AuthenticateAsync(AuthRequest request) {
//            var header = request.Headers["Authorization"];
//            if (header != null && header.StartsWith("Basic ", StringComparison.InvariantCultureIgnoreCase)) {
//                var credentialsBase64 = header.Substring("Basic ".Length).Trim();
//                var encoding = System.Text.Encoding.UTF8;
//                var usernamePassword = encoding.GetString(Convert.FromBase64String(credentialsBase64));
//                int i = usernamePassword.IndexOf(":");
//                if (i != -1) {
//                    var username = usernamePassword.Substring(0, i);
//                    var password = usernamePassword.Substring(i + 1);
//                    if (mUsers.TryGetValue(username, out var user)) {
//                        if (user.Password.Equals(password)) {
//                            return Task.FromResult(new AuthResponse(true, user.ToIdentity()));
//                        }
//                    }
//                }
//            }
//            return Task.FromResult(new AuthResponse(false, null));
//        }

        
//    }

//}