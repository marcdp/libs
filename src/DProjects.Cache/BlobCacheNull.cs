using System;
using System.IO;
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
            throw new NotImplementedException();
        }
        public Task<BlobCacheEntry> GetAsync(string key, TimeSpan expiration, Func<CancellationToken, Task<BlobCacheEntry>> func, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }
        public Task Clean(CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }


    }

}