
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {

    public interface ILogStorage : IDisposable {

        //methods
        Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken);
        Task RemoveBeforeAsync(int days, CancellationToken cancellationToken);
        IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, CancellationToken cancellationToken);
        IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, CancellationToken cancellationToken);

    }


}

