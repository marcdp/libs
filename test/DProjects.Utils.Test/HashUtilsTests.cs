using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class HashUtilsTests {

        //MD5
        [Theory()]
        [InlineData("hello world", "XrY7u+Ae7tCTyyK7j1rNww==")]
        public void ToHashMD5Base64Test(string text, string result) {
            Assert.Equal(result, HashUtils.ToHashMD5Base64(text));
        }
        [Theory()]
        [InlineData(new byte[] { 1,2,3,4,5,6,7,8,9,1,2,3,4,3,78}, "6AD51D590BB0840A54DC91950C8EA554")]
        public void ToHashMD5HexTest(byte[] buffer, string result) {
            Assert.Equal(result, HashUtils.ToHashMD5Hex(buffer));
        }


        //SHA1
        [Theory()]
        [InlineData("hello world", "Kq5sNclPz7QV2+lfQIuc6R7oRu0=")]
        public void ToHashSHA1Base64Test(string text, string result) {
            Assert.Equal(result, HashUtils.ToHashSHA1Base64(text));
        }
        [Theory()]
        [InlineData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 3, 78 }, "B071ADD9A2BB39C36D30C0752BA3F2E03A40C0DD")]
        public void ToHashSHA1HexTest(byte[] buffer, string result) {
            Assert.Equal(result, HashUtils.ToHashSHA1Hex(buffer));
        }


        //SHA256
        [Theory()]
        [InlineData("hello world", "uU0nuZNNPgilLlLX2n2r+sSE7+N6U4DukIj3rOLvzek=")]
        public void ToHashSHA256Base64Test(string text, string result) {
            Assert.Equal(result, HashUtils.ToHashSHA256Base64(text));
        }
        [Theory()]
        [InlineData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 3, 78 }, "B6E1A9EDDD9A8AD778ECF1D294E1327E26D8AE22A1A86FBED4987CD99AE7DCAB")]
        public void ToHashSHA256HexTest(byte[] buffer, string result) {
            Assert.Equal(result, HashUtils.ToHashSHA256Hex(buffer));
        }

        //SHA512
        [Theory()]
        [InlineData("hello world", "MJ7MSJwS1utMxA9QyQLytNDtd+5RGnx6m808qG1M2G+YndNbxf9JlnDaNCVbRbDP2DDoH2Bdz33FVC6TrpzXbw==")]
        public void ToHashSHA512Base64Test(string text, string result) {
            var value = HashUtils.ToHashSHA512Base64(text);
            Assert.Equal(result, value);
        }
        [Theory()]
        [InlineData(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 1, 2, 3, 4, 3, 78 }, "A61AA37885F92BEB9C593CAA9C3D5AE7683AD3CCE0F096CF6976FE2AF121560F08AAF2D61266B58E59111DDC46AFD6FA5FBC55EF79746A87A5F3EDE9EE1F99CB")]
        public void ToHashSHASHA512HexTest(byte[] buffer, string result) {
            Assert.Equal(result, HashUtils.ToHashSHA512Hex(buffer));
        }

    }
}