using DProjects.Streams;
using DProjects.Utils;

using Microsoft.Extensions.Logging;

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Fs {


    public class FilesystemRepository : FilesystemAsync {


        //interface
        public interface Repository {
            Task<Entry?> GetByIdAsync(string id, CancellationToken cancellationToken);
            IAsyncEnumerable<Entry> GetByPatternAsync(string? pattern, CancellationToken cancellationToken);
            IFilesystem CreateFilesystem(string id, bool isReadonly);
        }
        public interface RepositoryWritable { 
            Task<Entry> AddAsync(string id, CancellationToken cancellationToken);
            Task RemoveAsync(string id, CancellationToken cancellationToken);
        }


        //builder
        public class Builder {
            public bool IsReadOnly { get; set; }
            public Repository Repository { get; set; }
            public Builder(Repository repository, bool isReadOnly) {
                IsReadOnly = isReadOnly;
                Repository = repository;
            }
            public FilesystemRepository Build() {
                return new FilesystemRepository(Repository, IsReadOnly);
            }
        }


        //variables
        private Entry mRoot;
        private Repository mRepository;


        //constructor
        protected FilesystemRepository(Repository repository, bool isReadOnly) : base(isReadOnly) {
            mRepository = repository;
            mRoot = new Entry("/", EntryType.Directory, DateTime.Now, DateTime.Now, 0, "", 0);
        }
        public override void Dispose() {
            base.Dispose();
        }


        //properties
        public override string Url {
            get {
                return "repository:///" + mRepository.GetType().FullName;
            }
        }


        //methods LEVEL 0
        public override async Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            if (path.Equals("/")) {
                return mRoot;
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                return await mRepository.GetByIdAsync(PathUtils.GetPathName(path), cancellationToken);
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                var entry = await fs.GetEntryAsync(subPath, cancellationToken);
                if (entry != null) return entry.WithPath(PathUtils.Combine("/", name, entry.Path));
                return null;
            }
        }        
        public override async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            if (path.Equals("/")) {
                if (mode == GetModes.All) {
                    await foreach (var entry in mRepository.GetByPatternAsync(pattern, cancellationToken)) {
                        yield return entry;
                    }
                } else if (mode == GetModes.Files) {
                    await foreach (var entry in mRepository.GetByPatternAsync(pattern, cancellationToken)) {
                        if (entry.IsFile()) yield return entry;
                    }
                } else if (mode == GetModes.Directories) {
                    await foreach (var entry in mRepository.GetByPatternAsync(pattern, cancellationToken)) {
                        if (entry.IsDirectory()) yield return entry;
                    }
                } else if (mode == GetModes.Descendants) {
                    await foreach (var entry in mRepository.GetByPatternAsync(null, cancellationToken)) {
                        if (pattern == null || StringUtils.Like(entry.Name, pattern)) {
                            yield return entry;
                        }
                        if (entry.IsDirectory()) {
                            await foreach (var subEntry in GetEntriesAsync(entry.Path, mode, pattern, cancellationToken)) {
                                yield return subEntry;
                            }
                        }
                    }
                }
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                await foreach (var entry in fs.GetEntriesAsync(subPath, mode, pattern)) {
                    yield return entry.WithPath(PathUtils.Combine("/", name, entry.Path));
                }
            }
        }
        public override async Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            return await GetEntryAsync(path, cancellationToken) != null;
        }
        public override async Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings? settings, CancellationToken cancellationToken = default) {
            if (path.Equals("/")) {
                throw new NotImplementedException("Unable to load read stream: directory: " + path);
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                throw new NotImplementedException("Unable to load read stream: directory: " + path);
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                return await fs.LoadReadStreamAsync(subPath, settings, cancellationToken);
            }
        }
        public override async Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings? settings, CancellationToken cancellationToken = default) {
            if (path.Equals("/")) {
                throw new NotImplementedException("Unable to load write stream: directory: " + path);
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                throw new NotImplementedException("Unable to load write stream: directory: " + path);
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                return await fs.LoadWriteStreamAsync(subPath, settings, cancellationToken);
            }
        }


        //method1 LEVEL 1

        //methods LEVEL 2
        public override async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings? settings, CancellationToken cancellationToken = default) {
            if (path.Equals("/")) {
                throw new NotImplementedException("Unable to save file: " + path);  
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                throw new NotImplementedException("Unable to save file: " + path);
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                var entry = await fs.SaveFileAsync(subPath, stream, settings, cancellationToken);
                return entry.WithPath(PathUtils.Combine("/", name, entry.Path));
            }
        }
        public override async Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (path.Equals("/")) {  
                throw new NotImplementedException("Unable to create directory: " + path);
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                if (mRepository is RepositoryWritable) {
                    var repositoryWritable = (RepositoryWritable)mRepository;
                    return await repositoryWritable.AddAsync(PathUtils.GetPathName(path), cancellationToken);
                }
                throw new NotImplementedException("Unable to create directory: " + path);
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                var entry = await fs.CreateDirectoryAsync(subPath, cancellationToken);
                return entry.WithPath(PathUtils.Combine("/", name, entry.Path));
            }
        }
        public override async Task DeleteAsync(string path, CancellationToken cancellationToken) {
            if (path.Equals("/")) {
                throw new NotImplementedException("Unable to delete: " + path);
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                if (mRepository is RepositoryWritable) {
                    var repositoryWritable = (RepositoryWritable) mRepository;
                    await repositoryWritable.RemoveAsync(PathUtils.GetPathName(path), cancellationToken);
                } else {
                    throw new NotImplementedException("Unable to delete: " + path);
                }
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                await fs.DeleteAsync(subPath, cancellationToken);
            }
        }
        public override async Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            if (path.Equals("/")) {
                throw new NotImplementedException("Unable to touch: " + path);
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                throw new NotImplementedException("Unable to touch: " + path);
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                await fs.TouchAsync(subPath, aDate, cancellationToken);
            }
        }


        //method LEVEL 3
        public override async Task DeleteFileAsync(string path, CancellationToken cancellationToken) {
            if (path.Equals("/")) {
                throw new NotImplementedException("Unable to delete file: " + path);
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                throw new NotImplementedException("Unable to delete file: " + path);
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                await fs.DeleteFileAsync(subPath, cancellationToken);
            }
        }
        public override async Task DeleteDirectoryAsync(string path, CancellationToken cancellationToken) {
            if (path.Equals("/")) {
                throw new NotImplementedException("Unable to delete directory: " + path);
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                if (mRepository is RepositoryWritable) {
                    var repositoryWritable = (RepositoryWritable) mRepository;
                    await repositoryWritable.RemoveAsync(PathUtils.GetPathName(path), cancellationToken);
                } else {
                    throw new NotImplementedException("Unable to delete directory: " + path);
                }
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                await fs.DeleteDirectoryAsync(subPath, cancellationToken);
            }
        }
        public override async Task CopyAsync(string source, string destination, CopySettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (source.Equals("/")) {
                throw new NotImplementedException("Unable to move: " + source);
            } else if (PathUtils.GetPathParent(source).Equals("/")) {
                throw new NotImplementedException("Unable to move: " + source);
            } else if (destination.Equals("/")) {
                throw new NotImplementedException("Unable to move: " + destination);
            } else if (PathUtils.GetPathParent(destination).Equals("/")) {
                throw new NotImplementedException("Unable to move: " + destination);
            } else {
                var nameA = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(source, 1));
                var subPathA = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(source, 1));
                var nameB = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(destination, 1));
                var subPathB = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(destination, 1));
                if (nameA.Equals(nameB)) {
                    var fs = mRepository.CreateFilesystem(nameA, IsReadonly);
                    await fs.CopyAsync(subPathA, subPathB, settings, logger, cancellationToken);
                } else {
                    await base.CopyAsync(source, destination, settings, logger, cancellationToken);
                }
            }
        }
        public override async Task MoveAsync(string source, string destination, MoveSettings settings, ILogger<IFilesystem> logger, CancellationToken cancellationToken) {
            if (source.Equals("/")) {
                throw new NotImplementedException("Unable to move: " + source);
            } else if (PathUtils.GetPathParent(source).Equals("/")) {
                throw new NotImplementedException("Unable to move: " + source);
            } else if (destination.Equals("/")) {
                throw new NotImplementedException("Unable to move: " + destination);
            } else if (PathUtils.GetPathParent(destination).Equals("/")) {
                throw new NotImplementedException("Unable to move: " + destination);
            } else {
                var nameA = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(source, 1));
                var subPathA = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(source, 1));

                var nameB = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(destination, 1));
                var subPathB = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(destination, 1));
                if (nameA.Equals(nameB)) {
                    var fs = mRepository.CreateFilesystem(nameA, IsReadonly);
                    await fs.MoveAsync(subPathA, subPathB, settings, logger, cancellationToken);
                } else {
                    await base.MoveAsync(source, destination, settings, logger, cancellationToken);
                }
            }
        }
        


        //methods LEVEL 4
        public override async Task<IDictionary<string, string>> GetMetadataAsync(string path, CancellationToken cancellationToken) {
            if (path.Equals("/")) {
                return new Dictionary<string, string>();
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                return new Dictionary<string, string>();
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                return await fs.GetMetadataAsync(subPath, cancellationToken);
            }
        }
        public override async Task SetMetadataAsync(string path, IDictionary<string, string> metadata, CancellationToken cancellationToken) {
            if (path.Equals("/")) {
                throw new NotImplementedException("Unable to set metadata: " + path);
            } else if (PathUtils.GetPathParent(path).Equals("/")) {
                throw new NotImplementedException("Unable to set metadata: " + path);
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                await fs.SetMetadataAsync(subPath, metadata, cancellationToken);
            }
        }
        public override async Task<bool> SupportsAsync(string path, Features feature, CancellationToken cancellationToken) {
            if (path.Equals("/")) {
                return false; 
            } else {
                var name = PathUtils.GetPathName(PathUtils.GetPathCuttedByLevel(path, 1));
                var subPath = PathUtils.Combine("/", PathUtils.GetPathCuttedFromLevel(path, 1));
                var fs = mRepository.CreateFilesystem(name, IsReadonly);
                return await fs.SupportsAsync(subPath, feature, cancellationToken);
            }
        }

    }

}


