using System.Threading.Tasks;
using System.Threading;

namespace DProjects.Secrets {

    public interface ISecretProvider {

        //methods
        Secret? Get(string name);
        Task<Secret?> GetAsync(string name, CancellationToken cancellationToken);        

    }
     


}