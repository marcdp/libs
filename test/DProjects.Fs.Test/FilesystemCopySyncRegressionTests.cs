using DProjects.Fs.Extensions;
using DProjects.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text;

namespace DProjects.Fs.Test;

public class FilesystemCopySyncRegressionTests {
    private static readonly NullLogger<IFilesystem> Logger = NullLogger<IFilesystem>.Instance;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyFileToMissingPath(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/file.txt", "source");

        await Copy(filesystem, "/source/file.txt", "/destination/file.txt", new(), useAsync);

        Assert.Equal("source", ReadText(filesystem, "/destination/file.txt"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CopyFileOverExistingFileRespectsOverwrite(bool overwrite, bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/file.txt", "source");
        SaveText(filesystem, "/destination/file.txt", "destination");

        await Copy(filesystem, "/source/file.txt", "/destination/file.txt", new() { Overwrite = overwrite }, useAsync);

        Assert.Equal(overwrite ? "source" : "destination", ReadText(filesystem, "/destination/file.txt"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyFileIntoDirectory(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source.txt", "source");
        filesystem.CreateDirectory("/target");

        await Copy(filesystem, "/source.txt", "/target", new(), useAsync);

        Assert.Equal("source", ReadText(filesystem, "/target/source.txt"));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task CopyFileIntoDirectoryRespectsOverwriteAtEffectiveDestination(bool overwrite, bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source.txt", "source");
        SaveText(filesystem, "/target/source.txt", "destination");

        await Copy(filesystem, "/source.txt", "/target", new() { Overwrite = overwrite }, useAsync);

        Assert.Equal(overwrite ? "source" : "destination", ReadText(filesystem, "/target/source.txt"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyFileIntoDirectoryRejectsSameNameDirectory(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source.txt", "source");
        filesystem.CreateDirectory("/target/source.txt");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Copy(filesystem, "/source.txt", "/target", new(), useAsync));

        Assert.True(filesystem.ExistsDirectory("/target/source.txt"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyDirectoryOntoFileIsRejected(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/nested/file.txt", "source");
        SaveText(filesystem, "/destination/x", "destination");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Copy(filesystem, "/source", "/destination/x", new(), useAsync));

        Assert.Equal("destination", ReadText(filesystem, "/destination/x"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CopyDirectoryRecursivelyCopiesNestedEntries(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/nested/file.txt", "source");

        await Copy(filesystem, "/source", "/destination", new() { Recursive = true }, useAsync);

        Assert.True(filesystem.ExistsDirectory("/destination/nested"));
        Assert.Equal("source", ReadText(filesystem, "/destination/nested/file.txt"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LeftToRightSyncReplacesDestinationDirectoryWithSourceFile(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/x", "source");
        SaveText(filesystem, "/destination/x/old.txt", "old");

        await Sync(filesystem, SyncModes.LeftToRight, useAsync);

        Assert.Equal("source", ReadText(filesystem, "/destination/x"));
        Assert.False(filesystem.Exists("/destination/x/old.txt"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LeftToRightSyncReplacesDestinationFileWithSourceDirectory(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/x/file.txt", "source");
        SaveText(filesystem, "/destination/x", "destination");

        await Sync(filesystem, SyncModes.LeftToRight, useAsync);

        Assert.True(filesystem.ExistsDirectory("/destination/x"));
        Assert.Equal("source", ReadText(filesystem, "/destination/x/file.txt"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BidirectionalSyncReplacesLosingDirectoryWithWinningFile(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/x", "source");
        SaveText(filesystem, "/destination/x/old.txt", "old");
        filesystem.Touch("/source/x", new DateTime(2025, 1, 2));
        filesystem.Touch("/destination/x", new DateTime(2025, 1, 1));

        await Sync(filesystem, SyncModes.Bidirectional, useAsync);

        Assert.Equal("source", ReadText(filesystem, "/destination/x"));
        Assert.False(filesystem.Exists("/destination/x/old.txt"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task BidirectionalSyncReplacesLosingFileWithWinningDirectory(bool useAsync) {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/x", "source");
        SaveText(filesystem, "/destination/x/file.txt", "destination");
        filesystem.Touch("/source/x", new DateTime(2025, 1, 1));
        filesystem.Touch("/destination/x", new DateTime(2025, 1, 2));

        await Sync(filesystem, SyncModes.Bidirectional, useAsync);

        Assert.True(filesystem.ExistsDirectory("/source/x"));
        Assert.Equal("destination", ReadText(filesystem, "/source/x/file.txt"));
    }

    [Fact]
    public async Task CopyAsyncHonorsPreCanceledToken() {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/source/file.txt", "source");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            filesystem.CopyAsync("/source", "/destination", new(), Logger, cancellation.Token));
    }

    [Fact]
    public async Task SyncAsyncHonorsPreCanceledToken() {
        using var filesystem = CreateFilesystem();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            filesystem.SyncAsync("/source", "/destination", new(), Logger, cancellation.Token));
    }

    private static FilesystemMem CreateFilesystem() {
        var filesystem = new FilesystemMem(false, false);
        filesystem.CreateDirectory("/source");
        filesystem.CreateDirectory("/destination");
        filesystem.CreateDirectory("/status");
        return filesystem;
    }

    private static async Task Copy(
        IFilesystem filesystem,
        string source,
        string destination,
        CopySettings settings,
        bool useAsync) {
        if (useAsync) {
            await filesystem.CopyAsync(source, destination, settings, Logger, TestContext.Current.CancellationToken);
        } else {
            filesystem.Copy(source, destination, settings, Logger);
        }
    }

    private static async Task Sync(IFilesystem filesystem, SyncModes mode, bool useAsync) {
        var settings = new SyncSettings { Mode = mode, StatusPath = "/status" };
        if (useAsync) {
            await filesystem.SyncAsync(
                "/source",
                "/destination",
                settings,
                Logger,
                TestContext.Current.CancellationToken);
        } else {
            filesystem.Sync("/source", "/destination", settings, Logger);
        }
    }

    private static void SaveText(IFilesystem filesystem, string path, string content) {
        var parent = PathUtils.GetPathParent(path);
        if (!filesystem.ExistsDirectory(parent)) {
            filesystem.CreateDirectory(parent);
        }
        filesystem.SaveTextFile(path, content, Encoding.UTF8);
    }

    private static string ReadText(IFilesystem filesystem, string path) {
        return filesystem.LoadTextFile(path, Encoding.UTF8);
    }
}
