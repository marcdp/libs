using Xunit;
using System.IO;
using System.IO.Compression;
using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class GZipDecompressInputStreamTests
    {
        [Fact()]
        public void ReadTest()
        {
            var input = "Hello world";
            var compressedInput = new byte[1024];

            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    var buffer = Encoding.UTF8.GetBytes(input);
                    gzip.Write(buffer, 0, buffer.Length);
                }

                ms.Position = 0;
                ms.Read(compressedInput, 0, (int)ms.Length);
            }

            using (var ms = new MemoryStream(compressedInput))
            using (var gzip = new GZipDecompressInputStream(ms, true))
            {
                var buffer = new byte[1024];
                var bytesRead = gzip.Read(buffer, 0, buffer.Length);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(input, output);
            }
        }

        [Fact()]
        public async void ReadAsyncTest()
        {
            var input = "Hello world";
            var compressedInput = new byte[1024];

            using (var ms = new MemoryStream())
            {
                using (var gzip = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    var buffer = Encoding.UTF8.GetBytes(input);
                    await gzip.WriteAsync(buffer, 0, buffer.Length);
                }

                ms.Position = 0;
                ms.Read(compressedInput, 0, (int)ms.Length);
            }

            using (var ms = new MemoryStream(compressedInput))
            using (var gzip = new GZipDecompressInputStream(ms, true))
            {
                var buffer = new byte[1024];
                var bytesRead = await gzip.ReadAsync(buffer, 0, buffer.Length);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(input, output);
            }
        }
    }
}
