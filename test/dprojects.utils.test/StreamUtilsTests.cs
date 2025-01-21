using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DProjects.Utils.Tests {
    public class StreamUtilsTests {


        //read text
        [Theory()]
        [InlineData("hello world", "utf-8")]
        [InlineData("hello wòrld", "utf-32")]
        public void ReadTextTest(string text, string encodingName) {
            var encoding = Encoding.GetEncoding(encodingName);
            var buffer = encoding.GetBytes(text);
            Assert.Equal(text, StreamUtils.ReadText(new MemoryStream(buffer), encoding));
            Assert.Equal(text, AsyncUtils.RunSync(()=> StreamUtils.ReadTextAsync(new MemoryStream(buffer), encoding)));
        }
        [Theory()]
        [InlineData("hello world\nline second", "utf-8")]
        [InlineData("hello world\nline second\nThird line\n", "utf-8")]
        public void ReadTextLinesTest(string text, string encodingName) {
            var lines = (text.EndsWith("\n") ? text.Remove(text.Length-1) : text).Split('\n');
            var encoding = Encoding.GetEncoding(encodingName);
            var buffer = encoding.GetBytes(text);
            Assert.Equal(lines, StreamUtils.ReadTextLines(new MemoryStream(buffer), encoding));
            Assert.Equal(lines, AsyncUtils.RunSync(() => StreamUtils.ReadTextLinesAsync(new MemoryStream(buffer), encoding)));
        }


        //Read bytes
        [Theory()]
        [InlineData("hello world", "utf-8")]
        [InlineData("hello wòrld", "utf-32")]
        public void ReadBytesTest(string text, string encodingName) {
            var encoding = Encoding.GetEncoding(encodingName);
            var buffer = encoding.GetBytes(text);
            Assert.Equal(buffer, StreamUtils.ReadBytes(new MemoryStream(buffer)));
            Assert.Equal(buffer, AsyncUtils.RunSync(() => StreamUtils.ReadBytesAsync(new MemoryStream(buffer), default)));
        }
        [Theory()]
        [InlineData("hello world", "utf-8", 4, "hell")]
        [InlineData("hello world", "utf-8", 1024, "hello world")]
        public void ReadByteswithLengthTest(string text, string encodingName, int length, string expected) {
            var encoding = Encoding.GetEncoding(encodingName);
            Assert.Equal(encoding.GetBytes(expected), StreamUtils.ReadBytes(new MemoryStream(encoding.GetBytes(text)), length));
            Assert.Equal(encoding.GetBytes(expected), AsyncUtils.RunSync(() => StreamUtils.ReadBytesAsync(new MemoryStream(encoding.GetBytes(text)), default, length)));
        }

        //Fill buffer
        [Theory()]
        [InlineData("hello world", "utf-8")]
        [InlineData("hello wòrld", "utf-32")]
        public void FillBufferTest(string text, string encodingName) {
            var encoding = Encoding.GetEncoding(encodingName);
            var buffer = encoding.GetBytes(text);
            
            var buffer2 = new byte[buffer.Length];
            StreamUtils.FillBuffer(new MemoryStream(buffer), buffer2);
            Assert.Equal(buffer, buffer2);

            var buffer3 = new byte[buffer.Length];
            AsyncUtils.RunSync(() => StreamUtils.FillBufferAsync(new MemoryStream(buffer), buffer3, default));
            Assert.Equal(buffer, buffer3);
        }


        //read lines
        [Theory()]
        [InlineData("hello world\nline second", "utf-8")]
        [InlineData("hello world\nline second\nThird line\n", "utf-8")]
        public void ReadLineTest(string text, string encodingName) {
            var lines = (text.EndsWith("\n") ? text.Remove(text.Length - 1) : text).Split('\n');
            var encoding = Encoding.GetEncoding(encodingName);
            var buffer = encoding.GetBytes(text);
            var ms = new MemoryStream(buffer);
            foreach(var line in lines) {
                Assert.Equal(line, StreamUtils.ReadLine(ms, encoding));
            }
            ms = new MemoryStream(buffer);
            foreach (var line in lines) {
                Assert.Equal(line, AsyncUtils.RunSync(() => StreamUtils.ReadLineAsync(ms, encoding, default)));
            }
        }
        [Theory()]
        [InlineData("hello world\nline second", "utf-8", "hello", ' ', 0)]
        [InlineData("hello world\nline second", "utf-8", "hel", '\n', 3)]
        [InlineData("hello world\nline second", "utf-8", "hel", ' ', 3)]
        public void ReadLineWithDelimiterTest(string text, string encodingName, string expected, char newline, int maxlength) {
            var encoding = Encoding.GetEncoding(encodingName);
            var buffer = encoding.GetBytes(text);
            var ms = new MemoryStream(buffer);
            Assert.Equal(expected, StreamUtils.ReadLine(ms, encoding, newline, maxlength));
            ms = new MemoryStream(buffer);
            Assert.Equal(expected, AsyncUtils.RunSync(() => StreamUtils.ReadLineAsync(ms, encoding, default, newline, maxlength)));
        }



        //consume
        [Theory()]
        [InlineData("hello world\nline second", "utf-8")]
        [InlineData("hello world\nline second\nThird line\n", "utf-8")]
        public void ConsumeTest(string text, string encodingName) {
            var lines = (text.EndsWith("\n") ? text.Remove(text.Length - 1) : text).Split('\n');
            var encoding = Encoding.GetEncoding(encodingName);
            var buffer = encoding.GetBytes(text);

            var ms = new MemoryStream(buffer);
            StreamUtils.Consume(ms);
            Assert.Equal(-1, ms.ReadByte());

            ms = new MemoryStream(buffer);
            AsyncUtils.RunSync(async () => await StreamUtils.ConsumeAsync (ms, default));
            Assert.Equal(-1, ms.ReadByte());
        }


        //copy
        [Theory()]
        [InlineData("hello world\nline second", "utf-8")]
        [InlineData("hello world\nline second\nThird line\n", "utf-8")]
        public void Copy(string text, string encodingName) {
            var encoding = Encoding.GetEncoding(encodingName);
            var buffer = encoding.GetBytes(text);
            //copy
            var ms1 = new MemoryStream(buffer);
            var ms2 = new MemoryStream();
            StreamUtils.Copy(ms1,ms2);
            Assert.Equal(text, encoding.GetString(ms2.ToArray()));
            //copy async
            ms1 = new MemoryStream(buffer);
            ms2 = new MemoryStream();
            AsyncUtils.RunSync(() => StreamUtils.CopyAsync(ms1, ms2));
            Assert.Equal(text, encoding.GetString(ms2.ToArray()));
            //copy with bytesToCopy
            ms1 = new MemoryStream(buffer);
            ms2 = new MemoryStream();
            StreamUtils.Copy(ms1, ms2, 2);
            Assert.Equal(text.Substring(0,2), encoding.GetString(ms2.ToArray()));
            //copy with bytesToCopy async
            ms1 = new MemoryStream(buffer);
            ms2 = new MemoryStream();
            AsyncUtils.RunSync(() => StreamUtils.CopyAsync(ms1, ms2, 2));
            Assert.Equal(text.Substring(0, 2), encoding.GetString(ms2.ToArray()));
        }

    }
}