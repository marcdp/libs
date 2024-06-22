using DProjects.Streams;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;


namespace DProjects.Fs {


    public class FilesystemZip : FilesystemSync, IDisposable {


        //variables
        protected IFilesystem mFilesystem;
        protected string mPath;
        protected Encoding mEncoding;
        protected ZipArchive mZipArchive;
        protected List<Entry> mEntries;
        protected ReaderWriterLockSlim mReaderWriterLock;


        //constructor
        public FilesystemZip(IFilesystem filesystem, string path, Encoding? encoding, bool isReadOnly) : base(isReadOnly) {
            mFilesystem = filesystem;
            mPath = path;
            EncodingUtils.RegisterDefaultProvider();
            mEncoding = encoding ?? Encoding.GetEncoding("IBM437");
            mEntries = new List<Entry>();
            mReaderWriterLock = new ReaderWriterLockSlim();
            if (!mFilesystem.Exists(mPath)) {
                if (IsReadonly) {
                    throw new Exception("Unable to open zip file for reading: path not found");
                } else {
                    var zipArchive = new ZipArchive(mFilesystem.LoadWriteStream(mPath, new()), ZipArchiveMode.Create, false, mEncoding);
                    zipArchive.Dispose();
                }
            }
            if (IsReadonly) {
                mZipArchive = new ZipArchive(mFilesystem.LoadWriteStream(mPath, new()), ZipArchiveMode.Read, false, mEncoding);
            } else {
                mZipArchive = new ZipArchive(mFilesystem.LoadWriteStream(mPath, new()), ZipArchiveMode.Update, false, mEncoding);
            }
            foreach (var zipEntry in mZipArchive.Entries) {
                mEntries.Add(ZipEntryToEntry(zipEntry));
            }
        }
        public override void Dispose() {
            mZipArchive.Dispose();
        }


        //properties
        public override string Url {
            get {
                return "zip:" + mFilesystem.ToString() + (mPath.Equals("/") ? "" : mPath);
            }
        }


