using System.Text;

namespace DProjects.Crypto.Tests {

    public class BCryptTests {

        // methods
        [Fact]
        public void HashesAndVerifiesPasswordText() {
            // verify salted password hashes and UTF-8 text behavior
            using var bcrypt = new CryptoHashBCRYPT();
            var first = bcrypt.ToHashText("pässword 世界");
            var second = bcrypt.ToHashText("pässword 世界");
            Assert.NotEqual(first, second);
            Assert.True(bcrypt.VerifyText("pässword 世界", first));
            Assert.True(bcrypt.VerifyText("pässword 世界", second));
            Assert.False(bcrypt.VerifyText("wrong", first));
        }
        [Fact]
        public async Task StreamAndAsyncVerificationUseUtf8Text() {
            // confirm stream-shaped methods retain bcrypt's text semantics
            using var bcrypt = new CryptoHashBCRYPT();
            var input = Encoding.UTF8.GetBytes("mañana");
            using var hashInput = new MemoryStream(input);
            var hash = await bcrypt.ToHashAsync(hashInput, TestContext.Current.CancellationToken);
            using var verifyInput = new MemoryStream(input);
            Assert.True(await bcrypt.VerifyAsync(verifyInput, hash, TestContext.Current.CancellationToken));
        }
        [Fact]
        public void MalformedEncodedHashFailsPredictably() {
            // preserve the underlying bcrypt parser's explicit failure behavior
            using var bcrypt = new CryptoHashBCRYPT();
            Assert.ThrowsAny<Exception>(() => bcrypt.VerifyText("password", "not-a-bcrypt-hash"));
        }
    }
}
