using DProjects.Fs.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using System.Text;

namespace DProjects.Fs.Test;

public class FilesystemCompletenessTests {
    [Fact]
    public void GenericWriteStreamPersistsOverwriteAppendAndTruncate() {
        using var filesystem = new MinimalFilesystem();

        Write(filesystem.LoadWriteStream("/file", new()), "first");
        Write(filesystem.LoadWriteStream("/file", new() { Append = true }), "-second");
        Assert.Equal("first-second", Read(filesystem, "/file"));

        Write(filesystem.LoadWriteStream("/file", new() { Truncate = true }), "new");
        Assert.Equal("new", Read(filesystem, "/file"));
    }

    [Fact]
    public void GenericWriteStreamRejectsReadonlyMutation() {
        using var filesystem = new MinimalFilesystem { IsReadonly = true };
        Assert.Throws<InvalidOperationException>(() => filesystem.LoadWriteStream("/file", new()));
    }

    [Fact]
    public void GenericCopyAndMoveHandleNestedDirectories() {
        using var filesystem = new MinimalFilesystem();
        filesystem.CreateDirectory("/source/nested");
        using (var data = new MemoryStream(Encoding.UTF8.GetBytes("payload")))
            filesystem.SaveFile("/source/nested/file", data, new());

        filesystem.Copy("/source", "/copy", new() { Recursive = true, Overwrite = true }, NullLogger<IFilesystem>.Instance);
        Assert.Equal("payload", Read(filesystem, "/copy/nested/file"));

        filesystem.Move("/copy", "/moved", new(), NullLogger<IFilesystem>.Instance);
        Assert.False(filesystem.Exists("/copy"));
        Assert.Equal("payload", Read(filesystem, "/moved/nested/file"));
    }

    [Fact]
    public void GenericExistsAndTypedDeletesUseEntryPrimitives() {
        using var filesystem = new MinimalFilesystem();
        filesystem.CreateDirectory("/directory");
        using (var data = new MemoryStream([1])) filesystem.SaveFile("/file", data, new());

        Assert.True(filesystem.ExistsDirectory("/directory"));
        Assert.True(filesystem.ExistsFile("/file"));
        filesystem.DeleteFile("/directory");
        filesystem.DeleteDirectory("/file");
        Assert.True(filesystem.Exists("/directory"));
        Assert.True(filesystem.Exists("/file"));

        filesystem.DeleteFile("/file");
        filesystem.DeleteDirectory("/directory");
        Assert.False(filesystem.Exists("/file"));
        Assert.False(filesystem.Exists("/directory"));
    }

    [Fact]
    public async Task AsyncFirstGenericWriteStreamUsesAsyncPrimitives() {
        using var filesystem = new MinimalAsyncFilesystem();
        var stream = await filesystem.LoadWriteStreamAsync("/file", new(), TestContext.Current.CancellationToken);
        await stream.WriteAsync(Encoding.UTF8.GetBytes("async"), TestContext.Current.CancellationToken);
        await stream.DisposeAsync();
        Assert.Equal("async", Read(filesystem, "/file"));
        Assert.True(filesystem.AsyncSaveCalls > 0);
    }

    [Fact]
    public async Task SyncAdapterHonorsPreCancelledTokens() {
        using var filesystem = new MinimalSyncFilesystem();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => filesystem.GetEntryAsync("/", cancellation.Token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => filesystem.LoadWriteStreamAsync("/file", new(), cancellation.Token));
    }

