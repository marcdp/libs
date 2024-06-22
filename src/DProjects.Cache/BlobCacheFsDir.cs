using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Fs;
using DProjects.Utils;

using Microsoft.Extensions.Logging;

namespace DProjects.Cache {

    public class BlobCacheFsDir(IFilesystem filesystem, string path, ILogger<IFilesystem> logger) : IBlobCache {


        // constants
        private const string FILE_EXTENSION = ".blob";


        // ctor
        public void Dispose() {
        }


        // methods
        public async Task SetAsync(BlobCacheEntry entry, Stream stream, CancellationToken cancellationToken = default) {
            //set blob
            var keyEncoded = UrlUtils.UrlEncode(entry.Key);
            var tempPath = PathUtils.Combine(path, keyEncoded + ".tmp");
            // create temp file to compute length
            var tempEntry = await filesystem.SaveFileAsync(tempPath, stream, new(), cancellationToken);
            // write to temp2 file
            var tempPath2 = PathUtils.Combine(path, keyEncoded + ".tmp.2");
            using (var tempStream2 = await filesystem.LoadWriteStreamAsync(tempPath2, new(), cancellationToken)) {
                // set length
                entry.Headers.Set(HttpUtils.HEADER_CONTENT_LENGTH, tempEntry.Length.ToString());
                // set date header
                entry.Headers.Set(HttpUtils.HEADER_DATE, DateTime.Now.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601).Replace('.', ':'));
                // set expires header
                if (!entry.Headers.Contains(HttpUtils.HEADER_EXPIRES)) {
                    var timeSpan = TimeSpan.FromHours(1);
                    entry.Headers.Set(HttpUtils.HEADER_EXPIRES, DateTime.Now.Add(timeSpan).ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601).Replace('.', ':'));
                }
                // write headers
                await HeadersUtils.WriteHttpHeadersAsync(entry.Headers, tempStream2, cancellationToken: cancellationToken);
                // write content
                using (var tempStream = await filesystem.LoadReadStreamAsync(tempPath, new(), cancellationToken)) {
                    await tempStream.CopyToAsync(tempStream2);
                }
            }
            await filesystem.DeleteFileAsync(tempPath, cancellationToken);
            // move file to final path
            var itemPath = PathUtils.Combine(path, keyEncoded + FILE_EXTENSION);
            if (await filesystem.ExistsFileAsync(itemPath, cancellationToken)) await filesystem.DeleteFileAsync(itemPath, cancellationToken);
            await filesystem.MoveAsync(tempPath2, itemPath, new MoveSettings(), logger, cancellationToken);
        }
        public async Task<bool> GetAsync(string key, Func<BlobCacheEntry, Stream, Task> action, CancellationToken cancellationToken = default) {
            //get blob
            var keyEncoded = UrlUtils.UrlEncode(key);
            var itemPath = PathUtils.Combine(path, keyEncoded + FILE_EXTENSION);
            var entry = await filesystem.GetEntryAsync(itemPath);
            var returned = false;
            if (entry != null) {
                var expired = false;
                using (var stream = await filesystem.LoadReadStreamAsync(itemPath, new(), cancellationToken)) {
                    var blobCacheEntry = new BlobCacheEntry(key, await HeadersUtils.ReadHttpHeadersAsync(stream, cancellationToken: cancellationToken));
                    if (blobCacheEntry.Expires < DateTime.Now) {
                        expired = true;
                    } else {
                        await action.Invoke(blobCacheEntry, stream);
                        returned = true;
                    }
                };
                if (expired) await RemoveAsync(key);
            }
            return returned;
        }
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
            //remove blob
            var keyEncoded = UrlUtils.UrlEncode(key);
            var itemPath = PathUtils.Combine(path, keyEncoded + FILE_EXTENSION);
            return filesystem.DeleteFileAsync(itemPath, cancellationToken);
        }

    }

}