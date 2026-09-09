using System.Text;

using DProjects.Utils;

namespace DProjects.Utils.Tests {

    public class HeadersUtilsTests {

        // methods
        [Fact]
        public void ReadHttpHeaders_IsCaseInsensitiveIgnoresMalformedLinesAndUsesLastDuplicate() {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes("Content-Length: 10\r\nmalformed\r\ncontent-length: 20\r\nX-Test: value:with:colons\r\n\r\nbody"));

            var headers = HeadersUtils.ReadHttpHeaders(stream, Encoding.UTF8);

            Assert.True(headers.Contains("CONTENT-LENGTH"));
            Assert.Equal(20, headers.Get("Content-Length", -1));
            Assert.Equal("value:with:colons", headers.Get("x-test", ""));
            Assert.False(headers.Contains("malformed"));
        }
        [Fact]
        public void WriteThenRead_RoundTripsValuesAndTerminatesBeforeBody() {
            var expected = new HeadersUtils.Headers();
            expected.Set("X-Test", "value");
            expected.Set(HttpUtils.HEADER_CONTENT_LENGTH, 42);
            using var stream = new MemoryStream();

            HeadersUtils.WriteHttpHeaders(expected, stream, Encoding.UTF8);
            var body = Encoding.UTF8.GetBytes("body");
            stream.Write(body, 0, body.Length);
            stream.Position = 0;
            var actual = HeadersUtils.ReadHttpHeaders(stream, Encoding.UTF8);

            Assert.Equal("value", actual.Get("x-test", ""));
            Assert.Equal(42, actual.Get(HttpUtils.HEADER_CONTENT_LENGTH, -1));
            using var reader = new StreamReader(stream, Encoding.UTF8);
            Assert.Equal("body", reader.ReadToEnd());
        }
    }
}
