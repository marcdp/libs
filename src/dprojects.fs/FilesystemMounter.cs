using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace DProjects.Fs {


    public class FilesystemMounter : Filesystem, IFilesystemMounter, IDisposable {


        //inner class
        private class MountPointComparer : IComparer<MountPoint> {
            public int Compare(MountPoint x, MountPoint y) {
                return PathUtils.ComparePath(x.Path, y.Path);
            }
        }

        //variables
        private List<MountPoint> mMountPoints;
        private StringComparison mStringComparison;


        //constructor
        public FilesystemMounter(bool isReadonly = false) : base(isReadonly) {
            mMountPoints = new List<MountPoint>();
            mStringComparison = StringComparison.CurrentCultureIgnoreCase;
        }
        public override void Dispose() {
            for ( int i = mMountPoints.Count - 1; i >= 0; i--) {
                var mountPoint = mMountPoints[i];
                mMountPoints.RemoveAt(i);
                if (mountPoint.Owned) {
                    (mountPoint.Filesystem as IDisposable)?.Dispose();
                }
            }
        }


        //properties
        public override string Url {
            get {
                return "mounter:";
            }
        } 


        //methods LEVEL 0
        public override Entry? GetEntry(string path) {
            //get entry
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) return null;
            var entry = mountPoint.Filesystem.GetEntry(PathUtils.Combine(mountPoint.Prefix, path));
            if (entry != null) {
                entry = PrefixPathEntry(mountPoint, entry);
            }
            return entry;
        }
        public override async Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            //get entry
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) return null;
            var entry = await mountPoint.Filesystem.GetEntryAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
            if (entry != null) {
                entry = PrefixPathEntry(mountPoint, entry);
            }
            return entry;
        }
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            //get target mount points
            MountPoint? targetMountPoint = null;
            var additionalDirectories = new Stack<Entry>();
            for (int i = mMountPoints.Count - 1; i >= 0; i--) {
                var mountPoint = mMountPoints[i];
                if (path.StartsWith(mountPoint.Path, mStringComparison)) {
                    //root mountpoint
                    targetMountPoint = mountPoint;
                    break;
                } else if (PathUtils.GetPathParent(mountPoint.Path).Equals(path, mStringComparison)) {
                    // root mounts points to list
                    var entry = mountPoint.Filesystem.GetEntry(PathUtils.Combine("/", mountPoint.Prefix));
                    if (entry != null) {
                        entry = PrefixPathEntry(mountPoint, entry);
                        if (mode == GetModes.All ||
                            (mode == GetModes.Files && entry.IsFile()) ||
                            (mode == GetModes.Directories && entry.IsDirectory()) ||
                            mode == GetModes.Descendants
                            ) {
                            if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                additionalDirectories.Push(entry);
                            }
                        }
                    }
                }
            }
            if (targetMountPoint == null) {
                throw new NotImplementedException("Unhandled mount point for: " + path);
            }
            //list
            var targetPath = PathUtils.Uncombine(targetMountPoint.Path, path, mStringComparison);
            if (targetMountPoint.Prefix.Length > 0) {
                targetPath = PathUtils.Combine(targetMountPoint.Prefix, targetPath);
            } 
            foreach (var entry in targetMountPoint.Filesystem.GetEntries(targetPath, mode, pattern)) {
                var newEntry = PrefixPathEntry(targetMountPoint, entry);
                while (additionalDirectories.Count > 0 && PathUtils.ComparePath(additionalDirectories.Peek().Path, newEntry.Path) < 0) {
                    yield return additionalDirectories.Pop();
                }
                yield return newEntry;
            }
            while (additionalDirectories.Count > 0) {
                yield return additionalDirectories.Pop();
            }
        }
        public override async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            //get target mount points
            MountPoint? targetMountPoint = null;
            var additionalDirectories = new Stack<Entry>();
            for (int i = mMountPoints.Count - 1; i >= 0; i--) {
                var mountPoint = mMountPoints[i];
                if (path.StartsWith(mountPoint.Path, mStringComparison)) {
                    //root mountpoint
                    targetMountPoint = mountPoint;
                    break;
                } else if (PathUtils.GetPathParent(mountPoint.Path).Equals(path)) {
                    // root mounts points to list
                    var entry = await mountPoint.Filesystem.GetEntryAsync(PathUtils.Combine("/", mountPoint.Prefix), cancellationToken);
                    if (entry != null) {
                        entry = PrefixPathEntry(mountPoint, entry);
                        if (mode == GetModes.All ||
                            (mode == GetModes.Files && entry.IsFile()) ||
                            (mode == GetModes.Directories && entry.IsDirectory()) ||
                            mode == GetModes.Descendants
                            ) {
                            if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                additionalDirectories.Push(entry);
                            }
                        }
                    }
                }
            }
            if (targetMountPoint == null) {
                throw new NotImplementedException("Unhandled mount point for: " + path);
            }
            //list
            var targetPath = PathUtils.Uncombine(targetMountPoint.Path, path, mStringComparison);
            if (targetMountPoint.Prefix.Length > 0) {
                targetPath = PathUtils.Combine(targetMountPoint.Prefix, targetPath);
            }
            await foreach (var entry in targetMountPoint.Filesystem.GetEntriesAsync(targetPath, mode, pattern, cancellationToken)) {
                var newEntry = PrefixPathEntry(targetMountPoint, entry);
                while (additionalDirectories.Count > 0 && PathUtils.ComparePath(additionalDirectories.Peek().Path, newEntry.Path) < 0) {
                    yield return additionalDirectories.Pop();
                }
                yield return newEntry;
            }
            while (additionalDirectories.Count > 0) {
                yield return additionalDirectories.Pop();
            }
        }
        public override bool Exists(string path) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return mountPoint.Filesystem.Exists(PathUtils.Combine(mountPoint.Prefix, path));
        }
        public override async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return await mountPoint.Filesystem.ExistsAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
        }
        public override System.IO.Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return mountPoint.Filesystem.LoadReadStream(PathUtils.Combine(mountPoint.Prefix, path), settings);
        }
        public override async Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken ) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return await mountPoint.Filesystem.LoadReadStreamAsync(PathUtils.Combine(mountPoint.Prefix, path), settings, cancellationToken);
        }
        public override System.IO.Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return mountPoint.Filesystem.LoadWriteStream(PathUtils.Combine(mountPoint.Prefix, path), settings);
        }
        public override async Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings settings, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return await mountPoint.Filesystem.LoadWriteStreamAsync(PathUtils.Combine(mountPoint.Prefix, path), settings, cancellationToken);
        }

        //methods LEVEL 1
        public override bool ExistsDirectory(string path) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return mountPoint.Filesystem.ExistsDirectory(PathUtils.Combine(mountPoint.Prefix, path));
        }
        public override async Task<bool> ExistsDirectoryAsync(string path, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return await mountPoint.Filesystem.ExistsDirectoryAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
        }
        public override bool ExistsFile(string path) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return mountPoint.Filesystem.ExistsFile(PathUtils.Combine(mountPoint.Prefix, path));
        }
        public override async Task<bool> ExistsFileAsync(string path, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return await mountPoint.Filesystem.ExistsFileAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
        }
        public override Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            var entry = mountPoint.Filesystem.SaveFile(PathUtils.Combine(mountPoint.Prefix, path), stream, settings);
            if (entry == null) throw new NullReferenceException();
            return PrefixPathEntry(mountPoint, entry);
        }
        public override async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken = default) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            var entry = await mountPoint.Filesystem.SaveFileAsync(PathUtils.Combine(mountPoint.Prefix, path), stream, settings, cancellationToken);
            if (entry == null) throw new NullReferenceException();
            return PrefixPathEntry(mountPoint, entry);
        }
        public override Entry CreateDirectory(string path) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            var entry = mountPoint.Filesystem.CreateDirectory(PathUtils.Combine(mountPoint.Prefix, path));
            if (entry == null) throw new NullReferenceException();
            return PrefixPathEntry(mountPoint, entry);
        }
        public override async Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            var entry = await mountPoint.Filesystem.CreateDirectoryAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
            if (entry == null) throw new NullReferenceException();
            return PrefixPathEntry(mountPoint, entry);
        }
        public override void Delete(string path) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            mountPoint.Filesystem.Delete(PathUtils.Combine(mountPoint.Prefix, path));
        }
        public override async Task DeleteAsync(string path, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            await mountPoint.Filesystem.DeleteAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
        }
        public override void Touch(string path, DateTime aDate) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            mountPoint.Filesystem.Touch(PathUtils.Combine(mountPoint.Prefix, path), aDate);
        }
        public override async Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            await mountPoint.Filesystem.TouchAsync(PathUtils.Combine(mountPoint.Prefix, path), aDate, cancellationToken);
        }


        //methods LEVEL 3
        public override void DeleteFile(string path) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            mountPoint.Filesystem.DeleteFile(PathUtils.Combine(mountPoint.Prefix, path));
        }
        public override async Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            await mountPoint.Filesystem.DeleteFileAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
        }
        public override void DeleteDirectory(string path) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            mountPoint.Filesystem.DeleteDirectory(PathUtils.Combine(mountPoint.Prefix, path));
        }
        public override async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            await mountPoint.Filesystem.DeleteDirectoryAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
        }
        public override void Copy(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            string sourcePath = "" + source;
            string destinationPath = "" + destination;
            var mountPointA = GetMountPoint(ref sourcePath);
            var mountPointB = GetMountPoint(ref destinationPath);
            if (mountPointA == null) throw new Exception("Mount point not found: " + sourcePath);
            if (mountPointB == null) throw new Exception("Mount point not found: " + destinationPath);
            if (mountPointA == mountPointB) {
                mountPointA.Filesystem.Copy(PathUtils.Combine(mountPointA.Prefix, sourcePath), PathUtils.Combine(mountPointA.Prefix, destinationPath), settings, logger);
            } else {
                base.Copy(source, destination, settings, logger);
            }
        }
        public override async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            string sourcePath = "" + source;
            string destinationPath = "" + destination;
            var mountPointA = GetMountPoint(ref sourcePath);
            var mountPointB = GetMountPoint(ref destinationPath);
            if (mountPointA == null) throw new Exception("Mount point not found: " + sourcePath);
            if (mountPointB == null) throw new Exception("Mount point not found: " + destinationPath);
            if (mountPointA == mountPointB) {
                await mountPointA.Filesystem.CopyAsync(PathUtils.Combine(mountPointA.Prefix, sourcePath), PathUtils.Combine(mountPointA.Prefix, destinationPath), settings, logger, cancellationToken);
            } else {
                await base.CopyAsync(source, destination, settings, logger, cancellationToken);
            }
        }
        public override void Move(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger) {
            string sourcePath = "" + source;
            string destinationPath = "" + destination;
            var mountPointA = GetMountPoint(ref sourcePath);
            var mountPointB = GetMountPoint(ref destinationPath);
            if (mountPointA == null) throw new Exception("Mount point not found: " + sourcePath);
            if (mountPointB == null) throw new Exception("Mount point not found: " + destinationPath);
            if (mountPointA == mountPointB) {
                mountPointA.Filesystem.Move(PathUtils.Combine(mountPointA.Prefix, sourcePath), PathUtils.Combine(mountPointA.Prefix, destinationPath), settings, logger);
            } else {
                base.Move(source, destination, settings, logger);
            }
        }
        public override async Task MoveAsync(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            string sourcePath = "" + source;
            string destinationPath = "" + destination;
            var mountPointA = GetMountPoint(ref sourcePath);
            var mountPointB = GetMountPoint(ref destinationPath);
            if (mountPointA == null) throw new Exception("Mount point not found: " + sourcePath);
            if (mountPointB == null) throw new Exception("Mount point not found: " + destinationPath);
            if (mountPointA == mountPointB) {
                await mountPointA.Filesystem.MoveAsync(PathUtils.Combine(mountPointA.Prefix, sourcePath), PathUtils.Combine(mountPointA.Prefix, destinationPath), settings, logger, cancellationToken);
            } else {
                await base.MoveAsync(source, destination, settings, logger, cancellationToken);
            }
        }
        public override void Sync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger) {
            string sourcePath = "" + source;
            string destinationPath = "" + destination;
            var mountPointA = GetMountPoint(ref sourcePath);
            var mountPointB = GetMountPoint(ref destinationPath);
            if (mountPointA == null) throw new Exception("Mount point not found: " + sourcePath);
            if (mountPointB == null) throw new Exception("Mount point not found: " + destinationPath);
            if (mountPointA == mountPointB) {
                mountPointA.Filesystem.Sync(mountPointA.Prefix + sourcePath, mountPointA.Prefix + destinationPath, syncSettings, logger);
                mountPointA.Filesystem.Sync(PathUtils.Combine(mountPointA.Prefix, sourcePath), PathUtils.Combine(mountPointA.Prefix, destinationPath), syncSettings, logger);
            } else {
                base.Sync(source, destination, syncSettings, logger);
            }
        }
        public override async Task SyncAsync(string source, string destination, SyncSettings syncSettings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            string sourcePath = "" + source;
            string destinationPath = "" + destination;
            var mountPointA = GetMountPoint(ref sourcePath);
            var mountPointB = GetMountPoint(ref destinationPath);
            if (mountPointA == null) throw new Exception("Mount point not found: " + sourcePath);
            if (mountPointB == null) throw new Exception("Mount point not found: " + destinationPath);
            if (mountPointA == mountPointB) {
                await mountPointA.Filesystem.SyncAsync(mountPointA.Prefix + sourcePath, mountPointA.Prefix + destinationPath, syncSettings, logger, cancellationToken);
                await mountPointA.Filesystem.SyncAsync(PathUtils.Combine(mountPointA.Prefix, sourcePath), PathUtils.Combine(mountPointA.Prefix, destinationPath), syncSettings, logger, cancellationToken);
            } else {
                await base.SyncAsync(source, destination, syncSettings, logger, cancellationToken);
            }
        }



        //methods LEVEL 4
        public override Watcher CreateWatcher(string path, string filter, string[] excludes, bool recursive) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            var watcher = mountPoint.Filesystem.CreateWatcher(PathUtils.Combine(mountPoint.Prefix, path), filter, excludes, recursive);
            watcher.WithPath(PathUtils.Uncombine(mountPoint.Prefix, PathUtils.Combine(mountPoint.Path, watcher.Path), mStringComparison));
            return watcher;
        }
        public override IDictionary<string, string> GetMetadata(string path) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            var result = mountPoint.Filesystem.GetMetadata(PathUtils.Combine(mountPoint.Prefix, path));
            return result;
        }
        public override async Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return await mountPoint.Filesystem.GetMetadataAsync(PathUtils.Combine(mountPoint.Prefix, path), cancellationToken);
        }
        public override void SetMetadata(string path, IDictionary<string, string> metadata) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            mountPoint.Filesystem.SetMetadata(PathUtils.Combine(mountPoint.Prefix, path), metadata);
        }
        public override async Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            await mountPoint.Filesystem.SetMetadataAsync(PathUtils.Combine(mountPoint.Prefix, path), metadata, cancellationToken);
        }
        public override bool Supports(string path, Features feature) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return mountPoint.Filesystem.Supports(PathUtils.Combine(mountPoint.Prefix, path), feature);
        }
        public override async Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken) {
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) throw new Exception("Mount point not found: " + path);
            return await mountPoint.Filesystem.SupportsAsync(PathUtils.Combine(mountPoint.Prefix, path), feature, cancellationToken);
        }

        //custom methods
        public void Mount(string path, IFilesystem filesystem, bool takeOwnership, string prefix = "") {
            //check if there is some already mounted FilesystemMounter
            if (prefix.Length > 1 && prefix.EndsWith("/")) {
                prefix = prefix.Substring(0, prefix.Length - 1);
            }
            for (int i = mMountPoints.Count - 1; i >= 0; i--) {
                MountPoint objMountPoint = mMountPoints[i];
                if (objMountPoint.Path == path && objMountPoint.Filesystem is FilesystemMounter) {
                    path = "/";
                    ((FilesystemMounter)objMountPoint.Filesystem).Mount(path, filesystem, takeOwnership);
                    return;
                } else if ((path.StartsWith(objMountPoint.Path + "/") || objMountPoint.Path == "/") && objMountPoint.Filesystem is FilesystemMounter) {
                    path = PathUtils.Uncombine(objMountPoint.Path, path, mStringComparison);
                    ((FilesystemMounter)objMountPoint.Filesystem).Mount(path, filesystem, takeOwnership);
                    return;
                }
            }
            //mount here
            foreach (MountPoint mountPoint in mMountPoints) {
                if (mountPoint.Path == path) {
                    throw new Exception("Unable to mount filesystem: path already mounted: " + path);
                }
            }
            var newMountPoint = new MountPoint(path, filesystem, prefix, takeOwnership);
            mMountPoints.Add(newMountPoint);
            mMountPoints.Sort(new MountPointComparer());
            
        }
        public bool Unmount(string path) {
            //check if there is some already mounted FilesystemMounter
            for (int i = mMountPoints.Count - 1; i >= 0; i--) {
                var mountPoint = mMountPoints[i];
                if (mountPoint.Path == path && mountPoint.Filesystem is IFilesystemMounter) {
                    if (mountPoint.Owned) {
                        (mountPoint.Filesystem as IDisposable)?.Dispose();
                    }
                    mMountPoints.Remove(mountPoint);
                    return true;
                } else if ((path.StartsWith(mountPoint.Path + "/") || mountPoint.Path == "/") && mountPoint.Filesystem is FilesystemMounter) {
                    path = PathUtils.Uncombine(mountPoint.Path, path, mStringComparison);
                    return ((FilesystemMounter)mountPoint.Filesystem).Unmount(path);
                }
            }
            //unmount from here
            foreach (MountPoint mountPoint in mMountPoints.ToArray()) {
                if (mountPoint.Path == path) {
                    mMountPoints.Remove(mountPoint);
                    if (mountPoint.Owned) {
                        (mountPoint.Filesystem as IDisposable)?.Dispose();
                    }
                    return true;
                }
            }
            return false;
        }
        public MountPoint? GetMountPoint(ref string path) {
            for (int i = mMountPoints.Count - 1; i >= 0; i--) {
                var mountPoint = mMountPoints[i];
                if (mountPoint.Path.Equals(path, mStringComparison)) {
                    path = "/";
                    return mountPoint;
                } else if (path.StartsWith(mountPoint.Path + "/", mStringComparison) || mountPoint.Path == "/") {
                    path = PathUtils.Uncombine(mountPoint.Path, path, mStringComparison);
                    return mountPoint;
                }
            }
            return null;
        }
        public bool IsMountPoint(string path) {
            for (int i = mMountPoints.Count - 1; i >= 0; i--) {
                MountPoint mountPoint = mMountPoints[i];
                if (mountPoint.Path.Equals(path, mStringComparison)) {
                    return true;
                } else if (mountPoint.Filesystem is FilesystemMounter) {
                    if ((path.StartsWith(mountPoint.Path + "/", mStringComparison) || mountPoint.Path == "/") && ((FilesystemMounter)mountPoint.Filesystem).IsMountPoint(PathUtils.Uncombine(mountPoint.Path, path, mStringComparison))) {
                        return true;
                    }
                }
            }
            return false;
        }
        public string? GetNativeMountPath(string path) {
            string? result = null;
            var mountPoint = GetMountPoint(ref path);
            if (mountPoint == null) {
                result = null;
            } else if (mountPoint.Filesystem is FilesystemLocal) {
                var filesystemLocal = (FilesystemLocal)mountPoint.Filesystem;
                result = filesystemLocal.GetNativePath(path, mountPoint.Prefix);
            } else if (mountPoint.Filesystem is FilesystemMounter) {
                var filesystemMounter = (FilesystemMounter)mountPoint.Filesystem;
                result = filesystemMounter.GetNativeMountPath(path);
            } else {
                result = null;
            }
            return result;
        }
        private Entry PrefixPathEntry(MountPoint mountPoint, Entry entry, bool forceReadonly = false) {
            if (!string.IsNullOrEmpty(mountPoint.Prefix)) {
                int kk = 13;
            }
            string newPath = PathUtils.Combine(mountPoint.Path, PathUtils.Uncombine(mountPoint.Prefix, entry.Path, mStringComparison));
            //string newPath = PathUtils.Uncombine(mountPoint.Prefix, PathUtils.Combine(mountPoint.Path, entry.Path), mStringComparison);
            return entry.WithPath(newPath);
        }
        public MountPoint[] GetMountPoints() {
            return mMountPoints.ToArray();
        }


    }


}