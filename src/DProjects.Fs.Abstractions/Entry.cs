using DProjects.Utils;
using System;

namespace DProjects.Fs {


    public class Entry {


        //variables


        //constructor
        public Entry(string path, EntryType entryType, DateTime created, DateTime modified, long length, string etag, int flags) {
            if (path.IndexOf('\\') != -1) path = path.Replace('\\', '/');
            Path = path;
            EntryType = entryType;
            Created = created;
            Modified = modified;
            Length = length;
            Etag = etag;
            Flags = flags;
        }


        //properties
        public string Path;
        public string Name => PathUtils.GetPathName(Path);
        public EntryType EntryType;
        public DateTime Created;
        public DateTime Modified;
        public long Length;
        public string Etag;
        public int Flags;


        //methods
        public bool IsReadonly() {
            return HasFlag(Fs.Flags.Readonly);
        }
        public bool HasFlag(Flags flag) {
            return (Flags & (int)flag) == (int)flag;
        }
        public override string ToString() {
            return Path;
        }
        public bool IsDirectory() {
            return EntryType == EntryType.Directory;
        }
        public bool IsFile() {
            return EntryType == EntryType.File;
        }
        public Entry WithPath(string path) {
            if (Path.Equals(path)) return this;
            return new Entry(path, EntryType, Created, Modified, Length, Etag, Flags);
        }
        public Entry WithLength(long length) {
            if (Path.Length == length) return this;
            return new Entry(Path, EntryType, Created, Modified, length, Etag, Flags);
        }

    }

}