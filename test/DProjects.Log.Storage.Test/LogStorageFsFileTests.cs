using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Log.Storage.Serializers;
using DProjects.Streams;
using System.Text;

namespace DProjects.Log.Storage.Tests {

    public class LogStorageFsFileTests {

        // methods
        [Fact]
        public async Task QueryAsync_ReturnsPhysicalOrderAndStats() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/app.log", "first\nsecond\nthird\n");
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem);

            var entries = await LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);
            var stats = await storage.GetStatsAsync(TestContext.Current.CancellationToken);

            Assert.Equal(["first", "second", "third"], entries.Select(entry => entry.Message));
            Assert.Equal(1, stats.Files);
            Assert.Equal(0, stats.Directories);
            Assert.Equal(Encoding.UTF8.GetByteCount("first\nsecond\nthird\n"), stats.Size);
            Assert.NotNull(stats.From);
            Assert.NotNull(stats.To);
        }
        [Fact]
        public async Task QueryAsync_MalformedRecordFailsEnumeration() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/app.log", "not-a-classic-record\n");
            using var storage = new LogStorageFsFile(filesystem, "/logs/app.log", new LogStorageEntryDeserializerClassic());

            await Assert.ThrowsAsync<FormatException>(() => LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task MissingFile_UsesFileNotFoundException() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem, "/logs/missing.log");

            await Assert.ThrowsAsync<FileNotFoundException>(() => storage.GetStatsAsync(TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<FileNotFoundException>(() => LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task DirectoryPath_UsesInvalidOperationException() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem, "/logs");

            await Assert.ThrowsAsync<InvalidOperationException>(() => storage.GetStatsAsync(TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() => LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
            await Assert.ThrowsAsync<InvalidOperationException>(() => LogStorageTestUtils.CollectAsync(storage.TailAsync(1, false, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
        }
        [Fact]
#pragma warning disable xUnit1051 // this test deliberately supplies a pre-canceled token
        public async Task QueryAsync_ObservesCancellation() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/app.log", string.Join("\n", Enumerable.Repeat("record", 10000)));
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem);
            using var source = new CancellationTokenSource();
            source.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), source.Token), source.Token));
        }
#pragma warning restore xUnit1051
        [Fact]
#pragma warning disable xUnit1051 // this test cancels deterministically after records have started processing
        public async Task QueryAsync_CancelsDuringRecordProcessing() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/app.log", string.Join("\n", Enumerable.Range(0, 100).Select(index => "record-" + index)) + "\n");
            using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            var deserializer = new CancelingDeserializer(source, 3);
            using var storage = new LogStorageFsFile(filesystem, "/logs/app.log", deserializer);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), source.Token), source.Token));

            Assert.Equal(3, deserializer.RecordsProcessed);
        }
