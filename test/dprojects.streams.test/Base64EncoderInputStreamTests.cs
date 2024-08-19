using Xunit;
using System.IO;
using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests {
    public class Base64EncoderInputStreamTests {
        [Fact()]
        public void ReadByteTest() {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello")))
            using (var stream = new Base64EncoderInputStream(ms, 0)) {
                var result = stream.ReadByte();
                Assert.Equal((byte)'S', result);
            }
        }

        [Fact()]
        public void ReadTest() {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello")))
            using (var stream = new Base64EncoderInputStream(ms, 0)) {
                var buffer = new byte[8];
                var result = stream.Read(buffer, 0, buffer.Length);
                Assert.Equal(8, result);
                Assert.Equal("SGVsbG8=", Encoding.UTF8.GetString(buffer, 0, result));
            }
        }

        [Fact()]
        public async void ReadByteAsyncTest() {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello")))
            using (var stream = new Base64EncoderInputStream(ms, 0)) {
                var result = await stream.ReadByteAsync(default);
                Assert.Equal((byte)'S', result);
            }
        }

        [Fact()]
        public async void ReadAsyncTest() {
            var aux = Encoding.UTF8.GetBytes("Hello");
            using (var ms = new MemoryStream(aux))
            using (var stream = new Base64EncoderInputStream(ms, 0)) {
                var buffer = new byte[8];
                var result = await stream.ReadAsync(buffer, 0, buffer.Length, default);
                //await stream.ReadExactlyAsync(buffer, default);
                Assert.Equal(8, result);
                Assert.Equal("SGVsbG8=", Encoding.UTF8.GetString(buffer, 0, buffer.Length));
            }
        }

        [Fact()]
        public void DisposeTest() {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello"));
            var stream = new Base64EncoderInputStream(ms, 0);
            stream.Dispose();

            Assert.Throws<ObjectDisposedException>(() => ms.ReadByte());
        }

        [Fact()]
        public void DisposeLeaveOpenTest() {
            var ms = new MemoryStream(Encoding.UTF8.GetBytes("Hello"));
            var stream = new Base64EncoderInputStream(ms, 0, true);
            stream.Dispose();

            Assert.Equal((byte)'H', ms.ReadByte());
        }
    }
}