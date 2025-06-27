
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Commands {

    public interface ICommand {

        //methods
        Task<int> ExecuteAsync(CancellationToken cancellationToken);

    }

}