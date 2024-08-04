using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Utils;

namespace DProjects.Auth {

    public class AuthenticatorStatic(string login, string password, string realm, Claim[] claims, int maxAttempts) : IAuthenticator {


        //methods
        public async Task<AuthResponse> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken) {
            //if login not present, then ask for it
            if (!request.Headers.ContainsKey(AuthConstants.HEADER_LOGIN)) {
                return AuthResponse.DataRequired(new AuthField[] {
                    new AuthField(AuthConstants.HEADER_LOGIN, "login as", AuthFieldType.Text) {
                        PlaceHolder = "Enter your login",
                        Required = true,
                    },
                });
            }
            //if password not present, then ask for it
            if (!request.Headers.ContainsKey(AuthConstants.HEADER_PASSWORD)) {
                return AuthResponse.DataRequired(new AuthField[] {
                    new AuthField(AuthConstants.HEADER_PASSWORD, "password", AuthFieldType.Password) {
                        PlaceHolder = "Enter your password",
                        Required = true,
                    },
                });
            }
            //validate login and password
            var targetLogin = request.Headers[AuthConstants.HEADER_LOGIN];
            var targetPassword = request.Headers[AuthConstants.HEADER_PASSWORD];
            if (targetLogin.Equals(login) && targetPassword.Equals(password)) {
                //success
                var claimIdentity = new ClaimsIdentity();
                claimIdentity.AddClaim(new Claim(ClaimTypes.Name, login));
                claimIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, login));
                claimIdentity.AddClaim(new Claim(ClaimTypes.AuthenticationMethod, "static"));
                claimIdentity.AddClaim(new Claim(ClaimTypes.AuthenticationInstant, System.DateTime.Now.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS)));
                claimIdentity.AddClaim(new Claim(ClaimTypes.Dns, realm));
                claimIdentity.AddClaims(claims);
                var claimsPrincipal = new ClaimsPrincipal(claimIdentity);
                var authUser = new AuthUser(claimsPrincipal);                
                var response = AuthResponse.Success(authUser, [
                    //new AuthField("",  "", AuthFieldType.Value) { 
                    //    Value = "ASDFASDFASKDFHASDKJFHASKDJHASKDJFHASKJDFA"
                    //},
                ]);
                return response;
            } else {
                //failure
                await Task.Delay(250);
                var response = AuthResponse.Failure();
                if (request.GetHeader<int>(AuthConstants.HEADER_ATTEMPT, 0) < maxAttempts - 1) {
                    response.SetHeader(AuthConstants.HEADER_RETRY, true);
                }
                return response;
            }
        }
        
    }

}