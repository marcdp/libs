using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {


    //class
    public class LogStorageNull : ILogStorage {


        // methods
        public void Dispose() {
        }
        public Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new LogStorageStats(0, 0, 0, null, null));
        }
        public Task RemoveBeforeAsync(int days, CancellationToken cancellationToken) {
            if (days < 0) throw new ArgumentOutOfRangeException(nameof(days));
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        public IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, CancellationToken cancellationToken) {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value) throw new ArgumentException("From must be earlier than or equal to To.", nameof(query));
            return EmptyAsync<LogEntry>(cancellationToken);
        }

        public IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, CancellationToken cancellationToken) {
            if (lines < 0) throw new ArgumentOutOfRangeException(nameof(lines));
            return EmptyAsync<LogEntry>(cancellationToken);
        }

        // methods (private)
        private static async IAsyncEnumerable<T> EmptyAsync<T>([EnumeratorCancellation] CancellationToken cancellationToken) {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Yield();
            yield break;
        }
    }
}

