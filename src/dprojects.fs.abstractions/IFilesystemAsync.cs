using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs {


    public interface IFilesystemAsync :  IFilesystemInfo {


        //methods LEVEL 0
        Task<Entry?> GetEntryAsync(string path);
        IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null);
        Task<bool> ExistsAsync(string path);
        Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings? settings = null);
        Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings? settings = null);


        //methods LEVEL 1
        Task<bool> ExistsDirectoryAsync(string path);
        Task<bool> ExistsFileAsync(string path);


        //methods LEVEL 2
        Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings? settings = null);
        Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken);
        Task DeleteAsync(string path, CancellationToken cancellationToken);
        Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken);


        //methods LEVEL 3
        Task DeleteFileAsync(string path, CancellationToken cancellationToken);
        Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken);
        Task CopyAsync(string path, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken);
        Task MoveAsync(string path, string destination, MoveSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken);
        Task SyncAsync(string path, string destination, SyncSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken);

        //methods LEVEL 4        
        Task<Watcher> CreateWatcherAsync(string path, string filter, string[] excludes, bool recursive);
        Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken);
        Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken);
        Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken);


    }


}

