using Xunit;
using System.IO;
using System.Text;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class SpongeStreamTests {


        [Theory()]
        [InlineData(4, "abcdefgh12345678", 4)]
        [InlineData(4, "abcdefgh12345678", 8)]
        [InlineData(4, "abcdefgh12345678", 16)]
        [InlineData(8, "abcdefgh12345678", 3)]
        [InlineData(8, "abcdefgh12345678", 9)]
        [InlineData(8, "abcdefgh12345678", 15)]
        [InlineData(8, "abcdefgh12345678", 21)]
        [InlineData(16, "abcdefgh12345678", 4)]
        [InlineData(16, "abcdefgh12345678", 8)]
        [InlineData(16, "abcdefgh12345678", 16)]
        [InlineData(16, "abcdefgh12345678", 32)]
        [InlineData(32, "abcdefgh12345678", 4)]
        [InlineData(32, "abcdefgh12345678", 8)]
        [InlineData(32, "abcdefgh12345678", 16)]
        [InlineData(32, "abcdefgh12345678", 28)]
        public void WriteTest(int bufferSize, string text, int writeSize) {
            var buffer = Encoding.UTF8.GetBytes(text);
            using (var sponge = new SpongeOutputStream(bufferSize , (stream) => {
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                var buffer2 = ms.ToArray();
                Assert.Equal(buffer, buffer2);
            })) {
                var offset = 0;
                while (offset < buffer.Length) {
                    var count = Math.Min(writeSize, buffer.Length - offset);
                    sponge.Write(buffer, offset, count);
                    offset += count;
                }
            }
        }

        [Theory()]
        [InlineData(4, "abcdefgh12345678", 4)]
        [InlineData(4, "abcdefgh12345678", 8)]
        [InlineData(4, "abcdefgh12345678", 16)]
        [InlineData(8, "abcdefgh12345678", 3)]
        [InlineData(8, "abcdefgh12345678", 9)]
        [InlineData(8, "abcdefgh12345678", 15)]
        [InlineData(8, "abcdefgh12345678", 21)]
        [InlineData(16, "abcdefgh12345678", 4)]
        [InlineData(16, "abcdefgh12345678", 8)]
        [InlineData(16, "abcdefgh12345678", 16)]
        [InlineData(16, "abcdefgh12345678", 32)]
        [InlineData(32, "abcdefgh12345678", 4)]
        [InlineData(32, "abcdefgh12345678", 8)]
        [InlineData(32, "abcdefgh12345678", 16)]
        [InlineData(32, "abcdefgh12345678", 28)]
        public async Task WriteTestAsync(int bufferSize, string text, int writeSize) {
            var buffer = Encoding.UTF8.GetBytes(text);
            await using (var sponge = new SpongeOutputStream(bufferSize, (stream) => {
                var ms = new MemoryStream();
                stream.CopyTo(ms);
                var buffer2 = ms.ToArray();
                Assert.Equal(buffer, buffer2);
            })) {
                var offset = 0;
                while (offset < buffer.Length) {
                    var count = Math.Min(writeSize, buffer.Length - offset);
                    await sponge.WriteAsync(buffer, offset, count);
                    offset += count;
                }
            }
        }
    }
}
