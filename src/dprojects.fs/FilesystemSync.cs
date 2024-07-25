using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DProjects.Fs.Extensions;
using DProjects.Streams;
using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace DProjects.Fs {


    public abstract class FilesystemSync : IFilesystem {


        //constructor
        protected FilesystemSync(bool isReadonly) {
            IsReadonly = isReadonly;
        }
        public virtual void Dispose() {
        }


        //properties
        public bool IsReadonly { get; set; }
        public abstract string Url { get; }


        //methods LEVEL 0
        public abstract Entry? GetEntry(string path);
        public async Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            return await Task.FromResult(GetEntry(path));
        }
        public abstract IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null);
        public IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, CancellationToken cancellationToken = default) {
            return GetEntries(path, mode, pattern).ToAsyncEnumerable(cancellationToken);
        }
        public virtual bool Exists(string path) {
            return GetEntry(path) != null;
        }
        public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            return await Task.FromResult(Exists(path));
        }
        public abstract Stream LoadReadStream(string path, LoadReadStreamSettings settings);
        public async Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) {
            return await Task.FromResult(LoadReadStream(path, settings));
        }
        public virtual Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            settings ??= new LoadWriteStreamSettings();
            int bufferSize = 8 * 1024;
            if (settings.Truncate) this.SaveBinaryFile(path, []);
            if (settings.Append && !Exists(path)) this.SaveBinaryFile(path, []);
            var result = new SpongeOutputStream(bufferSize, (stream) => {
                this.SaveFile(path, stream, new() { Append = settings.Append });
            });
            return result;
        }
        public virtual async Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings settings, CancellationToken cancellationToken) {
            return await Task.FromResult(LoadWriteStream(path, settings));
        }


        //methods LEVEL 1
        public virtual bool ExistsDirectory(string path) {
            var entry = GetEntry(path);
            if (entry == null || !entry.IsDirectory()) {
                return false;
            }
            return true;
        }
        public virtual async Task<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken) {
            return await Task.FromResult(ExistsDirectory(path));
        }
        public virtual bool ExistsFile(string path) {
            var entry = GetEntry(path);
            if (entry == null || !entry.IsFile()) {
                return false;
            }
            return true;
        }
        public virtual async Task<bool> ExistsFileAsync(string path, CancellationToken cancellationToken) {
            return await Task.FromResult(ExistsFile(path));
        }


        //methods LEVEL 2
        public virtual Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            throw new Exception("Unable to modify filesystem: filesystem is readonly");
        }
        public virtual async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken = default) {
            return await Task.FromResult(SaveFile(path, stream, settings));
        }
        public virtual Entry CreateDirectory(string path) {
            throw new Exception("Unable to modify filesystem: filesystem is readonly");
        }
        public async Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            return await Task.FromResult(CreateDirectory(path));
        }
        public virtual void Delete(string path) {
            throw new Exception("Unable to modify filesystem: filesystem is readonly");
        }
        public Task DeleteAsync(string path, CancellationToken cancellationToken) {
            Delete(path);
            return Task.CompletedTask;
        }
        public virtual void Touch(string path, DateTime aDate) {
            throw new NotImplementedException();
        }
        public virtual Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            Touch(path, aDate);
            return Task.CompletedTask;
        }


        //methods LEVEL 3
        public virtual void DeleteFile(string path) {
            if (ExistsFile(path)) {
                Delete(path);
            }
        }
        public virtual Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            DeleteFile(path);
            return Task.CompletedTask;
        }
        public virtual void DeleteDirectory(string path) {
            if (ExistsDirectory(path)) {
                Delete(path);
            }
        }
        public virtual Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            DeleteDirectory(path);
            return Task.CompletedTask;
        }
        public virtual void Copy(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            this.CopyRecursive(source, destination, settings, logger);
        }        
        public virtual async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            await this.CopyRecursiveAsync(source, destination, settings, logger, cancellationToken);
        }
        public virtual void Move(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger) {
            var entry = GetEntry(source);
            if (entry == null) {
                throw new Exception("Unable to move: not found " + source);
            } else if (entry.IsDirectory()) {
                var copySettings = new CopySettings();
                copySettings.Recursive = true;
                copySettings.Overwrite = true;
                copySettings.IgnoreErrors = settings.IgnoreErrors;
                Copy(source, destination, copySettings, logger);
                DeleteDirectory(source);
            } else {
                var copySettings = new CopySettings();
                copySettings.Recursive = true;
                copySettings.Overwrite = true;
                copySettings.IgnoreErrors = settings.IgnoreErrors;
                Copy(source, destination, copySettings, logger);
                DeleteFile(source);
            }
        }
        public virtual async Task MoveAsync(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
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
        public virtual void Sync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger) {
            if (syncSettings.Mode == SyncModes.LeftToRight) {
                FilesystemSyncLeftToRightSync.SyncLeftToRight(this, source, destination, syncSettings, logger);
            } else if (syncSettings.Mode == SyncModes.Bidirectional) {
                FilesystemSyncBidirectionalSync.SyncBidirectional(this, source, destination, syncSettings, logger);
            } else {
                throw new NotImplementedException();
            }
        }
        public virtual async Task SyncAsync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (syncSettings.Mode == SyncModes.LeftToRight) {
                await FilesystemSyncLeftToRightAsync.SyncLeftToRightAsync(this, source, destination, syncSettings, logger, cancellationToken);
            } else if (syncSettings.Mode == SyncModes.Bidirectional) {
                await FilesystemSyncBidirectionalAsync.SyncBidirectionalAsync(this, source, destination, syncSettings, logger, cancellationToken);
            } else {
                throw new NotImplementedException();
            }
        }


        //methods LEVEL 4
        public virtual Watcher CreateWatcher(string path, string filter, string[] excludes, bool recursive) {
            throw new NotImplementedException();
        }
        public async Task<Watcher> CreateWatcherAsync(string path, string filter, string[] excludes, bool recursive) {
            return await Task.FromResult(CreateWatcher(path, filter, excludes, recursive));
        }
        public virtual IDictionary<string, string> GetMetadata(string path) {
            throw new NotImplementedException();
        }
        public virtual async Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken) {
            return await Task.FromResult(GetMetadata(path));
        }
        public virtual void SetMetadata(string path, IDictionary<string, string> metadata) {
            throw new NotImplementedException();
        }
        public virtual Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken) {
            SetMetadata(path, metadata);
            return Task.CompletedTask;
        }
        public virtual bool Supports(string path, Features feature) {
            if (feature == Features.Select) return true;
            return false;
        }
        public virtual async Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken) {
            return await Task.FromResult(Supports(path, feature));
        }


        ////utils
        //public void SetIsReadonly(bool value) {
        //    mIsReadonly = value;
        //}




    }


}