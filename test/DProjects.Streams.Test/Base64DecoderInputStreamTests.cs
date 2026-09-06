using Xunit;
using System.IO;
using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class Base64DecoderInputStreamTests
    {
        [Fact()]
        public void ReadByteTest()
        {
            var input = "SGVsbG8gd29ybGQ=";
            var expectedOutput = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var decoder = new Base64DecoderInputStream(ms))
            {
                var output = new StringBuilder();
                int b;
                while ((b = decoder.ReadByte()) != -1)
                {
                    output.Append((char)b);
                }

                Assert.Equal(expectedOutput, output.ToString());
            }
        }

        [Fact()]
        public void ReadTest()
        {
            var input = "SGVsbG8gd29ybGQ=";
            var expectedOutput = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var decoder = new Base64DecoderInputStream(ms))
            {
                var buffer = new byte[1024];
                var bytesRead = decoder.Read(buffer, 0, buffer.Length);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(expectedOutput, output);
            }
        }

        [Fact()]
        public async Task ReadByteAsyncTest()
        {
            var input = "SGVsbG8gd29ybGQ=";
            var expectedOutput = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var decoder = new Base64DecoderInputStream(ms))
            {
                var output = new StringBuilder();
                int b;
                while ((b = await decoder.ReadByteAsync(TestContext.Current.CancellationToken)) != -1)
                {
                    output.Append((char)b);
                }

                Assert.Equal(expectedOutput, output.ToString());
            }
        }

        [Fact()]  
        public async Task ReadAsyncTest()
        {
            var input = "SGVsbG8gd29ybGQ=";
            var expectedOutput = "Hello world";

            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(input)))
            using (var decoder = new Base64DecoderInputStream(ms))
            {
                var buffer = new byte[1024];
                var bytesRead = await decoder.ReadAsync(buffer, 0, buffer.Length, TestContext.Current.CancellationToken);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(expectedOutput, output);
            }
        }
    }
}
