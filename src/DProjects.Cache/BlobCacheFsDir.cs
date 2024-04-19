using System;
using System.Collections.Specialized;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Fs;
using DProjects.Utils;

using Microsoft.Extensions.Logging;

namespace DProjects.Cache {

    public class BlobCacheFsDir(IFilesystem filesystem, string path, ILogger<IFilesystem> logger) : IBlobCache {

        // constants
        private const string FILE_EXTENSION = ".entry";

        // ctor
        public void Dispose() {
        }

        // methods
        public async Task SetAsync(BlobCacheEntry entry, CancellationToken cancellationToken = default) {
            //if (value == null) throw new ArgumentNullException();
            //if (timeSpan == null) throw new ArgumentNullException();
            //if (timeSpan.TotalSeconds < 0) throw new ArgumentException();
            var keyEncoded = UrlUtils.UrlEncode(entry.Key);
            var tempPath = PathUtils.Combine(path, System.Guid.NewGuid().ToString() + ".tmp");
            // write to temp file
            using (var tempStream = await filesystem.LoadWriteStreamAsync(tempPath)) {
                entry.Headers[HttpUtils.HEADER_DATE] = DateTime.Now.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601).Replace('.', ':');
                //if not found!! ---> headers.Add(HttpUtils.HEADER_EXPIRES, DateTime.Now.Add(timeSpan).ToUniversalTime().ToString(ConstantsUtils.DATETIME_ISO8601).Replace('.', ':'));
                // headers
                var headers = HttpUtils.GetHttpHeadersString(entry.Headers);
                var headersBuffer = System.Text.Encoding.UTF8.GetBytes(headers);
                tempStream.Write(headersBuffer, 0, headersBuffer.Length);
                // content
                await entry.Stream.CopyToAsync(tempStream);
            }
            // move file to final path
            var itemPath = PathUtils.Combine(path, keyEncoded + FILE_EXTENSION);
            if (await filesystem.ExistsFileAsync(itemPath)) await filesystem.DeleteFileAsync(itemPath, cancellationToken);
            await filesystem.MoveAsync(tempPath, itemPath, new MoveSettings(), logger, cancellationToken);
        }
        public Task<BlobCacheEntry?> GetAsync(string key, CancellationToken cancellationToken = default) {
            return Task.FromResult<BlobCacheEntry?>(null);
        }
        public Task RefreshAsync(string key, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public Task RemoveAsync(string pattern, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
         
    }

}