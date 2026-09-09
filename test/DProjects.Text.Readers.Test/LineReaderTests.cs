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
        public async Task ReadAsyncTest()
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
        public async Task ReadBlockAsyncTest()
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
                Assert.Equal(NormalizeLineEndings("Test\r\nLine\r\n"), NormalizeLineEndings(result));
            }
        }

        [Fact()]
        public async Task ReadToEndAsyncTest()
        {
            using (var sr = new StringReader("Test\r\nLine"))
            using (var reader = new LineReader(sr))
            {
                var result = await reader.ReadToEndAsync();
                Assert.Equal(NormalizeLineEndings("Test\r\nLine\r\n"), NormalizeLineEndings(result));
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
        public async Task ReadLineAsyncTest()
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
                Assert.Equal(NormalizeLineEndings("Test\r\nLine\r\n"), NormalizeLineEndings(result));
            }
        }
        [Theory]
        [InlineData("")]
        [InlineData("\r")]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public void EmptyAndLineEndingOnlyInputs_ReachEofDeterministically(string input) {
            using var reader = new LineReader(new StringReader(input));

            var first = reader.ReadLine();
            var second = reader.ReadLine();

            if (input.Length == 0) Assert.Null(first); else Assert.Equal("", first);
            Assert.Null(second);
        }
        [Fact]
        public void RepeatedPushback_IsReadInInsertionOrderBeforeUnderlyingInput() {
            using var reader = new LineReader(new StringReader("source"));
            reader.PushBackLine("first");
            reader.PushBackLine("second");

            Assert.Equal("first", reader.ReadLine());
            Assert.Equal("second", reader.ReadLine());
            Assert.Equal("source", reader.ReadLine());
            Assert.Null(reader.ReadLine());
        }

        private static string NormalizeLineEndings(string value)
        {
            return value
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
        }
    }
}
