using Xunit;
using System.IO;
using System.Threading;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class FoldedOutputStreamTest {


        [Theory()]
        [InlineData("abcdefgh12345678", 4, "abcd\nefgh\n1234\n5678\n")]
        [InlineData("abcdefgh1234567890", 4, "abcd\nefgh\n1234\n5678\n90")]
        [InlineData("abc", 4, "abc")]
        [InlineData("abcd", 4, "abcd\n")]
        public void WriteTest(string input, int size, string expected) {
            using (var ms = new MemoryStream()) {
                //write
                using (var folded = new FoldedOutputStream(ms, size)) {
                    var buffer = System.Text.Encoding.UTF8.GetBytes(input);
                    folded.Write(buffer, 0, buffer.Length);
                }
                using (var reader = new StreamReader(new MemoryStream(ms.ToArray()))) {
                    var output = reader.ReadToEnd();
                    Assert.Equal(expected, output);
                }
            }
        }
         
    }
}
