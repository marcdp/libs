using DProjects.Streams;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace DProjects.Fs {


    public class FilesystemRes : FilesystemSync {


        //variables
        private System.Reflection.Assembly mAssembly;
        private DateTime mAssemblyDate;
        private string mPrefix;
        

        //constructor
        public FilesystemRes(System.Reflection.Assembly assembly, string path) : base(true) {
            mAssembly = assembly;
            mAssemblyDate = DateTime.Now;
            if (mAssembly.Location != null && System.IO.File.Exists(mAssembly.Location)) {
                mAssemblyDate = System.IO.File.GetCreationTimeUtc(mAssembly.Location);
            }
            mPrefix = path.Substring(1);
        }


        //properties
        public override string Url {
            get {
                return "res://" + mAssembly.GetName().Name + "/" + mPrefix;
            }
        } 


        //methods LEVEL 0
        public override Entry? GetEntry(string path) {
            if (path.Equals("/")) {
               return new Entry("/", EntryType.Directory, mAssemblyDate, mAssemblyDate, 0, "", 0);
            } else {
                //try get resource
                var resourceInfo = mAssembly.GetManifestResourceInfo(mPrefix + path);
                if (resourceInfo != null) {
                    var entryLength = GetEntryLength(path); 
                    var entryEtag = HashUtils.ToHashSHA1Hex(entryLength + "-" + mAssemblyDate.ToUniversalTime().ToString("yyyy-MM-dd-HH-mm-ss")).ToLower();
                    return new Entry(path, EntryType.File, mAssemblyDate, mAssemblyDate, entryLength, entryEtag, 0);
                }
                //try directory
                var resourceBase = mPrefix + path;
                var pathPrefix = (path + (path.Equals("/") ? "" : "/"));
                foreach (var resourceName in mAssembly.GetManifestResourceNames()) {
                    if (resourceName.StartsWith(resourceBase)) {
                        var entryPath = resourceName.Substring(mPrefix.Length);
                        if (entryPath.StartsWith(pathPrefix)) {
                            var entryLength = 0;
                            var entryEtag = HashUtils.ToHashSHA1Hex(entryLength + "-" + mAssemblyDate.ToUniversalTime().ToString("yyyy-MM-dd-HH-mm-ss")).ToLower();
                            return new Entry(path, EntryType.Directory, mAssemblyDate, mAssemblyDate, entryLength, entryEtag, 0);
                        }
                    }
                }
                return null;
            }
        }
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            var resourceBase = mPrefix + path;
            var pathPrefix = (path + (path.Equals("/") ? "" : "/"));
            //get all entries
            var entries = new List<Entry>();
            var entryParentPaths = new List<string>();
            foreach (var resourceName in mAssembly.GetManifestResourceNames()) {
                if (resourceName.StartsWith(resourceBase)) {
                    var entryPath = resourceName.Substring(mPrefix.Length);
                    var entryParentPath = PathUtils.GetPathParent(entryPath);
                    if (!entryParentPaths.Contains(entryParentPath)) {
                        var target = entryParentPath;
                        while(!target.Equals("/") && !entryParentPaths.Contains(target)) {
                            entryParentPaths.Add(target);
                            var targetLength = 0;
                            var targetEtag = HashUtils.ToHashSHA1Hex(targetLength + "-" + mAssemblyDate.ToUniversalTime().ToString("yyyy-MM-dd-HH-mm-ss")).ToLower();
                            entries.Add(new Entry(target, EntryType.Directory, mAssemblyDate, mAssemblyDate, targetLength, targetEtag, 0));
                            target = PathUtils.GetPathParent(target);
                        }
                    }
                    var entryLength = GetEntryLength(entryPath);
                    var entryEtag = HashUtils.ToHashSHA1Hex(entryLength + "-" + mAssemblyDate.ToUniversalTime().ToString("yyyy-MM-dd-HH-mm-ss")).ToLower();
                    entries.Add(new Entry(entryPath, EntryType.File, mAssemblyDate, mAssemblyDate, entryLength, entryEtag, 0));
                }
            }
            entries.Sort(new EntryComparer());
            //filter
            if (mode == GetModes.All || mode == GetModes.Files || mode == GetModes.Directories) {
                foreach (var entry in entries) {
                    var isValid = false;
                    if (PathUtils.GetPathParent(entry.Path).Equals(path)) {
                        if (entry.IsFile() && (mode == GetModes.All || mode == GetModes.Files || mode == GetModes.Descendants)) isValid = true;
                        if (entry.IsDirectory() && (mode == GetModes.All || mode == GetModes.Directories || mode == GetModes.Descendants)) isValid = true;
                    }
                    if (isValid) {
                        if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                            yield return entry;
                        }
                    }
                }
            } else if (mode == GetModes.Descendants) {
                foreach (var entry in entries) {
                    if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                        yield return entry;
                    }
                }
            }
        }
        public override bool Exists(string path) {
            return GetEntry(path) != null;
        }        
        public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            var resourceName = mPrefix + path;
            var result = mAssembly.GetManifestResourceStream(resourceName);
            if (result == null) throw new Exception("Unable to load read stream: file not found: " + path);
            if (settings != null && (settings.Offset != 0 || settings.Length != -1)) {
                result = new PartialInputStream(result, settings.Offset, settings.Length);
            }
            return result;
        }


        //private
        private long GetEntryLength(string path) {
            var resourceName = mPrefix + path;
            using var result = mAssembly.GetManifestResourceStream(resourceName);
            if (result == null) throw new Exception("Unable to get entry length: file not found: " + path);
            return result.Length;

        }

    }


}

