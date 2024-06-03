using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Cache {

    public interface IBlobCache : IDisposable {

        //methods
        Task SetAsync(BlobCacheEntry entry, Stream stream, CancellationToken cancellationToken = default);
        Task<bool> GetAsync(string key, Func<BlobCacheEntry, Stream, Task> func, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    }

}
