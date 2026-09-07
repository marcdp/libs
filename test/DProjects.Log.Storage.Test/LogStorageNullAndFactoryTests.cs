using DProjects.Factories;
using DProjects.Fs;
using DProjects.Log.Storage.Serializers;

namespace DProjects.Log.Storage.Tests {

    public class LogStorageNullAndFactoryTests {

        // methods
        [Fact]
        public async Task NullStorage_HasEmptyNoOpSemantics() {
            using var storage = new LogStorageNull();

            var stats = await storage.GetStatsAsync(TestContext.Current.CancellationToken);
            await storage.RemoveBeforeAsync(1, TestContext.Current.CancellationToken);

            Assert.Equal(0, stats.Files);
            Assert.Equal(0, stats.Directories);
            Assert.Equal(0, stats.Size);
            Assert.Empty(await LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
            Assert.Empty(await LogStorageTestUtils.CollectAsync(storage.TailAsync(10, true, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task NullStorage_ValidatesArgumentsAndCancellation() {
            using var storage = new LogStorageNull();
            using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            source.Cancel();

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => storage.RemoveBeforeAsync(-1, TestContext.Current.CancellationToken));
            Assert.Throws<ArgumentOutOfRangeException>(() => storage.TailAsync(-1, false, TestContext.Current.CancellationToken));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => storage.GetStatsAsync(source.Token));
        }
        [Fact]
        public void FsFileFactory_DefaultsToAutomaticDeserialization() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            var deserializers = new CapturingDeserializerFactory();
            var factory = new LogStorageFsFileFactory(filesystem, deserializers);

            using var storage = factory.Create("fs-file:///logs/app.log");

            Assert.Equal("auto", deserializers.LastSource);
        }
        [Fact]
        public async Task FsDirFactory_FilePatternControlsSelection() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/application-1.log", "included\n");
            filesystem.SaveText("/logs/other.log", "excluded\n");
            var factory = new LogStorageFsDirFactory(filesystem, new CapturingDeserializerFactory());
            using var storage = factory.Create("fs-dir:///logs?format=raw&filePattern=application-*.log");

            var entries = await LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

            Assert.Equal(["included"], entries.Select(entry => entry.Message));
        }

        private sealed class CapturingDeserializerFactory : IFactoryByUrl<ILogStorageEntryDeserializer> {

            // props
            public string? LastSource { get; private set; }

            // methods
            public ILogStorageEntryDeserializer Create(string url) {
                LastSource = url;
                return url == "raw" ? new LogStorageEntryDeserializerRaw() : new LogStorageEntryDeserializerAuto();
            }
        }
    }
}
