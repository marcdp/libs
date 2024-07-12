using DProjects.Fs;
using DProjects.Utils;

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {


    public class LogStorageFsFile : ILogStorage  {


        //variables
        private IFilesystem mFilesystem;
        private string mPath;
        private Encoding mEncoding;
        private ILogStorageEntryDeserializer mDeserializer;


        //constructor
        public LogStorageFsFile(IFilesystem filesystem, string path, ILogStorageEntryDeserializer deserializer, Encoding? encoding = null) {
            mFilesystem = filesystem;    
            mPath = path;
            mDeserializer = deserializer;
            mEncoding = encoding ?? System.Text.Encoding.UTF8;
        }
        public void Dispose() {
        }


        //methods
        public Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken) {
            var entry = mFilesystem.GetEntry(mPath);
            if (entry == null) throw new Exception("Path not found: " + mPath);
            var result = new LogStorageStats(1, 0, entry.Length, entry.Created, entry.Modified);
            return Task.FromResult(result);
        }
        public async IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, [EnumeratorCancellation] CancellationToken cancellationToken) {
            using (var textReader = new StreamReader(await mFilesystem.LoadReadStreamAsync(mPath, new(), cancellationToken), mEncoding)) {
                do {
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
            throw new NotImplementedException();
        }
        public async IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, [EnumeratorCancellation]CancellationToken cancellationToken) {
            var listOfLines = new List<string>();
            var skip = 64 * 1024;
            var delayBetweenChecks = 500;
            //fill buffer
            var entry = await mFilesystem.GetEntryAsync(mPath, cancellationToken);
            if (entry == null) throw new Exception("Unable to tail log file: path not found: " + mPath);
            var offset = Math.Max(0, entry.Length - skip);
            using (var readStream = await mFilesystem.LoadReadStreamAsync(mPath, new() {  Offset = offset }, cancellationToken)) {
                do {
                    var line = await StreamUtils.ReadLineAsync(readStream, mEncoding, cancellationToken);
                    if (line == null) {
                        break;
                    }
                    listOfLines.Add(line);
                    if (listOfLines.Count > lines) {
                        listOfLines.RemoveAt(0);
                    }
                } while (true);
                //return
                foreach (var line in listOfLines) {
                    yield return mDeserializer.Deserialize(line);
                }
                //follow
                if (follow) {
                    do {
                        var line = await StreamUtils.ReadLineAsync(readStream, mEncoding, cancellationToken);
                        if (line == null) {
                            await Task.Delay(delayBetweenChecks, cancellationToken);
                        } else {
                            yield return mDeserializer.Deserialize(line);
                        }
                    } while (true);
                }
            }
        }
    }


}