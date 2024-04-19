using System;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Cache {

    public interface IBlobCache : IDisposable {

        //methods
        Task SetAsync(BlobCacheEntry entry, CancellationToken cancellationToken = default);
        Task<BlobCacheEntry?> GetAsync(string key, CancellationToken cancellationToken = default);
        Task RemoveAsync(string pattern, CancellationToken cancellationToken = default);
        Task RefreshAsync(string key, CancellationToken cancellationToken = default);

    }

}
