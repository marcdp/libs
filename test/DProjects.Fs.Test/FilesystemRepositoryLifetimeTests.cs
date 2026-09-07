using System.Runtime.CompilerServices;

namespace DProjects.Fs.Test;

public class FilesystemRepositoryLifetimeTests {

    // methods
    [Fact]
    public async Task CompletedChildOperationDisposesFilesystemExactlyOnce() {
        var child = new RecordingFilesystem();
        using var filesystem = CreateRepositoryFilesystem(child);

        var entry = await filesystem.GetEntryAsync("/container/file", TestContext.Current.CancellationToken);

        Assert.Equal("/container/file", entry!.Path);
        Assert.Equal(1, child.DisposeCount);
    }
    [Fact]
    public async Task FailedChildOperationDisposesFilesystemAndPropagatesException() {
        var child = new RecordingFilesystem { ThrowOnGetEntry = true };
        using var filesystem = CreateRepositoryFilesystem(child);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.GetEntryAsync("/container/file", TestContext.Current.CancellationToken));

        Assert.Equal("child operation failed", exception.Message);
        Assert.Equal(1, child.DisposeCount);
    }
    [Fact]
    public async Task EnumerationKeepsChildAliveAndDisposesItOnCompletion() {
        var child = new RecordingFilesystem();
        using var filesystem = CreateRepositoryFilesystem(child);

        var count = 0;
        await foreach (var entry in filesystem.GetEntriesAsync("/container", cancellationToken: TestContext.Current.CancellationToken)) {
            Assert.Equal(0, child.DisposeCount);
            Assert.StartsWith("/container/", entry.Path);
            count++;
        }

        Assert.Equal(2, count);
        Assert.Equal(1, child.DisposeCount);
    }
    [Fact]
    public async Task AbandonedEnumerationDisposesChildFilesystem() {
        var child = new RecordingFilesystem();
        using var filesystem = CreateRepositoryFilesystem(child);

        await foreach (var entry in filesystem.GetEntriesAsync("/container", cancellationToken: TestContext.Current.CancellationToken)) {
            Assert.Equal(0, child.DisposeCount);
            Assert.Equal("/container/first", entry.Path);
            break;
        }

        Assert.Equal(1, child.DisposeCount);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ReturnedStreamOwnsUnderlyingStreamAndChildFilesystem(bool write) {
        var child = new RecordingFilesystem();
        using var filesystem = CreateRepositoryFilesystem(child);

        var stream = write
            ? await filesystem.LoadWriteStreamAsync("/container/file", new(), TestContext.Current.CancellationToken)
            : await filesystem.LoadReadStreamAsync("/container/file", new(), TestContext.Current.CancellationToken);

        Assert.Equal(0, child.DisposeCount);
        Assert.Equal(0, child.ReturnedStream!.DisposeCount);

        stream.Dispose();
        stream.Dispose();

        Assert.Equal(1, child.ReturnedStream.DisposeCount);
        Assert.Equal(1, child.DisposeCount);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedStreamCreationDisposesChildFilesystem(bool write) {
        var child = new RecordingFilesystem { ThrowOnStreamCreation = true };
        using var filesystem = CreateRepositoryFilesystem(child);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => write
            ? filesystem.LoadWriteStreamAsync("/container/file", new(), TestContext.Current.CancellationToken)
            : filesystem.LoadReadStreamAsync("/container/file", new(), TestContext.Current.CancellationToken));

        Assert.Equal("stream creation failed", exception.Message);
        Assert.Equal(1, child.DisposeCount);
    }

    // methods (private)
    private static FilesystemRepository CreateRepositoryFilesystem(RecordingFilesystem child) {
        return new FilesystemRepository.Builder(new RecordingRepository(child), false).Build();
    }

    private sealed class RecordingRepository(RecordingFilesystem child) : FilesystemRepository.Repository {

        // methods
        public Task<Entry?> GetByIdAsync(string id, CancellationToken cancellationToken) {
            return Task.FromResult<Entry?>(new Entry("/" + id, EntryType.Directory, default, default, 0, "", 0));
        }
        public async IAsyncEnumerable<Entry> GetByPatternAsync(string? pattern, [EnumeratorCancellation] CancellationToken cancellationToken) {
            await Task.CompletedTask;
            yield break;
        }
        public IFilesystem CreateFilesystem(string id, bool isReadonly) {
            return child;
        }
    }

    private sealed class RecordingFilesystem : Filesystem {

        // props
        public bool ThrowOnGetEntry { get; set; }
        public bool ThrowOnStreamCreation { get; set; }
        public int DisposeCount { get; private set; }
        public TrackingStream? ReturnedStream { get; private set; }
        public override string Url => "test://";

        // ctor
        public RecordingFilesystem() : base(false) {
        }

        // methods
        public override void Dispose() {
            DisposeCount++;
        }
        public override Entry? GetEntry(string path) {
            return new Entry(path, EntryType.File, default, default, 0, "", 0);
        }
        public override Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
            if (ThrowOnGetEntry) throw new InvalidOperationException("child operation failed");
            return Task.FromResult<Entry?>(GetEntry(path));
        }
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) {
            return Array.Empty<Entry>();
        }
        public override async IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield return new Entry("/first", EntryType.File, default, default, 0, "", 0);
            cancellationToken.ThrowIfCancellationRequested();
            yield return new Entry("/second", EntryType.File, default, default, 0, "", 0);
        }
        public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
            return CreateStream();
        }
        public override Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) {
            return Task.FromResult<Stream>(CreateStream());
        }
        public override Task<Stream> LoadWriteStreamAsync(string path, LoadWriteStreamSettings settings, CancellationToken cancellationToken) {
            return Task.FromResult<Stream>(CreateStream());
        }

        // methods (private)
        private TrackingStream CreateStream() {
            if (ThrowOnStreamCreation) throw new InvalidOperationException("stream creation failed");
            ReturnedStream = new TrackingStream();
            return ReturnedStream;
        }
    }

    private sealed class TrackingStream : MemoryStream {

        // props
        public int DisposeCount { get; private set; }

        // methods
        protected override void Dispose(bool disposing) {
            DisposeCount++;
            base.Dispose(disposing);
        }
    }
}
