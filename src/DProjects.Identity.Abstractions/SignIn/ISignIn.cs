using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Identity.SignIn {

    public interface ISignIn {

        //methods
        Task<SignInResponse> SignInAsync(SignInRequest request, CancellationToken cancellationToken);

    }
     


}