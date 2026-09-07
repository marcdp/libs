using DProjects.Fs;
using DProjects.Log.Storage.Serializers;

namespace DProjects.Log.Storage.Tests {

    public class LogStorageFsDirTests {

        // methods
        [Fact]
        public async Task QueryAsync_UsesPatternDeterministicPathOrderAndLineOrder() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/z.log", "z1\nz2\n");
            filesystem.SaveText("/logs/a.log", "a1\na2\n");
            filesystem.SaveText("/logs/unrelated.log", "excluded\n");
            using var storage = new LogStorageFsDir(filesystem, "/logs", "?.log", ".ignored", false, new LogStorageEntryDeserializerRaw());

            var entries = await LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

            Assert.Equal(["a1", "a2", "z1", "z2"], entries.Select(entry => entry.Message));
        }
        [Fact]
        public async Task QueryAsync_RespectsRecursiveSetting() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.CreateDirectory("/logs/child");
            filesystem.SaveText("/logs/root.log", "root\n");
            filesystem.SaveText("/logs/child/nested.log", "nested\n");
            using var flat = new LogStorageFsDir(filesystem, "/logs", "*.log", ".log", false, new LogStorageEntryDeserializerRaw());
            using var recursive = new LogStorageFsDir(filesystem, "/logs", "*.log", ".log", true, new LogStorageEntryDeserializerRaw());

            Assert.Equal(["root"], (await LogStorageTestUtils.CollectAsync(flat.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)).Select(entry => entry.Message));
            Assert.Equal(["nested", "root"], (await LogStorageTestUtils.CollectAsync(recursive.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken)).Select(entry => entry.Message));
        }
        [Fact]
        public async Task GetStatsAsync_UsesSelectedFilesAndTimestampExtrema() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/b.log", "bbbb");
            filesystem.SaveText("/logs/a.log", "aa");
            filesystem.SaveText("/logs/other.txt", "ignored");
            var old = DateTime.Now.AddDays(-10);
            var recent = DateTime.Now.AddDays(-2);
            filesystem.Touch("/logs/a.log", recent);
            filesystem.Touch("/logs/b.log", old);
            var a = filesystem.GetEntry("/logs/a.log")!;
            var b = filesystem.GetEntry("/logs/b.log")!;
            using var storage = new LogStorageFsDir(filesystem, "/logs", "*.log", ".ignored", false, new LogStorageEntryDeserializerRaw());

            var stats = await storage.GetStatsAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, stats.Files);
            Assert.Equal(1, stats.Directories);
            Assert.Equal(6, stats.Size);
            Assert.Equal(new[] { a.Created, b.Created }.Min(), stats.From);
            Assert.Equal(recent, stats.To);
        }
        [Fact]
        public async Task GetStatsAsync_EmptySelectionHasNullTimestamps() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            using var storage = new LogStorageFsDir(filesystem, "/logs", "*.log", ".log", false, new LogStorageEntryDeserializerRaw());

            var stats = await storage.GetStatsAsync(TestContext.Current.CancellationToken);

            Assert.Equal(0, stats.Files);
            Assert.Null(stats.From);
            Assert.Null(stats.To);
        }
        [Fact]
        public async Task RemoveBeforeAsync_DeletesOnlyMatchingOldFiles() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/old-app.log", "old");
            filesystem.SaveText("/logs/new-app.log", "new");
            filesystem.SaveText("/logs/unrelated.log", "unrelated");
            filesystem.Touch("/logs/old-app.log", DateTime.Now.AddDays(-20));
            filesystem.Touch("/logs/new-app.log", DateTime.Now);
            filesystem.Touch("/logs/unrelated.log", DateTime.Now.AddDays(-20));
            using var storage = new LogStorageFsDir(filesystem, "/logs", "*-app.log", ".log", false, new LogStorageEntryDeserializerRaw());

            await storage.RemoveBeforeAsync(10, TestContext.Current.CancellationToken);

            Assert.False(filesystem.Exists("/logs/old-app.log"));
            Assert.True(filesystem.Exists("/logs/new-app.log"));
            Assert.True(filesystem.Exists("/logs/unrelated.log"));
        }
        [Fact]
        public async Task RemoveBeforeAsync_RejectsNegativeDays() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            using var storage = new LogStorageFsDir(filesystem, "/logs", "*.log", ".log", false, new LogStorageEntryDeserializerRaw());

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => storage.RemoveBeforeAsync(-1, TestContext.Current.CancellationToken));
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TailAsync_IsUnsupported(bool follow) {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            using var storage = new LogStorageFsDir(filesystem, "/logs", "*.log", ".log", false, new LogStorageEntryDeserializerRaw());

            await Assert.ThrowsAsync<NotSupportedException>(() => LogStorageTestUtils.CollectAsync(storage.TailAsync(1, follow, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task MissingDirectory_UsesDirectoryNotFoundException() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            using var storage = new LogStorageFsDir(filesystem, "/missing", "*.log", ".log", false, new LogStorageEntryDeserializerRaw());

            await Assert.ThrowsAsync<DirectoryNotFoundException>(() => storage.GetStatsAsync(TestContext.Current.CancellationToken));
        }
        [Fact]
#pragma warning disable xUnit1051 // this test deliberately supplies a pre-canceled token
        public async Task Enumeration_ObservesCancellation() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/app.log", "record\n");
            using var storage = new LogStorageFsDir(filesystem, "/logs", "*.log", ".log", false, new LogStorageEntryDeserializerRaw());
            using var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.GetStatsAsync(source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), source.Token), source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.RemoveBeforeAsync(1, source.Token));
        }
#pragma warning restore xUnit1051
    }
}
