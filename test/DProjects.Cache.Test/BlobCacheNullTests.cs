using System.Text;

namespace DProjects.Cache.Tests {

    public class BlobCacheNullTests {

        // methods
        [Fact]
        public async Task DirectOperations_HaveNoStorageNoOpSemantics() {
            using var cache = new BlobCacheNull();
            using var syncEntry = CreateEntry("sync", "sync payload");
            using var asyncEntry = CreateEntry("async", "async payload");

            cache.Set(syncEntry);
            await cache.SetAsync(asyncEntry, TestContext.Current.CancellationToken);
            cache.Remove("sync");
            await cache.RemoveAsync("async", TestContext.Current.CancellationToken);
            await cache.Clean(TestContext.Current.CancellationToken);

            Assert.Null(cache.Get("sync"));
            Assert.Null(await cache.GetAsync("async", TestContext.Current.CancellationToken));
        }
        [Fact]
        public void GetOrCreate_InvokesFactoryAndReturnsItsLiveEntry() {
            using var cache = new BlobCacheNull();
            var calls = 0;
            var produced = CreateEntry("sync", "fresh sync");

            using var result = cache.Get("sync", TimeSpan.FromMinutes(5), () => {
                calls++;
                return produced;
            });

            Assert.Equal(1, calls);
            Assert.Same(produced, result);
            Assert.Equal("fresh sync", ReadText(result));
            Assert.Null(cache.Get("sync"));
        }
        [Fact]
        public async Task GetOrCreateAsync_InvokesFactoryAndReturnsItsLiveEntry() {
            using var cache = new BlobCacheNull();
            var calls = 0;
            var produced = CreateEntry("async", "fresh async");

            using var result = await cache.GetAsync("async", TimeSpan.FromMinutes(5), token => {
                Assert.Equal(TestContext.Current.CancellationToken, token);
                calls++;
                return Task.FromResult(produced);
            }, TestContext.Current.CancellationToken);

            Assert.Equal(1, calls);
            Assert.Same(produced, result);
            Assert.Equal("fresh async", ReadText(result));
            Assert.Null(await cache.GetAsync("async", TestContext.Current.CancellationToken));
        }

        // methods (private)
        private static BlobCacheEntry CreateEntry(string key, string content) {
            return new BlobCacheEntry(key, new MemoryStream(Encoding.UTF8.GetBytes(content)));
        }
        private static string ReadText(BlobCacheEntry entry) {
            using var copy = new MemoryStream();
            entry.Stream.CopyTo(copy);
            return Encoding.UTF8.GetString(copy.ToArray());
        }
    }
}