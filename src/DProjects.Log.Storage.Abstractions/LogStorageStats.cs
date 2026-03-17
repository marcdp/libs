
using System;

namespace DProjects.Log.Storage {

    public class LogStorageStats {

        //constructor
        public LogStorageStats(int files, int directories, long size, DateTime? from, DateTime? to) {
            Files = files;
            Directories = directories;
            Size = size;
            From = from;
            To = to;
        }

        //properties
        public int Files { get; }
        public int Directories { get; }
        public long Size { get; }
        public DateTime? From { get; }
        public DateTime? To { get; }

    }

}

