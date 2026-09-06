using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class InputOutputStreamContractTests
    {
        [Fact]
        public async Task InputStreamRejectsUnsupportedOperations()
        {
            Stream stream = new TestInputStream();
            var buffer = new byte[1];

            Assert.True(stream.CanRead);
            Assert.False(stream.CanWrite);
            Assert.False(stream.CanSeek);
            Assert.Throws<NotSupportedException>(() => stream.Length);
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
            Assert.Throws<NotSupportedException>(() => stream.Write(buffer, 0, buffer.Length));
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await stream.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None));
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2022:Avoid inexact read with Stream.ReadAsync", Justification = "Verifies that unsupported reads are rejected.")]
        public async Task OutputStreamRejectsUnsupportedOperations()
        {
            Stream stream = new TestOutputStream();
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
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
        }

        private sealed class TestInputStream : InputStream
        {
            public override int Read(byte[] buffer, int offset, int count) => 0;
        }

        private sealed class TestOutputStream : OutputStream
        {
            public override void Flush()
            {
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
            }
        }
    }
}
