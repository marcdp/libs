using DProjects.Fs;
using System.Runtime.CompilerServices;

namespace DProjects.Log.Storage.Tests {

    internal sealed class ControlledFilesystem : FilesystemAsync {

        // vars
        private readonly IFilesystem mInner;
        private int mEntriesEnumerated;
        private int mFilesDeleted;

        // props
        public override string Url => mInner.Url;
        public Action<int>? EntryEnumerated { get; set; }
        public Action<int>? FileDeleted { get; set; }
        public int EntriesEnumerated => mEntriesEnumerated;
        public int FilesDeleted => mFilesDeleted;

        // ctor
        public ControlledFilesystem(IFilesystem inner) : base(inner.IsReadonly) {
            mInner = inner;
        }

        // methods
        public override Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            return mInner.GetEntryAsync(path, cancellationToken);
        }
        public override async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            await foreach (var entry in mInner.GetEntriesAsync(path, mode, pattern, cancellationToken)) {
                yield return entry;
                mEntriesEnumerated++;
                EntryEnumerated?.Invoke(mEntriesEnumerated);
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
        public override Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) {
            return mInner.LoadReadStreamAsync(path, settings, cancellationToken);
        }
        public override Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken) {
            return mInner.SaveFileAsync(path, stream, settings, cancellationToken);
        }
        public override Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
            return mInner.CreateDirectoryAsync(path, cancellationToken);
        }
        public override async Task DeleteAsync(string path, CancellationToken cancellationToken) {
            await mInner.DeleteAsync(path, cancellationToken);
            mFilesDeleted++;
            FileDeleted?.Invoke(mFilesDeleted);
        }
        public override Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            return mInner.TouchAsync(path, aDate, cancellationToken);
        }
    }
}
