using System.Threading.Tasks;

namespace DProjects.Auth {

    public interface IAuthProvider {

        //methods
        Task<AuthResponse> AuthenticateAsync(AuthRequest request);

    }
     


}