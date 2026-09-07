using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Log.Storage.Serializers;
using System.Text;

namespace DProjects.Log.Storage.Tests {

    internal static class LogStorageTestUtils {

        // methods
        public static FilesystemMem CreateFilesystem() {
            var filesystem = new FilesystemMem(false, false);
            filesystem.CreateDirectory("/logs");
            return filesystem;
        }
        public static void SaveText(this IFilesystem filesystem, string path, string text) {
            filesystem.SaveTextFile(path, text, Encoding.UTF8);
        }
        public static async Task<List<LogEntry>> CollectAsync(IAsyncEnumerable<LogEntry> entries, CancellationToken cancellationToken = default) {
            var result = new List<LogEntry>();
            await foreach (var entry in entries.WithCancellation(cancellationToken)) result.Add(entry);
            return result;
        }
        public static LogStorageFsFile CreateRawFileStorage(IFilesystem filesystem, string path = "/logs/app.log") {
            return new LogStorageFsFile(filesystem, path, new LogStorageEntryDeserializerRaw());
        }
    }
}
