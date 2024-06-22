using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Fs {


    public class FilesystemFilter : IFilesystem {


        //builder
        public enum FilterType {
            Exclude,
            Include
        }
        public class Filter {
            public FilterType Type { get; set; } = FilterType.Exclude;
            public string Value { get; set; } = "";
        } 


        //variables
        protected bool mIsStarted;
        private IFilesystem mFilesystem;
        private Filter[] mFilters;
        private StringComparison mStringComparison;


        //constructor
        public FilesystemFilter(IFilesystem filesystem, Filter[] filters, StringComparison stringComparison) {
            mFilesystem = filesystem;
            mFilters = filters;
            mStringComparison = stringComparison;
        }
        public void Dispose() {
        }


        //properties
        public bool IsReadonly {
            get => mFilesystem.IsReadonly;
            set => mFilesystem.IsReadonly = value; 
        }
        public bool IsStarted => mIsStarted;
        public string Url {
            get {
                var query = new List<string>();
                foreach(var filter in mFilters) {
                    if (filter.Type == FilterType.Exclude) {
                        query.Add("exclude=" + filter.Value);
                    } else if (filter.Type == FilterType.Include) {
                        query.Add("include=" + filter.Value );
                    }                    
                }
                return "filter:" + mFilesystem.Url + "!?" + string.Join("&", query.ToArray());
            }
        }


        //methods LEVEL 0
        public Entry? GetEntry(string path) {
            if (!path.Equals("/") && IsExcluded(path)) return null;
            return mFilesystem.GetEntry(path);
        }
        public async Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            if (!path.Equals("/") && IsExcluded(path)) return null;
            return await mFilesystem.GetEntryAsync(path, cancellationToken);
        }
        public IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            if (!path.Equals("/") && IsExcluded(path)) yield break;
            foreach (var entry in mFilesystem.GetEntries(path, mode, pattern)) {
                if (IsExcluded(entry.Path)) continue;
                yield return entry;
            }
        }
        public async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null) {
            if (!path.Equals("/") && IsExcluded(path)) yield break;
            await foreach (var entry in mFilesystem.GetEntriesAsync(path, mode, pattern)) {
                if (IsExcluded(entry.Path)) continue;
                yield return entry;
            }
        }
        public bool Exists(string path) {
            if (!path.Equals("/") && IsExcluded(path)) return false;
            return mFilesystem.Exists(path);
        }
        public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            if (!path.Equals("/") && IsExcluded(path)) return false;
            return await mFilesystem.ExistsAsync(path, cancellationToken);
        }
        public Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            if (IsExcluded(path)) throw new Exception("Unable to load read stream: path not found: " + path);
            return mFilesystem.LoadReadStream(path, settings);
        }
        public async Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to load read stream: path not found: " + path);
            return await mFilesystem.LoadReadStreamAsync(path, settings, cancellationToken);
        }
        public Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            if (IsExcluded(path)) throw new Exception("Unable to load write stream: path not found: " + path);
            return mFilesystem.LoadWriteStream(path, settings);
        }
        public async Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings settings, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to load write stream: path not found: " + path);
            return await mFilesystem.LoadWriteStreamAsync(path, settings, cancellationToken);
        }

        //methods LEVEL 1
        public bool ExistsDirectory(string path) {
            if (!path.Equals("/") && IsExcluded(path)) return false;
            return mFilesystem.ExistsDirectory(path);
        }
        public async Task<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (!path.Equals("/") && IsExcluded(path)) return false;
            return await mFilesystem.ExistsDirectoryAsync(path, cancellationToken);
        }
        public bool ExistsFile(string path) {
            if (IsExcluded(path)) return false;
            return mFilesystem.ExistsFile(path);
        }
        public async Task<bool> ExistsFileAsync(string path, CancellationToken cancellationToken) {
            if (IsExcluded(path)) return false;
            return await mFilesystem.ExistsFileAsync(path, cancellationToken);
        }

        //methods LEVEL 2
        public Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            if (IsExcluded(path)) throw new Exception("Unable to save file: path not found: " + path);
            return mFilesystem.SaveFile(path, stream, settings);
        }
        public async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken = default) {
            if (IsExcluded(path)) throw new Exception("Unable to save file: path not found: " + path);
            return await mFilesystem.SaveFileAsync(path, stream, settings, cancellationToken);
        }
        public Entry CreateDirectory(string path) {
            if (IsExcluded(path)) throw new Exception("Unable to create directory: path not found: " + path);
            return mFilesystem.CreateDirectory(path);
        }
        public async Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to create directory: path not found: " + path);
            return await mFilesystem.CreateDirectoryAsync(path, cancellationToken);
        }
        public void Delete(string path) {
            if (IsExcluded(path)) throw new Exception("Unable to delete: path not found: " + path);
            mFilesystem.Delete(path);
        }
        public async Task DeleteAsync(string path, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to delete: path not found: " + path);
            await mFilesystem.DeleteAsync(path, cancellationToken);
        }
        public void Touch(string path, DateTime aDate) {
            if (IsExcluded(path)) throw new Exception("Unable to touch: path not found: " + path);
            mFilesystem.Touch(path, aDate);
        }
        public async Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to touch: path not found: " + path);
            await mFilesystem.TouchAsync(path, aDate, cancellationToken);
        }


        //methods LEVEL 3
        public void DeleteFile(string path) {
            if (IsExcluded(path)) throw new Exception("Unable to delete file: path not found: " + path);
            mFilesystem.DeleteFile(path);
        }
        public async Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to delete file: path not found: " + path);
            await mFilesystem.DeleteFileAsync(path, cancellationToken);
        }
        public void DeleteDirectory(string path) {
            if (IsExcluded(path)) throw new Exception("Unable to delete directory: path not found: " + path);
            mFilesystem.DeleteDirectory(path);
        }
        public async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to delete directory: path not found: " + path);
            await mFilesystem.DeleteDirectoryAsync(path, cancellationToken);
        }
        public void Copy(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            if (IsExcluded(source)) throw new Exception("Unable to copy: path not found: " + source);
            if (IsExcluded(destination)) throw new Exception("Unable to copy: path not found: " + destination);
            mFilesystem.Copy(source, destination, settings, logger);
        }
        public async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (IsExcluded(source)) throw new Exception("Unable to copy: path not found: " + source);
            if (IsExcluded(destination)) throw new Exception("Unable to copy: path not found: " + destination);
            await mFilesystem.CopyAsync(source, destination, settings, logger, cancellationToken);
        }
        public void Move(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger) {
            if (IsExcluded(source)) throw new Exception("Unable to move: path not found: " + source);
            if (IsExcluded(destination)) throw new Exception("Unable to move: path not found: " + destination);
            mFilesystem.Move(source, destination, settings, logger);
        }
        public async Task MoveAsync(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (IsExcluded(source)) throw new Exception("Unable to move: path not found: " + source);
            if (IsExcluded(destination)) throw new Exception("Unable to move: path not found: " + destination);
            await mFilesystem.MoveAsync(source, destination, settings, logger, cancellationToken);
        }
        public void Sync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger) {
            if (IsExcluded(source)) throw new Exception("Unable to sync: path not found: " + source);
            if (IsExcluded(destination)) throw new Exception("Unable to sync: path not found: " + destination);
            mFilesystem.Sync(source, destination, syncSettings, logger);
        }
        public async Task SyncAsync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (IsExcluded(source)) throw new Exception("Unable to sync: path not found: " + source);
            if (IsExcluded(destination)) throw new Exception("Unable to sync: path not found: " + destination);
            await mFilesystem.SyncAsync(source, destination, syncSettings, logger, cancellationToken);
        }


        //methods LEVEL 4
        public Watcher CreateWatcher(string path, string filter, string[] excludes, bool recursive) {
            if (IsExcluded(path)) throw new Exception("Unable to Create watcher: path not found: " + path);
            return mFilesystem.CreateWatcher(path, filter, excludes, recursive);
        }
        public async Task<Watcher> CreateWatcherAsync(string path, string filter, string[] excludes, bool recursive) {
            if (IsExcluded(path)) throw new Exception("Unable to Create watcher: path not found: " + path);
            return await mFilesystem.CreateWatcherAsync(path, filter, excludes, recursive);
        }
        public IDictionary<string, string> GetMetadata(string path) {
            if (IsExcluded(path)) throw new Exception("Unable to get metadata: path not found: " + path);
            return mFilesystem.GetMetadata(path);
        }
        public async Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to get metadata: path not found: " + path);
            return await mFilesystem.GetMetadataAsync(path, cancellationToken);
        }
        public void SetMetadata(string path, IDictionary<string, string> metadata) {
            if (IsExcluded(path)) throw new Exception("Unable to set metadata: path not found: " + path);
            mFilesystem.SetMetadata(path, metadata);
        }
        public async Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to set metadata: path not found: " + path);
            await mFilesystem.SetMetadataAsync(path, metadata, cancellationToken);
        }
        public bool Supports(string path, Features feature) {
            if (IsExcluded(path)) throw new Exception("Unable to support: path not found: " + path);
            return mFilesystem.Supports(path, feature);
        }
        public async Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken) {
            if (IsExcluded(path)) throw new Exception("Unable to support: path not found: " + path);
            return await mFilesystem.SupportsAsync(path, feature, cancellationToken);
        }


        //private methods
        private bool IsExcluded(string path) {
            var result = false;
            foreach (var filter in mFilters) {
                if (filter.Type == FilterType.Exclude) {
                    if (filter.Value.Equals(path, mStringComparison)) {
                        result = true;
                    } else if ((path + "/").StartsWith(filter.Value, mStringComparison)) {
                        result = true;
                    } else if (StringUtils.Like(path, filter.Value)) {
                        result = true;
                    }
                } else if (filter.Type == FilterType.Include) { 
                    if (filter.Value.Equals(path, mStringComparison)) {
                        result = false;
                    } else if ((path + "/").StartsWith(filter.Value, mStringComparison)) {
                        result = false;
                    } else if (StringUtils.Like(path, filter.Value)) {
                        result = false;
                    }
                }
            }
            return result;
        }


    }


}

