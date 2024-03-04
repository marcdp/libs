using Xunit;
using System.IO;
using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class ByteCounterInputStreamTests
    {
        [Fact()]
        public void ReadTest()
        {
            var input = "Hello world";
            var expectedCount = Encoding.UTF8.GetByteCount(input);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var counter = new ByteCounterInputStream(ms))
            {
                var buffer = new byte[1024];
                var bytesRead = counter.Read(buffer, 0, buffer.Length);

                Assert.Equal(expectedCount, counter.Count);
            }
        }

        [Fact()]
        public async void ReadAsyncTest()
        {
            var input = "Hello world";
            var expectedCount = Encoding.UTF8.GetByteCount(input);

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var counter = new ByteCounterInputStream(ms))
            {
                var buffer = new byte[1024];
                var bytesRead = await counter.ReadAsync(buffer, 0, buffer.Length);

                Assert.Equal(expectedCount, counter.Count);
            }
        }

        [Fact()]
        public void WriteTest()
        {
            var input = "Hello world";
            var expectedCount = Encoding.UTF8.GetByteCount(input);

            using (var ms = new MemoryStream())
            using (var counter = new ByteCounterInputStream(ms))
            {
                var buffer = Encoding.UTF8.GetBytes(input);
                counter.Write(buffer, 0, buffer.Length);

                Assert.Equal(expectedCount, counter.Count);
            }
        }

        [Fact()]
        public async void WriteAsyncTest()
        {
            var input = "Hello world";
            var expectedCount = Encoding.UTF8.GetByteCount(input);

            using (var ms = new MemoryStream())
            using (var counter = new ByteCounterInputStream(ms))
            {
                var buffer = Encoding.UTF8.GetBytes(input);
                await counter.WriteAsync(buffer, 0, buffer.Length);

                Assert.Equal(expectedCount, counter.Count);
            }
        }
    }
}
