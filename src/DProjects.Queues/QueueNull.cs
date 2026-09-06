using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Fs;

namespace DProjects.Queues {

    public class QueueNull() : IQueue {


        //ctor
        public void Dispose() {
        }

        //methods
        public Task WriteAsync(Message message, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }
        public Task<Message?> ReadAsync(int waitTimeout = 0, CancellationToken cancellationToken = default) {
            return Task.FromResult<Message?>(null);
        }
        public Task DeleteAsync(Message message, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }
        public Task PurgeAsync(CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }


    }

}