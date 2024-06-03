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
        public Task SetAsync(BlobCacheEntry entry, Stream stream, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }
        public Task<bool> GetAsync(string key, Func<BlobCacheEntry, Stream, Task> func, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
            return Task.CompletedTask;
        }

        
    }

}