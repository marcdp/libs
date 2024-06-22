using System.Threading.Tasks;
using System.Threading;

namespace DProjects.Secrets {

    public interface ISecretManager {

        //props

        //methods
        Task<bool> Unseal(string password, CancellationToken cancellationToken);
        Task<bool> IsSealed(CancellationToken cancellationToken);
        Task Seal(string? password, CancellationToken cancellationToken);

        //methods
        Task<Secret[]> ListAsync(string? pattern, CancellationToken cancellationToken);
        Task<Secret?> GetAsync(string name, CancellationToken cancellationToken);
        Task SetAsync(Secret secret, CancellationToken cancellationToken);
        Task<bool> DelAsync(string name, CancellationToken cancellationToken);
        

    }
     


}