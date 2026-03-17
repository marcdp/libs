using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace DProjects.Utils.Tests {
    public class HexUtilsTests {


        //hex
        [Theory()]
        [InlineData(123, "7B")]
        [InlineData(12332, "302C")]
        public void HexTest(int expression, string result) {
            Assert.Equal(result, HexUtils.Hex(expression));
        }
        [Theory()]
        [InlineData((long)123, "7B")]
        [InlineData((long)12332, "302C")]
        [InlineData((long)12332321, "BC2D21")]
        public void HexLongTest(long expression, string result) {
            Assert.Equal(result, HexUtils.Hex(expression));
        }
        [Theory()]
        [InlineData(new byte[] { 32,56,98,253,2,4,5,54,3,21,4,7,54}, "203862FD245363154736")]
        public void HexBytesTest(byte[] expression, string result) {
            Assert.Equal(result, HexUtils.Hex(expression));
        }


    }
}