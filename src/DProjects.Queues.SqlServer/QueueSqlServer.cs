using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Fs;

namespace DProjects.Queues.SqlServer {

    public class QueueSqlServer() : IQueue {


        //ctor
        public void Dispose() {
            throw new NotImplementedException();
        }

        //methods
        public Task WriteAsync(Message message, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public Task<Message?> ReadAsync(int waitTimeout = 0, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public Task DeleteAsync(Message message, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public Task PurgeAsync(CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }


    }

}