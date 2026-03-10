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


        // sync methods
        public void Set(BlobCacheEntry entry) {

        }
        public BlobCacheEntry? Get(string key) {
            throw new NotImplementedException();
        }
        public BlobCacheEntry Get(string key, TimeSpan expiration, Func<BlobCacheEntry> func) {
            throw new NotImplementedException();
        }
        public void Remove(string key) { 
        }


        // async methods
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