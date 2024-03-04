using DProjects.Fs;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Storage {


    public class LogStorageFsDir : ILogStorage  {


        //variables
        private readonly IFilesystem mFilesystem;
        private readonly string mPath;
        private readonly Encoding mEncoding;
        private readonly string mFileName;
        private readonly string mFileExtension;
        private readonly bool mRecursive;
        private readonly ILogStorageEntryDeserializer mDeserializer;


        //constructor
        public LogStorageFsDir(IFilesystem filesystem, string path, string fileName, string fileExtension ,bool recursive, ILogStorageEntryDeserializer deserializer, Encoding? encoding = null) {
            mFilesystem = filesystem;    
            mPath = path;
            mFileName = fileName;
            mFileExtension = fileExtension;
            mDeserializer = deserializer;
            mRecursive = recursive;
            mEncoding = encoding ?? System.Text.Encoding.UTF8;
        }
        public void Dispose() {
        }



        //methods
        public async Task<LogStorageStats> GetStatsAsync(CancellationToken cancellationToken) {
            var entry = mFilesystem.GetEntry(mPath);
            if (entry == null) throw new Exception("Path not found: " + mPath);
            var files = 0;
            var dirs = 1;
            var size = (long)0;
            DateTime? from = null;
            DateTime? to = null;
            await foreach (var childEntry in mFilesystem.GetEntriesAsync(mPath, (mRecursive ? GetModes.Descendants : GetModes.Files), "*" + mFileExtension)) {
                if (childEntry.IsFile()) {
                    files++;
                    size += childEntry.Length;
                    if (from == null) from = childEntry.Created;
                    to = childEntry.Modified;
                }
            }
            return new LogStorageStats(files, dirs, size, from, to);
        }
        public async IAsyncEnumerable<LogEntry> QueryAsync(LogStorageQuery query, [EnumeratorCancellation] CancellationToken cancellationToken) {
            var paths = new List<string>();
            await foreach (var entry in mFilesystem.GetEntriesAsync(mPath, (mRecursive ? GetModes.Descendants : GetModes.Files), "*" + mFileExtension)) {
                if (entry.IsFile()) {
                    using (var textReader = new StreamReader(await mFilesystem.LoadReadStreamAsync(entry.Path), mEncoding)) {
                        do {
                            var line = await textReader.ReadLineAsync();
                            if (line == null) break;
                            var logEntry = mDeserializer.Deserialize(line);
                            if (query.Check(logEntry)) {
                                yield return logEntry;
                            }
                        } while (true);
                    }
                    paths.Add(entry.Path);
                }
            }
        }
        public async Task RemoveBeforeAsync(int days, CancellationToken cancellationToken) {
            var entriesToRemove = new List<Entry>();
            await foreach (var entry in mFilesystem.GetEntriesAsync(mPath, (mRecursive ? GetModes.Descendants : GetModes.Files), "*" + mFileExtension)) {
                if (entry.IsFile()) {
                    if (entry.Modified < DateTime.Now.AddDays(-days)) {
                        entriesToRemove.Add(entry);
                    }
                }
            }
            foreach (var entry in entriesToRemove) {
                await mFilesystem.DeleteFileAsync(entry.Path, cancellationToken);
            }
        }
        public IAsyncEnumerable<LogEntry> TailAsync(int lines, bool follow, CancellationToken cancellationToken) {
            throw new NotImplementedException();
        }
    }


}