using DProjects.Fs;
using DProjects.Utils;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemZip {


        //methods
        public static void Zip(this IFilesystemSync fs, string[] paths, string destination) {
            using (var zipFileStream = fs.LoadWriteStream(destination))
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create)) {
                foreach (var path in paths) {
                    var entry = fs.GetEntry(path);
                    if (entry == null) {
                    } else if (entry.IsFile()) {
                        var aux = entry.Path.Substring(path.Length).Substring(1);
                        var zipEntry = zipArchive.CreateEntry(aux);
                        using (var zipStream = zipEntry.Open())
                        using (var subEntryStream = fs.LoadReadStream(entry.Path)) {
                            StreamUtils.Copy(subEntryStream, zipStream);
                        }
                    } else if (entry.IsDirectory()) {
                        foreach (var subEntry in fs.GetEntries(path, GetModes.Descendants)) {
                            var aux = subEntry.Path.Substring(path.Length).Substring(1);
                            if (subEntry.IsFile()) {
                                var zipEntry = zipArchive.CreateEntry(aux);
                                using (var zipStream = zipEntry.Open())
                                using (var subEntryStream = fs.LoadReadStream(subEntry.Path)) {
                                    StreamUtils.Copy(subEntryStream, zipStream);
                                }
                            } else if (subEntry.IsDirectory()) {
                                var zipEntry = zipArchive.CreateEntry(aux + "/");
                            }
                        }
                    }
                }
            }
        }
        public static async Task ZipAsync(this IFilesystemAsync fs, string[] paths, string destination, CancellationToken cancellationToken) {
            using (var zipFileStream = await fs.LoadWriteStreamAsync(destination))
            using (var zipArchive = new ZipArchive(zipFileStream, ZipArchiveMode.Create)) {
                foreach (var path in paths) {
                    var entry = await fs.GetEntryAsync(path);
                    if (entry == null) {
                    } else if (entry.IsFile()) {
                        var aux = entry.Path.Substring(path.Length).Substring(1);
                        var zipEntry = zipArchive.CreateEntry(aux);
                        using (var zipStream = zipEntry.Open())
                        using (var subEntryStream = await fs.LoadReadStreamAsync(entry.Path)) {
                            await StreamUtils.CopyAsync(subEntryStream, zipStream, cancellationToken: cancellationToken);
                        }
                    } else if (entry.IsDirectory()) {
                        await foreach (var subEntry in fs.GetEntriesAsync(path, GetModes.Descendants)) {
                            var aux = subEntry.Path.Substring(path.Length).Substring(1);
                            if (subEntry.IsFile()) {
                                var zipEntry = zipArchive.CreateEntry(aux);
                                using (var zipStream = zipEntry.Open())
                                using (var subEntryStream = await fs.LoadReadStreamAsync(subEntry.Path)) {
                                    await StreamUtils.CopyAsync(subEntryStream, zipStream, cancellationToken: cancellationToken);
                                }
                            } else if (subEntry.IsDirectory()) {
                                var zipEntry = zipArchive.CreateEntry(aux + "/");
                            }
                        }
                    }
                }
            }
        }


    }


}