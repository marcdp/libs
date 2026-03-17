using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Utils.Tests {
    public class EncodingUtilsTests {
          
        [Fact()]
        public void GetBufferAsStringTest() {
            //Assert.True(false, "This test needs an implementation");
        }

        [Theory()]
        [InlineData(new byte[] { 0xEF, 0xBB, 0xBF, 65, 65 }, 3, "AA", "utf-8")]
        [InlineData(new byte[] { 195, 160, 65, 65, 65}, 0, "àAAA", "utf-8")]
        [InlineData(new byte[] { 0x00, 0x00, 0xFE, 0xFF, 0, 0, 0, 65, 0, 0, 0, 65 }, 4, "AA", "utf-32BE")]
        [InlineData(new byte[] { 0xFF, 0xFE, 0x00, 0x00, 65, 0, 0, 0, 65, 0, 0, 0 }, 4, "AA", "utf-32")]
        [InlineData(new byte[] { 0xFE, 0xFF, 0, 65, 0, 65}, 2, "AA", "utf-16BE")]
        [InlineData(new byte[] { 0xFF, 0xFE, 65, 0, 65, 0 }, 2, "AA", "utf-16")]
        //2B 2F 76
        public void DetectEncodingTest(byte[] buffer, int bomlength, string text, string enc) {
            //var a = System.Text.Encoding.UTF8.GetBytes("à");
            var encoding = Encoding.GetEncoding(enc);
            Assert.Equal(encoding, EncodingUtils.DetectEncoding(buffer, out int bomlength_result));
            Assert.Equal(bomlength, bomlength_result);

            var textDecoded = encoding.GetString(buffer,bomlength, buffer.Length - bomlength);
            Assert.Equal(text, textDecoded);
        }
    }
}