    [Fact]
    public async Task InvalidSyncModeIsRejected() {
        using var filesystem = new MinimalFilesystem();
        var settings = new SyncSettings { Mode = (SyncModes)int.MaxValue };
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            filesystem.Sync("/", "/", settings, NullLogger<IFilesystem>.Instance));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            filesystem.SyncAsync("/", "/", settings, NullLogger<IFilesystem>.Instance, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void XmlMetadataPersistsAndReadonlyMutationFails() {
        using var storage = new FilesystemMem(false, false);
        using (var filesystem = new FilesystemXml(storage, "/data.xml", false, true, false, false, null, false)) {
            filesystem.CreateDirectory("/item");
            filesystem.SetMetadata("/item", new Dictionary<string, string> { ["Key"] = "value" });
        }

        using (var reopened = new FilesystemXml(storage, "/data.xml", false, false, false, false, null, false))
            Assert.Equal("value", reopened.GetMetadata("/item")["key"]);

        using var readonlyFilesystem = new FilesystemXml(storage, "/data.xml", true, false, false, false, null, false);
        Assert.Throws<InvalidOperationException>(() => readonlyFilesystem.SetMetadata("/item", new Dictionary<string, string>()));
        Assert.False(readonlyFilesystem.Supports("/item", Features.Touch));
    }

    private static void Write(Stream stream, string value) {
        using (stream) {
            var bytes = Encoding.UTF8.GetBytes(value);
            stream.Write(bytes, 0, bytes.Length);
        }
    }

    private static string Read(IFilesystem filesystem, string path) {
        using var stream = filesystem.LoadReadStream(path, new());
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private class MinimalFilesystem : Filesystem {
        protected readonly FilesystemMem Inner = new(false, false);
        public MinimalFilesystem() : base(false) { }
        public override string Url => "minimal:/";
        public override Entry? GetEntry(string path) => Inner.GetEntry(path);
        public override Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) => Inner.GetEntryAsync(path, cancellationToken);
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) => Inner.GetEntries(path, mode, pattern);
        public override IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, CancellationToken cancellationToken = default) => Inner.GetEntriesAsync(path, mode, pattern, cancellationToken);
        public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) => Inner.LoadReadStream(path, settings);
        public override Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) => Inner.LoadReadStreamAsync(path, settings, cancellationToken);
        public override Entry SaveFile(string path, Stream stream, SaveFileSettings settings) => Inner.SaveFile(path, stream, settings);
        public override Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken = default) => Inner.SaveFileAsync(path, stream, settings, cancellationToken);
        public override Entry CreateDirectory(string path) => Inner.CreateDirectory(path);
        public override Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) => Inner.CreateDirectoryAsync(path, cancellationToken);
        public override void Delete(string path) => Inner.Delete(path);
        public override Task DeleteAsync(string path, CancellationToken cancellationToken) => Inner.DeleteAsync(path, cancellationToken);
        public override void Dispose() => Inner.Dispose();
    }

    private sealed class MinimalSyncFilesystem : FilesystemSync {
        private readonly FilesystemMem _inner = new(false, false);
        public MinimalSyncFilesystem() : base(false) { }
        public override string Url => "minimal-sync:/";
        public override Entry? GetEntry(string path) => _inner.GetEntry(path);
        public override IEnumerable<Entry> GetEntries(string path, GetModes mode = GetModes.All, string? pattern = null) => _inner.GetEntries(path, mode, pattern);
        public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) => _inner.LoadReadStream(path, settings);
        public override Entry SaveFile(string path, Stream stream, SaveFileSettings settings) => _inner.SaveFile(path, stream, settings);
        public override Entry CreateDirectory(string path) => _inner.CreateDirectory(path);
        public override void Delete(string path) => _inner.Delete(path);
        public override void Dispose() => _inner.Dispose();
    }

    private sealed class MinimalAsyncFilesystem : FilesystemAsync {
        private readonly FilesystemMem _inner = new(false, false);
        public int AsyncSaveCalls { get; private set; }
        public MinimalAsyncFilesystem() : base(false) { }
        public override string Url => "minimal-async:/";
        public override Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) => _inner.GetEntryAsync(path, cancellationToken);
        public override IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null, CancellationToken cancellationToken = default) => _inner.GetEntriesAsync(path, mode, pattern, cancellationToken);
        public override Task<bool> ExistsAsync(string path, CancellationToken cancellationToken) => _inner.ExistsAsync(path, cancellationToken);
        public override Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) => _inner.LoadReadStreamAsync(path, settings, cancellationToken);
        public override async Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken) {
            AsyncSaveCalls++;
            return await _inner.SaveFileAsync(path, stream, settings, cancellationToken);
        }
        public override Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) => _inner.CreateDirectoryAsync(path, cancellationToken);
        public override Task DeleteAsync(string path, CancellationToken cancellationToken) => _inner.DeleteAsync(path, cancellationToken);
        public override void Dispose() => _inner.Dispose();
    }
}
