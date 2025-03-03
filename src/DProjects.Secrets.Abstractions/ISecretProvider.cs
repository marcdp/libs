using System.Threading.Tasks;
using System.Threading;

namespace DProjects.Secrets {

    public interface ISecretProvider {

        //methods
        Task<Secret?> GetAsync(string name, CancellationToken cancellationToken);
        Task<string[]> GetNamesAsync(CancellationToken cancellationToken);

    }
     


}