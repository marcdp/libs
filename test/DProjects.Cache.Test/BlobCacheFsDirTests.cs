using System.Text;

using DProjects.Fs;
using DProjects.Fs.Extensions;
using DProjects.Utils;

using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Cache.Tests {

    public class BlobCacheFsDirTests {

        // methods
        [Fact]
        public void SetGet_RoundTripsContentKeyAndCallerMetadata() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            var expires = FutureDate();
            var modified = new DateTime(2026, 8, 9, 10, 11, 12, DateTimeKind.Utc);
            using var source = CreateEntry("round-trip", "known payload", entry => {
                entry.ContentType = "text/plain";
                entry.Etag = "caller-etag";
                entry.Expires = expires;
                entry.LastModified = modified;
                entry.Headers.Set("X-Cache-Test", "retained");
            });

            cache.Set(source);
            using var result = cache.Get("round-trip");

            Assert.NotNull(result);
            Assert.Equal("round-trip", result.Key);
            Assert.Equal("known payload", ReadText(result));
            Assert.Equal(13, result.ContentLength);
            Assert.Equal("text/plain", result.ContentType);
            Assert.Equal("caller-etag", result.Etag);
            Assert.Equal(expires, result.Expires!.Value.ToUniversalTime());
            Assert.Equal(modified, result.LastModified!.Value.ToUniversalTime());
            Assert.Equal("retained", result.Headers.Get<string?>("X-Cache-Test", null));
        }
        [Fact]
        public void Set_AddsContentLengthDateAndDefaultExpiration() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            var before = DateTime.UtcNow;
            using var source = CreateEntry("automatic-metadata", "123456789");

            cache.Set(source);
            var after = DateTime.UtcNow;
            using var result = cache.Get("automatic-metadata");

            Assert.NotNull(result);
            Assert.Equal(9, result.ContentLength);
            Assert.NotNull(result.Date);
            Assert.InRange(result.Date.Value.ToUniversalTime(), before.AddSeconds(-1), after.AddSeconds(1));
            Assert.NotNull(result.Expires);
            Assert.InRange(result.Expires.Value.ToUniversalTime(), before.AddMinutes(59), after.AddMinutes(61));
        }
        [Fact]
        public void Set_PreservesExplicitExpiration() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            var expires = FutureDate();
            using var source = CreateEntry("explicit-expiration", "payload", entry => entry.Expires = expires);

            cache.Set(source);
            using var result = cache.Get("explicit-expiration");

            Assert.NotNull(result);
            Assert.Equal(expires, result.Expires!.Value.ToUniversalTime());
        }
        [Fact]
        public async Task MissingKey_ReturnsNullForSyncAndAsyncGet() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);

            Assert.Null(cache.Get("missing"));
            Assert.Null(await cache.GetAsync("missing", TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task Replacement_CompletelyReplacesPayloadForSyncAndAsyncSet() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            using (var first = CreateEntry("sync-replacement", "a much longer stale payload")) cache.Set(first);
            using (var second = CreateEntry("sync-replacement", "new")) cache.Set(second);

            using (var result = cache.Get("sync-replacement")) {
                Assert.NotNull(result);
                Assert.Equal("new", ReadText(result));
                Assert.Equal(3, result.ContentLength);
            }

            using (var first = CreateEntry("async-replacement", "another stale payload")) {
                await cache.SetAsync(first, TestContext.Current.CancellationToken);
            }
            using (var second = CreateEntry("async-replacement", "fresh")) {
                await cache.SetAsync(second, TestContext.Current.CancellationToken);
            }
            using var asyncResult = await cache.GetAsync("async-replacement", TestContext.Current.CancellationToken);
            Assert.NotNull(asyncResult);
            Assert.Equal("fresh", await ReadTextAsync(asyncResult, TestContext.Current.CancellationToken));
            Assert.Equal(5, asyncResult.ContentLength);
        }
        [Theory]
        [InlineData("key with spaces")]
        [InlineData("path/segment")]
        [InlineData("query?value:part")]
        [InlineData("Málaga-日本語")]
        public void FilesystemUnsafeKeys_RoundTripWithoutEscapingCacheDirectory(string key) {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            using var source = CreateEntry(key, "payload for " + key);

            cache.Set(source);
            using var result = cache.Get(key);

            Assert.NotNull(result);
            Assert.Equal(key, result.Key);
            Assert.Equal("payload for " + key, ReadText(result));
            Assert.Single(filesystem.GetEntries("/cache", GetModes.Files));
            Assert.Empty(filesystem.GetEntries("/", GetModes.Files));
        }
        [Fact]
        public void ExpiredEntry_GetReturnsNullAndRemovesStoredObject() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            using var source = CreateEntry("expired-sync", "expired", entry => entry.Expires = DateTime.UtcNow.AddMinutes(-1));
            cache.Set(source);
            Assert.Single(filesystem.GetEntries("/cache", GetModes.Files));

            Assert.Null(cache.Get("expired-sync"));

            Assert.Empty(filesystem.GetEntries("/cache", GetModes.Files));
        }
        [Fact]
        public async Task ExpiredEntry_GetAsyncReturnsNullAndRemovesStoredObject() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            using var source = CreateEntry("expired-async", "expired", entry => entry.Expires = DateTime.UtcNow.AddMinutes(-1));
            await cache.SetAsync(source, TestContext.Current.CancellationToken);
            Assert.Single(filesystem.GetEntries("/cache", GetModes.Files));

            Assert.Null(await cache.GetAsync("expired-async", TestContext.Current.CancellationToken));

            Assert.Empty(filesystem.GetEntries("/cache", GetModes.Files));
        }
        [Fact]
        public async Task FutureExpiration_RemainsRetrievableThroughSyncAndAsyncGet() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            using var source = CreateEntry("valid", "still valid", entry => entry.Expires = FutureDate());
            cache.Set(source);

            using var syncResult = cache.Get("valid");
            using var asyncResult = await cache.GetAsync("valid", TestContext.Current.CancellationToken);

            Assert.NotNull(syncResult);
            Assert.NotNull(asyncResult);
            Assert.Equal("still valid", ReadText(syncResult));
            Assert.Equal("still valid", await ReadTextAsync(asyncResult, TestContext.Current.CancellationToken));
        }
        [Fact]
        public async Task MissingExpiration_IsTreatedAsInvalidAndRemoved() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            SaveRawBlob(filesystem, "/cache/missing-expiration.blob", new HeadersUtils.Headers(), "payload");

            Assert.Null(await cache.GetAsync("missing-expiration", TestContext.Current.CancellationToken));

            Assert.False(filesystem.ExistsFile("/cache/missing-expiration.blob"));
        }
        [Fact]
        public async Task Clean_RemovesAllExpiredEntriesAndPreservesValidAndUnrelatedFiles() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            using (var expiredA = CreateEntry("expired-a", "a", entry => entry.Expires = DateTime.UtcNow.AddHours(-2))) cache.Set(expiredA);
            using (var valid = CreateEntry("valid", "valid", entry => entry.Expires = FutureDate())) cache.Set(valid);
            using (var expiredB = CreateEntry("expired-b", "b", entry => entry.Expires = DateTime.UtcNow.AddHours(-1))) cache.Set(expiredB);
            filesystem.SaveTextFile("/cache/unrelated.txt", "unrelated", Encoding.UTF8);

            await cache.Clean(TestContext.Current.CancellationToken);

            Assert.Null(cache.Get("expired-a"));
            Assert.Null(cache.Get("expired-b"));
            using var validResult = cache.Get("valid");
            Assert.NotNull(validResult);
            Assert.Equal("valid", ReadText(validResult));
            Assert.Equal("unrelated", filesystem.LoadTextFile("/cache/unrelated.txt", Encoding.UTF8));
        }
        [Fact]
        public async Task Clean_DiscardsBlobWithoutExpiration() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            SaveRawBlob(filesystem, "/cache/corrupt.blob", new HeadersUtils.Headers(), "payload");

            await cache.Clean(TestContext.Current.CancellationToken);

            Assert.False(filesystem.ExistsFile("/cache/corrupt.blob"));
        }
        [Fact]
        public async Task Clean_FailsFastForInvalidExpirationWithoutDeletingTheEntry() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            var headers = new HeadersUtils.Headers();
            headers.Set(HttpUtils.HEADER_EXPIRES, "not-a-date");
            SaveRawBlob(filesystem, "/cache/invalid.blob", headers, "payload");

            await Assert.ThrowsAsync<FormatException>(() => cache.Clean(TestContext.Current.CancellationToken));

            Assert.True(filesystem.ExistsFile("/cache/invalid.blob"));
        }
        [Fact]
        public void GetOrCreate_OnMissInvokesFactoryOncePersistsValueAndReturnsUsableStream() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            var calls = 0;
            var factoryStream = new TrackingMemoryStream(Encoding.UTF8.GetBytes("created"));

            using var created = cache.Get("created-sync", TimeSpan.FromMinutes(20), () => {
                calls++;
                return new BlobCacheEntry("created-sync", factoryStream);
            });

            Assert.Equal(1, calls);
            Assert.True(factoryStream.IsDisposed);
            Assert.Equal("created", ReadText(created));
            Assert.InRange(created.Expires!.Value.ToUniversalTime(), DateTime.UtcNow.AddMinutes(19), DateTime.UtcNow.AddMinutes(21));
            using var persisted = cache.Get("created-sync", TimeSpan.FromMinutes(20), () => throw new InvalidOperationException("factory must not run on a hit"));
            Assert.Equal("created", ReadText(persisted));
            Assert.Equal(1, calls);
        }
        [Fact]
        public async Task GetOrCreateAsync_OnMissInvokesFactoryOncePersistsValueAndReturnsUsableStream() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            var calls = 0;
            var factoryStream = new TrackingMemoryStream(Encoding.UTF8.GetBytes("created async"));

            using var created = await cache.GetAsync("created-async", TimeSpan.FromMinutes(20), token => {
                Assert.Equal(TestContext.Current.CancellationToken, token);
                calls++;
                return Task.FromResult(new BlobCacheEntry("created-async", factoryStream));
            }, TestContext.Current.CancellationToken);

            Assert.Equal(1, calls);
            Assert.True(factoryStream.IsDisposed);
            Assert.Equal("created async", await ReadTextAsync(created, TestContext.Current.CancellationToken));
            Assert.InRange(created.Expires!.Value.ToUniversalTime(), DateTime.UtcNow.AddMinutes(19), DateTime.UtcNow.AddMinutes(21));
            using var persisted = await cache.GetAsync("created-async", TimeSpan.FromMinutes(20), _ => throw new InvalidOperationException("factory must not run on a hit"),
                TestContext.Current.CancellationToken);
            Assert.Equal("created async", await ReadTextAsync(persisted, TestContext.Current.CancellationToken));
            Assert.Equal(1, calls);
        }
        [Fact]
        public async Task RemoveAndRemoveAsync_AreIdempotentAndMakeEntriesUnavailable() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            using (var sync = CreateEntry("remove-sync", "sync")) cache.Set(sync);
            using (var asyncEntry = CreateEntry("remove-async", "async")) await cache.SetAsync(asyncEntry, TestContext.Current.CancellationToken);

            cache.Remove("remove-sync");
            cache.Remove("remove-sync");
            await cache.RemoveAsync("remove-async", TestContext.Current.CancellationToken);
            await cache.RemoveAsync("remove-async", TestContext.Current.CancellationToken);

            Assert.Null(cache.Get("remove-sync"));
            Assert.Null(await cache.GetAsync("remove-async", TestContext.Current.CancellationToken));
        }
        [Fact]
