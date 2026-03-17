using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Buffers.Text;
using System.Numerics;

namespace DProjects.Utils.Tests {
    public class Base64UtilsTests {

        //tests ToBase64Test
        [Theory()]
        [InlineData(new byte[] { 1, 2, 3, 4 }, Base64FormattingOptions.None, "AQIDBA==")]
        [InlineData(new byte[] { 1, 2, 3, 4, 248 }, Base64FormattingOptions.None, "AQIDBPg=")]
        [InlineData(new byte[] { 1, 2, 3, 4, 248, 0 }, Base64FormattingOptions.None, "AQIDBPgA")]
        public void ToBase64Test(byte[] buffer, Base64FormattingOptions options, string base64) {
            Assert.Equal(base64, Base64Utils.ToBase64(buffer, options));
            Assert.Equal(buffer, Base64Utils.FromBase64(base64));
        }
        [Theory()]
        [InlineData("Neque porro2 quisquam est qui dolorem ipsum quia dolor sit amet, consectetur, adipisci velit", Base64FormattingOptions.InsertLineBreaks, "TmVxdWUgcG9ycm8yIHF1aXNxdWFtIGVzdCBxdWkgZG9sb3JlbSBpcHN1bSBxdWlhIGRvbG9yIHNp\r\ndCBhbWV0LCBjb25zZWN0ZXR1ciwgYWRpcGlzY2kgdmVsaXQ=")]
        public void ToBase64Test1(string text, Base64FormattingOptions options, string base64) {
            Assert.Equal(base64, Base64Utils.ToBase64(System.Text.Encoding.UTF8.GetBytes(text), options));
            Assert.Equal(System.Text.Encoding.UTF8.GetBytes(text), Base64Utils.FromBase64(base64));
        }


        //tests ToBase64UrlSafe
        [Theory()]
        [InlineData("This string has @#$%^&*()_+-=[]{}|;:',./<>? characters!", Base64FormattingOptions.None, "VGhpcyBzdHJpbmcgaGFzIEAjJCVeJiooKV8rLT1bXXt9fDs6JywuLzw-PyBjaGFyYWN0ZXJzIQ")]
        public void ToBase64UrlSafeTest(string text, Base64FormattingOptions options, string base64) {
            Assert.Equal(base64, Base64Utils.ToBase64UrlSafe(System.Text.Encoding.UTF8.GetBytes(text), options));
            Assert.Equal(System.Text.Encoding.UTF8.GetBytes(text), Base64Utils.FromBase64UrlSafe(base64));
        }

        //tests ToBase64UrlSafe
        [Theory()]
        [InlineData("AQIDBA==", true)]
        [InlineData("AQIDBA==k", false)]
        [InlineData("VGhpcyBzdHJpbmcgaGFzIEAjJCVeJiooKV8rLT1bXXt9fDs6JywuLzw+PyBjaGFyYWN0ZXJzIQ==", true)]
        [InlineData("VGhpcyBzdHJpbmcgaGFzIEAjJCVeJiooKV8rLT1bXXt9fDs6JywuLzw-PyBjaGFyYWN0ZXJzIQ", false)]
        public void IsBase64Test(string base64, bool result) {
            Assert.Equal(result, Base64Utils.IsBase64(base64));
        } 
    }
}