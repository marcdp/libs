using DProjects.Utils;
using System.Collections;
using System.Collections.Generic;

namespace DProjects.Fs {


    public class EntryComparer : IComparer<Entry>, IComparer {

        //methods
        public int Compare(object? x, object? y) {
            if (!(x is Entry)) return 0;
            if (!(y is Entry)) return 0;
            return Compare((Entry)x, (Entry)y);
        }
        public int Compare(Entry? x, Entry? y) {
            var pathX = x?.Path ?? "";
            var pathY = y?.Path ?? "";
            return PathUtils.ComparePath(pathX, pathY);
        }

    }

}