#pragma warning restore xUnit1051
        [Fact]
        public async Task QueryAsync_DisposesStreamWhenConsumerStopsEarly() {
            using var filesystem = new TrackingFilesystem();
            filesystem.CreateDirectory("/logs");
            filesystem.SaveText("/logs/app.log", "first\nsecond\n");
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem);

            var enumerator = storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken).GetAsyncEnumerator(TestContext.Current.CancellationToken);
            Assert.True(await enumerator.MoveNextAsync());
            await enumerator.DisposeAsync();

            Assert.True(filesystem.ReadStreamDisposed);
        }
        [Fact]
        public async Task QueryAsync_DisposesStreamWhenDeserializationFails() {
            using var filesystem = new TrackingFilesystem();
            filesystem.CreateDirectory("/logs");
            filesystem.SaveText("/logs/app.log", "invalid\n");
            using var storage = new LogStorageFsFile(filesystem, "/logs/app.log", new LogStorageEntryDeserializerClassic());

            await Assert.ThrowsAsync<FormatException>(() => LogStorageTestUtils.CollectAsync(storage.QueryAsync(new LogStorageQuery(), TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));

            Assert.True(filesystem.ReadStreamDisposed);
        }
        [Fact]
        public async Task RemoveBeforeAsync_IsUnsupported() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem);

            await Assert.ThrowsAsync<NotSupportedException>(() => storage.RemoveBeforeAsync(1, TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task TailAsync_ReturnsFinalRecordsInOrderForLargeUtf8File() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            var prefix = Enumerable.Range(0, 20).Select(index => "prefix-" + index);
            var expected = Enumerable.Range(0, 4).Select(index => index + "-" + new string('界', 20000)).ToArray();
            filesystem.SaveText("/logs/app.log", string.Join("\n", prefix.Concat(expected)) + "\n");
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem);

            var entries = await LogStorageTestUtils.CollectAsync(storage.TailAsync(4, false, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

            Assert.Equal(expected, entries.Select(entry => entry.Message));
        }
        [Theory]
        [InlineData(0, 0)]
        [InlineData(2, 2)]
        [InlineData(10, 3)]
        public async Task TailAsync_HandlesZeroExactAndFewerRecords(int lines, int expectedCount) {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/app.log", "one\ntwo\nthree\n");
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem);

            var entries = await LogStorageTestUtils.CollectAsync(storage.TailAsync(lines, false, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken);

            Assert.Equal(expectedCount, entries.Count);
        }
        [Fact]
        public async Task TailAsync_RejectsNegativeLines() {
            using var filesystem = LogStorageTestUtils.CreateFilesystem();
            filesystem.SaveText("/logs/app.log", "one\n");
            using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem);

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => LogStorageTestUtils.CollectAsync(storage.TailAsync(-1, false, TestContext.Current.CancellationToken), TestContext.Current.CancellationToken));
        }
        [Fact]
#pragma warning disable xUnit1051 // this test needs a separately controlled timeout/cancellation token
        public async Task FollowAsync_YieldsSameFileAppendsAndCancelsPromptly() {
            var tempPath = Path.Combine(Path.GetTempPath(), "DProjects.Log.Storage.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempPath);
            try {
                using var filesystem = new FilesystemLocal(tempPath, false, true, false);
                filesystem.SaveTextFile("/app.log", "initial\n", Encoding.UTF8);
                using var storage = LogStorageTestUtils.CreateRawFileStorage(filesystem, "/app.log");
                using var source = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await using var enumerator = storage.TailAsync(1, true, source.Token).GetAsyncEnumerator(source.Token);

                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("initial", enumerator.Current.Message);
                filesystem.AppendFile("/app.log", "appended\n", Encoding.UTF8);
                Assert.True(await enumerator.MoveNextAsync());
                Assert.Equal("appended", enumerator.Current.Message);

                var pending = enumerator.MoveNextAsync().AsTask();
                source.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending);
            } finally {
                Directory.Delete(tempPath, true);
            }
        }
#pragma warning restore xUnit1051

        private sealed class TrackingFilesystem : FilesystemMem {

            // props
            public bool ReadStreamDisposed { get; private set; }

            // ctor
            public TrackingFilesystem() : base(false, false) {
            }

            // methods
            public override Stream LoadReadStream(string path, LoadReadStreamSettings settings) {
                return new DisposableStream(base.LoadReadStream(path, settings), () => ReadStreamDisposed = true);
            }
        }
        private sealed class CancelingDeserializer : ILogStorageEntryDeserializer {

            // vars
            private readonly CancellationTokenSource mCancellationTokenSource;
            private readonly int mCancelAfter;

            // props
            public int RecordsProcessed { get; private set; }

            // ctor
            public CancelingDeserializer(CancellationTokenSource cancellationTokenSource, int cancelAfter) {
                mCancellationTokenSource = cancellationTokenSource;
                mCancelAfter = cancelAfter;
            }

            // methods
            public LogEntry Deserialize(string line) {
                RecordsProcessed++;
                if (RecordsProcessed == mCancelAfter) mCancellationTokenSource.Cancel();
                return new LogEntry(LogLevel.Information, line) { Date = DateTime.MinValue };
            }
        }
    }
}
