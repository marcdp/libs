using DProjects.Fs.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace DProjects.Fs.Test;

public class FilesystemCorrectnessTests {
    [Fact]
    public void LocalPathsRemainInsideConfiguredRoot() {
        var testPath = Path.Combine(Path.GetTempPath(), "DProjects.Fs.Test", Guid.NewGuid().ToString("N"));
        var rootPath = Path.Combine(testPath, "root");
        Directory.CreateDirectory(Path.Combine(rootPath, "child", "nested"));
        try {
            using var filesystem = new FilesystemLocal(rootPath, false, false, false);

            Assert.Equal(Path.GetFullPath(rootPath), filesystem.GetNativePath("/"));
            Assert.Equal(Path.GetFullPath(Path.Combine(rootPath, "child")), filesystem.GetNativePath("/child"));
            Assert.Equal(
                Path.GetFullPath(Path.Combine(rootPath, "child", "nested")),
                filesystem.GetNativePath("/child/nested"));
            Assert.Equal(
                Path.GetFullPath(Path.Combine(rootPath, "child")),
                filesystem.GetNativePath("/", "/child"));

            Assert.Throws<ArgumentException>(() => filesystem.GetNativePath("/../outside"));
            Assert.Throws<ArgumentException>(() => filesystem.GetNativePath("/../../outside"));
        } finally {
            Directory.Delete(testPath, true);
        }
    }

    [Fact]
    public void EscapedLocalOperationsAreRejectedWithoutTouchingRoot() {
        var testPath = Path.Combine(Path.GetTempPath(), "DProjects.Fs.Test", Guid.NewGuid().ToString("N"));
        var rootPath = Path.Combine(testPath, "root");
        Directory.CreateDirectory(rootPath);
        var sentinelPath = Path.Combine(rootPath, "sentinel.txt");
        File.WriteAllText(sentinelPath, "sentinel");
        try {
            using var filesystem = new FilesystemLocal(rootPath, false, false, false);

            Assert.Throws<ArgumentException>(() => filesystem.LoadReadStream("/../outside.txt", new()));
            Assert.Throws<ArgumentException>(() => filesystem.LoadWriteStream("/../outside.txt", new()));
            Assert.Throws<ArgumentException>(() => filesystem.Delete("/.."));

            Assert.True(Directory.Exists(rootPath));
            Assert.Equal("sentinel", File.ReadAllText(sentinelPath));

            filesystem.SaveFile("/valid.txt", new MemoryStream(Encoding.UTF8.GetBytes("valid")), new());
            Assert.Equal("valid", File.ReadAllText(Path.Combine(rootPath, "valid.txt")));
        } finally {
            Directory.Delete(testPath, true);
        }
    }

    [Fact]
    public void MounterSyncInvokesSameMountOnceWithCombinedPrefix() {
        using var mounted = new CountingFilesystem();
        using var mounter = new FilesystemMounter();
        mounter.Mount("/mount", mounted, false, "/prefix");

        mounter.Sync("/mount/source", "/mount/destination", new(), NullLogger<IFilesystem>.Instance);

        Assert.Equal(1, mounted.SyncCalls);
        Assert.Equal("/prefix/source", mounted.SyncSource);
        Assert.Equal("/prefix/destination", mounted.SyncDestination);
    }

    [Fact]
    public async Task MounterSyncAsyncInvokesSameMountOnceWithCombinedPrefix() {
        using var mounted = new CountingFilesystem();
        using var mounter = new FilesystemMounter();
        mounter.Mount("/mount", mounted, false, "/prefix");

        await mounter.SyncAsync(
            "/mount/source",
            "/mount/destination",
            new(),
            NullLogger<IFilesystem>.Instance,
            TestContext.Current.CancellationToken);

        Assert.Equal(1, mounted.SyncAsyncCalls);
        Assert.Equal("/prefix/source", mounted.SyncAsyncSource);
        Assert.Equal("/prefix/destination", mounted.SyncAsyncDestination);
    }

