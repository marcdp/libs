using DProjects.Fs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {


    public class LogStorageFsDir : ILogStorage  {


        // vars
        private readonly IFilesystem mFilesystem;
        private readonly string mPath;
        private readonly Encoding mEncoding;
        private readonly string mFilePattern;
        private readonly bool mRecursive;
        private readonly ILogStorageEntryDeserializer mDeserializer;


        //constructor
        public LogStorageFsDir(IFilesystem filesystem, string path, string filePattern, bool recursive, ILogStorageEntryDeserializer deserializer, Encoding? encoding = null) {
            mFilesystem = filesystem;    
            mPath = path;
            mFilePattern = string.IsNullOrWhiteSpace(filePattern) ? "*.log" : filePattern;
            mDeserializer = deserializer;
            mRecursive = recursive;
            mEncoding = encoding ?? System.Text.Encoding.UTF8;
        }
        public LogStorageFsDir(IFilesystem filesystem, string path, string fileName, string fileExtension, bool recursive, ILogStorageEntryDeserializer deserializer, Encoding? encoding = null)
            : this(filesystem, path, GetEffectiveFilePattern(fileName, fileExtension), recursive, deserializer, encoding) {
        }
        //methods
        public void Dispose() {
        }
        public async Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken) {
            var selectedFiles = await GetSelectedFilesAsync(cancellationToken);
            long size = 0;
            DateTime? from = null;
            DateTime? to = null;
            foreach (var entry in selectedFiles) {
                cancellationToken.ThrowIfCancellationRequested();
                size += entry.Length;
                if (!from.HasValue || entry.Created < from.Value) from = entry.Created;
                if (!to.HasValue || entry.Modified > to.Value) to = entry.Modified;
            }
            return new LogStorageStats(selectedFiles.Count, 1, size, from, to);
        }
        public async IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, [EnumeratorCancellation] CancellationToken cancellationToken) {
            if (query == null) throw new ArgumentNullException(nameof(query));
            ValidateQuery(query);
            var selectedFiles = await GetSelectedFilesAsync(cancellationToken);
            foreach (var entry in selectedFiles) {
                using (var textReader = new StreamReader(await mFilesystem.LoadReadStreamAsync(entry.Path, new(), cancellationToken), mEncoding)) {
                    do {
                        cancellationToken.ThrowIfCancellationRequested();
                        var line = await textReader.ReadLineAsync();
                        if (line == null) break;
                        var logEntry = mDeserializer.Deserialize(line);
                        if (query.Check(logEntry)) yield return logEntry;
                    } while (true);
                }
            }
        }
        public async Task RemoveBeforeAsync(int days, CancellationToken cancellationToken) {
            if (days <= 0) throw new ArgumentOutOfRangeException(nameof(days), "Retention days must be greater than zero.");
            var cutoff = DateTime.Now.AddDays(-days);
            var selectedFiles = await GetSelectedFilesAsync(cancellationToken);
            foreach (var entry in selectedFiles) {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.Modified < cutoff) await mFilesystem.DeleteFileAsync(entry.Path, cancellationToken);
            }
        }
        public IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, CancellationToken cancellationToken) {
            throw new NotSupportedException("Tail and follow are not supported for directory log storage because no active file is defined.");
        }

        // methods (private)
        private async Task<List<Entry>> GetSelectedFilesAsync(CancellationToken cancellationToken) {
            var root = await mFilesystem.GetEntryAsync(mPath, cancellationToken);
            if (root == null) throw new DirectoryNotFoundException("Log storage directory was not found: " + mPath);
            if (!root.IsDirectory()) throw new InvalidOperationException("Directory log storage requires a directory path.");
            var result = new List<Entry>();
            await foreach (var entry in mFilesystem.GetEntriesAsync(mPath, mRecursive ? GetModes.Descendants : GetModes.Files, mFilePattern, cancellationToken)) {
                cancellationToken.ThrowIfCancellationRequested();
                if (entry.IsFile()) result.Add(entry);
            }
            result.Sort((left, right) => StringComparer.Ordinal.Compare(NormalizePath(left.Path), NormalizePath(right.Path)));
            return result;
        }
        private static string GetEffectiveFilePattern(string fileName, string fileExtension) {
            if (!string.IsNullOrWhiteSpace(fileName)) return fileName;
            if (!string.IsNullOrWhiteSpace(fileExtension)) return fileExtension.StartsWith("*", StringComparison.Ordinal) ? fileExtension : "*" + fileExtension;
            return "*.log";
        }
        private static string NormalizePath(string path) {
            return path.Replace('\\', '/');
        }
        private static void ValidateQuery(LogStorageQuery query) {
            if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value) throw new ArgumentException("From must be earlier than or equal to To.", nameof(query));
        }
    }


}
