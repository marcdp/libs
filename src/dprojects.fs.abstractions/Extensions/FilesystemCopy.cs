using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs.Extensions {


    public static class FilesystemCopy {

        //methods
        public static void CopyRecursive(this IFilesystemSync filesystem, string source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            var entrySource = filesystem.GetEntry(source);
            if (entrySource == null) {
                throw new Exception("Unable to copy: not found " + source);
            } else if (entrySource.IsDirectory()) {
                IDictionary<string, string>? metadata = null;
                if (filesystem.Supports(source, Features.Metadata) && filesystem.Supports(destination, Features.Metadata)) {
                    metadata = filesystem.GetMetadata(source);
                }
                if (!filesystem.ExistsDirectory(destination)) {
                    logger.LogInformation("Copy {from} to {to}", entrySource.Path, destination);
                    filesystem.CreateDirectory(destination);
                }
                if (metadata != null && metadata.Count > 0) {
                    filesystem.SetMetadata(destination, metadata);
                }
                if (settings.Recursive) {
                    foreach (Entry entryChildsource in filesystem.GetEntries(source)) {
                        string childPath = PathUtils.Combine(destination, entryChildsource.Name);
                        try {
                            filesystem.CopyRecursive(entryChildsource.Path, childPath, settings, logger);
                        } catch (TaskCanceledException) {
                            throw;
                        } catch (Exception ex) {
                            if (settings.IgnoreErrors) {
                                logger.LogError("Error: {message}", ex.Message);
                            } else {
                                throw;
                            }
                        }
                    }
                } else {
                    logger.LogInformation("Omitting directory {path}", destination);
                }
            } else {
                var entryDestination = filesystem.GetEntry(destination);
                if (entryDestination == null) {
                    CopyFileRecursive(filesystem, entrySource, destination, settings, logger);
                } else if (!entryDestination.IsDirectory()) {
                    if (settings.Overwrite) {
                        CopyFileRecursive(filesystem, entrySource, destination, settings, logger);
                    }
                } else if (entryDestination.IsDirectory()) {
                    string destinationInDirectory = PathUtils.Combine(destination, PathUtils.GetPathName(source));
                    var entryDestinationInDirectory = filesystem.GetEntry(destinationInDirectory);
                    if (entryDestinationInDirectory == null) {
                        CopyFileRecursive(filesystem, entrySource, destinationInDirectory, settings, logger);
                    } else if (!entryDestination.IsDirectory()) {
                        throw new Exception("Unable to copy: destination exists and is a directory");
                    } else {
                        if (settings.Overwrite) {
                            CopyFileRecursive(filesystem, entrySource, destinationInDirectory, settings, logger);
                        }
                    }
                }
            }
        }
        private static void CopyFileRecursive(this IFilesystemSync filesystem, Entry source, string destination, CopySettings settings, ILogger<IFilesystem> logger) {
            for (int trie = 0; trie <= settings.Tries - 1; trie++) {
                logger.LogInformation("Copy {from} to {to}", source.Path, destination);
                try {
                    IDictionary<string, string>? metadata = null;
                    if (filesystem.Supports(source.Path, Features.Metadata) && filesystem.Supports(destination, Features.Metadata)) {
                        metadata = filesystem.GetMetadata(source.Path);
                    }
                    using (var stream = filesystem.LoadReadStream(source.Path)) {
                        filesystem.SaveFile(destination, stream);
                    }
                    if (metadata != null && metadata.Count > 0) {
                        filesystem.SetMetadata(destination, metadata);
                    }
                    return;
                } catch (TaskCanceledException) {
                    throw;
                } catch (Exception ex) {
                    logger.LogError("Error copying {from} to {to}: {message}", source.Path, destination, ex.Message);
                    if (trie == settings.Tries - 1) {
                        if (!settings.IgnoreErrors) throw;
                    } else {
                        System.Threading.Thread.Sleep(250);
                    }
                }
            }
        }


        //methods
        public static async Task CopyRecursiveAsync(this IFilesystemAsync filesystem, string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            var entrySource = await filesystem.GetEntryAsync(source);
            if (entrySource == null) {
                throw new Exception("Unable to copy: not found " + source);
            } else if (entrySource.IsDirectory()) {
                IDictionary<string, string>? metadata = null;
                if (await filesystem.SupportsAsync(source, Features.Metadata, cancellationToken) && await filesystem.SupportsAsync(destination, Features.Metadata, cancellationToken)) {
                    metadata = await filesystem.GetMetadataAsync(source, cancellationToken);
                }
                if (!await filesystem.ExistsDirectoryAsync(destination)) {
                    logger.LogInformation("Copy {from} to {to}", entrySource.Path, destination);
                    await filesystem.CreateDirectoryAsync(destination, cancellationToken);
                }
                if (metadata != null && metadata.Count > 0) {
                    await filesystem.SetMetadataAsync(destination, metadata, cancellationToken);
                }
                if (settings.Recursive) {
                    await foreach (Entry entryChildsource in filesystem.GetEntriesAsync(source)) {
                        string childPath = PathUtils.Combine(destination, entryChildsource.Name);
                        try {
                            await filesystem.CopyRecursiveAsync(entryChildsource.Path, childPath, settings, logger, cancellationToken);
                        } catch (TaskCanceledException) {
                            throw;
                        } catch (Exception ex) {
                            if (settings.IgnoreErrors) {
                                logger.LogError("Error: {message}", ex.Message);
                            } else {
                                throw;
                            }
                        }
                    }
                } else {
                    logger.LogInformation("Omitting directory {path}", destination);
                }
            } else {
                var entryDestination = await filesystem.GetEntryAsync(destination);
                if (entryDestination == null) {
                    await CopyFileRecursiveAsync(filesystem, entrySource, destination, settings, logger, cancellationToken);
                } else if (!entryDestination.IsDirectory()) {
                    if (settings.Overwrite) {
                        await CopyFileRecursiveAsync(filesystem, entrySource, destination, settings, logger, cancellationToken);
                    }
                } else if (entryDestination.IsDirectory()) {
                    string destinationInDirectory = PathUtils.Combine(destination, PathUtils.GetPathName(source));
                    var entryDestinationInDirectory = await filesystem.GetEntryAsync(destinationInDirectory);
                    if (entryDestinationInDirectory == null) {
                        await CopyFileRecursiveAsync(filesystem, entrySource, destinationInDirectory, settings, logger, cancellationToken);
                    } else if (!entryDestination.IsDirectory()) {
                        throw new Exception("Unable to copy: destination exists and is a directory");
                    } else {
                        if (settings.Overwrite) {
                            await CopyFileRecursiveAsync(filesystem, entrySource, destinationInDirectory, settings, logger, cancellationToken);
                        }
                    }
                }
            }
        }
        private static async Task CopyFileRecursiveAsync(this IFilesystemAsync filesystem, Entry source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            for (int trie = 0; trie <= settings.Tries - 1; trie++) {
                logger.LogInformation("Copy {from} to {to}", source.Path, destination);
                try {
                    IDictionary<string, string>? metadata = null;
                    if (await filesystem.SupportsAsync(source.Path, Features.Metadata, cancellationToken) && await filesystem.SupportsAsync(destination, Features.Metadata, cancellationToken)) {
                        metadata = await filesystem.GetMetadataAsync(source.Path, cancellationToken);
                    }
                    using (var stream = await filesystem.LoadReadStreamAsync(source.Path)) {
                        await filesystem.SaveFileAsync(destination, stream);
                    }
                    if (metadata != null && metadata.Count > 0) {
                        await filesystem.SetMetadataAsync(destination, metadata, cancellationToken);
                    }
                    return;
                } catch (TaskCanceledException) {
                    throw;
                } catch (Exception ex) {
                    logger.LogError("Error copying {from} to {to}: {message}", source.Path, destination, ex.Message);
                    if (trie == settings.Tries - 1) {
                        if (!settings.IgnoreErrors) throw;
                    } else {
                        System.Threading.Thread.Sleep(250);
                    }
                }
            }
        }

    }


}


