using System.Threading.Tasks;

namespace DProjects.Secrets {

    public interface ISecretProvider {

        //methods
        Task<Secret?> GetAsync(string name);

    }
     


}