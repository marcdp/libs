using Xunit;
using System.IO;
using System.IO.Compression;
using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class GZipCompressOutputStreamTests
    {
        [Fact()]
        public void WriteTest()
        {
            var input = "Hello world";
            var expectedOutput = Encoding.UTF8.GetBytes(input);

            using (var ms = new MemoryStream())
            using (var gzip = new GZipCompressOutputStream(ms, true))
            {
                gzip.Write(expectedOutput, 0, expectedOutput.Length);
                gzip.Flush();
                using (var gzipStream = new GZipStream(new MemoryStream(ms.ToArray()), CompressionMode.Decompress))
                using (var reader = new StreamReader(gzipStream)) {
                    var output = reader.ReadToEnd();
                    Assert.Equal(input, output);
                }
            }
        }

        [Fact()]
        public async Task WriteAsyncTest()
        {
            var input = "Hello world";
            var expectedOutput = Encoding.UTF8.GetBytes(input);

            using (var ms = new MemoryStream())
            using (var gzip = new GZipCompressOutputStream(ms, true))
            {
                await gzip.WriteAsync(expectedOutput, 0, expectedOutput.Length);
                await gzip.FlushAsync();

                using (var gzipStream = new GZipStream(new MemoryStream(ms.ToArray()), CompressionMode.Decompress))
                using (var reader = new StreamReader(gzipStream))
                {
                    var output = await reader.ReadToEndAsync();
                    Assert.Equal(input, output);
                }
            }
        }
    }
}
