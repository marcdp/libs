using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Utils;

namespace DProjects.Identity.SignIn {

    public class SignInStatic(string login, string password, string realm, Claim[] claims, int maxAttempts) : ISignIn {


        //methods
        public async Task<SignInResponse> SignInAsync(SignInRequest request, CancellationToken cancellationToken) {
            //if login not present, then ask for it
            if (!request.Headers.ContainsKey(SignInConstants.HEADER_LOGIN)) {
                return SignInResponse.DataRequired(new SignInField[] {
                    new SignInField(SignInConstants.HEADER_LOGIN, "login as", SignInFieldType.Text) {
                        PlaceHolder = "Enter your login",
                        Required = true,
                    },
                });
            }
            //if password not present, then ask for it
            if (!request.Headers.ContainsKey(SignInConstants.HEADER_PASSWORD)) {
                return SignInResponse.DataRequired(new SignInField[] {
                    new SignInField(SignInConstants.HEADER_PASSWORD, "password", SignInFieldType.Password) {
                        PlaceHolder = "Enter your password",
                        Required = true,
                    },
                });
            }
            //validate login and password
            var targetLogin = request.Headers[SignInConstants.HEADER_LOGIN];
            var targetPassword = request.Headers[SignInConstants.HEADER_PASSWORD];
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
                var response = SignInResponse.Success(claimsPrincipal, [
                    //new AuthField("",  "", AuthFieldType.Value) { 
                    //    Value = "ASDFASDFASKDFHASDKJFHASKDJHASKDJFHASKJDFA"
                    //},
                ]);
                return response;
            } else {
                //failure
                await Task.Delay(250);
                var response = SignInResponse.Failure();
                if (request.GetHeader<int>(SignInConstants.HEADER_ATTEMPT, 0) < maxAttempts - 1) {
                    response.SetHeader(SignInConstants.HEADER_RETRY, true);
                }
                return response;
            }
        }
        
    }

}