using System.Text;

namespace DProjects.Streams.Tests
{
    public class Base64EncoderOutputStreamTests
    {
        [Fact]
        public void ConstructorTest()
        {
            using var stream = new MemoryStream();
            var encoder = new Base64EncoderOutputStream(stream);
            Assert.NotNull(encoder);
        }

        [Fact]
        public void WriteTest()
        {
            var stream = new MemoryStream();
            using (var encoder = new Base64EncoderOutputStream(stream, true)) {
                var data = Encoding.UTF8.GetBytes("Hello, World!");
                encoder.Write(data, 0, data.Length);
                encoder.Flush();
            }
            var result = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("SGVsbG8sIFdvcmxkIQ==", result);

        }

    [Fact]
        public async void WriteAsyncTest()
        {
            var stream = new MemoryStream();
            using (var encoder = new Base64EncoderOutputStream(stream)) {
                var data = Encoding.UTF8.GetBytes("Hello, World!");
                await encoder.WriteAsync(data, 0, data.Length, CancellationToken.None);
                await encoder.FlushAsync(CancellationToken.None);
            }
            var result = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("SGVsbG8sIFdvcmxkIQ==", result);
        }

    }
}
