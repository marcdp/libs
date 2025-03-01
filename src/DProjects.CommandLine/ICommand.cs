
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.CommandLine {

    public interface ICommand {

        //methods
        Task<int> ExecuteAsync(CancellationToken cancellationToken);

    }

}