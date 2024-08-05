
using DProjects.Utils;
using System;
using System.Text.Json;

namespace System.Runtime.CompilerServices {
    internal static class IsExternalInit { }
}

namespace DProjects.Fs {

    public class EntryFactory {


        //methods
        public static Entry? FromJson(string json, string? pathPrefix = null, string? pathBase = null) {
            var entryDTO = EntryDTO.FromJson(json);
            if (entryDTO == null) return default;
            return entryDTO.ToEntry(pathPrefix, pathBase);
        }


    }


}

