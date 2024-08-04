using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Auth {

    public class AuthenticatorNull() : IAuthenticator {


        //methods
        public Task<AuthResponse> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken) {
            return Task.FromResult(AuthResponse.Failure());
        }
        
    }

}