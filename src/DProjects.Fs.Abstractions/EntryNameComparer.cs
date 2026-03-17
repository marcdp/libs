using DProjects.Utils;
using System;
using System.Collections;
using System.Collections.Generic;

namespace DProjects.Fs {


    public class EntryNameComparer : IComparer<String>, IComparer {
        public int Compare(object? x, object? y) {
            if (!(x is string)) return 0;
            if (!(y is string)) return 0;
            return Compare((string)x, (string)y);
        }
        public int Compare(string? x, string? y) {
            var pathX = x ?? "";
            var pathY = y ?? "";
            return PathUtils.CompareName(pathX, pathY);
        }
    }

}