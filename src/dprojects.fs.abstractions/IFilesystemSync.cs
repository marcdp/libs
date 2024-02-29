using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;


namespace DProjects.Fs {


    public interface IFilesystemSync : IFilesystemInfo, IDisposable {


        //methods LEVEL 0
        Entry? GetEntry(string path);
        IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null);
        bool Exists(string path);
        Stream LoadReadStream(string path, LoadReadStreamSettings? settings = null);
        Stream LoadWriteStream(string path, LoadWriteStreamSettings? settings = null);

        //methods LEVEL 1
        bool ExistsDirectory(string path);
        bool ExistsFile(string path);

        //methods LEVEL 2
        Entry SaveFile(string path, Stream stream, SaveFileSettings? settings = null);
        Entry CreateDirectory(string path);
        void Delete(string path);
        void Touch(string path, DateTime aDate);

        //methods LEVEL 3
        void DeleteFile(string path);
        void DeleteDirectory(string path);
        void Copy(string path, string destination, CopySettings settings, ILogger<IFilesystem> logger);
        void Move(string path, string destination, MoveSettings settings, ILogger<IFilesystem> logger);
        void Sync(string path, string destination, SyncSettings settings, ILogger<IFilesystem> logger);

        //methods LEVEL 4        
        Watcher CreateWatcher(string path, string filter, string[] excludes, bool recursive);
        IDictionary<string, string> GetMetadata(string path);
        void SetMetadata(string path, IDictionary<string, string> metadata);
        bool Supports(string path, Features feature);


    }


}

