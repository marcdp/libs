using System.Collections.Generic;
using System.IO.Compression;

namespace DProjects.Utils {


    public static class ZipUtils {


        //zip
        public static void ZipFile(string fileName, string zipFileName) {
            ZipFiles(new string[] { fileName }, zipFileName);
        }
        public static void ZipFiles(string[] fileNames, string zipFileName) {
            using (var zipFileStream = System.IO.File.OpenWrite(zipFileName))
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create)) {
                foreach (var fileName in fileNames) {
                    var name = System.IO.Path.GetFileName(fileName);
                    var zipEntry = zipArchive.CreateEntry(name);
                    zipEntry.LastWriteTime = System.IO.File.GetLastWriteTime(fileName);
                    using (var entryStream = zipEntry.Open()) {
                        using (var fileStream = System.IO.File.OpenRead(fileName)) {
                            StreamUtils.Copy(fileStream, entryStream);
                        }
                    }
                }
            }
        }
        public static void ZipFolder(string directory, string zipFileName) {
            if (System.IO.File.Exists(zipFileName)) System.IO.File.Delete(zipFileName);
            System.IO.Compression.ZipFile.CreateFromDirectory(directory, zipFileName);
        }


        //unzip
        public static bool HasEntry(string zipFileName, string path) {
            using (var zipFileStream = System.IO.File.OpenRead(zipFileName)) 
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read)) {
                if (zipArchive.GetEntry(path) != null) {
                    return true;
                }
            }
            return false;
        }
        public static void UnZip(string directory, string zipFileName) {
            using (var zipFileStream = System.IO.File.OpenRead(zipFileName))
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read)) {
                foreach (var zipEntry in zipArchive.Entries) {
                    var entryPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, zipEntry.FullName.Replace("..", "").Replace(":", "")));
                    if (entryPath.StartsWith(directory)) {
                        if (zipEntry.FullName.EndsWith("/")) {
                            entryPath = entryPath.Substring(0, entryPath.Length - 1);
                            System.IO.Directory.CreateDirectory(entryPath);
                        } else {
                            var entryPathParent = System.IO.Path.GetDirectoryName(entryPath);
                            if (!System.IO.Directory.Exists(entryPathParent)) {
                                System.IO.Directory.CreateDirectory(entryPathParent);
                            }
                            using (var zipEntryStream = zipEntry.Open()) {
                                using (var fileStream = System.IO.File.OpenWrite(entryPath)) {
                                    StreamUtils.Copy(zipEntryStream, fileStream);
                                }
                            }
                        }
                    }
                }
            }
        }
        public static string[] GetZipList(string zipFileName) {
            var result =new List<string>();
            using (var zipFileStream = System.IO.File.OpenRead(zipFileName))
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read)) {
                foreach (var zipEntry in zipArchive.Entries) {
                    result.Add(zipEntry.FullName);
                }
            }
            return result.ToArray();
        }


    }

}


