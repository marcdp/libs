using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemSyncBidirectionalSync {


        //methods
        public static void SyncBidirectional(this IFilesystemSync fs, string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger) {
            if (syncSettings.SourceExcludes == null) syncSettings.SourceExcludes = [];
            if (syncSettings.DestinationExcludes == null) syncSettings.DestinationExcludes = [];
            //status
            var status = new Status();
            foreach (var entry in fs.GetEntries(source, GetModes.Descendants)) {
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
            foreach (var entry in fs.GetEntries(destination, GetModes.Descendants)) {
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
            if (fs.ExistsFile(statusIndexPath)) {
                prevStatus = new Status(new StringReader(fs.LoadTextFile(statusIndexPath)));
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
                        CopyEntry(fs, entryA, pathB, processedPaths, syncSettings, logger, status);
                    } else {
                        var seBprev = prevStatus.GetEntry(pathB);
                        if (seBprev == null) {
                            // a --> b
                            CopyEntry(fs, entryA, pathB, processedPaths, syncSettings, logger, status);
                        } else {
                            //remove a
                            DeleteEntry(fs, entryA, processedPaths, syncSettings, logger, status);
                        }
                    }
                } else {
                    if (prevStatus == null) {
                        // CONFLICT: copy newer file to other side (first synchronization)
                        CopyNewerEntry(fs, entryA, entryB, processedPaths, syncSettings, logger, status);
                    } else {
                        var entryAprev = prevStatus.GetEntry(pathA);
                        var entryBprev = prevStatus.GetEntry(pathB);
                        if (entryAprev == null || entryBprev == null) {
                            // CONFLICT: copy newer file to other side (the file apears in source and dest, but NOT in status!!!)
                            CopyNewerEntry(fs, entryA, entryB, processedPaths, syncSettings, logger, status);
                        } else {
                            // CONFLICT: files exists in both places
                            if (entryAprev.Modified == entryA.Modified && entryBprev.Modified == entryB.Modified) {
                                //no changed
                            } else if (entryAprev.Modified != entryA.Modified && entryBprev.Modified == entryB.Modified) {
                                //changed entryA: a --> b
                                CopyEntry(fs, entryA, pathB, processedPaths, syncSettings, logger, status);
                            } else if (entryAprev.Modified == entryA.Modified && entryBprev.Modified != entryB.Modified) {
                                //changed entryB
                                CopyEntry(fs, entryB, pathA, processedPaths, syncSettings, logger, status);
                            } else if (entryAprev.Modified != entryA.Modified && entryBprev.Modified != entryB.Modified) {
                                // CONFLICT: copy newer file to other side (both changed)
                                CopyNewerEntry(fs, entryA, entryB, processedPaths, syncSettings, logger, status);
                            }
                        }
                    }
                }
            }
            //save status
            fs.SaveTextFile(statusIndexPath, status.Serialize(), Encoding.UTF8);
        }
        private static bool DeleteEntry(IFilesystemSync fs, Entry entry, List<string> processedPaths, SyncSettings syncSettings, ILogger<IFilesystem> logger, Status status) {
            for (var trie = 0; trie < syncSettings.Tries; trie++) {
                logger.LogInformation("delete {path}", entry.Path);
                try {
                    if (entry.IsDirectory()) {
                        fs.DeleteDirectory(entry.Path);
                    } else {
                        fs.DeleteFile(entry.Path);
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
        private static bool CopyNewerEntry(IFilesystemSync fs, Entry entryA, Entry entryB, List<string> processedPaths, SyncSettings syncSettings, ILogger<IFilesystem> logger, Status status) {
            if (entryA.Modified < entryB.Modified) {
                return CopyEntry(fs, entryB, entryA.Path, processedPaths, syncSettings, logger, status);
            } else {
                return CopyEntry(fs, entryA, entryB.Path, processedPaths, syncSettings, logger, status);
            }
        }
        private static bool CopyEntry(IFilesystemSync fs, Entry source, string destination, List<string> processedPaths, SyncSettings syncSettings, ILogger<IFilesystem> logger, Status status) {
            for (var trie = 0; trie < syncSettings.Tries; trie++) {
                logger.LogInformation("copy {from} to {to}", source.Path, destination);
                try {
                    if (source.IsDirectory()) {
                        var entry = fs.CreateDirectory(destination);
                        status.Modify(entry.Path, entry);
                    } else {
                        using (var stream = fs.LoadReadStream(source.Path, new())) {
                            var entry = fs.SaveFile(destination, stream, new());
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