    [Fact]
    public void MounterSyncAcrossMountsUsesGenericBehavior() {
        using var source = new FilesystemMem(false, false);
        using var destination = new FilesystemMem(false, false);
        source.SaveFile("/file.txt", new MemoryStream(Encoding.UTF8.GetBytes("value")), new());
        using var mounter = new FilesystemMounter();
        mounter.Mount("/source", source, false);
        mounter.Mount("/destination", destination, false);

        mounter.Sync("/source", "/destination", new(), NullLogger<IFilesystem>.Instance);

        Assert.Equal("value", ReadText(destination, "/file.txt"));
    }

    [Fact]
    public async Task ReadonlyMounterRejectsMutationsButLeavesMountedFilesystemWritable() {
        using var mounted = new FilesystemMem(false, false);
        mounted.CreateDirectory("/directory");
        mounted.SaveFile("/file", new MemoryStream([1]), new());
        using var mounter = new FilesystemMounter(true);
        mounter.Mount("/", mounted, false);

        Assert.Throws<InvalidOperationException>(() =>
            mounter.SaveFile("/new-file", new MemoryStream([1]), new()));
        Assert.Throws<InvalidOperationException>(() => mounter.CreateDirectory("/new-directory"));
        Assert.Throws<InvalidOperationException>(() => mounter.Delete("/file"));
        Assert.Throws<InvalidOperationException>(() => mounter.Touch("/file", DateTime.UtcNow));
        Assert.Throws<InvalidOperationException>(() =>
            mounter.SetMetadata("/file", new Dictionary<string, string> { ["key"] = "value" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mounter.SaveFileAsync("/new-file", new MemoryStream([1]), new(), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mounter.CreateDirectoryAsync("/new-directory", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mounter.DeleteAsync("/file", TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mounter.TouchAsync("/file", DateTime.UtcNow, TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mounter.SetMetadataAsync(
                "/file",
                new Dictionary<string, string> { ["key"] = "value" },
                TestContext.Current.CancellationToken));

        mounted.SaveFile("/direct", new MemoryStream([2]), new());
        Assert.True(mounted.ExistsFile("/direct"));
        Assert.True(mounted.ExistsFile("/file"));
    }

    [Fact]
    public async Task ReadonlyHttpRejectsEveryMutationBeforeNetworkAccess() {
        using var filesystem = new FilesystemHttp(
            new Uri("http://127.0.0.1:1/files"),
            0,
            FilesystemHttp.AuthSchemes.None,
            true);
        var logger = NullLogger<IFilesystem>.Instance;
        var cancellationToken = TestContext.Current.CancellationToken;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.SaveFileAsync("/file", new MemoryStream([1]), new(), cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.CreateDirectoryAsync("/directory", cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => filesystem.DeleteAsync("/file", cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => filesystem.DeleteFileAsync("/file", cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.DeleteDirectoryAsync("/directory", cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.TouchAsync("/file", DateTime.UtcNow, cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.CopyAsync("/source", "/destination", new(), logger, cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.MoveAsync("/source", "/destination", new(), logger, cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.SyncAsync("/source", "/destination", new(), logger, cancellationToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            filesystem.SetMetadataAsync("/file", new Dictionary<string, string>(), cancellationToken));
    }

    [Fact]
    public void ReadonlyZipReadsFromReadonlyBackingFilesystemAndRejectsWriteStream() {
        using var backing = new FilesystemMem(false, false);
        using (var writableZip = new FilesystemZip(backing, "/archive.zip", null, false)) {
            writableZip.CreateDirectory("/folder");
            writableZip.SaveFile(
                "/folder/file.txt",
                new MemoryStream(Encoding.UTF8.GetBytes("content")),
                new());
        }
        backing.IsReadonly = true;

        using var readonlyZip = new FilesystemZip(backing, "/archive.zip", null, true);

        Assert.True(readonlyZip.GetEntry("/folder")!.IsDirectory());
        Assert.Contains(readonlyZip.GetEntries("/folder"), entry => entry.Path == "/folder/file.txt");
        Assert.Equal("content", ReadText(readonlyZip, "/folder/file.txt"));
        Assert.Throws<InvalidOperationException>(() => readonlyZip.LoadWriteStream("/new-file", new()));
        Assert.Equal("zip:" + backing.Url + "/archive.zip", readonlyZip.Url);
    }

    [Fact]
    public void ZipTouchUpdatesDirectoryAndFileEntries() {
        using var backing = new FilesystemMem(false, false);
        using var filesystem = new FilesystemZip(backing, "/archive.zip", null, false);
        filesystem.CreateDirectory("/folder");
        filesystem.SaveFile("/file.txt", new MemoryStream([1]), new());
        var directoryDate = new DateTime(2024, 1, 2, 3, 4, 6);
        var fileDate = new DateTime(2024, 2, 3, 4, 5, 8);

        filesystem.Touch("/folder", directoryDate);
        filesystem.Touch("/file.txt", fileDate);

        Assert.Equal(directoryDate, filesystem.GetEntry("/folder")!.Modified);
        Assert.Equal(fileDate, filesystem.GetEntry("/file.txt")!.Modified);
        Assert.Null(filesystem.GetEntry("/older"));
    }

    [Fact]
    public void MemoryMetadataIsReturnedAsSnapshotAndSetMetadataStillWorks() {
        using var filesystem = new FilesystemMem(false, false);
        filesystem.SaveFile("/file", new MemoryStream([1]), new());
        filesystem.SetMetadata("/file", new Dictionary<string, string> { ["a"] = "1" });

        var metadata = filesystem.GetMetadata("/file");
        metadata["a"] = "2";
        metadata["b"] = "3";

        var metadataAgain = filesystem.GetMetadata("/file");
        Assert.Equal("1", metadataAgain["a"]);
        Assert.False(metadataAgain.ContainsKey("b"));

        filesystem.SetMetadata("/file", new Dictionary<string, string> { ["a"] = "updated" });
        Assert.Equal("updated", filesystem.GetMetadata("/file")["a"]);
    }

    [Fact]
    public void MemoryMetadataSnapshotCannotBypassReadonlyState() {
        using var filesystem = new FilesystemMem(false, false);
        filesystem.SaveFile("/file", new MemoryStream([1]), new());
        filesystem.SetMetadata("/file", new Dictionary<string, string> { ["a"] = "1" });
        filesystem.IsReadonly = true;

        var metadata = filesystem.GetMetadata("/file");
        metadata["a"] = "changed";

        Assert.Equal("1", filesystem.GetMetadata("/file")["a"]);
        Assert.Throws<InvalidOperationException>(() =>
            filesystem.SetMetadata("/file", new Dictionary<string, string> { ["a"] = "changed" }));
    }

    private static string ReadText(IFilesystem filesystem, string path) {
        using var stream = filesystem.LoadReadStream(path, new());
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class CountingFilesystem : FilesystemMem {
        public CountingFilesystem() : base(false, false) { }

        public int SyncCalls { get; private set; }
        public int SyncAsyncCalls { get; private set; }
        public string? SyncSource { get; private set; }
        public string? SyncDestination { get; private set; }
        public string? SyncAsyncSource { get; private set; }
        public string? SyncAsyncDestination { get; private set; }

        public override void Sync(
            string source,
            string destination,
            SyncSettings syncSettings,
            ILogger<IFilesystem> logger) {
            SyncCalls++;
            SyncSource = source;
            SyncDestination = destination;
        }

        public override Task SyncAsync(
            string source,
            string destination,
            SyncSettings syncSettings,
            ILogger<IFilesystem> logger,
            CancellationToken cancellationToken) {
            SyncAsyncCalls++;
            SyncAsyncSource = source;
            SyncAsyncDestination = destination;
            return Task.CompletedTask;
        }
    }
}
