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
            return null;
        }
        public BlobCacheEntry Get(string key, TimeSpan expiration, Func<BlobCacheEntry> func) {
            return func();
        }
        public void Remove(string key) { 
        }


        // async methods
        public Task SetAsync(BlobCacheEntry entry, CancellationToken cancellationToken = default) {
            return Task.CompletedTask; 
        }
        public Task<BlobCacheEntry?> GetAsync(string key, CancellationToken cancellationToken = default) {
            return Task.FromResult<BlobCacheEntry?>(null);
        }
        public async Task<BlobCacheEntry> GetAsync(string key, TimeSpan expiration, Func<CancellationToken, Task<BlobCacheEntry>> func, CancellationToken cancellationToken = default) {
            //get blob
            var result = await GetAsync(key, cancellationToken);
            if (result == null) {
                using (var blobCacheentry = await func(cancellationToken)) {
                    blobCacheentry.Expires = DateTime.Now.Add(expiration);
                    await SetAsync(blobCacheentry);
                }
                return await GetAsync(key, cancellationToken) ?? throw new Exception("xxx");
            }
            return result;
        }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }
        public Task Clean(CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }


    }

}