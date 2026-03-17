using DProjects.Utils;
using System.Collections.Generic;

namespace DProjects.Fs {


    public class PathComparer : IComparer<string> {

        //methods
        public int Compare(string x, string y) {
            return PathUtils.ComparePath(x, y);
        }

    }
}