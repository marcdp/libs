using System.Runtime.CompilerServices;

namespace DProjects.Fs.Test;

public class FilesystemHardeningTests {
    [Fact]
    public void EntryWithLengthUsesExistingLengthForIdentityOptimization() {
        var entry = new Entry("/path", EntryType.File, DateTime.MinValue, DateTime.MaxValue, 42, "etag", 7);

        var unchanged = entry.WithLength(42);
        var changed = entry.WithLength(entry.Path.Length);

        Assert.Same(entry, unchanged);
        Assert.NotSame(entry, changed);
        Assert.Equal(entry.Path.Length, changed.Length);
        Assert.Equal(42, entry.Length);
    }

    [Fact]
    public void MounterEnumerationUsesPathSegmentBoundaries() {
        using var root = CreateMounterRoot();
        using var mounted = CreateFooMount();
        using var mounter = new FilesystemMounter();
        mounter.Mount("/", root, false);
        mounter.Mount("/foo", mounted, false);

        Assert.Contains(mounter.GetEntries("/foo"), entry => entry.Path == "/foo/bar");
        Assert.Contains(mounter.GetEntries("/foo/bar"), entry => entry.Path == "/foo/bar/mounted.txt");
        Assert.Contains(mounter.GetEntries("/foobar"), entry => entry.Path == "/foobar/root.txt");
        Assert.DoesNotContain(mounter.GetEntries("/foobar"), entry => entry.Path == "/foobar/wrong.txt");
        Assert.Contains(mounter.GetEntries("/foo2/bar"), entry => entry.Path == "/foo2/bar/root.txt");
        Assert.Contains(mounter.GetEntries("/root-child"), entry => entry.Path == "/root-child/root.txt");
    }

    [Fact]
    public async Task MounterEnumerationAsyncUsesPathSegmentBoundaries() {
        using var root = CreateMounterRoot();
        using var mounted = CreateFooMount();
        using var mounter = new FilesystemMounter();
        mounter.Mount("/", root, false);
        mounter.Mount("/foo", mounted, false);

        var cancellationToken = TestContext.Current.CancellationToken;
        Assert.Contains(await CollectAsync(mounter.GetEntriesAsync("/foo", cancellationToken: cancellationToken), cancellationToken), entry => entry.Path == "/foo/bar");
        Assert.Contains(await CollectAsync(mounter.GetEntriesAsync("/foo/bar", cancellationToken: cancellationToken), cancellationToken), entry => entry.Path == "/foo/bar/mounted.txt");
        Assert.Contains(await CollectAsync(mounter.GetEntriesAsync("/foobar", cancellationToken: cancellationToken), cancellationToken), entry => entry.Path == "/foobar/root.txt");
        Assert.DoesNotContain(await CollectAsync(mounter.GetEntriesAsync("/foobar", cancellationToken: cancellationToken), cancellationToken), entry => entry.Path == "/foobar/wrong.txt");
        Assert.Contains(await CollectAsync(mounter.GetEntriesAsync("/foo2/bar", cancellationToken: cancellationToken), cancellationToken), entry => entry.Path == "/foo2/bar/root.txt");
        Assert.Contains(await CollectAsync(mounter.GetEntriesAsync("/root-child", cancellationToken: cancellationToken), cancellationToken), entry => entry.Path == "/root-child/root.txt");
    }

    [Fact]
    public async Task UnionTouchAsyncUsesOnlyAsyncOperationsAndForwardsCancellation() {
        using var provider = new AsyncTouchFilesystem();
        using var union = new FilesystemUnion([provider], false);
        using var source = new CancellationTokenSource();

        await union.TouchAsync("/file", DateTime.UtcNow, source.Token);

        Assert.Equal(0, provider.SyncExistsCalls);
        Assert.Equal(1, provider.AsyncExistsCalls);
        Assert.Equal(1, provider.AsyncTouchCalls);
        Assert.Equal(source.Token, provider.ExistsToken);
        Assert.Equal(source.Token, provider.TouchToken);
    }

    [Fact]
    public async Task UnionTouchAsyncHonorsPreCancelledToken() {
        using var provider = new AsyncTouchFilesystem();
        using var union = new FilesystemUnion([provider], false);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            union.TouchAsync("/file", DateTime.UtcNow, source.Token));

