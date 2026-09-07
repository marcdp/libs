
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {

    public interface ILogStorage : IDisposable {

        // methods
        /// <summary>Returns storage-level file statistics. Timestamps describe filesystem creation and modification times.</summary>
        Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken);
        /// <summary>Removes records older than a storage-specific cutoff. Implementations may reject unsupported retention semantics.</summary>
        Task RemoveBeforeAsync(int days, CancellationToken cancellationToken);
        /// <summary>Streams matching records in deterministic storage order. Date bounds are inclusive and level is a minimum severity.</summary>
        IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, CancellationToken cancellationToken);
        /// <summary>Streams up to the final <paramref name="lines"/> records and optionally follows append-only writes when supported.</summary>
        IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, CancellationToken cancellationToken);

    }


}

