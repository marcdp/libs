using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemSyncLeftToRightSync {


        //private enum
        public enum CompareMethod {
            Timestamp,
            TimestampCache
        }


        //methods
        public static void SyncLeftToRight(this IFilesystemSync fs, string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger) {
            if (fs.Supports(destination, Features.Touch)) {
                //CompareMethod.Timestamp
                SyncLeftToRightRecursive(fs, source, destination, syncSettings, logger, CompareMethod.Timestamp, new Dictionary<string, string>());
            } else {
                //CompareMethod.TimestampCache
                var timestampCache = new Dictionary<string, string>();
                var timestampCachePath = PathUtils.Combine(syncSettings.StatusPath, "index.db");
                if (fs.ExistsFile(timestampCachePath)) {
                    timestampCache = JsonSerializer.Deserialize<Dictionary<string, string>>(fs.LoadTextFile(timestampCachePath));
                }
                SyncLeftToRightRecursive(fs, source, destination, syncSettings, logger, CompareMethod.TimestampCache, timestampCache ?? new Dictionary<string, string>());
                fs.SaveTextFile(timestampCachePath, JsonSerializer.Serialize(timestampCache), System.Text.Encoding.UTF8);
            }
        }
        private static void SyncLeftToRightRecursive(IFilesystemSync fs, string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CompareMethod compareMethod, Dictionary<string, string> timestampCache) {
            if (syncSettings.SourceExcludes == null) syncSettings.SourceExcludes = [];
            if (syncSettings.DestinationExcludes == null) syncSettings.DestinationExcludes = [];
            //get entries
            var srcEntries = new List<Entry>(fs.GetEntries(source));
            var dstEntries = new List<Entry>(fs.GetEntries(destination));
            //source
            var srcEntriesCache = new Dictionary<string, Entry>(srcEntries.Count);
            foreach (var srcEntry in srcEntries) {
                srcEntriesCache.Add(srcEntry.Name, srcEntry);
            }
            for (var i = srcEntries.Count - 1; i >= 0; i--) {
                var srcEntry = srcEntries[i];
                bool isExcluded = false;
                foreach (string exclude in syncSettings.SourceExcludes) {
                    if (StringUtils.Like(srcEntry.Path, exclude)) {
                        isExcluded = true;
                    }
                }
                if (isExcluded) {
                    srcEntries.RemoveAt(i);
                }
            }
            //dest
            var dstEntriesCache = new Dictionary<string, Entry>(dstEntries.Count);
            foreach (var dstEntry in dstEntries) {
                dstEntriesCache.Add(dstEntry.Name, dstEntry);
            }
            for (var i = dstEntries.Count - 1; i >= 0; i--) {
                var dstEntry = dstEntries[i];
                bool isExcluded = false;
                foreach (string exclude in syncSettings.DestinationExcludes) {
                    if (StringUtils.Like(dstEntry.Path, exclude)) {
                        isExcluded = true;
                    }
                }
                if (isExcluded) {
                    dstEntries.RemoveAt(i);
                }
            }
            //sync
            foreach (var srcEntry in srcEntries) {
                Entry? dstEntry = null;
                dstEntriesCache.TryGetValue(srcEntry.Name, out dstEntry);
                if (dstEntry == null) {
                    //create dst entry
                    if (srcEntry.IsDirectory()) {
                        logger.LogInformation("creating {path} ...", PathUtils.Combine(destination, srcEntry.Path.Substring(source.Length)));
                        dstEntry = fs.CreateDirectory(PathUtils.Combine(destination, srcEntry.Path.Substring(source.Length)));
                        if (syncSettings.Recursive) SyncLeftToRightRecursive(fs, srcEntry.Path, dstEntry.Path, syncSettings, logger, compareMethod, timestampCache);
                    } else {
                        for (int trie = 0; trie <= syncSettings.Tries - 1; trie++) {
                            logger.LogInformation("creating {path} ...", PathUtils.Combine(destination, srcEntry.Path.Substring(source.Length)));
                            try {
                                Entry? entrySaved = null;
                                using (var stream = fs.LoadReadStream(srcEntry.Path)) {
                                    entrySaved = fs.SaveFile(PathUtils.Combine(destination, srcEntry.Path.Substring(source.Length)), stream);
                                }
                                if (entrySaved == null) {
                                } else if (compareMethod == CompareMethod.Timestamp) {
                                    fs.Touch(entrySaved.Path, srcEntry.Modified);
                                } else if (compareMethod == CompareMethod.TimestampCache) {
                                    timestampCache[srcEntry.Path] = HashUtils.ToHashSHA256hex(srcEntry.Modified.ToUniversalTime().Ticks + ":" + srcEntry.Length + ":" + entrySaved.Modified.ToUniversalTime().Ticks);
                                }
                                break;
                            } catch (TaskCanceledException) {
                                throw;
                            } catch (Exception ex) {
                                logger.LogError("Unable to create {path} {trie}/{tries}: {message} {ex}", PathUtils.Combine(destination, srcEntry.Path.Substring(source.Length)), (trie + 1), syncSettings.Tries, ex.Message, ex);
                                if (trie == syncSettings.Tries - 1) {
                                    if (!syncSettings.IgnoreErrors) throw;
                                } else {
                                    System.Threading.Thread.Sleep(250);
                                }
                            }
                        }
                    }

                } else if (dstEntry.IsDirectory()) {
                    //directory
                    if (syncSettings.Recursive) {
                        SyncLeftToRightRecursive(fs, srcEntry.Path, dstEntry.Path, syncSettings, logger, compareMethod, timestampCache);
                    }

                } else if (!dstEntry.IsDirectory()) {
                    //dstEntry already exists
                    bool changed = false;
                    if (compareMethod == CompareMethod.Timestamp) {
                        if (!DateTimeUtils.EqualsWithoutMilliseconds(srcEntry.Modified, dstEntry.Modified)) {
                            changed = true;
                        } else if (srcEntry.Length != dstEntry.Length) {
                            changed = true;
                        }
                    } else if (compareMethod == CompareMethod.TimestampCache) {
                        string? key = null;
                        if (timestampCache.TryGetValue(srcEntry.Path, out key)) {
                            var computedKey = HashUtils.ToHashSHA256hex(srcEntry.Modified.ToUniversalTime().Ticks + ":" + srcEntry.Length + ":" + dstEntry.Modified.ToUniversalTime().Ticks);
                            if (!key.Equals(computedKey)) {
                                changed = true;
                            }
                        } else {
                            changed = true;
                        }
                    }
                    if (changed) {
                        for (int trie = 0; trie <= syncSettings.Tries - 1; trie++) {
                            logger.LogInformation("updating {path} ...", PathUtils.Combine(destination, srcEntry.Path.Substring(source.Length)));
                            try {
                                Entry? entrySaved = null;
                                using (var stream = fs.LoadReadStream(srcEntry.Path)) {
                                    entrySaved = fs.SaveFile(PathUtils.Combine(destination, srcEntry.Path.Substring(source.Length)), stream);
                                }
                                if (entrySaved == null) {
                                } else if (compareMethod == CompareMethod.Timestamp) {
                                    fs.Touch(entrySaved.Path, srcEntry.Modified);
                                } else if (compareMethod == CompareMethod.TimestampCache) {
                                    timestampCache[srcEntry.Path] = HashUtils.ToHashSHA256hex(srcEntry.Modified.ToUniversalTime().Ticks + ":" + srcEntry.Length + ":" + entrySaved.Modified.ToUniversalTime().Ticks);
                                }
                                break;
                            } catch (TaskCanceledException) {
                                throw;
                            } catch (Exception ex) {
                                logger.LogError("Unable to create {path} {trie}/{tries}: {message} {ex}", PathUtils.Combine(destination, srcEntry.Path.Substring(source.Length)), (trie + 1), syncSettings.Tries, ex.Message, ex);
                                if (trie == syncSettings.Tries - 1) {
                                    if (!syncSettings.IgnoreErrors) throw;
                                } else {
                                    System.Threading.Thread.Sleep(250);
                                }
                            }
                        }
                    }
                }

            }
            //delete
            foreach (var dstEntry in dstEntries) {
                if (!srcEntriesCache.ContainsKey(dstEntry.Name)) {
                    bool isExcluded = false;
                    foreach (string exclude in syncSettings.DestinationExcludes) {
                        if (StringUtils.Like(dstEntry.Path, exclude)) {
                            isExcluded = true;
                        }
                    }
                    if (!isExcluded) {
                        logger.LogInformation("deleting {path} ...", dstEntry.Path);
                        try {
                            if (dstEntry.IsDirectory()) {
                                fs.DeleteDirectory(dstEntry.Path);
                            } else {
                                fs.DeleteFile(dstEntry.Path);
                            }
                        } catch (TaskCanceledException) {
                            throw;
                        } catch (Exception) {
                            if (syncSettings.IgnoreErrors) continue;
                            throw;
                        }
                    }
                    if (compareMethod == CompareMethod.Timestamp) {
                    } else if (compareMethod == CompareMethod.TimestampCache) {
                        timestampCache.Remove("");
                    }
                }
            }
            if (compareMethod == CompareMethod.TimestampCache) {
                var toRemove = new List<string>();
                foreach (var key in timestampCache.Keys) {
                    if (PathUtils.GetPathParent(key).Equals(source) && !srcEntriesCache.ContainsKey(PathUtils.GetPathName(key))) {
                        toRemove.Add(key);
                    }
                }
                foreach (var key in toRemove) timestampCache.Remove(key);
            }
        }
    }


}