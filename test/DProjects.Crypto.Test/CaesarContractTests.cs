namespace DProjects.Crypto.Tests {

    public class CaesarContractTests {

        // methods
        [Fact]
        public void RepresentativeBytesRoundTripAndStreamsRemainOpen() {
            // pin byte wrapping and caller stream ownership
            var plaintext = new byte[] { 0, 1, 127, 128, 254, 255 };
            using var encrypt = new CryptoSymmetricEncryptCaesar(new() { Header = false });
            using var output = new MemoryStream();
            using (var cryptoStream = encrypt.GetStream(output, "257")) {
                cryptoStream.Write(plaintext);
            }
            Assert.True(output.CanWrite);
            output.Position = 0;
            using var decrypt = new CryptoSymmetricDecryptCaesar(new() { Header = false });
            using var decrypted = decrypt.GetStream(output, "257");
            var actual = new byte[plaintext.Length];
            Assert.Equal(plaintext.Length, decrypted.Read(actual));
            Assert.Equal(plaintext, actual);
            decrypted.Dispose();
            Assert.True(output.CanRead);
        }
        [Theory]
        [InlineData("")]
        [InlineData("not-a-number")]
        public void InvalidPasswordFails(string password) {
            // reject non-numeric compatibility keys
            using var encrypt = new CryptoSymmetricEncryptCaesar(new() { Header = false });
            Assert.Throws<FormatException>(() => encrypt.Encrypt("plaintext", password));
        }
        [Fact]
        public void MalformedHeaderFails() {
            // reject input without the configured header separator
            using var decrypt = new CryptoSymmetricDecryptCaesar(new() { Header = true });
            Assert.ThrowsAny<Exception>(() => decrypt.Decrypt("not-a-header", "1"));
        }
    }
}
