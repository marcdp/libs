using DProjects.Streams;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace DProjects.Fs {


    public class FilesystemMem : FilesystemSync {

         
        //inner classes
        protected class MyEntry : DProjects.Fs.Entry {
            private IDictionary<string, string> mMetadata;
            private SortedList<string, MyEntry> mChilds;
            private byte[] mContent;
            public MyEntry(string path, EntryType entryType, DateTime created, DateTime modified, long length, string etag, int flags) : base(path, entryType, created, modified, length, etag, flags) {
                mMetadata = new Dictionary<string, string>();
                mChilds = new SortedList<string, MyEntry>(new EntryNameComparer());
                mContent = [];
            }
            public IDictionary<string, string> Metadata => mMetadata;
            public MyEntry? GetEntry(string path) {
                if (StringUtils.Equals(Path, path)) {
                    return this;
                }
                foreach (var entry in mChilds.Values) {
                    var aux = entry.GetEntry(path);
                    if (aux != null) {
                        return aux;
                    }
                }
                return null;
            }
            public SortedList<string, MyEntry> Childs {
                get {
                    return mChilds;
                }
            }
            public MyEntry[] Descendants {
                get {
                    List<MyEntry> aux = new List<MyEntry>();
                    foreach (var childEntry in mChilds.Values) {
                        aux.Add(childEntry);
                        aux.AddRange(childEntry.Descendants);
                    }
                    return aux.ToArray();
                }
            }
            public byte[] Content {
                get {
                    return mContent;
                }
                set {
                    Length = value.Length;
                    mContent = value;
                }
            }
            public void SetLastWritetime(DateTime lastWriteTime) {
                Modified = lastWriteTime;
            }
        }

        //variables
        protected MyEntry mEntry;
        protected ReaderWriterLockSlim mReaderWriterLock;
        protected bool mDirty;
        protected bool mAutoFlush;


        //constructor
        public FilesystemMem(bool isReadonly, bool autoFlush) : base(isReadonly) {
            mReaderWriterLock = new ReaderWriterLockSlim();
            mEntry = new MyEntry("/", EntryType.Directory, DateTime.Now, DateTime.Now, 0, "", isReadonly ? (int)Flags.Readonly : 0);
            mAutoFlush = autoFlush;
        }


        //properties
        public override string Url {
            get {
                return "mem://";
            }
        }


        //methods LEVEL 0
        public override Entry? GetEntry(string path) {
            mReaderWriterLock.EnterReadLock();
            try {
                return mEntry.GetEntry(path);
            } finally {
                mReaderWriterLock.ExitReadLock();
            }
        }
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            List<MyEntry> result = new List<MyEntry>();
            PathUtils.Validate(path);
            mReaderWriterLock.EnterReadLock();
            try {
                if (mode == GetModes.All) {
                    var parent = mEntry.GetEntry(path);
                    if (parent == null) {
                        throw new Exception("path not found: " + path);
                    }
                    foreach (var entry in parent.Childs.Values) {
                        if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                            result.Add(entry);
                        }
                    }
                } else if (mode == GetModes.Directories) {
                    var parent = mEntry.GetEntry(path);
                    if (parent == null) {
                        throw new Exception("path not found: " + path);
                    }
                    foreach (var entry in parent.Childs.Values) {
                        if (entry.IsDirectory()) {
                            if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                result.Add(entry);
                            }
                        }
                    }
                } else if (mode == GetModes.Files) {
                    var parent = mEntry.GetEntry(path);
                    if (parent == null) {
                        throw new Exception("path not found: " + path);
                    }
                    foreach (var entry in parent.Childs.Values) {
                        if (!entry.IsDirectory()) {
                            if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                                result.Add(entry);
                            }
                        }
                    }
                } else if (mode == GetModes.Descendants) {
                    var entry = mEntry.GetEntry(path);
                    if (entry == null) {
                        throw new Exception("path not found: " + path);
                    }
                    foreach (MyEntry subentry in entry.Descendants) {
                        if (pattern == null || StringUtils.Like(subentry.Name, pattern)) {
                            result.Add(subentry);
                        }
                    }
                }
            } finally {
                mReaderWriterLock.ExitReadLock();
            }            
            return result.ToArray();
        }
        public override bool Exists(string path) {
            return GetEntry(path) != null;
        }
        public override Stream LoadReadStream(string path, LoadReadStreamSettings? settings = null) {
            PathUtils.Validate(path);
            mReaderWriterLock.EnterReadLock();
            try {
                var entry = mEntry.GetEntry(path);
                if (entry == null) {
                    throw new Exception("Unable to load stream \'" + path + "\': not found");
                }
                if (entry.IsDirectory()) {
                    throw new Exception("Unable to load stream \'" + path + "\': directory");
                }
                Stream result = new MemoryStream(entry.Content);
                if (settings != null && (settings.Offset != 0 || settings.Length != -1)) {
                    result = new PartialInputStream(result, settings.Offset, settings.Length);
                }
                return result;
            } finally {
                mReaderWriterLock.ExitReadLock();
            }
        }
        public override Stream LoadWriteStream(string path, LoadWriteStreamSettings? settings = null) {
            if (mIsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            if (!ExistsDirectory(PathUtils.GetPathParent(path))) throw new Exception("Unable to modify filesystem: parent path not found");
            var memoryStream = new MemoryStream();
            settings ??= new LoadWriteStreamSettings();
            MyEntry? entry = null;
            mReaderWriterLock.EnterReadLock();
            try {
                entry = mEntry.GetEntry(path);
            } finally {
                mReaderWriterLock.ExitReadLock();
            }
            if (entry != null) {
                if (settings.Truncate) {
                } else if (settings.Append) {
                    memoryStream.Write(entry.Content, 0, entry.Content.Length);
                } else {
                    memoryStream.Write(entry.Content, 0, entry.Content.Length);
                    memoryStream.Seek(0, SeekOrigin.Begin);
                }
            }
            var stream = new DisposableStream(memoryStream, () => {
                SaveFile(path, new MemoryStream(memoryStream.ToArray()), new());
            });
            return stream;
        }


        //methods LEVEL 2
        public override Entry SaveFile(string path, Stream stream, SaveFileSettings? settings = null) {
            if (mIsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            if (!ExistsDirectory(PathUtils.GetPathParent(path))) throw new Exception("Unable to modify filesystem: parent path not found");
            MyEntry? entry = null;
            byte[] content = StreamUtils.ReadBytes(stream);
            PathUtils.Validate(path);
            mReaderWriterLock.EnterUpgradeableReadLock();
            try {
                entry = mEntry.GetEntry(path);
                if (entry == null) {
                    var parent = mEntry.GetEntry(PathUtils.GetPathParent(path));
                    if (parent == null) {
                        mReaderWriterLock.EnterWriteLock();
                        try {
                            parent = CreateDirectoryRecursively(PathUtils.GetPathParent(path));
                        } finally {
                            mReaderWriterLock.ExitWriteLock();
                        }
                    }
                    DateTime objLastWriteDate = DateTime.Now;
                    string etag = HashUtils.ToHashSHA1Hex(content.Length + "-" + objLastWriteDate.ToUniversalTime().ToString("YYYY-MM-dd-HH-mm-ss")).ToLower();
                    entry = new MyEntry(path, EntryType.File, objLastWriteDate, objLastWriteDate, content.Length, etag, 0);
                    parent.Childs.Add(entry.Name, entry);
                }
                mReaderWriterLock.EnterWriteLock();
                try {
                    if (settings != null && settings.Append) {
                        entry.Content = ByteUtils.Concat(entry.Content, content);
                    } else {
                        entry.Content = content;
                    }
                    entry.SetLastWritetime(DateTime.Now);
                } finally {
                    mReaderWriterLock.ExitWriteLock();
                }
            } finally {
                mReaderWriterLock.ExitUpgradeableReadLock();
                MarkAsDirty();
            }
            return entry;
        }
        public override Entry CreateDirectory(string path) {
            if (mIsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            MyEntry? entry = null;
            mReaderWriterLock.EnterUpgradeableReadLock();
            try {
                var parent = mEntry.GetEntry(PathUtils.GetPathParent(path));
                if (parent == null) {
                    mReaderWriterLock.EnterWriteLock();
                    try {
                        parent = CreateDirectoryRecursively(PathUtils.GetPathParent(path));
                    } finally {
                        mReaderWriterLock.ExitWriteLock();
                    }
                }
                entry = parent.GetEntry(path);
                if (entry == null) {
                    mReaderWriterLock.EnterWriteLock();
                    try {
                        entry = new MyEntry(path, EntryType.Directory, DateTime.Now, DateTime.Now, 0, "", 0);
                        parent.Childs.Add(entry.Name, entry);
                    } finally {
                        mReaderWriterLock.ExitWriteLock();
                    }
                }
            } finally {
                mReaderWriterLock.ExitUpgradeableReadLock();
                MarkAsDirty();
            }
            return entry;
        }
        public override void Delete(string path) {
            if (mIsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            if (path == "/") {
                return;
            }
            mReaderWriterLock.EnterUpgradeableReadLock();
            try {
                var parent = mEntry.GetEntry(PathUtils.GetPathParent(path));
                if (parent != null) {
                    var entry = parent.GetEntry(path);
                    if (entry != null) {
                        mReaderWriterLock.EnterWriteLock();
                        try {
                            parent.Childs.Remove(entry.Name);
                        } finally {
                            mReaderWriterLock.ExitWriteLock();
                        }
                    }
                }
            } finally {
                mReaderWriterLock.ExitUpgradeableReadLock();
                MarkAsDirty();
            }
        }
        public override void Touch(string path, DateTime aDate) {
            if (mIsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            MyEntry? entry = null;
            mReaderWriterLock.EnterUpgradeableReadLock();
            try {
                entry = mEntry.GetEntry(path);
                if (!(entry == null)) {
                    mReaderWriterLock.EnterWriteLock();
                    try {
                        entry.SetLastWritetime(aDate);
                    } finally {
                        mReaderWriterLock.ExitWriteLock();
                    }
                }
            } finally {
                mReaderWriterLock.ExitUpgradeableReadLock();
                MarkAsDirty();
            }
        }

        //method LEVEL 4
        public override IDictionary<string, string> GetMetadata(string path) {
            mReaderWriterLock.EnterReadLock();
            try {
                var entry = mEntry.GetEntry(path);
                if (entry == null) throw new Exception("Unable to get metadata: path not found: " + path);
                return entry.Metadata;
            } finally {
                mReaderWriterLock.ExitReadLock();
            }
        }
        public override void SetMetadata(string path, IDictionary<string, string> metadata) {
            if (mIsReadonly) throw new Exception("Unable to modify filesystem: filesystem is readonly");
            PathUtils.Validate(path);
            mReaderWriterLock.EnterWriteLock();
            try {
                var entry = mEntry.GetEntry(path);
                if (entry == null) throw new Exception("Unable to set metadata: path not found: " + path);
                entry.Metadata.Clear();
                var dataKeysAdded = new List<string>();
                foreach (var key in metadata.Keys) {
                    var keyToUse = key.ToLower().Trim();
                    if (!dataKeysAdded.Contains(keyToUse)) {
                        entry.Metadata[keyToUse] = metadata[key];
                        dataKeysAdded.Add(keyToUse);
                    }
                }
            } finally {
                mReaderWriterLock.ExitWriteLock();
                MarkAsDirty();
            }
        }
        public override bool Supports(string path, Features feature) {
            if (feature == Features.Touch) return true;
            if (feature == Features.Metadata) return true;
            return false;
        }


        //utils
        protected MyEntry CreateDirectoryRecursively(string path) {
            string[] pathParts = path.Split('/');
            string pathAux = "/";
            MyEntry entry = mEntry;
            for (int i = 1; i <= pathParts.Length - 1; i++) {
                pathAux = PathUtils.Combine(pathAux, pathParts[i]);
                var targetEntry = entry.GetEntry(pathAux);
                if (targetEntry == null) {
                    DateTime d = DateTime.Now;
                    targetEntry = new MyEntry(pathAux, EntryType.Directory, d, d, 0, "", 0);
                    entry.Childs.Add(targetEntry.Name, targetEntry);
                }
                entry = targetEntry;
            }
            return entry;
        }
        protected void MarkAsDirty() {
            mDirty = true;
            if (mAutoFlush) Flush();
        }
        protected virtual void SaveChanges() {
        }
        public virtual void Flush() {
            if (mDirty) {
                mReaderWriterLock.EnterWriteLock();
                try {
                    SaveChanges();
                } finally {
                    mReaderWriterLock.ExitWriteLock();
                }
                mDirty = false;
            }
        }

    }


}