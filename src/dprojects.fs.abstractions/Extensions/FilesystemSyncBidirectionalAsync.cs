using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemSyncBidirectionalAsync {


        //methods
        public static async Task SyncBidirectionalAsync(this IFilesystemAsync fs, string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (syncSettings.SourceExcludes == null) syncSettings.SourceExcludes = [];
            if (syncSettings.DestinationExcludes == null) syncSettings.DestinationExcludes = [];
            //status
            var status = new Status();
            await foreach (var entry in fs.GetEntriesAsync(source, GetModes.Descendants)) {
                bool isExcluded = false;
                foreach (string exclude in syncSettings.SourceExcludes) {
                    if (StringUtils.Like(entry.Path, exclude)) {
                        isExcluded = true;
                    }
                }
                if (!isExcluded) {
                    status.Add(entry);
                };
            }
            await foreach (var entry in fs.GetEntriesAsync(destination, GetModes.Descendants)) {
                bool isExcluded = false;
                foreach (string exclude in syncSettings.DestinationExcludes) {
                    if (StringUtils.Like(entry.Path, exclude)) {
                        isExcluded = true;
                    }
                }
                if (!isExcluded) {
                    status.Add(entry);
                }
            }
            //prev status
            string statusIndexPath = PathUtils.Combine(syncSettings.StatusPath, "index.db");
            Status? prevStatus = null;
            if (await fs.ExistsFileAsync(statusIndexPath)) {
                prevStatus = new Status(new StringReader(await fs.LoadTextFileAsync(statusIndexPath)));
            }
            //process
            var processedPaths = new List<string>();
            foreach (var entryA in status.GetEntries()) {
                var pathA = entryA.Path;
                var pathB = "";
                if (pathA.StartsWith(source)) {
                    pathB = PathUtils.Combine(destination, PathUtils.Uncombine(source, pathA));
                } else {
                    pathB = PathUtils.Combine(source, PathUtils.Uncombine(destination, pathA));
                }
                var entryB = status.GetEntry(pathB);
                //process
                if (processedPaths.Contains(pathA)) {
                } else if (entryB == null) {
                    if (prevStatus == null) {
                        // a --> b (first synchronization)
                        await CopyEntryAsync(fs, entryA, pathB, processedPaths, syncSettings, logger, status, cancellationToken);
                    } else {
                        var seBprev = prevStatus.GetEntry(pathB);
                        if (seBprev == null) {
                            // a --> b
                            await CopyEntryAsync(fs, entryA, pathB, processedPaths, syncSettings, logger, status, cancellationToken);
                        } else {
                            //remove a
                            await DeleteEntryAsync(fs, entryA, processedPaths, syncSettings, logger, status, cancellationToken);
                        }
                    }
                } else {
                    if (prevStatus == null) {
                        // CONFLICT: copy newer file to other side (first synchronization)
                        await CopyNewerEntryAsync(fs, entryA, entryB, processedPaths, syncSettings, logger, status, cancellationToken);
                    } else {
                        var entryAprev = prevStatus.GetEntry(pathA);
                        var entryBprev = prevStatus.GetEntry(pathB);
                        if (entryAprev == null || entryBprev == null) {
                            // CONFLICT: copy newer file to other side (the file apears in source and dest, but NOT in status!!!)
                            await CopyNewerEntryAsync(fs, entryA, entryB, processedPaths, syncSettings, logger, status, cancellationToken);
                        } else {
                            // CONFLICT: files exists in both places
                            if (entryAprev.Modified == entryA.Modified && entryBprev.Modified == entryB.Modified) {
                                //no changed
                            } else if (entryAprev.Modified != entryA.Modified && entryBprev.Modified == entryB.Modified) {
                                //changed entryA: a --> b
                                await CopyEntryAsync(fs, entryA, pathB, processedPaths, syncSettings, logger, status, cancellationToken);
                            } else if (entryAprev.Modified == entryA.Modified && entryBprev.Modified != entryB.Modified) {
                                //changed entryB
                                await CopyEntryAsync(fs, entryB, pathA, processedPaths, syncSettings, logger, status, cancellationToken);
                            } else if (entryAprev.Modified != entryA.Modified && entryBprev.Modified != entryB.Modified) {
                                // CONFLICT: copy newer file to other side (both changed)
                                await CopyNewerEntryAsync(fs, entryA, entryB, processedPaths, syncSettings, logger, status, cancellationToken);
                            }
                        }
                    }
                }
            }
            //save status
            await fs.SaveTextFileAsync(statusIndexPath, status.Serialize(), Encoding.UTF8);
        }
        private static async Task<bool> DeleteEntryAsync(IFilesystemAsync fs, Entry entry, List<string> processedPaths, SyncSettings syncSettings, ILogger<IFilesystem> logger, Status status, CancellationToken cancellationToken) {
            for (var trie = 0; trie < syncSettings.Tries; trie++) {
                logger.LogInformation("delete {path}", entry.Path);
                try {
                    if (entry.IsDirectory()) {
                        await fs.DeleteDirectoryAsync(entry.Path, cancellationToken);
                    } else {
                        await fs.DeleteFileAsync(entry.Path, cancellationToken);
                    }
                    status.Remove(entry.Path);
                    processedPaths.Add(entry.Path);
                    return true;
                } catch (Exception ex) {
                    logger.LogError("Error deleting {path}: {message} {ex}", entry.Path, ex.Message, ex);
                    if (trie == syncSettings.Tries - 1) {
                        if (!syncSettings.IgnoreErrors) throw;
                    } else {
                        System.Threading.Thread.Sleep(250);
                    }
                }
            }
            return false;
        }
        private static async Task<bool> CopyNewerEntryAsync(IFilesystemAsync fs, Entry entryA, Entry entryB, List<string> processedPaths, SyncSettings syncSettings, ILogger<IFilesystem> logger, Status status, CancellationToken cancellationToken) {
            if (entryA.Modified < entryB.Modified) {
                return await CopyEntryAsync(fs, entryB, entryA.Path, processedPaths, syncSettings, logger, status, cancellationToken );
            } else {
                return await CopyEntryAsync(fs, entryA, entryB.Path, processedPaths, syncSettings, logger, status, cancellationToken);
            }
        }
        private static async Task<bool> CopyEntryAsync(IFilesystemAsync fs, Entry source, string destination, List<string> processedPaths, SyncSettings syncSettings, ILogger<IFilesystem> logger, Status status, CancellationToken cancellationToken) {
            for (var trie = 0; trie < syncSettings.Tries; trie++) {
                logger.LogInformation("copy {from} to {to}", source.Path, destination);
                try {
                    if (source.IsDirectory()) {
                        var entry = await fs.CreateDirectoryAsync(destination, cancellationToken);
                        status.Modify(entry.Path, entry);
                    } else {
                        using (var stream = await fs.LoadReadStreamAsync(source.Path)) {
                            var entry = await fs.SaveFileAsync(destination, stream);
                            status.Modify(entry.Path, entry);
                        }
                    }
                    processedPaths.Add(source.Path);
                    processedPaths.Add(destination);
                    return true;
                } catch (Exception ex) {
                    logger.LogError("Error copying {from} to {to}: {message} {ex}", source.Path, destination, ex.Message, ex);
                    if (trie == syncSettings.Tries - 1) {
                        if (!syncSettings.IgnoreErrors) throw;
                    } else {
                        System.Threading.Thread.Sleep(250);
                    }
                }
            }
            return false;
        }

        //inner class
        private class Status {
            //variables
            private SortedDictionary<string, Entry> mEntries;
            //constructor
            public Status() {
                mEntries = new SortedDictionary<string, Entry>();
            }
            public Status(TextReader reader) : this() {
                do {
                    var line = reader.ReadLine();
                    if (line == null) break;
                    string[] lineParts = line.Split('|');
                    var entry = new Entry(lineParts[0],
                        lineParts[1].Equals("dir") ? EntryType.Directory : EntryType.File,
                        new DateTime(long.Parse(lineParts[2]), DateTimeKind.Utc).ToLocalTime(),
                        new DateTime(long.Parse(lineParts[3]), DateTimeKind.Utc).ToLocalTime(),
                        long.Parse(lineParts[4]),
                        lineParts[5],
                        int.Parse(lineParts[6]));
                    mEntries.Add(entry.Path, entry);
                } while (true);
            }
            //methods
            public void Remove(string path) {
                mEntries.Remove(path);
            }
            public void Modify(string path, Entry entry) {
                mEntries[path] = entry;
            }
            public void Add(Entry entry) {
                mEntries[entry.Path] = entry;
            }
            public Entry[] GetEntries() {
                var result = new List<Entry>();
                foreach (var entry in mEntries.Values) {
                    result.Add(entry);
                }
                return result.ToArray();
            }
            public Entry? GetEntry(string path) {
                if (mEntries.TryGetValue(path, out Entry? result)) {
                    return result;
                }
                return null;
            }
            public string Serialize() {
                var sb = new StringBuilder();
                foreach (var entry in mEntries.Values) {
                    var line = entry.Path + "|" + (entry.IsDirectory() ? "dir" : "file") + "|" + entry.Created.ToUniversalTime().Ticks.ToString() + "|" + entry.Modified.ToUniversalTime().Ticks.ToString() + "|" + entry.Length + "|" + entry.Etag + "|" + entry.Flags;
                    sb.AppendLine(line);
                }
                return sb.ToString();
            }
        }

    }


}