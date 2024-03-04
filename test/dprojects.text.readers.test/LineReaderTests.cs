using Xunit;
using System.IO;
using System.Text;
using DProjects.Text.Readers;

namespace DProjects.Text.Readers.Tests
{
    public class LineReaderTests
    {
        [Fact()]
        public void ReadTest()
        {
            using (var sr = new StringReader("Test"))
            using (var reader = new LineReader(sr))
            {
                var result = reader.Read();
                Assert.Equal('T', (char)result);
            }
        }

        [Fact()]
        public async void ReadAsyncTest()
        {
            using (var sr = new StringReader("Test"))
            using (var reader = new LineReader(sr))
            {
                var buffer = new char[4];
                var result = await reader.ReadAsync(buffer, 0, 4);
                Assert.Equal(4, result);
                Assert.Equal("Test", new string(buffer));
            }
        }

        [Fact()]
        public void ReadBlockTest()
        {
            using (var sr = new StringReader("Test"))
            using (var reader = new LineReader(sr))
            {
                var buffer = new char[4];
                var result = reader.ReadBlock(buffer, 0, 4);
                Assert.Equal(4, result);
                Assert.Equal("Test", new string(buffer));
            }
        }

        [Fact()]
        public async void ReadBlockAsyncTest()
        {
            using (var sr = new StringReader("Test"))
            using (var reader = new LineReader(sr))
            {
                var buffer = new char[4];
                var result = await reader.ReadBlockAsync(buffer, 0, 4);
                Assert.Equal(4, result);
                Assert.Equal("Test", new string(buffer));
            }
        }

        [Fact()]
        public void ReadToEndTest()
        {
            using (var sr = new StringReader("Test\r\nLine"))
            using (var reader = new LineReader(sr))
            {
                var result = reader.ReadToEnd();
                Assert.Equal("Test\r\nLine\r\n", result);
            }
        }

        [Fact()]
        public async Task ReadToEndAsyncTest()
        {
            using (var sr = new StringReader("Test\r\nLine"))
            using (var reader = new LineReader(sr))
            {
                var result = await reader.ReadToEndAsync();
                Assert.Equal("Test\r\nLine\r\n", result);
            }
        }

        [Fact()]
        public void ReadLineTest()
        {
            using (var sr = new StringReader("Test\nLine"))
            using (var reader = new LineReader(sr))
            {
                var result = reader.ReadLine();
                Assert.Equal("Test", result);
            }
        }

        [Fact()]
        public async void ReadLineAsyncTest()
        {
            using (var sr = new StringReader("Test\nLine"))
            using (var reader = new LineReader(sr))
            {
                var result = await reader.ReadLineAsync();
                Assert.Equal("Test", result);
            }
        }

        [Fact()]
        public void PushBackLineTest()
        {
            using (var sr = new StringReader("Test\r\nLine"))
            using (var reader = new LineReader(sr))
            {
                reader.PushBackLine("Pushed");
                var result = reader.ReadLine();
                Assert.Equal("Pushed", result);
                result = reader.ReadToEnd();
                Assert.Equal("Test\r\nLine\r\n", result);
            }
        }
    }
}