        Assert.Equal(0, provider.SyncExistsCalls);
        Assert.Equal(1, provider.AsyncExistsCalls);
        Assert.Equal(0, provider.AsyncTouchCalls);
    }

    [Fact]
    public async Task RepositoryEnumerationForwardsCallerCancellationToken() {
        using var child = new RecordingEntriesFilesystem();
        using var repository = new RecordingRepository(child);
        using var filesystem = new FilesystemRepository.Builder(repository, false).Build();
        using var source = new CancellationTokenSource();

        var entries = await CollectAsync(filesystem.GetEntriesAsync("/container", cancellationToken: source.Token), source.Token);

        Assert.Single(entries);
        Assert.Equal(source.Token, child.ReceivedToken);
    }

    [Fact]
    public async Task RepositoryEnumerationHonorsPreCancelledToken() {
        using var child = new RecordingEntriesFilesystem();
        using var repository = new RecordingRepository(child);
        using var filesystem = new FilesystemRepository.Builder(repository, false).Build();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CollectAsync(filesystem.GetEntriesAsync("/container", cancellationToken: source.Token), source.Token));

        Assert.Equal(source.Token, child.ReceivedToken);
    }

    [Fact]
    public void RepositoryDisposeDisposesDisposableRepository() {
        using var child = new RecordingEntriesFilesystem();
        var repository = new RecordingRepository(child);
        var filesystem = new FilesystemRepository.Builder(repository, false).Build();

        filesystem.Dispose();

        Assert.True(repository.IsDisposed);
    }

    private static FilesystemMem CreateMounterRoot() {
        var filesystem = new FilesystemMem(false, false);
        foreach (var path in new[] { "/foobar", "/foo2", "/foo2/bar", "/root-child" }) {
            filesystem.CreateDirectory(path);
        }
        filesystem.SaveFile("/foobar/root.txt", new MemoryStream([1]), new());
        filesystem.SaveFile("/foo2/bar/root.txt", new MemoryStream([1]), new());
        filesystem.SaveFile("/root-child/root.txt", new MemoryStream([1]), new());
        return filesystem;
    }

    private static FilesystemMem CreateFooMount() {
        var filesystem = new FilesystemMem(false, false);
        filesystem.CreateDirectory("/bar");
        filesystem.SaveFile("/bar/mounted.txt", new MemoryStream([1]), new());
        filesystem.SaveFile("/bar/wrong.txt", new MemoryStream([1]), new());
        return filesystem;
    }

    private static async Task<List<Entry>> CollectAsync(
        IAsyncEnumerable<Entry> entries,
        CancellationToken cancellationToken) {
        var result = new List<Entry>();
        await foreach (var entry in entries.WithCancellation(cancellationToken)) result.Add(entry);
        return result;
    }

    private sealed class AsyncTouchFilesystem : DelegatingFilesystem {
        public int SyncExistsCalls { get; private set; }
        public int AsyncExistsCalls { get; private set; }
        public int AsyncTouchCalls { get; private set; }
        public CancellationToken ExistsToken { get; private set; }
        public CancellationToken TouchToken { get; private set; }

        public override bool Exists(string path) {
            SyncExistsCalls++;
            throw new InvalidOperationException("The synchronous path must not be used.");
        }

        public override Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) {
            AsyncExistsCalls++;
            ExistsToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public override Task TouchAsync(string path, DateTime aDate, CancellationToken cancellationToken) {
            AsyncTouchCalls++;
            TouchToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingEntriesFilesystem : DelegatingFilesystem {
        public CancellationToken ReceivedToken { get; private set; }

        public override async IAsyncEnumerable<Entry> GetEntriesAsync(
            string path,
            GetModes mode = GetModes.All,
            string? pattern = null,
            [EnumeratorCancellation] CancellationToken cancellationToken = default) {
            ReceivedToken = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            await Task.CompletedTask;
            yield return new Entry("/file", EntryType.File, default, default, 0, "", 0);
        }
    }

    private class DelegatingFilesystem : Filesystem {
        private readonly FilesystemMem inner = new(false, false);
        public DelegatingFilesystem() : base(false) { }
        public override string Url => "test:/";
        public override Entry? GetEntry(string path) => inner.GetEntry(path);
        public override Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) =>
            inner.GetEntryAsync(path, cancellationToken);
        public override IEnumerable<Entry> GetEntries(
            string path,
            GetModes mode = GetModes.All,
            string? pattern = null) => inner.GetEntries(path, mode, pattern);
        public override IAsyncEnumerable<Entry> GetEntriesAsync(
            string path,
            GetModes mode = GetModes.All,
            string? pattern = null,
            CancellationToken cancellationToken = default) =>
            inner.GetEntriesAsync(path, mode, pattern, cancellationToken);
        public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) =>
            inner.LoadReadStream(path, settings);
        public override Task<Stream> LoadReadStreamAsync(
            string path,
            LoadReadStreamSettings settings,
            CancellationToken cancellationToken) => inner.LoadReadStreamAsync(path, settings, cancellationToken);
        public override void Dispose() => inner.Dispose();
    }

    private sealed class RecordingRepository(RecordingEntriesFilesystem child) :
        FilesystemRepository.Repository,
        IDisposable {
        public bool IsDisposed { get; private set; }
        public Task<Entry?> GetByIdAsync(string id, CancellationToken cancellationToken) =>
            Task.FromResult<Entry?>(new Entry("/" + id, EntryType.Directory, default, default, 0, "", 0));
        public async IAsyncEnumerable<Entry> GetByPatternAsync(
            string? pattern,
            [EnumeratorCancellation] CancellationToken cancellationToken) {
            await Task.CompletedTask;
            yield break;
        }
        public IFilesystem CreateFilesystem(string id, bool isReadonly) => child;
        public void Dispose() => IsDisposed = true;
    }
}
