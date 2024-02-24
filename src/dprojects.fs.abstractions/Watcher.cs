using DProjects.Utils;
using System;


namespace DProjects.Fs {


    public class Watcher : IDisposable {

        //enum
        public enum ChangeType {
            Changed,
            Created,
            Deleted,
            Renamed
        }

        //delegates
        public delegate void DisposedEventHandler(Watcher sender);
        public event DisposedEventHandler? Disposed;
        public delegate void ChangedEventHandler(Watcher sender, ChangeType type, string path);
        public event ChangedEventHandler? Changed;


        //variables
        private string mPath;
        private bool mRecursive;
        private string[] mExcludes;
        private string mFilter;


        //constructor
        public Watcher(string path, string filter, string[] excludes, bool recursive) {
            mPath = path;
            mFilter = filter;
            mExcludes = excludes;
            mRecursive = recursive;
        }
        public virtual void Dispose() {
            if (Disposed != null) Disposed(this);
        }

        //properties
        public string Path => mPath;
        public bool Recursive => mRecursive;
        public string[] Excludes => mExcludes;
        public string Filter => mFilter;

        //methods
        public void WithPath(string path) {
            mPath = path;
        }
        protected void Raise(ChangeType type, string path) {
            bool isValid = true;
            foreach (string exclude in mExcludes) {
                if (StringUtils.Like(path, exclude)) {
                    isValid = false;
                    break;
                }
            }
            if (isValid) {
                if (Changed != null) {
                    Changed(this, type, PathUtils.Combine(mPath, path));
                }
            }
        }
    }


}