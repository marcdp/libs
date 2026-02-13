using Xunit;
using System.IO;
using System.Text;
using System.Collections.Generic;
using DProjects.Streams;

namespace DProjects.Streams.Tests
{
    public class CatInputStreamTests
    {
        [Fact()]
        public void ReadTest()
        {
            var input1 = "Hello ";
            var input2 = "world";
            var expectedOutput = "Hello world";

            using (var ms1 = new MemoryStream(Encoding.UTF8.GetBytes(input1)))
            using (var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(input2)))
            using (var concatenated = new CatInputStream(new List<Stream> { ms1, ms2 }))
            {
                var buffer = new byte[1024];
                var bytesRead = concatenated.Read(buffer, 0, buffer.Length);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(expectedOutput, output);
            }
        }

        [Fact()]
        public async Task ReadAsyncTest()
        {
            var input1 = "Hello ";
            var input2 = "world";
            var expectedOutput = "Hello world";

            using (var ms1 = new MemoryStream(Encoding.UTF8.GetBytes(input1)))
            using (var ms2 = new MemoryStream(Encoding.UTF8.GetBytes(input2)))
            using (var concatenated = new CatInputStream(new List<Stream> { ms1, ms2 }))
            {
                var buffer = new byte[1024];
                var bytesRead = await concatenated.ReadAsync(buffer, 0, buffer.Length);
                var output = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                Assert.Equal(expectedOutput, output);
            }
        }
    }
}
