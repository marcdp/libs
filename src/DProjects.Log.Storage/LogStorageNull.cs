using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {


    //class
    public class LogStorageNull : ILogStorage {


        //methods
        public void Dispose() {
        }

        // helper to produce an empty IAsyncEnumerable<T> without external packages
        private static async IAsyncEnumerable<T> EmptyAsync<T>() {
            await Task.Yield();
            yield break;
        }

        //mehods
        public Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken) {
            return Task.FromResult(new LogStorageStats(0, 0, 0, null, null));
        }
        public Task RemoveBeforeAsync(int days, CancellationToken cancellationToken) {
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, CancellationToken cancellationToken) {
            return EmptyAsync<LogEntry>();
        }

        public IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, CancellationToken cancellationToken) {
            return EmptyAsync<LogEntry>();
        }
    }
}

