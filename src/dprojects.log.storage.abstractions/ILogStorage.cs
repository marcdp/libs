
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {

    public interface ILogStorage  {

        //methods
        Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken);
        Task RemoveBeforeAsync(int days, CancellationToken cancellationToken);
        Task<ILogStorageEntryReader> QueryAsync(LogStorageQuery query, CancellationToken cancellationToken);
        Task<ILogStorageEntryReader> TailAsync(int lines, bool follow, CancellationToken cancellationToken);

    }


}

