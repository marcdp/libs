using Xunit;
using System.IO;
using System.Threading;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class FoldedOutputStreamTest {


        [Theory()]
        [InlineData("abcdefgh12345678", 4, "abcd\nefgh\n1234\n5678\n")]
        [InlineData("abcdefgh1234567890", 4, "abcd\nefgh\n1234\n5678\n90")]
        [InlineData("abc", 4, "abc")]
        [InlineData("abcd", 4, "abcd\n")]
        public void WriteTest(string input, int size, string expected) {
            using (var ms = new MemoryStream()) {
                //write
                using (var folded = new FoldedOutputStream(ms, size)) {
                    var buffer = System.Text.Encoding.UTF8.GetBytes(input);
                    folded.Write(buffer, 0, buffer.Length);
                }
                using (var reader = new StreamReader(new MemoryStream(ms.ToArray()))) {
                    var output = reader.ReadToEnd();
                    Assert.Equal(expected, output);
                }
            }
        }

        [Fact]
        public async Task WriteAsyncProducesFoldedOutput() {
            using var stream = new MemoryStream();
            await using (var folded = new FoldedOutputStream(stream, 4, true)) {
                var buffer = System.Text.Encoding.UTF8.GetBytes("abcdefgh12");
                await folded.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);
            }

            Assert.Equal("abcd\nefgh\n12", System.Text.Encoding.UTF8.GetString(stream.ToArray()));
        }

        [Fact]
        public void ReportsCapabilitiesAndRejectsUnsupportedOperations() {
            using var destination = new MemoryStream();
            using Stream stream = new FoldedOutputStream(destination, leaveOpen: true);
            var buffer = new byte[1];

            Assert.False(stream.CanRead);
            Assert.True(stream.CanWrite);
            Assert.False(stream.CanSeek);
            Assert.Throws<NotSupportedException>(() => stream.Length);
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
            Assert.Throws<NotSupportedException>(() => stream.Read(buffer, 0, buffer.Length));
        }

        [Fact]
        public void FlushReachesUnderlyingStream() {
            using var destination = new TrackingMemoryStream();
            using var stream = new FoldedOutputStream(destination, leaveOpen: true);

            stream.Flush();

            Assert.True(destination.WasFlushed);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public void DisposeHonorsLeaveOpen(bool leaveOpen, bool expectedDisposed) {
            var destination = new TrackingMemoryStream();
            var stream = new FoldedOutputStream(destination, leaveOpen: leaveOpen);

            stream.Dispose();

            Assert.Equal(expectedDisposed, destination.WasDisposed);
            destination.Dispose();
        }

        private sealed class TrackingMemoryStream : MemoryStream {
            public bool WasFlushed { get; private set; }
            public bool WasDisposed { get; private set; }

            public override void Flush() {
                WasFlushed = true;
                base.Flush();
            }

            protected override void Dispose(bool disposing) {
                WasDisposed = true;
                base.Dispose(disposing);
            }
        }
    }
}
