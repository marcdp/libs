using System.IO;
using System.Threading.Tasks;

namespace DProjects.Commands {

    public interface IOutput {

        // methods
        Task WriteAsync(string text);
        Task WriteLineAsync(string text);
        TextWriter CreateTextWriter();

    }

}