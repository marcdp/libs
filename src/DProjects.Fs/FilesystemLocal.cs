using DProjects.Streams;
using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DProjects.Fs {


    public class FilesystemLocal : FilesystemSync {
         

        //variables
        protected string mPath;


        //constructor
        public FilesystemLocal(string path, bool isReadonly, bool init, bool file) : base(isReadonly) {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException();
            if (path.StartsWith("file://")) {
                path = new Uri(path).LocalPath;
            }
            mPath = System.IO.Path.GetFullPath(new DirectoryInfo(path).FullName.Replace('\\', System.IO.Path.DirectorySeparatorChar));
            if (init) {
                if (!Directory.Exists(mPath) && !File.Exists(mPath)) {
                    if (file) {
                        FileUtils.WriteFile(path, []);
                    } else {
                        Directory.CreateDirectory(mPath);
                    }
                }
            }
            if (!Directory.Exists(mPath) && !File.Exists(mPath)) {
                throw new Exception("Unable to start FilesystemLocal: path not found: " + mPath);
            }
        } 


        //properties
        public override string Url {
            get {
                if (mPath.StartsWith("\\\\") && !mPath.StartsWith("\\\\?\\")) {
                    return "file:" + mPath.Replace("\\", "/");
                }
                return "file:///" + mPath.Replace("\\\\?\\", "").Replace(":", "").Replace(System.IO.Path.DirectorySeparatorChar, '/');
            }
        } 


        //methods LEVEL 0
        public override Entry? GetEntry(string path) {
            string fullPath = GetNativePath(path);
            if (StringUtils.Equals(fullPath, mPath)) {
                var directoryInfo = new DirectoryInfo(fullPath);
                if (directoryInfo.Exists) {
                    return new Entry("/", EntryType.Directory, directoryInfo.CreationTime, directoryInfo.LastWriteTime, 0, "", 0);
                } else {
                    var fileInfo = new FileInfo(fullPath);
                    if (fileInfo.Exists) {
                        return CreateEntry(new FileInfo(fullPath));
                    }
                }
                return null;
            } else {
                var fileInfo = new FileInfo(fullPath);
                if (fileInfo.Exists) return CreateEntry(new FileInfo(fullPath));
                var directoryInfo = new DirectoryInfo(fullPath);
                if (directoryInfo.Exists) return CreateEntry(directoryInfo);
                return null;
            }
        }
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            var di = new DirectoryInfo(GetNativePath(path));
            var entries = new List<Entry>();
            foreach (var fsi in di.GetFileSystemInfos()) {
                entries.Add(CreateEntry(fsi));
            }
            entries.Sort(new EntryComparer());
            foreach (var entry in entries) {
                var isValid = false;
                if (entry.IsFile() && (mode == GetModes.All || mode == GetModes.Files || mode == GetModes.Descendants)) isValid = true;
                if (entry.IsDirectory() && (mode == GetModes.All || mode == GetModes.Directories || mode == GetModes.Descendants)) isValid = true;
                if (isValid) {
                    if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                        yield return entry;
                    }
                }
                if (mode == GetModes.Descendants && entry.IsDirectory()) {
                    foreach (var subentry in GetEntries(entry.Path, mode, pattern)) {
                        yield return subentry;
                    }
                }
            }
        }
        public override bool Exists(string path) {
            var fullPath = GetNativePath(path);
            return File.Exists(fullPath) || Directory.Exists(fullPath);
        }        
        public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            var fullPath = GetNativePath(path);
            if (Directory.Exists(fullPath)) {
                throw new Exception("Unable to load read stream: directory: " + mPath);
            }
            if (!File.Exists(fullPath)) {
                throw new Exception("Unable to load read stream: file not found: " + mPath);
            }
            int bufferSize = 4096;
            Stream result = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, bufferSize, FileOptions.SequentialScan);
            if (settings != null && (settings.Offset != 0 || settings.Length != -1)) {
                result = new PartialInputStream(result, settings.Offset, settings.Length);
            }
            return result;
        }
        public override Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            settings ??= new LoadWriteStreamSettings();
            string fullPath = GetNativePath(path);
            if (Directory.Exists(fullPath)) {
                throw new Exception("Unable to load write stream: directory: " + mPath);
            }
            if ((settings.Append || settings.Truncate) && !File.Exists(fullPath)) FileUtils.WriteFile(fullPath, []);
            if (settings.Append) {
                return new FileStream(fullPath, (settings.Truncate ? FileMode.Truncate : FileMode.Append), FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
            } else {
                return new FileStream(fullPath, (settings.Truncate ? FileMode.Truncate : FileMode.OpenOrCreate) , FileAccess.ReadWrite, FileShare.ReadWrite);
            }
        }


        //methods LEVEL 2
        public override Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            string fullPath = GetNativePath(path);
            var append = (settings != null && settings.Append);
            FileUtils.WriteFile(fullPath, stream, append: append);
            var entry = GetEntry(path);
            if (entry == null) throw new NullReferenceException();
            return entry;
        } 
        public override async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            string fullPath = GetNativePath(path);
            var append = (settings != null && settings.Append);
            await FileUtils.WriteFileAsync(fullPath, stream, append: append);
            var entry = await GetEntryAsync(path, cancellationToken);
            if (entry == null) throw new NullReferenceException();
            return entry;
        }
        public override Entry CreateDirectory(string path) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            var fullPath = GetNativePath(path);
            if (!Directory.Exists(fullPath)) {
                Directory.CreateDirectory(fullPath);
            }
            var entry = GetEntry(path);
            if (entry == null) throw new NullReferenceException();
            return entry;
        }
        public override void Delete(string path) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            var fullPath = GetNativePath(path);
            if (File.Exists(fullPath)) {
                FileUtils.DeleteFile(fullPath);
            } else if (Directory.Exists(fullPath)) {
                FileUtils.DeleteFolder(fullPath);
            }
        }
        public override void Touch(string path, DateTime aDate) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            var fullPath = GetNativePath(path);
            if (File.Exists(fullPath)) {
                File.SetLastWriteTimeUtc(fullPath, aDate);
            }
        }


        //methods LEVEL 3
        public override void Copy(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            string sourceFullPath = GetNativePath(source);
            string destinationFullPath = GetNativePath(destination);
            if (Directory.Exists(sourceFullPath)) {
                if (!System.IO.Directory.Exists(destinationFullPath)) {
                    FileUtils.CreateFolder(destinationFullPath);
                }
                if (settings.Recursive) {
                    FileUtils.CopyDirectory(sourceFullPath, destinationFullPath, settings.Overwrite, "*", settings.Recursive, "", false, true);
                }
            } else if (File.Exists(sourceFullPath)) {
                if (settings.Overwrite) {
                    if (Directory.Exists(destinationFullPath)) {
                        destinationFullPath = System.IO.Path.Combine(destinationFullPath, System.IO.Path.GetFileName(source));
                    }
                    File.Copy(sourceFullPath, destinationFullPath, true);
                    File.SetLastWriteTime(destinationFullPath, DateTime.Now);
                } else {
                    if (Directory.Exists(destinationFullPath)) {
                        destinationFullPath = System.IO.Path.Combine(destinationFullPath, System.IO.Path.GetFileName(source));
                    }
                    if (!File.Exists(destinationFullPath)) {
                        File.Copy(sourceFullPath, destinationFullPath, true);
                    }
                }
            } else {
                throw new Exception("Path not found \'" + source + "\'");
            }
        }
        public override void Move(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger) {
            if (IsReadonly) throw new InvalidOperationException("Unable to modify filesystem: filesystem is readonly");
            string fullSourcePath = GetNativePath(source);
            string fullDestinationPath = GetNativePath(destination);
            if (File.Exists(fullSourcePath)) {
                if (File.Exists(fullDestinationPath)) {
                    File.Copy(fullSourcePath, fullDestinationPath, true);
                    File.Delete(fullSourcePath);
                } else {
                    File.Move(fullSourcePath, fullDestinationPath);
                }
            } else if (Directory.Exists(fullSourcePath)) {
                Directory.Move(fullSourcePath, fullDestinationPath);
            }
        }


        //methods LEVEL 4
        public override Watcher CreateWatcher(string path, string filter, string[] excludes, bool recursive) {
            string fullPath = GetNativePath(path);
            return new MyWatcher(path, filter, excludes, recursive, fullPath);
        }
        private class MyWatcher : Watcher {
            private FileSystemWatcher mFileSystemWatcher;
            public MyWatcher(string path, string filter, string[] excludes, bool recursive, string localpath) : base(path, filter, excludes, recursive) {
                mFileSystemWatcher = new FileSystemWatcher(localpath, filter);
                mFileSystemWatcher.Created += (source, e) => {
                    string relativePath = e.FullPath.Substring(localpath.Length).Replace("\\", "/");
                    foreach (var exclude in excludes) if (StringUtils.Like(relativePath, exclude)) return;
                    Raise(ChangeType.Created, relativePath);
                };
                mFileSystemWatcher.Changed += (source, e) => {
                    string relativePath = e.FullPath.Substring(localpath.Length).Replace("\\", "/");
                    foreach (var exclude in excludes) if (StringUtils.Like(relativePath, exclude)) return;
                    Raise(ChangeType.Changed, relativePath);
                };
                mFileSystemWatcher.Deleted += (source, e) => {
                    string relativePath = e.FullPath.Substring(localpath.Length).Replace("\\", "/");
                    foreach (var exclude in excludes) if (StringUtils.Like(relativePath, exclude)) return;
                    Raise(ChangeType.Deleted, relativePath);
                };
                mFileSystemWatcher.Renamed += (source, e) => {
                    string relativePath = e.FullPath.Substring(localpath.Length).Replace("\\", "/");
                    foreach (var exclude in excludes) if (StringUtils.Like(relativePath, exclude)) return;
                    Raise(ChangeType.Renamed, relativePath);
                };
                mFileSystemWatcher.IncludeSubdirectories = recursive;
                mFileSystemWatcher.InternalBufferSize = mFileSystemWatcher.InternalBufferSize;
                mFileSystemWatcher.EnableRaisingEvents = true;
            }
            public override void Dispose() {
                mFileSystemWatcher.EnableRaisingEvents = false;
                mFileSystemWatcher.Dispose();
                base.Dispose();
            }
        }
        public override bool Supports(string path, Features feature) {
            if (feature == Features.Touch) return true;
            if (feature == Features.CreateWatcher) return true;
            return false;
        } 


        //private methods
        protected Entry CreateEntry(FileSystemInfo fsi) {
            string path = fsi.FullName.Substring(GetNativePath("").Length);
            if (path.IndexOf('\\') != -1) path = path.Replace('\\', '/');
            int flags = 0;
            if (IsReadonly || fsi.Attributes.HasFlag(FileAttributes.ReadOnly)) flags += (int)Flags.Readonly;
            if (fsi.Attributes.HasFlag(FileAttributes.Hidden)) flags += (int)Flags.Hidden;
            if (fsi is DirectoryInfo) {
                return new Entry(path, EntryType.Directory, fsi.CreationTime, fsi.LastWriteTime, 0, "", flags);
            } else if (fsi is FileInfo) {
                var fi = (FileInfo)fsi;
                string etag = HashUtils.ToHashSHA1Hex(fi.Length + "-" + fi.LastWriteTimeUtc.ToUniversalTime().ToString("yyyy-MM-dd-HH-mm-ss")).ToLower();
                return new Entry(path, EntryType.File, fi.CreationTime, fi.LastWriteTime, fi.Length, etag, flags);
            }
            throw new NullReferenceException();
        }
        public string GetNativePath(string path, string prefix = "") {
            if (path.IndexOf("/") != -1) {
                path = path.Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
            while (path.StartsWith("\\") || path.StartsWith("/")) {
                path = path.Substring(1);
            }
            path = (path.Length == 0 ? mPath : System.IO.Path.Combine(mPath, (prefix.StartsWith("/") ? prefix.Substring(1) : ""), path));
            if (!FileUtils.GetFileIsDescendantFromDirectory(path, mPath)) path = mPath;
            return path;
        }

    }


}

