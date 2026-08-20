using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DProjects.Utils {


    public static class FileUtils {

        //constants
        public const int FILESYSTEM_MAX_PATH = 260;
        private const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetFileAttributesW(string lpFileName);


        //utils
        private static void PrefixWindowsPath(ref string path) {
            if (EnvironmentUtils.IsWindows() && EnvironmentUtils.IsNetFramework() && path.Length >= FILESYSTEM_MAX_PATH && path.IndexOf(@"\\") == -1) {
                path = @"\\?\" + path;
            }
        }

        public static string DetectEndOfLine(string filePath) {
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
                int previous = -1;
                int current;
                while ((current = stream.ReadByte()) != -1) {
                    if (current == '\n') {
                        return previous == '\r' ? "\r\n" : "\n";
                    }
                    if (previous == '\r') {
                        return "\r";
                    }
                    previous = current;
                }
            }
            return Environment.NewLine; // fallback if file has no line endings
        }

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

        // read methods
        public static Task<string> ReadTextFileAsync(string uri, System.Reflection.Assembly? resourceAssembly = null, System.Text.Encoding? encoding = null) {
            var result = ReadTextFile(uri, resourceAssembly, encoding);
            return Task.FromResult(result);
        }
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
                PrefixWindowsPath(ref uri);
                return System.IO.File.ReadAllText(uri, encoding);
            }
        }
        public static string[] ReadTextFileLines(string uri, System.Reflection.Assembly? resourceAssembly = null, System.Text.Encoding? encoding = null) {
            var text = ReadTextFile(uri, resourceAssembly, encoding);
            return text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
        }
        public static byte[] ReadFile(string uri) {
            if (uri.StartsWith("http://") || uri.StartsWith("https://")) {
                throw new NotImplementedException();
            } else {
                PrefixWindowsPath(ref uri);
                return System.IO.File.ReadAllBytes(uri);
            }
        }
        public static byte[] ReadFile(string uri, long offset, int length) {
            PrefixWindowsPath(ref uri);
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
            PrefixWindowsPath(ref filename);
            return WriteFile(filename, new MemoryStream(buffer), bytesToCopy, append);
        }
        public static long WriteFile(string filename, Stream inputStream, long bytesToCopy = 0, bool append = false, int bufferSize = 64 * 1024) {
            PrefixWindowsPath(ref filename);
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
            PrefixWindowsPath(ref filename);
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
        
        public static void WriteTextFile(string filename, string fileContent, bool append = false, System.Text.Encoding? encoding = null, int bufferSize = 64 * 1024) {
            PrefixWindowsPath(ref filename);
            encoding ??= EncodingUtils.GetDefault();
            using (var fileStream = new FileStream(filename, (append ? FileMode.Append : FileMode.OpenOrCreate), FileAccess.ReadWrite)) {
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

          
        // files/directories
        public static bool ExistsFile(string path) {
            //if (EnvironmentUtils.IsWindows() && EnvironmentUtils.IsNetFramework()) {
            //    if (string.IsNullOrWhiteSpace(path)) return false;
            //    if (!path.StartsWith(@"\\?\")) path = @"\\?\" + path;
            //    uint attrs = GetFileAttributesW(path);
            //    return attrs != INVALID_FILE_ATTRIBUTES;
            //}
            PrefixWindowsPath(ref path);
            return System.IO.File.Exists(path);
        }
        public static bool ExistsDirectory(string path) {
            PrefixWindowsPath(ref path);
            return System.IO.Directory.Exists(path);
        }
        public static bool Exists(string path) {
            PrefixWindowsPath(ref path);
            return System.IO.File.Exists(path) || System.IO.Directory.Exists(path);
        }
        public static void Delete(string path) {
            PrefixWindowsPath(ref path);
            if (File.Exists(path)) {
                File.Delete(path);
            } else if (Directory.Exists(path)) {
                DeleteFolder(path);
            }
        }
        public static void DeleteFile(string filename) {
            PrefixWindowsPath(ref filename);
            if (File.Exists(filename)) {
                File.Delete(filename);
            }
        }
        public static Task DeleteFileAsync(string filename) {
            PrefixWindowsPath(ref filename);
            DeleteFile(filename);
            return Task.CompletedTask;
        }
        public static void CreateFolder(string dirname) {
            PrefixWindowsPath(ref dirname);
            if (!Directory.Exists(dirname)) {
                Directory.CreateDirectory(dirname);
            }
        }
        public static Task CreateFolderAsync(string dirname) {
            PrefixWindowsPath(ref dirname);
            CreateFolder(dirname);
            return Task.CompletedTask;
        }
        public static void DeleteFolder(string dirName, bool onlyChilds = false, int indent = 0) {
            PrefixWindowsPath(ref dirName);
            if (Directory.Exists(dirName)) {
                DirectoryInfo directoryInfo = new DirectoryInfo(dirName);
                if (directoryInfo.Attributes.HasFlag(FileAttributes.ReadOnly)) {
                    directoryInfo.Attributes = FileAttributes.Normal;
                }
                foreach (string aux in Directory.GetFiles(dirName)) {
                    var filename = aux;
                    PrefixWindowsPath(ref filename);
                    FileInfo fileInfo = new FileInfo(filename);
                    if (fileInfo.IsReadOnly) {
                        File.SetAttributes(filename, FileAttributes.Normal);
                    }
                    DeleteFile(filename);
                }
                foreach (string subDirectoryName in Directory.GetDirectories(dirName)) {
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
            PrefixWindowsPath(ref dirName);
            DeleteFolder(dirName, onlyChilds, indent);
            return Task.CompletedTask;
        }
        public static void DeleteFolder(string dirName, int tries) {
            PrefixWindowsPath(ref dirName);
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
            PrefixWindowsPath(ref dirName);
            DeleteFolder(dirName, tries);
            return Task.CompletedTask;
        }
        public static void MoveFile(string src, string dst) {
            PrefixWindowsPath(ref src);
            PrefixWindowsPath(ref dst);
            System.IO.File.Move(src, dst);
        }

        public static void MoveFolder(string src, string dst) {
            PrefixWindowsPath(ref src);
            PrefixWindowsPath(ref dst);
            System.IO.Directory.Move(src, dst);
        }
        public static void CopyFile(string src, string dst, bool overwrite = false) {
            PrefixWindowsPath(ref src);
            PrefixWindowsPath(ref dst);
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
            if (EnvironmentUtils.IsWindows() && (fileName.Length > FILESYSTEM_MAX_PATH || directoryName.Length > FILESYSTEM_MAX_PATH) && fileName.IndexOf(@"\\")==-1 && directoryName.IndexOf(@"\\") == -1) {
                fileName = @"\\?\" + fileName;
                directoryName = @"\\?\" + directoryName;
                return fileName.StartsWith(directoryName, StringComparison.OrdinalIgnoreCase);
            }
            return System.IO.Path.GetFullPath(fileName).ToLower().StartsWith(directoryName.ToLower());
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


        // lock methods
        public static bool IsFileLocked(string path) {
            if (!File.Exists(path)) return false;
            try {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                return false;
            } catch (IOException) {
                return true;
            }
        }
        public sealed class IndexLock : IDisposable {
            private readonly string path;
            private readonly FileStream stream;
            private bool disposed;
            public IndexLock(string path, FileStream stream) {
                this.path = path;
                this.stream = stream;
            }
            public void Dispose() {
                if (disposed) return;
                disposed = true;
                stream.Dispose();
                try {
                    File.Delete(path);
                } catch {
                    // Best-effort cleanup only.
                    // The file itself is not the lock; the open exclusive handle is.
                }
            }
        }
        public static async Task<IndexLock> AcquireIndexLockAsync(string dataPath, TimeSpan timeout, System.Threading.CancellationToken cancellationToken) {
            Directory.CreateDirectory(dataPath);
            var lockPath = Path.Combine(dataPath, ".lock");
            var startedAt = DateTimeOffset.UtcNow;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                try {
                    var stream = new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    stream.SetLength(0);
                    using (var writer = new StreamWriter(stream, System.Text.Encoding.UTF8, 1024, true)) {
                        var json =
                            "{" +
                            "\"processId\":" + System.Diagnostics.Process.GetCurrentProcess().Id + "," +
                            "\"startedAt\":\"" + DateTimeOffset.UtcNow.ToString("O") + "\"," +
                            "\"command\":\"xtrader index\"" +
                            "}";
                        writer.Write(json);
                        writer.Flush();
                    }
                    stream.Position = 0;
                    return new IndexLock(lockPath, stream);
                } catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException) {
                    if (DateTimeOffset.UtcNow - startedAt >= timeout) {
                        throw new TimeoutException(
                            $"Unable to acquire index lock '{lockPath}' within {timeout}. Another xtrader index process may be running or hung.",
                            exception
                        );
                    }
                    await Task.Delay(250, cancellationToken);
                }
            }
        }
    }

}


