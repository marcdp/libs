using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs {


    public class FilesystemUnion : Filesystem {



        //variables
        private IFilesystem[] mFilesystems;


        //constructor
        public FilesystemUnion(IFilesystem[] filesystems, bool isReadonly) : base(isReadonly) {
            mFilesystems = filesystems;
        } 


        //properties
        public override string Url {
            get {
                var counter = 0;
                var sb = new StringBuilder();
                sb.Append("union:");
                foreach(var fs in mFilesystems) {
                    sb.Append((counter++==0 ? "?" : "&") + "fs=" + UrlUtils.UrlEncodePart(fs.Url).Replace("%3A",":"));
                }
                return sb.ToString();
            }
        }


        //methods LEVEL 0
        public override Entry? GetEntry(string path) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                var entry = filesystem.GetEntry(path);
                if (entry != null) return entry;
            }
            return null;
        }
        public override async Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                var entry = await filesystem.GetEntryAsync(path, cancellationToken);
                if (entry != null) return entry;
            }
            return null;
        }
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            var filesystems = mFilesystems;
            var entries = new List<Entry>();
            foreach (var fs in filesystems) {
                if (fs.ExistsDirectory(path)) {
                    foreach (var entry in fs.GetEntries(path, mode, pattern)) {
                        entries.Add(entry);
                    }
                }
            }
            entries.Sort(new EntryComparer());
            foreach (var entry in entries) {
                yield return entry;
            }
        }
        public override async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null) {
            var filesystems = mFilesystems;
            var entries = new List<Entry>();
            foreach (var fs in filesystems) {
                if (fs.ExistsDirectory(path)) {
                    await foreach (var entry in fs.GetEntriesAsync(path, mode, pattern)) {
                        entries.Add(entry);
                    }
                }
            }
            entries.Sort(new EntryComparer());
            foreach (var entry in entries) {
                yield return entry;
            }
        }
        public override bool Exists(string path) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.Exists(path)) return true;
            }
            return false;
        }
        public override async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (await filesystem.ExistsAsync(path, cancellationToken)) return true;
            }
            return false;
        }
        public override System.IO.Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.Exists(path)) {
                    return filesystem.LoadReadStream(path, settings);
                }
            }
            throw new FileNotFoundException();
        }
        public override async Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.Exists(path)) {
                    return await filesystem.LoadReadStreamAsync(path, settings, cancellationToken);
                }
            }
            throw new FileNotFoundException();
        }
        public override System.IO.Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var filesystems = mFilesystems;
            var pathParent = PathUtils.GetPathParent(path); 
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.ExistsFile(path) || filesystem.ExistsDirectory(pathParent) || i == 0) {
                    return filesystem.LoadWriteStream(path, settings);
                }
            }
            throw new NotImplementedException();
        }
        public override async Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings settings, CancellationToken cancellationToken) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var filesystems = mFilesystems;
            var pathParent = PathUtils.GetPathParent(path);
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.ExistsFile(path) || filesystem.ExistsDirectory(pathParent) || i == 0) {
                    return await filesystem.LoadWriteStreamAsync(path, settings, cancellationToken);
                }
            }
            throw new NotImplementedException();
        }


        //methods LEVEL 1
        public override bool ExistsDirectory(string path) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.ExistsDirectory(path)) return true;
            }
            return false;
        }
        public override async Task<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (await filesystem.ExistsDirectoryAsync(path, cancellationToken)) return true;
            }
            return false;
        }
        public override bool ExistsFile(string path) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.ExistsFile(path)) return true;
            }
            return false;
        }
        public override async Task<bool> ExistsFileAsync(string path, CancellationToken cancellationToken) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (await filesystem.ExistsFileAsync(path, cancellationToken)) return true;
            }
            return false;
        }
        public override Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var filesystems = mFilesystems;
            var pathParent = PathUtils.GetPathParent(path);
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.ExistsFile(path) || filesystem.ExistsDirectory(pathParent) || i == 0) {
                    return filesystem.SaveFile(path, stream, settings);
                }
            }
            throw new NotImplementedException();
        }
        public override async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var filesystems = mFilesystems;
            var pathParent = PathUtils.GetPathParent(path);
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (await filesystem.ExistsFileAsync(path, default) || await filesystem.ExistsDirectoryAsync(pathParent, default) || i == 0) {
                    return await filesystem.SaveFileAsync(path, stream, settings, cancellationToken);
                }
            }
            throw new NotImplementedException();
        }
        public override Entry CreateDirectory(string path) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var filesystems = mFilesystems;
            var pathParent = PathUtils.GetPathParent(path);
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.ExistsDirectory(pathParent) || i==0) {
                    return filesystem.CreateDirectory(path);
                }
            }
            throw new NotImplementedException();
        }
        public override async Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var filesystems = mFilesystems;
            var pathParent = PathUtils.GetPathParent(path);
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.ExistsDirectory(pathParent) || i == 0) {
                    return await filesystem.CreateDirectoryAsync(path, cancellationToken);
                }
            }
            throw new NotImplementedException();
        }
        public override void Delete(string path) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.Exists(path)) {
                    filesystem.Delete(path);
                    break;
                }
            }
        }
        public override async Task DeleteAsync(string path, CancellationToken cancellationToken) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.Exists(path)) {
                    await filesystem.DeleteAsync(path, cancellationToken);
                    break;
                }
            }
        }
        public override void Touch(string path, DateTime aDate) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.Exists(path)) {
                    filesystem.Touch(path, aDate);
                    break;
                }
            }
        }
        public override async Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            var filesystems = mFilesystems;
            for (var i = filesystems.Length - 1; i >= 0; i--) {
                var filesystem = filesystems[i];
                if (filesystem.Exists(path)) {
                    await filesystem.TouchAsync(path, aDate, cancellationToken);
                    break;
                }
            }
        }
         

    }


}