using DProjects.Fs.Extensions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace DProjects.Fs {


    public class FilesystemMetadata : IFilesystem {

        //variables
        private IFilesystem mFilesystem;
        private string mSuffix;


        //constructor
        public FilesystemMetadata(IFilesystem filesystem, string suffix = ".metadata") {
            mFilesystem = filesystem;
            mSuffix = suffix;
        }
        public virtual void Dispose() {
        }


        //properties
        public bool IsReadonly {
            get => mFilesystem.IsReadonly;
            set => mFilesystem.IsReadonly = value;
        }
        public string Url => "metadata:" + mFilesystem.Url;


        //methods LEVEL 0
        public Entry? GetEntry(string path) {
            if (path.EndsWith(mSuffix)) return null;
            return mFilesystem.GetEntry(path);
        }
        public async Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) return null;
            return await mFilesystem.GetEntryAsync(path, cancellationToken);
        }
        public IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to get entries: path not found: " + path);
            foreach (var entry in mFilesystem.GetEntries(path, mode, pattern)) {
                if (entry.Name.EndsWith(mSuffix)) {
                } else {
                    yield return entry;
                }
            }
        }
        public async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to get entries: path not found: " + path);
            await foreach (var entry in mFilesystem.GetEntriesAsync(path, mode, pattern, cancellationToken)) {
                if (entry.Name.EndsWith(mSuffix)) {
                } else {
                    yield return entry;
                }
            }
        }
        public bool Exists(string path) {
            if (path.EndsWith(mSuffix)) return false;
            return mFilesystem.Exists(path);
        }
        public async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) return false;
            return await mFilesystem.ExistsAsync(path, cancellationToken);
        }
        public Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to load read stream: path not found: " + path);
            return mFilesystem.LoadReadStream(path, settings);
        }
        public async Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to load read stream: path not found: " + path);
            return await mFilesystem.LoadReadStreamAsync(path, settings, cancellationToken);
        }
        public Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to load write stream: path not found: " + path);
            return mFilesystem.LoadWriteStream(path, settings);
        }
        public async Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings settings, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to load write stream: path not found: " + path);
            return await mFilesystem.LoadWriteStreamAsync(path, settings, cancellationToken);
        }


        //methods LEVEL 1
        public bool ExistsDirectory(string path) {
            if (path.EndsWith(mSuffix)) return false;
            return mFilesystem.ExistsDirectory(path);
        }
        public async Task<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) return false;
            return await mFilesystem.ExistsDirectoryAsync(path, cancellationToken);
        }
        public bool ExistsFile(string path) {
            if (path.EndsWith(mSuffix)) return false;
            return mFilesystem.ExistsFile(path);
        }
        public async Task<bool> ExistsFileAsync(string path, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) return false;
            return await mFilesystem.ExistsFileAsync(path, cancellationToken);
        }


        //methds LEVEL 2
        public Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to save file: path invalid: " + path);
            return mFilesystem.SaveFile(path, stream, settings);
        }
        public async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken) {
            return await mFilesystem.SaveFileAsync(path, stream, settings, cancellationToken);
        }
        public Entry CreateDirectory(string path) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to create directory: path invalid: " + path);
            return mFilesystem.CreateDirectory(path);
        }
        public async Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to create directory: path invalid: " + path);
            return await mFilesystem.CreateDirectoryAsync(path, cancellationToken);
        }
        public void Delete(string path) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to delete directory: path not found: " + path);
            mFilesystem.Delete(path);
            if (mFilesystem.ExistsFile(path + mSuffix)) mFilesystem.DeleteFile(path + mSuffix);
        }
        public async Task DeleteAsync(string path, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to delete directory: path not found: " + path);
            await mFilesystem.DeleteAsync(path, cancellationToken);
            if (await mFilesystem.ExistsFileAsync(path + mSuffix, cancellationToken)) await mFilesystem.DeleteFileAsync(path + mSuffix, cancellationToken);
        }
        public void Touch(string path, DateTime aDate) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to touch directory: path not found: " + path);
            mFilesystem.Touch(path, aDate);
        }
        public async Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to touch directory: path not found: " + path);
            await mFilesystem.TouchAsync(path, aDate, cancellationToken);
        }


        //methods LEVEL 3
        public void DeleteFile(string path) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to delete file: path not found: " + path);
            mFilesystem.DeleteFile(path);
            if (mFilesystem.ExistsFile(path + mSuffix)) mFilesystem.DeleteFile(path + mSuffix);
        }
        public async Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to delete file: path not found: " + path);
            await mFilesystem.DeleteFileAsync(path, cancellationToken);
            if (mFilesystem.ExistsFile(path + mSuffix)) await mFilesystem.DeleteFileAsync(path + mSuffix, cancellationToken);
        }
        public void DeleteDirectory(string path) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to delete directory: path not found: " + path);
            mFilesystem.DeleteDirectory(path);
            if (mFilesystem.ExistsFile(path + mSuffix)) mFilesystem.DeleteFile(path + mSuffix);
        }
        public async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to delete directory: path not found: " + path);
            await mFilesystem.DeleteDirectoryAsync(path, cancellationToken);
            if (mFilesystem.ExistsFile(path + mSuffix)) await mFilesystem.DeleteFileAsync(path + mSuffix, cancellationToken);
        }
        public void Copy(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            if (source.EndsWith(mSuffix)) throw new Exception("Unable to copy: path not found: " + source);
            if (destination.EndsWith(mSuffix)) throw new Exception("Unable to copy: path not found: " + destination);
            mFilesystem.Copy(source, destination, settings, logger);
            if (mFilesystem.ExistsFile(source + mSuffix)) mFilesystem.Copy(source + mSuffix, destination + mSuffix, settings, logger);
        }
        public async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (source.EndsWith(mSuffix)) throw new Exception("Unable to copy: path not found: " + source);
            if (destination.EndsWith(mSuffix)) throw new Exception("Unable to copy: path not found: " + destination);
            await mFilesystem.CopyAsync(source, destination, settings, logger, cancellationToken);
            if (await mFilesystem.ExistsFileAsync(source + mSuffix, cancellationToken)) await mFilesystem.CopyAsync(source + mSuffix, destination + mSuffix, settings, logger, cancellationToken);
        }
        public void Move(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger) {
            if (source.EndsWith(mSuffix)) throw new Exception("Unable to move: path not found: " + source);
            if (destination.EndsWith(mSuffix)) throw new Exception("Unable to move: path not found: " + destination);
            mFilesystem.Move(source, destination, settings, logger);
            if (mFilesystem.ExistsFile(source + mSuffix)) mFilesystem.Move(source + mSuffix, destination + mSuffix, settings, logger);
        }
        public async Task MoveAsync(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (source.EndsWith(mSuffix)) throw new Exception("Unable to move: path not found: " + source);
            if (destination.EndsWith(mSuffix)) throw new Exception("Unable to move: path not found: " + destination);
            await mFilesystem.MoveAsync(source, destination, settings, logger, cancellationToken);
            if (mFilesystem.ExistsFile(source + mSuffix)) mFilesystem.Move(source + mSuffix, destination + mSuffix, settings, logger);
        }
        public void Sync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger) {
            if (source.EndsWith(mSuffix)) throw new Exception("Unable to sync: path not found: " + source);
            if (destination.EndsWith(mSuffix)) throw new Exception("Unable to sync: path not found: " + destination);
            mFilesystem.Sync(source, destination, syncSettings, logger);
        }
        public async Task SyncAsync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (source.EndsWith(mSuffix)) throw new Exception("Unable to sync: path not found: " + source);
            if (destination.EndsWith(mSuffix)) throw new Exception("Unable to sync: path not found: " + destination);
            await mFilesystem.SyncAsync(source, destination, syncSettings, logger, cancellationToken);
        }

        //methods LEVEL 4
        public Watcher CreateWatcher(string path, string filter, string[] excludes, bool recursive) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to create watcher: path not found: " + path);
            return mFilesystem.CreateWatcher(path, filter, excludes, recursive);
        }
        public async Task<Watcher> CreateWatcherAsync(string path, string filter, string[] excludes, bool recursive, CancellationToken cancellationToken) {
            if (path.EndsWith(mSuffix)) throw new Exception("Unable to create watcher: path not found: " + path);
            return await mFilesystem.CreateWatcherAsync(path, filter, excludes, recursive, cancellationToken);
        }
        public IDictionary<string, string> GetMetadata(string path) {
            var entry = mFilesystem.GetEntry(path);
            if (entry == null) throw new Exception("Unable to get metadata: path not found: " + path);
            var entryMetadata = mFilesystem.GetEntry(path + mSuffix);
            if (entryMetadata == null) {
                return new Dictionary<string, string>();
            } else {
                var json = mFilesystem.LoadTextFile(path + mSuffix);
                return JsonSerializer.Deserialize<IDictionary<string,string>>(json)!;
            }
        }
        public async Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken) {
            return await Task.FromResult(GetMetadata(path));
        }
        public void SetMetadata(string path, IDictionary<string, string> metadata) {
            var entry = mFilesystem.GetEntry(path);
            if (entry == null) throw new Exception("Unable to set metadata: path not found: " + path);
            var data = new Dictionary<string, string>();
            var dataKeysAdded = new List<string>();
            foreach(var key in metadata.Keys) {
                var keyToUse = key.ToLower().Trim();
                if (!dataKeysAdded.Contains(keyToUse)) {
                    data[keyToUse] = metadata[key];
                    dataKeysAdded.Add(keyToUse);
                }
            }
            var json = JsonSerializer.Serialize(data);
            mFilesystem.SaveTextFile(path + mSuffix, json, System.Text.Encoding.UTF8);
        }
        public Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken) {
            SetMetadata(path, metadata);
            return Task.CompletedTask;
        }
        public bool Supports(string path, Features feature) {
            if (feature == Features.Metadata) return true;
            return mFilesystem.Supports(path, feature);
        }
        public async Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken) {
            if (feature == Features.Metadata) return true;
            return await mFilesystem.SupportsAsync(path, feature, cancellationToken);
        }

    }

}

