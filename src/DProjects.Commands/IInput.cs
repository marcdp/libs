using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Commands {

    public interface IInput : IDisposable {

        //methods
        TextReader CreateTextReader();
        Task<string> ReadLineAsync(CancellationToken cancellationToken=default);

    }

}