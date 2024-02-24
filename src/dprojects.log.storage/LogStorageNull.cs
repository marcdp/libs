using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {


    //class
    public class LogStorageNull : ILogStorage {


        //methods
        public void Dispose() {
        }

        //mehods
        public Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }

        public Task<ILogStorageEntryReader> QueryAsync(LogStorageQuery query, CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }

        public Task RemoveBeforeAsync(int days, CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }

        public Task<ILogStorageEntryReader> TailAsync(int lines, bool follow, CancellationToken cancellationToken) {
            throw new System.NotImplementedException();
        }

    }
}