        //methods LEVEL 0
        public override Entry? GetEntry(string path) {
            mReaderWriterLock.EnterReadLock();
            try {
                return GetEntryRaw(path);
            } finally {
                mReaderWriterLock.ExitReadLock();
            }
        }
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            mReaderWriterLock.EnterReadLock();
            try {
                var result = new List<Entry>();
                foreach (var entry in mEntries) {
                    var entryPathParent = PathUtils.GetPathParent(entry.Path);
                    if (mode == GetModes.All) {
                        if (entryPathParent.Equals(path)) {
                            if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                result.Add(entry);
                            }
                        }
                    } else if (mode == GetModes.Files) {
                        if (entryPathParent.Equals(path) && entry.IsFile()) {
                            if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                result.Add(entry);
                            }
                        }
                    } else if (mode == GetModes.Directories) {
                        if (entryPathParent.Equals(path) && entry.IsDirectory()) {
                            if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                result.Add(entry);
                            }
                        }
                    } else if (mode == GetModes.Descendants) {
                        if (entry.Path.StartsWith(path + "/")) {
                            if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                result.Add(entry);
                            }
                        }
                    }
                }
                result.Sort(new EntryComparer());
                return result.ToArray();
            } finally {
                mReaderWriterLock.ExitReadLock();
            }
        }
        public override bool Exists(string path) {
            mReaderWriterLock.EnterReadLock();
            try {
                return GetEntryRaw(path) != null;
            } finally {
                mReaderWriterLock.ExitReadLock();
            }
        }
        public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            mReaderWriterLock.EnterReadLock();
            try {
                var zipPath = path.Substring(1);
                var zipEntry = mZipArchive.GetEntry(zipPath);
                if (zipEntry == null) throw new Exception("Unable to load stream: path not found: " + path);
                Stream result = zipEntry.Open();
                if (settings != null && (settings.Offset != 0 || settings.Length != -1)) {
                    result = new PartialInputStream(result, settings.Offset, settings.Length);
                }
                return result;
            } finally {
                mReaderWriterLock.ExitReadLock();
            }
        }
        public override Stream LoadWriteStream(string path, LoadWriteStreamSettings settings) {
            mReaderWriterLock.EnterWriteLock();
            try {
                var zipPath = path.Substring(1);
                var zipEntry = mZipArchive.GetEntry(zipPath);
                if (zipEntry == null) {
                    zipEntry = mZipArchive.CreateEntry(zipPath);
                    var entry = ZipEntryToEntry(zipEntry);
                    mEntries.Add(entry);
                }
                var zipEntryStream = zipEntry.Open();
                if (settings.Append) zipEntryStream.Seek(0, SeekOrigin.End);
                if (settings.Truncate) zipEntryStream.SetLength(0);
                var outputStream = new DisposableStream(zipEntryStream, () => {
                    mReaderWriterLock.EnterWriteLock();
                    try {
                        var length = zipEntryStream.Length;
                        for (var i = 0; i < mEntries.Count; i++) {
                            if (mEntries[i].Path.Equals(path)) {
                                var entry = mEntries[i];
                                var zipPath = path.Substring(1);
                                var zipEntry = mZipArchive.GetEntry(zipPath);
                                zipEntry.LastWriteTime = DateTime.Now;
                                var entryEtag = HashUtils.ToHashSHA1Hex(length + "-" + zipEntry.LastWriteTime.ToUniversalTime().ToString("yyyy-MM-dd-HH-mm-ss")).ToLower();
                                mEntries[i] = new Entry(path, entry.EntryType, zipEntry.LastWriteTime.DateTime, zipEntry.LastWriteTime.DateTime, length, entryEtag, 0);
                            }
                        }
                    } finally {
                        mReaderWriterLock.ExitWriteLock();
                    }
                });
                return outputStream;
            } finally {
                mReaderWriterLock.ExitWriteLock();
            }
        }


        //methods LEVEL 2
        public override Entry SaveFile(string path, Stream stream, SaveFileSettings settings) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            if (!ExistsDirectory(PathUtils.GetPathParent(path))) throw new Exception("Unable to save file: parent directory not found");
            var append = (settings != null && settings.Append);
            using (var zipEntryStream = LoadWriteStream(path, new())) {
                if (append) {
                    zipEntryStream.Seek(0, SeekOrigin.End);
                } else {
                    zipEntryStream.SetLength(0);
                }
                StreamUtils.Copy(stream, zipEntryStream);
            }
            return GetEntry(path)!;
        } 
        public override Entry CreateDirectory(string path) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            if (!ExistsDirectory(PathUtils.GetPathParent(path))) CreateDirectory(PathUtils.GetPathParent(path));
            //create
            mReaderWriterLock.EnterWriteLock();
            try {
                var tempPath = "/";
                var pathParts = path.Substring(1).Split('/');
                for (var i = 0; i < pathParts.Length; i++) {
                    tempPath = PathUtils.Combine(tempPath, pathParts[i]);
                    if (GetEntryRaw(tempPath) == null) {
                        var zipPath = tempPath.Substring(1) + "/";
                        var zipEntry = mZipArchive.CreateEntry(zipPath);
                        var entry = ZipEntryToEntry(zipEntry);
                        mEntries.Add(entry);
                    }
                }
                return GetEntryRaw(path)!;
            } finally {
                mReaderWriterLock.ExitWriteLock();
            }
        }
        public override void Delete(string path) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            //delete
            mReaderWriterLock.EnterWriteLock();
            try {
                for (var i = mEntries.Count - 1; i >= 0; i--) {
                    var entry = mEntries[i];
                    if (entry.Path.Equals(path) || entry.Path.StartsWith(path + '/')) {
                        mEntries.RemoveAt(i);
                        if (entry.IsDirectory()) {
                            var zipPath = entry.Path.Substring(1) + '/';
                            var zipEntry = mZipArchive.GetEntry(zipPath);
                            zipEntry.Delete();
                        } else {
                            var zipPath = entry.Path.Substring(1);
                            var zipEntry = mZipArchive.GetEntry(zipPath);
                            zipEntry.Delete();
                        }
                    }
                }
            } finally {
                mReaderWriterLock.ExitWriteLock();
            }
        }
        public override void Touch(string path, DateTime aDate) {
            if (IsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            //touch
            mReaderWriterLock.EnterWriteLock();
            try {
                var zipPath = path.Substring(1);
                var zipEntry = mZipArchive.GetEntry(zipPath);
                if (zipEntry == null) {
                    zipPath = zipPath.Substring(1);
                    zipEntry = mZipArchive.GetEntry(zipPath);
                    if (zipEntry == null) throw new Exception("Unable to load stream: path not found: " + path);
                }
                zipEntry.LastWriteTime = aDate;
            } finally {
                mReaderWriterLock.ExitWriteLock();
            }
        }


        //methods LEVEL 4
        public override bool Supports(string path, Features feature) {
            if (feature == Features.Touch) return true;
            return base.Supports(path, feature);
        }

        //private
        private Entry? GetEntryRaw(string path) {
            if (path.Equals("/")) {
                var entry = mFilesystem.GetEntry(mPath);
                if (entry == null) throw new Exception("unable to get entry: path not found");
                return new Entry(path, EntryType.Directory, entry.Created, entry.Modified, 0, "", 0);
            } else {
                foreach (var entry in mEntries) {
                    if (entry.Path.Equals(path)) return entry;
                }
                return null;
            }
        }
        private Entry ZipEntryToEntry(ZipArchiveEntry zipEntry) {
            var entryPath = "/" + zipEntry.FullName;
            var entryType = EntryType.File;
            var entryEtag = "";
            if (entryPath.EndsWith("/")) {
                entryPath = entryPath.Substring(0, entryPath.Length - 1);
                entryType = EntryType.Directory;
            } else { 
                entryEtag = HashUtils.ToHashSHA1Hex(zipEntry.Length + "-" + zipEntry.LastWriteTime.ToUniversalTime().ToString("yyyy-MM-dd-HH-mm-ss")).ToLower();
            }
            return new Entry(entryPath, entryType, zipEntry.LastWriteTime.DateTime, zipEntry.LastWriteTime.DateTime, zipEntry.Length, entryEtag, 0);
        }
    }

}


