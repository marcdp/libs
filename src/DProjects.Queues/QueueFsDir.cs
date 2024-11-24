using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;

using Microsoft.Extensions.Logging;

namespace DProjects.Queues {

    public class QueueFsDir(IFilesystem filesystem, string path, ILogger<IFilesystem> logger) : IQueue {


        //long
        private static long mCounter;

        // constants
        private const string FILE_EXTENSION = ".message";


        //vars
        private bool mInitialized = false;

        // ctor
        public void Dispose() {
        }


        // methods
        public async Task WriteAsync(Message message, CancellationToken cancellationToken = default) {
            if (!mInitialized) await InitializeAsync(cancellationToken);
            var id = System.Guid.NewGuid().ToString();
            var name = GetFileName(id);
            var pathTmp = PathUtils.Combine(path, "tmp", name);
            var pathNew = PathUtils.Combine(path, "new", name);
            message.Headers.Set(Message.HEADER_X_ID, id);
            var encoding = System.Text.Encoding.UTF8;
            using (var stream = filesystem.LoadWriteStream(pathTmp, new() { })) {
                await HeadersUtils.WriteHttpHeadersAsync(message.Headers, stream, encoding, cancellationToken);
                await stream.WriteAsync(message.Body, 0, message.Body.Length);
            }
            await filesystem.MoveAsync(pathTmp, pathNew, new(), logger, cancellationToken);
        }
        public async Task<Message?> ReadAsync(int waitTimeout = 0, CancellationToken cancellationToken = default) {
            if (!mInitialized) await InitializeAsync(cancellationToken);
            var pathNew = PathUtils.Combine(path, "new");
            for(var i = 0; i< waitTimeout; i++) {
                var mesage = await ReadNewMessage(cancellationToken);
                if (mesage != null) return mesage;
                await Task.Delay(1000);
            }
            return null;
        }
        public async Task DeleteAsync(Message message, CancellationToken cancellationToken = default) {
            if (!mInitialized) await InitializeAsync(cancellationToken);
            var id = message.Headers.Get<string>(Message.HEADER_X_ID, "");
            var pathTarget = PathUtils.Combine(path, "cur", id);
            await filesystem.DeleteFileAsync(pathTarget, cancellationToken);
        }
        public async Task PurgeAsync(CancellationToken cancellationToken = default) {
            if (!mInitialized) await InitializeAsync(cancellationToken);
            foreach (var key in new string[] { "new", "cur", "tmp" }) {
                var pathTarget = PathUtils.Combine(path, key);
                await foreach (var entry in filesystem.GetEntriesAsync(pathTarget, GetModes.Files, "*" + FILE_EXTENSION, cancellationToken)) {
                    await filesystem.DeleteFileAsync(entry.Path, cancellationToken);
                }
            }
        }


        //private
        private async Task InitializeAsync(CancellationToken cancellationToken) {
            foreach(var key in new string[] { "new", "cur", "tmp"}) {
                var pathTarget = PathUtils.Combine(path, key);
                if (!await filesystem.ExistsDirectoryAsync(pathTarget, cancellationToken)) {
                    await filesystem.CreateDirectoryAsync(pathTarget, cancellationToken);
                }
            }
            mInitialized = true;
        }
        public async Task<Message?> ReadNewMessage(CancellationToken cancellationToken = default) {
            var pathNew = PathUtils.Combine(path, "new");
            var pathCur = PathUtils.Combine(path, "cur");
            await foreach (var entry in filesystem.GetEntriesAsync(pathNew,GetModes.Files, "*" + FILE_EXTENSION , cancellationToken)) {
                var pathToMoveTo = PathUtils.Combine(pathCur, entry.Name);
                await filesystem.MoveAsync(entry.Path, pathToMoveTo, new(), logger, cancellationToken);
                using(var stream = await filesystem.LoadReadStreamAsync(pathToMoveTo, new(), cancellationToken )) {
                    var headers = await HeadersUtils.ReadHttpHeadersAsync(stream, System.Text.Encoding.UTF8, cancellationToken);
                    var body = await StreamUtils.ReadBytesAsync(stream, cancellationToken);
                    return new Message(body, headers);
                }
            }
            return null;
        }
        private string GetFileName(string id) {
            return DateTime.UtcNow.ToString("yyyyMMddhhmmssffffff-") + id + FILE_EXTENSION;
        }

    }

}