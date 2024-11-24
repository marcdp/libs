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
        public async Task SetAsync(BlobCacheEntry entry, CancellationToken cancellationToken = default) {
            //set blob
            var keyEncoded = UrlUtils.UrlEncode(entry.Key);
            var tempPath = PathUtils.Combine(path, keyEncoded + ".tmp");
            // create temp file to compute length
            var tempEntry = await filesystem.SaveFileAsync(tempPath, entry.Stream, new(), cancellationToken);
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
        public async Task<BlobCacheEntry?> GetAsync(string key, CancellationToken cancellationToken = default) { 
            //get blob
            var keyEncoded = UrlUtils.UrlEncode(key);
            var itemPath = PathUtils.Combine(path, keyEncoded + FILE_EXTENSION);
            var entry = await filesystem.GetEntryAsync(itemPath, cancellationToken);
            if (entry == null) return null;
            //read stream
            var stream = await filesystem.LoadReadStreamAsync(itemPath, new(), cancellationToken);
            //read headers
            HeadersUtils.Headers? headers = null;
            try {
                headers = await HeadersUtils.ReadHttpHeadersAsync(stream, cancellationToken: cancellationToken);
            } catch (Exception) {
                stream.Dispose();
                throw;
            }
            var contentLength = headers.Get<int>("Content-Length", 0);
            var limitedStream = new DProjects.Streams.LimitedInputStream(stream, contentLength);
            var blobCacheEntry = new BlobCacheEntry(key, limitedStream, headers);
            if (blobCacheEntry.Expires < DateTime.Now) {
                stream.Dispose();
                await RemoveAsync(key, cancellationToken);
                return null;
            }
            //return
            return blobCacheEntry;
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
            //remove blob
            var keyEncoded = UrlUtils.UrlEncode(key);
            var itemPath = PathUtils.Combine(path, keyEncoded + FILE_EXTENSION);
            return filesystem.DeleteFileAsync(itemPath, cancellationToken);
        }
        public async Task Clean(CancellationToken cancellationToken = default) {
            //clean expired blobs
            await foreach (var entry in filesystem.GetEntriesAsync(path, GetModes.Files, FILE_EXTENSION, cancellationToken)) {
                var valid = false;
                using (var stream = await filesystem.LoadReadStreamAsync(entry.Path, new(), cancellationToken)) {
                    try {
                        var headers = await HeadersUtils.ReadHttpHeadersAsync(stream, cancellationToken: cancellationToken);
                        var expires = headers.Get<DateTime>(HttpUtils.HEADER_EXPIRES, default);
                        if (expires < DateTime.Now) valid = true;
                    } catch (Exception) {
                        stream.Dispose();
                        throw;
                    }
                }
                if (!valid) {
                    await filesystem.DeleteFileAsync(entry.Path, cancellationToken);
                }
            }
        }


    }

}