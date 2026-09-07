
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {

    public interface ILogStorage : IDisposable {

        // methods
        /// <summary>Returns storage-level file statistics. Timestamps describe filesystem creation and modification times.</summary>
        Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken);
        /// <summary>
        /// Applies implementation-specific retention; <paramref name="days"/> must be greater than zero. Implementations may throw <see cref="NotSupportedException"/>.
        /// Directory storage deletes complete selected files whose
        /// provider-supplied modification timestamp is older than a cutoff captured at operation start. Deletion is best-effort: cancellation or I/O failure can leave partial
        /// completion, and retrying is expected to be safe.
        /// </summary>
        Task RemoveBeforeAsync(int days, CancellationToken cancellationToken);
        /// <summary>Streams matching records in deterministic storage order. Date bounds are inclusive and level is a minimum severity.</summary>
        IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, CancellationToken cancellationToken);
        /// <summary>
        /// Streams up to the final <paramref name="lines"/> records and optionally follows append-only writes when supported. Implementations may throw
        /// <see cref="NotSupportedException"/> for tail, follow, or both.
        /// </summary>
        IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, CancellationToken cancellationToken);

    }


}

