using System;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Fs;

namespace DProjects.Cache {

    public class BlobCacheNull() : IBlobCache {

         
        // ctor
        public void Dispose() {
        }

        // methods
        public Task SetAsync(BlobCacheEntry entry, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }
        public Task<BlobCacheEntry?> GetAsync(string key, CancellationToken cancellationToken = default) {
            return Task.FromResult<BlobCacheEntry?>(null);
        }
        public Task RefreshAsync(string key, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }
        public Task RemoveAsync(string pattern, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }
    }

}