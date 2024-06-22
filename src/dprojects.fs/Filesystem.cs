using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using DProjects.Fs.Extensions;
using DProjects.Utils;
using Microsoft.Extensions.Logging;

namespace DProjects.Fs {


    public abstract class Filesystem : IFilesystem {


        //variables

        //constructor
        protected Filesystem(bool isReadonly) {
            IsReadonly = isReadonly;
        } 
        public virtual void Dispose() {
        }
                 

        //properties
        public bool IsReadonly { get; set; }
        public abstract string Url { get; }


        //methods LEVEL 0
        public abstract Entry? GetEntry(string path);
        public abstract Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken);
        public abstract IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null);
        public abstract IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null);
        public virtual bool Exists(string path) {
            return GetEntry(path) != null;
        }
        public virtual async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            return await GetEntryAsync(path, cancellationToken) != null;
        }
        public abstract Stream LoadReadStream(string path, LoadReadStreamSettings settings);
        public abstract Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken);
        public virtual Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            PathUtils.Validate(path);
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            //var pipeStream = new PipeStream(64 * 1024); 
            //var disposableOutputStream = new DisposableOutputStream(pipeStream);
            //settings ??= new LoadWriteStreamSettings();
            //Exception? exception = null;
            //var thread = new Thread(() => {
            //    try {
            //        this.SaveFile(path, pipeStream, new() { 
            //            Append = settings.Append
            //        });
            //    } catch (Exception e) {
            //        exception = e;
            //    }
            //});
            //thread.IsBackground = true;
            //thread.Start();
            //disposableOutputStream.Disposed += (e) => {
            //    thread.Join();
            //    if (exception != null) throw exception;
            //};
            //return disposableOutputStream;
            throw new NotImplementedException();
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
            var entry = await GetEntryAsync(path, cancellationToken);
            if (entry == null || !entry.IsDirectory()) {
                return false;
            }
            return true;
        }
        public virtual bool ExistsFile(string path) {
            var entry = GetEntry(path);
            if (entry == null || !entry.IsFile()) {
                return false;
            }
            return true;
        }
        public virtual async Task<bool> ExistsFileAsync(string path, CancellationToken cancellationToken) {
            var entry = await GetEntryAsync(path, cancellationToken);
            if (entry == null || !entry.IsFile()) return false;
            return true;
        }


        //methods LEVEL 2
        public virtual Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            throw new Exception("Unable to modify filesystem: filesystem is readonly");
        }
        public virtual Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken = default) {
            throw new Exception("Unable to modify filesystem: filesystem is readonly");
        }
        public virtual Entry CreateDirectory(string path) {
            throw new Exception("Unable to modify filesystem: filesystem is readonly");
        }
        public virtual Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            return Task.FromResult(CreateDirectory(path));
        }
        public virtual void Delete(string path) {
            throw new Exception("Unable to modify filesystem: filesystem is readonly");
        }
        public virtual Task DeleteAsync(string path, CancellationToken cancellationToken) {
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
        public virtual async Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            if (await ExistsFileAsync(path, cancellationToken)) {
                await DeleteAsync(path, cancellationToken);
            }
        }
        public virtual void DeleteDirectory(string path) {
            if (ExistsDirectory(path)) {
                Delete(path);
            }
        }
        public virtual async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (await ExistsDirectoryAsync(path, cancellationToken)) {
                await DeleteAsync(path, cancellationToken);
            }
        }
        public virtual void Copy(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            this.CopyRecursive(source, destination, settings, logger);
        }
        public virtual async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            await this.CopyRecursiveAsync(source, destination, settings, logger, cancellationToken);
        }
        public virtual void Move(string source, string destination, MoveSettings moveSettings, ILogger<IFilesystem> logger) {
            var entry = GetEntry(source);
            if (entry == null) {
                throw new Exception("Unable to move: not found " + source);
            } else if (entry.IsDirectory()) {
                var settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                settings.IgnoreErrors = moveSettings.IgnoreErrors;
                Copy(source, destination, settings, logger);
                DeleteDirectory(source);
            } else {
                var settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                settings.IgnoreErrors = moveSettings.IgnoreErrors;                
                Copy(source, destination, settings, logger);
                DeleteFile(source);
            }
        }
        public virtual async Task MoveAsync(string source, string destination, MoveSettings moveSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            var entry = await GetEntryAsync(source, cancellationToken);
            if (entry == null) {
                throw new Exception("Unable to move: not found " + source);
            } else if (entry.IsDirectory()) {
                var settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                settings.IgnoreErrors = moveSettings.IgnoreErrors;
                await CopyAsync(source, destination, settings, logger, cancellationToken);
                await DeleteDirectoryAsync(source, cancellationToken);
            } else {
                var settings = new CopySettings();
                settings.Recursive = true;
                settings.Overwrite = true;
                settings.IgnoreErrors = moveSettings.IgnoreErrors;
                await CopyAsync(source, destination, settings, logger, cancellationToken);
                await DeleteFileAsync(source, cancellationToken);
            }
        }
        public virtual void Sync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger) {
            if (syncSettings.Mode == SyncModes.LeftToRight) {
                this.SyncLeftToRight(source, destination, syncSettings, logger);
            } else if (syncSettings.Mode == SyncModes.Bidirectional) {
                this.SyncBidirectional(source, destination, syncSettings, logger);
            } else {
                throw new NotImplementedException();
            }
        }
        public virtual async Task SyncAsync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (syncSettings.Mode == SyncModes.LeftToRight) {
                await this.SyncLeftToRightAsync(source, destination, syncSettings, logger, cancellationToken);
            } else if (syncSettings.Mode == SyncModes.Bidirectional) {
                await this.SyncBidirectionalAsync(source, destination, syncSettings, logger, cancellationToken);
            } else {
                throw new NotImplementedException();
            }
        }


        //methods LEVEL 4
        public virtual Watcher CreateWatcher(string path, string filter, string[] excludes, bool recursive) {
            throw new NotImplementedException();
        }
        public virtual Task<Watcher> CreateWatcherAsync(string path, string filter, string[] excludes, bool recursive) {
            throw new NotImplementedException();
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


    }


}