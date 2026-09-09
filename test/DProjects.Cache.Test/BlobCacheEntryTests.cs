using DProjects.Utils;

namespace DProjects.Cache.Tests {

    public class BlobCacheEntryTests {

        // methods
        [Fact]
        public void Constructor_PreservesKeyStreamAndSuppliedHeaders() {
            var stream = new MemoryStream([1, 2, 3]);
            var headers = new HeadersUtils.Headers();
            headers.Set("X-Cache-Test", "retained");

            using var entry = new BlobCacheEntry("cache-key", stream, headers);

            Assert.Equal("cache-key", entry.Key);
            Assert.Same(stream, entry.Stream);
            Assert.Same(headers, entry.Headers);
            Assert.Equal("retained", entry.Headers.Get<string?>("X-Cache-Test", null));
        }
        [Fact]
        public void Constructor_CreatesUsableHeadersWhenNoneAreSupplied() {
            using var entry = new BlobCacheEntry("cache-key", new MemoryStream());

            entry.Headers.Set("X-Cache-Test", "value");

            Assert.Equal("value", entry.Headers.Get<string?>("X-Cache-Test", null));
        }
        [Fact]
        public void TypedMetadata_RoundTripsThroughHeaders() {
            var expires = new DateTime(2030, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            var modified = new DateTime(2029, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            var date = new DateTime(2028, 3, 4, 5, 6, 7, DateTimeKind.Utc);
            using var entry = new BlobCacheEntry("cache-key", new MemoryStream()) {
                ContentType = "application/octet-stream",
                ContentLength = 123456789L,
                Etag = "etag-value",
                Expires = expires,
                LastModified = modified,
                Date = date
            };

            Assert.Equal("application/octet-stream", entry.ContentType);
            Assert.Equal(123456789L, entry.ContentLength);
            Assert.Equal("etag-value", entry.Etag);
            Assert.Equal(expires, entry.Expires!.Value.ToUniversalTime());
            Assert.Equal(modified, entry.LastModified!.Value.ToUniversalTime());
            Assert.Equal(date, entry.Date!.Value.ToUniversalTime());
        }
        [Fact]
        public void Dispose_DisposesOwnedStream() {
            var stream = new TrackingMemoryStream();
            var entry = new BlobCacheEntry("cache-key", stream);

            entry.Dispose();

            Assert.True(stream.IsDisposed);
        }

        private sealed class TrackingMemoryStream : MemoryStream {

            // props
            public bool IsDisposed { get; private set; }

            // methods
            protected override void Dispose(bool disposing) {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}