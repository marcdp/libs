using DProjects.Utils;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemUnzip {


        //methods
        public static void Unzip(this IFilesystemSync fs, string path, string destination) {
            using (var zipFileStream = fs.LoadReadStream(path, new()))
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read)) {
                foreach (var zipEntry in zipArchive.Entries) {
                    var entryPath = PathUtils.Combine(destination, zipEntry.FullName);
                    if (zipEntry.FullName.EndsWith("/")) {
                        entryPath = entryPath.Substring(0, entryPath.Length - 1);
                        fs.CreateDirectory(entryPath);
                    } else {
                        var entryPathParent = PathUtils.GetPathParent(entryPath);
                        if (!fs.ExistsDirectory(entryPathParent)) {
                            fs.CreateDirectory(entryPathParent);
                        }
                        using (var zipEntryStream = zipEntry.Open()) {
                            fs.SaveFile(entryPath, zipEntryStream, new());
                        }
                    }
                }
            }
        }
        public static async Task UnzipAsync(this IFilesystemAsync fs, string path, string destination, CancellationToken cancellationToken) {
            using (var zipFileStream = await fs.LoadReadStreamAsync(path, new(), cancellationToken))
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Read)) {
                foreach (var zipEntry in zipArchive.Entries) {
                    //check cancellationToken
                    cancellationToken.ThrowIfCancellationRequested();
                    //action
                    var entryPath = PathUtils.Combine(destination, zipEntry.FullName);
                    if (zipEntry.FullName.EndsWith("/")) {
                        entryPath = entryPath.Substring(0, entryPath.Length - 1);
                        await fs.CreateDirectoryAsync(entryPath, cancellationToken);
                    } else {
                        var entryPathParent = PathUtils.GetPathParent(entryPath);
                        if (!await fs.ExistsDirectoryAsync(entryPathParent, cancellationToken)) {
                            await fs.CreateDirectoryAsync(entryPathParent, cancellationToken);
                        }
                        using (var zipEntryStream = zipEntry.Open()) {
                            await fs.SaveFileAsync(entryPath, zipEntryStream, new(), cancellationToken);
                        }
                    }
                }
            }
        }


    }


}