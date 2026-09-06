using DProjects.Streams;
using DProjects.Utils;
using DProjects.Fs.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;


namespace DProjects.Fs {


    public abstract class FilesystemAsync : IFilesystem {


        //variables
        protected bool mIsStarted;


        //constructor
        protected FilesystemAsync(bool isReadOnly) {
            IsReadonly = isReadOnly;
        }
        public virtual void Dispose() {
        }


        //properties
        public bool IsReadonly { get; set; }
        public bool IsStarted => mIsStarted;
        public abstract string Url { get; }


        //methods LEVEL 0
        public Entry? GetEntry(string path) {
            return AsyncUtils.RunSync(async () => await GetEntryAsync(path, default));
        }
        public abstract Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken);
        public virtual IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            var iAsyncEnumerator = GetEntriesAsync(path, mode, pattern);
            var enumerable = iAsyncEnumerator.ToEnumerable();
            foreach (var entry in enumerable) {
                yield return entry;
            }
        }
        public abstract IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, CancellationToken cancellationToken = default);
        public bool Exists(string path) {
            return AsyncUtils.RunSync(async () => await ExistsAsync(path, default));
        }
        public virtual async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            return await GetEntryAsync(path, cancellationToken) != null;
        }
        public Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            return AsyncUtils.RunSync(async () => await LoadReadStreamAsync(path, settings, new()));
        }
        public abstract Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken);
        public Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            return AsyncUtils.RunSync(async () => await LoadWriteStreamAsync(path, settings, default));
        }
        public virtual async Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings settings, CancellationToken cancellationToken) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            cancellationToken.ThrowIfCancellationRequested();
            settings ??= new LoadWriteStreamSettings();
            int bufferSize = 8 * 1024;
            if (settings.Truncate) await SaveFileAsync(path, Stream.Null, new SaveFileSettings(), cancellationToken);
            if (settings.Append && !await ExistsAsync(path, cancellationToken))
                await SaveFileAsync(path, Stream.Null, new SaveFileSettings(), cancellationToken);
            return new SpongeOutputStream(bufferSize, stream =>
                SaveFileAsync(path, stream, new SaveFileSettings { Append = settings.Append }, cancellationToken));
        }


        //method1 LEVEL 1
        public bool ExistsFile(string path) {
            return AsyncUtils.RunSync(async () => await ExistsFileAsync(path, default));
        }
        public virtual async Task<bool> ExistsFileAsync(string path, CancellationToken cancellationToken) {
            var entry = await GetEntryAsync(path, cancellationToken);
            return ((entry == null || !entry.IsFile()) ? false : true);
        }
        public bool ExistsDirectory(string path) {
            return AsyncUtils.RunSync(async () => await ExistsDirectoryAsync(path, default));
        }
        public virtual async Task<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken) {
            var entry = await GetEntryAsync(path, cancellationToken);
            return ((entry == null || !entry.IsDirectory()) ? false : true);
        }


        //methods LEVEL 2
        public Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            return AsyncUtils.RunSync(async () => await SaveFileAsync(path, stream, settings, default));
        }
        public abstract Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken);
        public Entry CreateDirectory(string path) {
            return AsyncUtils.RunSync(async () => await CreateDirectoryAsync(path, default));
        }
        public abstract Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken);
        public void Delete(string path) {
            AsyncUtils.RunSync(async () => await DeleteAsync(path, default));
        }
        public abstract Task DeleteAsync(string path, CancellationToken cancellationToken);
        public void Touch(string path, DateTime aDate) {
            AsyncUtils.RunSync(async () => await TouchAsync(path, aDate, default));
        }
        public virtual Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            throw new NotSupportedException("Touch is not supported by this filesystem.");
        }


        //method LEVEL 3
        public void DeleteFile(string path) {
            AsyncUtils.RunSync(async () => await DeleteFileAsync(path, default));
        }
        public virtual async Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            if (await ExistsFileAsync(path, cancellationToken)) {
                await DeleteAsync(path, cancellationToken);
            }
        }    
        public virtual void DeleteDirectory(string path) { 
            AsyncUtils.RunSync(async () => await DeleteDirectoryAsync(path, default));
        }
        public virtual async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (await ExistsDirectoryAsync(path, cancellationToken)) {
                await DeleteAsync(path, cancellationToken);
            }
        }
        public void Copy(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            AsyncUtils.RunSync(async () => await CopyAsync(source, destination, settings, logger, default));
        }
        public virtual async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            await this.CopyRecursiveAsync(source, destination, settings, logger, cancellationToken);
        }
        public void Move(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger) {
            AsyncUtils.RunSync(async () => await MoveAsync(source, destination, settings, logger, default));
        }
        public virtual async Task MoveAsync(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            var entry = await GetEntryAsync(source, cancellationToken);
            if (entry == null) {
                throw new Exception("Unable to move: not found " + source);
            } else if (entry.IsDirectory()) {
                var copySettings = new CopySettings();
                copySettings.Recursive = true;
                copySettings.Overwrite = true;
                copySettings.IgnoreErrors = settings.IgnoreErrors;
                await CopyAsync(source, destination, copySettings, logger, cancellationToken);
                await DeleteDirectoryAsync(source, cancellationToken);
            } else {
                var copySettings = new CopySettings();
                copySettings.Recursive = true;
                copySettings.Overwrite = true;
                copySettings.IgnoreErrors = settings.IgnoreErrors;
                await CopyAsync(source, destination, copySettings, logger, cancellationToken);
                await DeleteFileAsync(source, cancellationToken);
            }
        }
        public void Sync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger) {
            AsyncUtils.RunSync(async () => await SyncAsync(source, destination, syncSettings, logger, default));
        }
        public virtual async Task SyncAsync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (syncSettings.Mode == SyncModes.LeftToRight) {
                await this.SyncLeftToRightAsync(source, destination, syncSettings, logger, cancellationToken);
            } else if (syncSettings.Mode == SyncModes.Bidirectional) {
                await this.SyncBidirectionalAsync(source, destination, syncSettings, logger, cancellationToken);
            } else {
                throw new ArgumentOutOfRangeException(nameof(syncSettings.Mode), syncSettings.Mode, "Unsupported sync mode.");
            }
        }


        //method LEVEL 4
        public Watcher CreateWatcher(string path, string filter, string[] excludes, bool recursive) {  
            return AsyncUtils.RunSync(async () => await CreateWatcherAsync(path, filter, excludes, recursive, default));
        }
        public virtual Task<Watcher> CreateWatcherAsync(string path, string filter, string[] excludes, bool recursive, CancellationToken cancellationToken) {
            throw new NotSupportedException("Watchers are not supported by this filesystem.");
        }
        public IDictionary<string, string> GetMetadata(string path) {
            return AsyncUtils.RunSync(async () => await GetMetadataAsync(path, default));
        }
        public virtual Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken) {
            throw new NotSupportedException("Metadata is not supported by this filesystem.");
        }
        public void SetMetadata(string path, IDictionary<string, string> metadata) {
            AsyncUtils.RunSync(async () => await SetMetadataAsync(path, metadata, default));
        }
        public virtual Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken) {
            throw new NotSupportedException("Metadata is not supported by this filesystem.");
        }
        public bool Supports(string path, Features feature) {
            return AsyncUtils.RunSync(async () => await SupportsAsync(path,  feature, default));
        }
        public virtual Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken) {
            return Task.FromResult(false);
        }


        //Lifecycle
        public virtual void Start() {
            mIsStarted = true;
        }
        public virtual void Stop() {
            mIsStarted = false;
        }

    }

}


