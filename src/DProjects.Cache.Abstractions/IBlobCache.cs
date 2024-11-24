using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Cache {

    public interface IBlobCache : IDisposable {

        //methods
        Task SetAsync(BlobCacheEntry entry, CancellationToken cancellationToken = default);
        Task<BlobCacheEntry?> GetAsync(string key, CancellationToken cancellationToken = default);
        Task<BlobCacheEntry> GetAsync(string key, TimeSpan expiration, Func<CancellationToken, Task<BlobCacheEntry>> func, CancellationToken cancellationToken = default);
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);
        Task Clean(CancellationToken cancellationToken = default);

    }

}
