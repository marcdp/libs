using Xunit;
using Xunit;
using System.IO;
using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class PartialInputStreamTests
    {
        [Fact()]
        public void ReadTest()
        {
            var input = "Hello world";
            var expectedOutput = "Hello";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var partial = new PartialInputStream(ms, 0, 5))
            {
                var buffer = new byte[1024];
                var bytesRead = partial.Read(buffer, 0, buffer.Length);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(expectedOutput, output);
            }
        }

        [Fact()]
        public async Task ReadAsyncTest()
        {
            var input = "Hello world";
            var expectedOutput = "Hello";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var partial = new PartialInputStream(ms, 0, 5))
            {
                var buffer = new byte[1024];
                var bytesRead = await partial.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(expectedOutput, output);
            }
        }

        [Fact()]
        public void BytesLeftTest()
        {
            var input = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var partial = new PartialInputStream(ms, 0, 5))
            {
                Assert.Equal(5, partial.BytesLeft);
            }
        }

        [Fact()]
        public void BytesReadTest()
        {
            var input = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var partial = new PartialInputStream(ms, 0, 5))
            {
                var buffer = new byte[1024];
                var bytesRead = partial.Read(buffer, 0, buffer.Length);

                Assert.Equal(bytesRead, partial.BytesRead);
            }
        }
    }
}
