using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Queues {

    public interface IQueue : IDisposable {

        //methods
        Task WriteAsync(Message message, CancellationToken cancellationToken = default);
        Task <Message?> ReadAsync(int waitTimeout = 0, CancellationToken cancellationToken = default);
        Task DeleteAsync(Message message, CancellationToken cancellationToken = default);
        Task PurgeAsync(CancellationToken cancellationToken = default);

    }

}
