using System.IO;
using System.Threading.Tasks;

using DProjects.Db;

namespace DProjects.Commands {

    public interface IOutput {

        // methods
        Task WriteAsync(string text);
        Task WriteLineAsync(string text);
        TextWriter CreateTextWriter();
        IDBWriter CreateDBWriter(string format);
        bool IsTerminal { get; }
        

    }

}