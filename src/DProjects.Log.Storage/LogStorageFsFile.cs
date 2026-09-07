using DProjects.Fs;
using DProjects.Utils;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {


    public class LogStorageFsFile : ILogStorage  {


        // consts
        private const int FOLLOW_DELAY_MS = 100;

        // vars
        private readonly IFilesystem mFilesystem;
        private readonly string mPath;
        private readonly Encoding mEncoding;
        private readonly ILogStorageEntryDeserializer mDeserializer;


        //constructor
        public LogStorageFsFile(IFilesystem filesystem, string path, ILogStorageEntryDeserializer deserializer, Encoding? encoding = null) {
            mFilesystem = filesystem;    
            mPath = path;
            mDeserializer = deserializer;
            mEncoding = encoding ?? System.Text.Encoding.UTF8;
        }
        //methods
        public void Dispose() {
        }
        public async Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken) {
            var entry = await mFilesystem.GetEntryAsync(mPath, cancellationToken);
            if (entry == null || !entry.IsFile()) throw new FileNotFoundException("Log storage file was not found.", mPath);
            return new LogStorageStats(1, 0, entry.Length, entry.Created, entry.Modified);
        }
        public async IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, [EnumeratorCancellation] CancellationToken cancellationToken) {
            if (query == null) throw new ArgumentNullException(nameof(query));
            ValidateQuery(query);
            var entry = await mFilesystem.GetEntryAsync(mPath, cancellationToken);
            if (entry == null || !entry.IsFile()) throw new FileNotFoundException("Log storage file was not found.", mPath);
            using (var textReader = new StreamReader(await mFilesystem.LoadReadStreamAsync(mPath, new(), cancellationToken), mEncoding)) {
                do {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = await textReader.ReadLineAsync();
                    if (line == null) break;
                    var logEntry = mDeserializer.Deserialize(line);
                    if (query.Check(logEntry)) {
                        yield return logEntry;
                    }
                } while (true);
            }
        }
        public Task RemoveBeforeAsync(int days, CancellationToken cancellationToken) {
            throw new NotSupportedException("Entry-level retention is not supported for single-file log storage.");
        }
        public async IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, [EnumeratorCancellation]CancellationToken cancellationToken) {
            if (lines < 0) throw new ArgumentOutOfRangeException(nameof(lines));
            cancellationToken.ThrowIfCancellationRequested();
            var entry = await mFilesystem.GetEntryAsync(mPath, cancellationToken);
            if (entry == null || !entry.IsFile()) throw new FileNotFoundException("Log storage file was not found.", mPath);

            // retain only the requested records while scanning the complete file, which is correct for arbitrary record sizes and encodings
            var tail = new Queue<string>(lines);
            using (var readStream = await mFilesystem.LoadReadStreamAsync(mPath, new(), cancellationToken))
            using (var textReader = new StreamReader(readStream, mEncoding)) {
                do {
                    cancellationToken.ThrowIfCancellationRequested();
                    var line = await textReader.ReadLineAsync();
                    if (line == null) break;
                    if (lines > 0) {
                        if (tail.Count == lines) tail.Dequeue();
                        tail.Enqueue(line);
                    }
                } while (true);

                // return the initial tail in physical line order
                foreach (var line in tail) {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return mDeserializer.Deserialize(line);
                }

                // follow only appends visible through this same open stream
                if (follow) {
                    do {
                        cancellationToken.ThrowIfCancellationRequested();
                        var line = await textReader.ReadLineAsync();
                        if (line == null) {
                            await Task.Delay(FOLLOW_DELAY_MS, cancellationToken);
                        } else {
                            yield return mDeserializer.Deserialize(line);
                        }
                    } while (true);
                }
            }
        }

        // methods (private)
        private static void ValidateQuery(LogStorageQuery query) {
            if (query.From.HasValue && query.To.HasValue && query.From.Value > query.To.Value) throw new ArgumentException("From must be earlier than or equal to To.", nameof(query));
        }
    }


}
