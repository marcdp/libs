using DProjects.Utils;
using System.Buffers.Text;
using Xunit;

namespace DProjects.Crypto.Tests
{
    public class HashTests {

        [Theory]
        [InlineData("123456", "E1ADC3949BA59ABBE56E057F2F883E")]
        [InlineData("hèllo world", "369953ECB9CFCB6B519BED2CAE3E3CE4")]
        public void MD5Test(string input, string expected) {
            using var md5 = new CryptoHashMD5(new CryptoHashMD5.Options() {});
            Assert.Equal(expected, HexUtils.Hex(md5.ToHash(input)));
        }

        [Theory]
        [InlineData("123456", "7C4A8D9CA3762AF61E59520943DC26494F8941B")]
        [InlineData("hèllo world", "7AB79CBB60D310F8A68E27980D6E52937582AE0")]
        public void SHA1Test2(string input, string expected) {
            using var sha1 = new CryptoHashSHA1(new CryptoHashSHA1.Options() {  });
            //Base64.From
            Assert.Equal(expected, HexUtils.Hex(sha1.ToHash(input)));
        }

        [Theory]
        [InlineData("123456", "8D969EEF6ECAD3C29A3A629280E686CFC3F5D5A86AFF3CA122C923ADC6C92")]
        [InlineData("hèllo world", "58E868692756B524CF20BFB22CE3EE2CA9378F74744B461DBFBBA7C20884158")]
        public void SHA1Test256(string input, string expected) {
            using var sha256 = new CryptoHashSHA256(new CryptoHashSHA256.Options() { });
            //Base64.From
            Assert.Equal(expected, HexUtils.Hex(sha256.ToHash(input)));
        }

        [Theory]
        [InlineData("123456", "BA3253876AED6BC22D4A6FF53D846C6AD864195ED144AB5C87621B6C233B548BAEAE6956DF346EC8C17F5EA10F35EE3CBC514797ED7DDD3145464E2A0BAB413")]
        [InlineData("hèllo world", "4EA1A0BF4CECAB7BDB795A9EADD8FAB81BAA0F52078E758E6E3AB2089A04335F33E84BD5F67CFAA155D17E86D7E5FB3442261878A7FF5992D387EB6FCB9931")]
        public void SHA1Test512(string input, string expected) {
            using var sha512 = new CryptoHashSHA512(new CryptoHashSHA512.Options() { });
            //Base64.From
            Assert.Equal(expected, HexUtils.Hex(sha512.ToHash(input)));
        }

    }

}