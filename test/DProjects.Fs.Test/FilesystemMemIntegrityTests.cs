using System.Text;

namespace DProjects.Fs.Test;

public class FilesystemMemIntegrityTests {

    [Fact]
    public void GetEntryReturnsSnapshot() {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/file", "abc");

        var first = filesystem.GetEntry("/file")!;
        Mutate(first);

        var second = filesystem.GetEntry("/file")!;
        Assert.Equal("/file", second.Path);
        Assert.Equal(EntryType.File, second.EntryType);
        Assert.Equal(3, second.Length);
        Assert.NotEqual("corrupted", second.Etag);
        Assert.Null(filesystem.GetEntry("/corrupted"));
    }

    [Theory]
    [InlineData(GetModes.All)]
    [InlineData(GetModes.Descendants)]
    public void GetEntriesReturnsSnapshots(GetModes mode) {
        using var filesystem = CreateFilesystem();
        filesystem.CreateDirectory("/directory");
        SaveText(filesystem, "/directory/file", "abc");

        var first = filesystem.GetEntries("/", mode).Single(entry => entry.Path == "/directory");
        Mutate(first);

        var second = filesystem.GetEntries("/", mode).Single(entry => entry.Path == "/directory");
        Assert.Equal("/directory", second.Path);
        Assert.Equal(EntryType.Directory, second.EntryType);
        Assert.Equal(0, second.Length);
        Assert.Equal("", second.Etag);
        Assert.Null(filesystem.GetEntry("/corrupted"));
    }

    [Fact]
    public void CreateProducesNonEmptyEtag() {
        using var filesystem = CreateFilesystem();

        var entry = SaveText(filesystem, "/file", "abc");

        Assert.NotEmpty(entry.Etag);
        Assert.Equal(entry.Etag, filesystem.GetEntry("/file")!.Etag);
    }

    [Fact]
    public void SameLengthOverwriteChangesEtag() {
        using var filesystem = CreateFilesystem();
        var first = SaveText(filesystem, "/file", "abc");

        var second = SaveText(filesystem, "/file", "xyz");

        Assert.NotEqual(first.Etag, second.Etag);
        Assert.Equal(second.Etag, filesystem.GetEntry("/file")!.Etag);
    }

    [Fact]
    public void AppendChangesEtagFromFinalState() {
        using var filesystem = CreateFilesystem();
        var first = SaveText(filesystem, "/file", "abc");

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("def"));
        var second = filesystem.SaveFile("/file", stream, new() { Append = true });

        Assert.NotEqual(first.Etag, second.Etag);
        Assert.Equal(6, second.Length);
        Assert.Equal(second.Etag, filesystem.GetEntry("/file")!.Etag);
    }

    [Fact]
    public void TouchFileChangesModifiedAndEtag() {
        using var filesystem = CreateFilesystem();
        var first = SaveText(filesystem, "/file", "abc");
        var modified = first.Modified.AddDays(1);

        filesystem.Touch("/file", modified);

        var second = filesystem.GetEntry("/file")!;
        Assert.Equal(modified, second.Modified);
        Assert.NotEqual(first.Etag, second.Etag);
    }

    [Fact]
    public void SnapshotCannotReplaceLatestEtag() {
        using var filesystem = CreateFilesystem();
        SaveText(filesystem, "/file", "abc");
        var snapshot = filesystem.GetEntry("/file")!;
        var latest = SaveText(filesystem, "/file", "xyz");

        snapshot.Etag = "corrupted";

        var current = filesystem.GetEntry("/file")!;
        Assert.Equal(latest.Etag, current.Etag);
        Assert.NotEqual("corrupted", current.Etag);
    }

    private static FilesystemMem CreateFilesystem() {
        return new FilesystemMem(false, false);
    }

    private static Entry SaveText(FilesystemMem filesystem, string path, string content) {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return filesystem.SaveFile(path, stream, new());
    }

    private static void Mutate(Entry entry) {
        entry.Path = "/corrupted";
        entry.EntryType = EntryType.Directory;
        entry.Length = 999999;
        entry.Modified = DateTime.MinValue;
        entry.Etag = "corrupted";
        entry.Flags = 12345;
    }
}
