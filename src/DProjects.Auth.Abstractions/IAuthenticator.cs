using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Auth {

    public interface IAuthenticator {

        //methods
        Task<AuthResponse> AuthenticateAsync(AuthRequest request, CancellationToken cancellationToken);

    }
     


}