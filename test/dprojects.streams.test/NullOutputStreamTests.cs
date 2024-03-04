using Xunit;
using System.IO;
using System.Threading;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class NullOutputStreamTests
    {
        [Fact()]
        public void WriteTest()
        {
            using (var nullStream = new NullOutputStream())
            {
                var buffer = new byte[1024];
                nullStream.Write(buffer, 0, buffer.Length); // Should not throw an exception
            }
        }

        [Fact()]
        public async void WriteAsyncTest()
        {
            using (var nullStream = new NullOutputStream())
            {
                var buffer = new byte[1024];
                await nullStream.WriteAsync(buffer, 0, buffer.Length, CancellationToken.None); // Should not throw an exception
            }
        }

        [Fact()]
        public void ReadTest()
        {
            using (var nullStream = new NullOutputStream())
            {
                var buffer = new byte[1024];
                var bytesRead = nullStream.Read(buffer, 0, buffer.Length);

                Assert.Equal(0, bytesRead);
            }
        }

        [Fact()]
        public async void ReadAsyncTest()
        {
            using (var nullStream = new NullOutputStream())
            {
                var buffer = new byte[1024];
                var bytesRead = await nullStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

                Assert.Equal(0, bytesRead);
            }
        }
    }
}
