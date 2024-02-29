using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DProjects.Utils {


    public static class FileUtils {


        //temp
        public static string GetTempPath() {
            return Path.GetTempPath();
        }
        public static string GetTempFileName() {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".tmp");
        }
        public static string GetTempFileName(string extension) {
            return Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + "." + extension);
        }
        public static string GetTempFileName(string tempPath, string extension) {
            return Path.Combine(tempPath, Guid.NewGuid().ToString() + "." + extension);
        }


        //read methods
        public static string ReadTextFile(string uri, System.Reflection.Assembly? resourceAssembly = null, System.Text.Encoding? encoding = null) {
            if (encoding == null) encoding = EncodingUtils.GetDefault();
            if (uri.StartsWith("http://") || uri.StartsWith("https://")) {
                throw new NotImplementedException();
            } else if (uri.StartsWith("res://")) {
                if (resourceAssembly != null) {
                    using (var stream = resourceAssembly.GetManifestResourceStream(uri.Substring(6))) {
                        if (stream != null) return StreamUtils.ReadText(stream, encoding);
                    }
                }
                throw new FileNotFoundException();
            } else if (uri.StartsWith("file://")) {
                var aUri = new System.Uri(uri);
                return System.IO.File.ReadAllText(aUri.AbsolutePath, encoding);
            } else {
                return System.IO.File.ReadAllText(uri, encoding);
            }
        }
        public static byte[] ReadFile(string uri) {
            if (uri.StartsWith("http://") || uri.StartsWith("https://")) {
                throw new NotImplementedException();
            } else {
                return System.IO.File.ReadAllBytes(uri);
            }
        }
        public static byte[] ReadFile(string uri, long offset, int length) {
            long fileLength = new FileInfo(uri).Length;
            if (length == -1) length = (int)fileLength;
            if (offset + length > fileLength) length = System.Convert.ToInt32(fileLength - offset);
            using (var fileStream = new FileStream(uri, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                var buffer = new byte[length];
                fileStream.Seek(offset, SeekOrigin.Begin);
                fileStream.Read(buffer, 0, length);
                return buffer;
            }
        }


        //write
        public static long WriteFile(string filename, byte[] buffer, long bytesToCopy = 0, bool append = false) {
            return WriteFile(filename, new MemoryStream(buffer), bytesToCopy, append);
        }
        public static long WriteFile(string filename, Stream inputStream, long bytesToCopy = 0, bool append = false, int bufferSize = 64 * 1024) {
            if (append) {
                using (var fileStream = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, bufferSize)) {
                    return StreamUtils.Copy(inputStream: inputStream, outputStream: fileStream, bytesToCopy: bytesToCopy, bufferSize: bufferSize);
                }
            } else {
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize)) {
                    return StreamUtils.Copy(inputStream: inputStream, outputStream: fileStream, bytesToCopy: bytesToCopy, bufferSize: bufferSize);
                }
            }
        }
        public static async Task<long> WriteFileAsync(string filename, Stream inputStream, long bytesToCopy = 0, bool append = false, int bufferSize = 64 * 1024) {
            if (append) {
                using (var fileStream = new FileStream(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, bufferSize)) {
                    return await StreamUtils.CopyAsync(inputStream: inputStream, outputStream: fileStream, bytesToCopy: bytesToCopy, bufferSize: bufferSize);
                }
            } else {
                using (var fileStream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, bufferSize)) {
                    return await StreamUtils.CopyAsync(inputStream: inputStream, outputStream: fileStream, bytesToCopy: bytesToCopy, bufferSize: bufferSize);
                }
            }
        }
        
        public static void WriteTextFile(string filePath, string fileContent, bool append = false, System.Text.Encoding? encoding = null, int bufferSize = 64 * 1024) {
            encoding ??= EncodingUtils.GetDefault();
            using (var fileStream = new FileStream(filePath, (append ? FileMode.Append : FileMode.OpenOrCreate), FileAccess.ReadWrite)) {
                if (!append) fileStream.SetLength(0);
                using (var streamWriter = new StreamWriter(fileStream, encoding, bufferSize, true)) {
                    streamWriter.Write(fileContent);
                }
            }
        }
        public static string WriteTempFile(string fileContent, System.Text.Encoding? encoding = null, int bufferSize = 64 * 1024) {
            var tempFile = GetTempFileName();
            encoding ??= System.Text.Encoding.UTF8;
            WriteTextFile(tempFile, fileContent, false, encoding, bufferSize);
            return tempFile;
        }
        public static string WriteTempFile(Stream fileContent, int bufferSize = 64 * 1024) {
            string tempFile = GetTempFileName();
            WriteFile(tempFile, fileContent, bufferSize);
            return tempFile;
        }
        public static string GetFilenameWithoutDots(string filename) {
            return new FileInfo(filename).FullName;
        }


        //directories
        public static void Delete(string path) {
            if (File.Exists(path)) {
                File.Delete(path);
            } else if (Directory.Exists(path)) {
                DeleteFolder(path);
            }
        }
        public static void DeleteFile(string filename) {
            if (File.Exists(filename)) {
                File.Delete(filename);
            }
        }
        public static Task DeleteFileAsync(string filename) {
            DeleteFile(filename);
            return Task.CompletedTask;
        }
        public static void CreateFolder(string directoryName) {
            if (!Directory.Exists(directoryName)) {
                Directory.CreateDirectory(directoryName);
            }
        }
        public static Task CreateFolderAsync(string dirName) {
            CreateFolder(dirName);
            return Task.CompletedTask;
        }
        public static void DeleteFolder(string irName, bool onlyChilds = false, int indent = 0) {
            if (Directory.Exists(irName)) {
                DirectoryInfo directoryInfo = new DirectoryInfo(irName);
                if (directoryInfo.Attributes.HasFlag(FileAttributes.ReadOnly)) {
                    directoryInfo.Attributes = FileAttributes.Normal;
                }
                foreach (string filename in Directory.GetFiles(irName)) {
                    FileInfo fileInfo = new FileInfo(filename);
                    if (fileInfo.IsReadOnly) {
                        File.SetAttributes(filename, FileAttributes.Normal);
                    }
                    DeleteFile(filename);
                }
                foreach (string subDirectoryName in Directory.GetDirectories(irName)) {
                    DeleteFolder(subDirectoryName, false, indent + 1);
                }
                if (!onlyChilds) {
                    directoryInfo.Delete(true);
                    if (indent == 0) {
                        //ensures directory is really deleted, because in windows directory is marked 
                        //as deleted (only really deleted when all handles are closed, if you has explorer 
                        //opened in that directory, it can take some time)
                        directoryInfo.Refresh();
                        var iterations = 0;
                        while (directoryInfo.Exists) {
                            System.Threading.Thread.Sleep(25);
                            directoryInfo.Refresh();
                            iterations++;
                            if (iterations > 100) break;
                        }
                    }
                }
            }
        }
        public static Task DeleteFolderAsync(string dirName, bool onlyChilds = false, int indent = 0) {
            DeleteFolder(dirName, onlyChilds, indent);
            return Task.CompletedTask;
        }
        public static void DeleteFolder(string dirName, int tries) {
            if (Directory.Exists(dirName)) {
                DirectoryInfo directoryInfo = new DirectoryInfo(dirName);
                if (directoryInfo.Attributes.HasFlag(FileAttributes.ReadOnly)) {
                    directoryInfo.Attributes = FileAttributes.Normal;
                }
                foreach (string filename in Directory.GetFiles(dirName)) {
                    FileInfo fileInfo = new FileInfo(filename);
                    if (fileInfo.IsReadOnly) {
                        File.SetAttributes(filename, FileAttributes.Normal);
                    }
                    DeleteFile(filename);
                }
                foreach (string subDirectoryName in Directory.GetDirectories(dirName)) {
                    DeleteFolder(subDirectoryName, tries);
                }
                for (int i = 0; i <= tries; i++) {
                    try {
                        Directory.Delete(dirName, true);
                        break;
                    } catch (Exception) {
                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
        }
        public static Task DeleteFolderAsync(string dirName, int tries) {
            DeleteFolder(dirName, tries);
            return Task.CompletedTask;
        }
        public static void CopyFile(string src, string dst, bool overwrite) {
            if (overwrite) {
                File.Copy(src, dst, true);
            } else {
                if (!File.Exists(dst)) {
                    File.Copy(src, dst, true);
                }
            }
        }
        public static void CopyDirectory(string src, string dst, bool overwrite, string filter, bool recursive, string excludePatterns, bool continueIfError = false, bool copyLastWriteDateTime = false) {
            DirectoryInfo di = new DirectoryInfo(src);
            foreach (FileSystemInfo fsi in di.GetFileSystemInfos()) {
                bool isValid = true;
                foreach (string excludePattern in excludePatterns.Split(',')) {
                    if (excludePattern.Length > 0 && StringUtils.Like(fsi.Name, excludePattern)) {
                        isValid = false;
                    }
                }
                if (isValid) {
                    string destName = Path.Combine(dst, fsi.Name);
                    if (fsi is FileInfo) {
                        bool isValidFullName = true;
                        try {
                            isValidFullName = StringUtils.Like(fsi.FullName, filter);
                        } catch (Exception) {
                        }
                        if (isValidFullName) {
                            try {
                                if (!overwrite && File.Exists(destName)) {
                                } else {
                                    File.Copy(fsi.FullName, destName, overwrite);
                                    if (copyLastWriteDateTime) {
                                        File.SetLastWriteTimeUtc(fsi.FullName, fsi.LastWriteTimeUtc);
                                    }
                                }
                            } catch (Exception ex) {
                                if (!continueIfError) {
                                    throw new Exception("Error Copying file \'" + fsi.FullName + "\' to \'" + destName + "\', " + ex.Message, ex);
                                }
                            }
                        }
                    } else {
                        if (recursive) {
                            try {
                                Directory.CreateDirectory(destName);
                            } catch (Exception ex) {
                                if (!continueIfError) {
                                    throw new Exception("Unable to create folder \'" + destName + "\': " + ex.Message);
                                }
                            }
                            CopyDirectory(fsi.FullName, destName, overwrite, filter, recursive, excludePatterns, continueIfError, copyLastWriteDateTime);
                        }
                    }
                }
            }
        }
        public static async Task CopyDirectoryAsync(string src, string dst, bool overwrite, string filter, bool recursive, string excludePatterns, bool continueIfError = false, bool copyLastWriteDateTime = false) {
            DirectoryInfo di = new DirectoryInfo(src);
            foreach (FileSystemInfo fsi in di.GetFileSystemInfos()) {
                bool isValid = true;
                foreach (string excludePattern in excludePatterns.Split(',')) {
                    if (excludePattern.Length > 0 && StringUtils.Like(fsi.Name, excludePattern)) {
                        isValid = false;
                    }
                }
                if (isValid) {
                    string destName = Path.Combine(dst, fsi.Name);
                    if (fsi is FileInfo) {
                        bool isValidFullName = true;
                        try {
                            isValidFullName = StringUtils.Like(fsi.FullName, filter);
                        } catch (Exception) {
                        }
                        if (isValidFullName) {
                            try {
                                if (!overwrite && File.Exists(destName)) {
                                } else {
                                    File.Copy(fsi.FullName, destName, overwrite);
                                    if (copyLastWriteDateTime) {
                                        File.SetLastWriteTimeUtc(fsi.FullName, fsi.LastWriteTimeUtc);
                                    }
                                }
                            } catch (Exception ex) {
                                if (!continueIfError) {
                                    throw new Exception("Error Copying file \'" + fsi.FullName + "\' to \'" + destName + "\', " + ex.Message, ex);
                                }
                            }
                        }
                    } else {
                        if (recursive) {
                            try {
                                Directory.CreateDirectory(destName);
                            } catch (Exception ex) {
                                if (!continueIfError) {
                                    throw new Exception("Unable to create folder \'" + destName + "\': " + ex.Message);
                                }
                            }
                            await CopyDirectoryAsync(fsi.FullName, destName, overwrite, filter, recursive, excludePatterns, continueIfError, copyLastWriteDateTime);
                        }
                    }
                }
            }
        }
        public static bool GetFileIsDescendantFromDirectory(string fileName, string directoryName) {
            if (fileName == null) {
                return false;
            }
            if (fileName.IndexOf("?") != -1) {
                fileName = fileName.Substring(0, fileName.IndexOf("?"));
            }
            if (directoryName == null) {
                return false;
            }
            return Path.GetFullPath(fileName).ToLower().StartsWith(directoryName.ToLower());
        }
        public static string[] GetFileAndFolderList(string folder, string filter) {
            var result = new List<string>();
            foreach (string filename in Directory.GetFiles(folder, filter)) {
                result.Add(filename);
            }
            foreach (string folderName in Directory.GetDirectories(folder)) {
                result.Add(folderName);
                result.AddRange(GetFileAndFolderList(folderName, filter));
            }
            return result.ToArray();
        }
        public static string[] GetFileList(string folder, string filter, bool recursive = true) {
            var result = new List<string>();
            foreach (string filename in Directory.GetFiles(folder, filter)) {
                result.Add(filename);
            }
            if (recursive) {
                foreach (string folderName in Directory.GetDirectories(folder)) {
                    result.AddRange(GetFileList(folderName, filter));
                }
            }
            return result.ToArray();
        }
        public static string[] GetFolderList(string folder) {
            var result = new List<string>();
            result.Add(folder);
            foreach (string folderName in Directory.GetDirectories(folder)) {
                result.AddRange(GetFolderList(folderName));
            }
            return result.ToArray();
        }


    }


}


