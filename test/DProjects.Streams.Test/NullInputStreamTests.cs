using Xunit;
using System.IO;
using System.Threading;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class NullInputStreamTests
    {
        [Fact()]
        public async Task SupportsReadingAtEndOfStreamAndFlushing()
        {
            using Stream stream = new NullInputStream();
            var buffer = new byte[1024];

            Assert.True(stream.CanRead);
            Assert.False(stream.CanWrite);
            Assert.False(stream.CanSeek);
            Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));
            Assert.Equal(0, stream.Read(buffer, 0, buffer.Length));
            Assert.Equal(0, await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));
            Assert.Equal(0, await stream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None));

            stream.Flush();
            await stream.FlushAsync(CancellationToken.None);
        }

        [Fact()]
        public async Task RejectsWritingAndSeeking()
        {
            using Stream stream = new NullInputStream();
            var buffer = new byte[1];

            Assert.Throws<NotSupportedException>(() => stream.Length);
            Assert.Throws<NotSupportedException>(() => stream.Position);
            Assert.Throws<NotSupportedException>(() => stream.Position = 0);
            Assert.Throws<NotSupportedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<NotSupportedException>(() => stream.SetLength(0));
            Assert.Throws<NotSupportedException>(() => stream.Write(buffer, 0, buffer.Length));
            await Assert.ThrowsAsync<NotSupportedException>(async () =>
                await stream.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None));
        }
    }
}
