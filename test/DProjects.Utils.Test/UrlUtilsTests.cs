using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class UrlUtilsTests {


        //encode/decode
        [Theory()]
        [InlineData("hello&%=123à", "hello%26%25%3D123%C3%A0")]
        [InlineData("hello&%=123à/asd:23", "hello%26%25%3D123%C3%A0%2Fasd%3A23")]
        public void UrlEncodeTest(string text, string result) {
            Assert.Equal(result, UrlUtils.UrlEncode(text));
            Assert.Equal(text, UrlUtils.UrlDecode(result));
        }



        //wrap/unwrap
        [Theory()]
        [InlineData("schema1:scheme2://host/path!/subpath", "schema1:/subpath", "scheme2://host/path")]
        [InlineData("schema1:scheme2://host/path!?kk=123123", "schema1:?kk=123123", "scheme2://host/path")]
        public void WrapUnwrapTest(string url, string part1, string part2) {
            var parts = UrlUtils.UnwrapUrl(url);
            Assert.Equal(part1, parts.Item1);
            Assert.Equal(part2, parts.Item2);
        }


        //getqueryvalue
        [Theory()]
        [InlineData("?var1=11&var2", "var1", 11)]
        [InlineData("?var1=0&var2", "var1", false)]
        [InlineData("?var1=1&var2", "var1", true)]
        [InlineData("?var1=1&var2=my+value", "var2", "my value")]
        public void GetQueryValue(string query, string key, object value) {
            Assert.Equal(value, UrlUtils.GetQueryValue(value.GetType(), query, key));
        }


        //deserialize
        [Fact()]
        public void DeserializeTest() {
            var url = "myscheme://myuser:myp%C3%A0ss@myhost:897/my/path?var1=hello&var2=123&var3=true&var4=h%C3%A8llo";
            var settings = UrlUtils.Deserialize<MySettings>(url);
            Assert.Equal("myscheme", settings.Scheme);
            Assert.Equal("myhost", settings.Host);
            Assert.Equal(897, settings.Port); 
            Assert.Equal("/my/path", settings.Path);
            Assert.Equal("myuser", settings.User);
            Assert.Equal("mypàss", settings.Password);
            Assert.Equal("hello", settings.Var1);
            Assert.Equal(123, settings.Var2);
            Assert.True(settings.Var3);
            Assert.Equal("hèllo", settings.Var4);

        }
        private class MySettings {
            public string Scheme { get; set; }
            public string Host { get; set; }
            public int Port { get; set; }
            public string Path { get; set; }
            public string User { get; set; }
            public string Password { get; set; }
            public string Var1 { get; set; }
            public int Var2 { get; set; }
            public bool Var3 { get; set; }
            public string Var4 { get; set; }
        }


        //getqueryvalue
        [Theory()]
        [InlineData("hello world, hòw are you?", "hello-world-how-are-you")]
        public void ToPrettyUrl(string text, string result) {
            Assert.Equal(result, UrlUtils.ToPrettyUrl(text));
        }

    }
}