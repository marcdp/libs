using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Identity.SignIn {

    public class SignInNull() : ISignIn {


        //methods
        public Task<SignInResponse> SignInAsync(SignInRequest request, CancellationToken cancellationToken) {
            return Task.FromResult(SignInResponse.Failure());
        }
        
    }

}