#pragma warning disable xUnit1051 // this test deliberately verifies propagation of a pre-canceled token
        public async Task AsyncFilesystemOperations_ObserveCancellation() {
            using var filesystem = CreateFilesystem();
            using var cache = CreateCache(filesystem);
            using var source = new CancellationTokenSource();
            source.Cancel();
            using var entry = CreateEntry("canceled", "payload");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.SetAsync(entry, source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.GetAsync("canceled", source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.RemoveAsync("canceled", source.Token));
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.Clean(source.Token));
        }
#pragma warning restore xUnit1051
        [Fact]
#pragma warning disable xUnit1051 // this test controls cancellation at the payload-copy boundary
        public async Task SetAsync_PropagatesCancellationDuringPayloadCopy() {
            using var inner = CreateFilesystem();
            using var source = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
            using var filesystem = new CancelDuringPayloadCopyFilesystem(inner, source);
            using var cache = CreateCache(filesystem);
            using var entry = CreateEntry("cancel-during-copy", "payload");

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cache.SetAsync(entry, source.Token));

            Assert.False(inner.ExistsFile("/cache/cancel-during-copy.blob"));
        }
#pragma warning restore xUnit1051

        // methods (private)
        private static FilesystemMem CreateFilesystem() {
            var filesystem = new FilesystemMem(false, false);
            filesystem.CreateDirectory("/cache");
            return filesystem;
        }
        private static BlobCacheFsDir CreateCache(IFilesystem filesystem) {
            return new BlobCacheFsDir(filesystem, "/cache", NullLogger<IFilesystem>.Instance);
        }
        private static BlobCacheEntry CreateEntry(string key, string content, Action<BlobCacheEntry>? configure = null) {
            var entry = new BlobCacheEntry(key, new MemoryStream(Encoding.UTF8.GetBytes(content)));
            configure?.Invoke(entry);
            return entry;
        }
        private static string ReadText(BlobCacheEntry entry) {
            using var copy = new MemoryStream();
            entry.Stream.CopyTo(copy);
            return Encoding.UTF8.GetString(copy.ToArray());
        }
        private static async Task<string> ReadTextAsync(BlobCacheEntry entry, CancellationToken cancellationToken) {
            using var copy = new MemoryStream();
            await entry.Stream.CopyToAsync(copy, cancellationToken);
            return Encoding.UTF8.GetString(copy.ToArray());
        }
        private static DateTime FutureDate() {
            var value = DateTime.UtcNow.AddHours(2);
            return new DateTime(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, DateTimeKind.Utc);
        }
        private static void SaveRawBlob(IFilesystem filesystem, string path, HeadersUtils.Headers headers, string content) {
            using var stream = new MemoryStream();
            HeadersUtils.WriteHttpHeaders(headers, stream);
            var bytes = Encoding.UTF8.GetBytes(content);
            stream.Write(bytes, 0, bytes.Length);
            stream.Position = 0;
            filesystem.SaveFile(path, stream, new());
        }

        private sealed class TrackingMemoryStream(byte[] buffer) : MemoryStream(buffer) {

            // props
            public bool IsDisposed { get; private set; }

            // methods
            protected override void Dispose(bool disposing) {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }
        private sealed class CancelDuringPayloadCopyFilesystem(IFilesystem inner, CancellationTokenSource source) : FilesystemAsync(false) {

            // props
            public override string Url => inner.Url;

            // methods
            public override Task<Entry?> GetEntryAsync(string path, CancellationToken cancellationToken) {
                return inner.GetEntryAsync(path, cancellationToken);
            }
            public override IAsyncEnumerable<Entry> GetEntriesAsync(string path, GetModes mode = GetModes.All, string? pattern = null,
                CancellationToken cancellationToken = default) {
                return inner.GetEntriesAsync(path, mode, pattern, cancellationToken);
            }
            public override Task<Stream> LoadReadStreamAsync(string path, LoadReadStreamSettings settings, CancellationToken cancellationToken) {
                if (path.EndsWith(".tmp", StringComparison.Ordinal)) source.Cancel();
                return inner.LoadReadStreamAsync(path, settings, CancellationToken.None);
            }
            public override Task<Entry> SaveFileAsync(string path, Stream stream, SaveFileSettings settings, CancellationToken cancellationToken) {
                return inner.SaveFileAsync(path, stream, settings, cancellationToken);
            }
            public override Task<Entry> CreateDirectoryAsync(string path, CancellationToken cancellationToken) {
                return inner.CreateDirectoryAsync(path, cancellationToken);
            }
            public override Task DeleteAsync(string path, CancellationToken cancellationToken) {
                return inner.DeleteAsync(path, cancellationToken);
            }
        }
    }
}