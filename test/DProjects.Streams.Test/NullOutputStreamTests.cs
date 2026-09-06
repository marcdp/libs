using Xunit;
using System.IO;
using System.Threading;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class NullOutputStreamTests
    {
        [Fact()]
        public async Task SupportsWritingAndFlushing()
        {
            using Stream stream = new NullOutputStream();
            var buffer = new byte[1024];

            Assert.False(stream.CanRead);
            Assert.True(stream.CanWrite);
            Assert.False(stream.CanSeek);
            stream.Write(buffer, 0, buffer.Length);
            stream.Write(buffer, 0, buffer.Length);
            await stream.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);
            await stream.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None);

            stream.Flush();
            await stream.FlushAsync(CancellationToken.None);
        }

        [Fact()]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2022:Avoid inexact read with Stream.ReadAsync", Justification = "Verifies that unsupported reads are rejected.")]
        public async Task RejectsReadingAndSeeking()
        {
            using Stream stream = new NullOutputStream();
            var buffer = new byte[1];

            Assert.Throws<NotSupportedException>(() => stream.Length);
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
            Assert.Throws<NotSupportedException>(() => stream.Read(buffer, 0, buffer.Length));
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
        }
    }
}
