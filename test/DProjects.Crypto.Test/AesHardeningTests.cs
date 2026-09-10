using System.Security.Cryptography;
using System.Text;

namespace DProjects.Crypto.Tests {

    public class AesHardeningTests {

        // consts
        private const string Base64Vector = "aes:?saltLength=12&version=compat-1,MTIzNDU2Nzg5MDEyMzQ1NgECAwQFBgcICQoLDOgDAABKMfnXJXaBNZzhkNLKgXvGW+5I6t+CKRs1UpF3wDfuZeXl0QY1dsMya6fZfzMD+bc=";
        private const string BinaryVectorBase64 = "YWVzOj9zYWx0TGVuZ3RoPTEyJmVuY29kaW5nPUJpbmFyeSZ2ZXJzaW9uPWNvbXBhdC0xLDEyMzQ1Njc4OTAxMjM0NTYBAgMEBQYHCAkKCwzoAwAAHWA9UaOagtuzDeg7RAtVwzMK2UqeE+kc8ICajSvW4tszFfghvxYlEXEMAasLNajs";

        // methods
        [Theory]
        [InlineData(0)]
        [InlineData(12)]
        [InlineData(16)]
        [InlineData(32)]
        public void Base64SaltMarkerRoundTripsWithoutConsumingPlaintext(int saltLength) {
            // verify the encoded salt marker never consumes caller plaintext
            using var encrypt = new CryptoSymmetricEncryptAES(new() {
                IterationsMin = 1000,
                IterationsRandomRange = 0,
                SaltLength = saltLength,
                IV = Encoding.ASCII.GetBytes("1234567890123456")
            });
            using var decrypt = new CryptoSymmetricDecryptAES();
            var ciphertext = encrypt.Encrypt("non-empty plaintext detects over-read", "password");
            Assert.Equal("non-empty plaintext detects over-read", decrypt.Decrypt(ciphertext, "password"));
        }
        [Fact]
        public void Base64PersistedVectorDecryptsAndPassesVersionToPasswordProvider() {
            // pin the complete v1 header, metadata ordering, ciphertext, and version callback
            using var decrypt = new CryptoSymmetricDecryptAES();
            string? callbackVersion = null;
            var plaintext = decrypt.Decrypt(Base64Vector, version => {
                callbackVersion = version;
                return "correct horse";
            });
            Assert.Equal("compat-1", callbackVersion);
            Assert.Equal("compatibility plaintext", plaintext);
        }
        [Fact]
        public void BinaryPersistedVectorDecrypts() {
            // pin the complete binary v1 representation through a transport-safe literal
            using var decrypt = new CryptoSymmetricDecryptAES();
            var plaintext = decrypt.Decrypt(Convert.FromBase64String(BinaryVectorBase64), "correct horse");
            Assert.Equal("compatibility plaintext", Encoding.UTF8.GetString(plaintext));
        }
        [Fact]
        public async Task AsyncHelpersMatchSynchronousSemantics() {
            // verify async-shaped stream helpers preserve the same v1 representation semantics
            using var encrypt = new CryptoSymmetricEncryptAES(new() { IterationsMin = 1000, IterationsRandomRange = 0 });
            using var encrypted = new MemoryStream();
            using var plaintext = new MemoryStream(Encoding.UTF8.GetBytes("async plaintext"));
            await encrypt.EncryptAsync(plaintext, encrypted, "password");
            encrypted.Position = 0;
            using var decrypt = new CryptoSymmetricDecryptAES();
            using var decrypted = new MemoryStream();
            await decrypt.DecryptAsync(encrypted, decrypted, "password");
            Assert.Equal("async plaintext", Encoding.UTF8.GetString(decrypted.ToArray()));
        }
        [Fact]
        public void FoldedBase64RoundTripsWithConsumerConfiguration() {
            // exercise the Fold=76 representation used by Secrets and FilesystemXml
            using var encrypt = new CryptoSymmetricEncryptAES(new() { Fold = 76, IterationsMin = 1000, IterationsRandomRange = 0 });
            using var decrypt = new CryptoSymmetricDecryptAES();
            var plaintext = new string('x', 300);
            var ciphertext = encrypt.Encrypt(plaintext, "password");
            Assert.Contains('\n', ciphertext);
            Assert.Equal(plaintext, decrypt.Decrypt(ciphertext, "password"));
        }
        [Fact]
        public void CorruptedIvInDiscardedMarkerBlockMayRemainUndetected() {
            // demonstrate without overstating detection: v1 does not authenticate its IV
            var separator = Base64Vector.IndexOf(',');
            var header = Base64Vector.Substring(0, separator + 1);
            var payload = Convert.FromBase64String(Base64Vector.Substring(separator + 1));
            payload[0] ^= 0x40;
            using var decrypt = new CryptoSymmetricDecryptAES();
            Assert.Equal("compatibility plaintext", decrypt.Decrypt(header + Convert.ToBase64String(payload), "correct horse"));
        }
        [Fact]
        public void CorruptedSaltOrCiphertextDoesNotRecoverKnownPlaintext() {
            // corruption may fail or produce garbage because v1 has no authenticity contract
            var separator = Base64Vector.IndexOf(',');
            var header = Base64Vector.Substring(0, separator + 1);
            var payload = Convert.FromBase64String(Base64Vector.Substring(separator + 1));
            foreach (var index in new[] { 16, payload.Length - 1 }) {
                var corrupted = (byte[])payload.Clone();
                corrupted[index] ^= 0x40;
                using var decrypt = new CryptoSymmetricDecryptAES();
                try {
                    Assert.NotEqual("compatibility plaintext", decrypt.Decrypt(header + Convert.ToBase64String(corrupted), "correct horse"));
                } catch (CryptographicException) {
                    // invalid padding is one permitted corruption outcome
                }
            }
        }
        [Fact]
        public void DirectStreamsLeaveCallerStreamsOpen() {
            // pin caller ownership of streams wrapped by AES
            using var encrypt = new CryptoSymmetricEncryptAES(new() { IterationsMin = 1000, IterationsRandomRange = 0 });
            using var output = new MemoryStream();
            using (var cryptoStream = encrypt.GetStream(output, "password")) {
                cryptoStream.Write(Encoding.UTF8.GetBytes("stream ownership"));
            }
            Assert.True(output.CanWrite);
            output.Position = 0;
            using var decrypt = new CryptoSymmetricDecryptAES();
            using (var cryptoStream = decrypt.GetStream(output, "password")) {
                using var reader = new StreamReader(cryptoStream, Encoding.UTF8, false, 1024, true);
                Assert.Equal("stream ownership", reader.ReadToEnd());
            }
            Assert.True(output.CanRead);
        }
        [Theory]
        [InlineData("not base64")]
        [InlineData("aes:?saltLength=16,")]
        [InlineData("aes:?saltLength=16,AAAA")]
        public void MalformedOrTruncatedBase64Fails(string ciphertext) {
            // reject malformed or incomplete persisted input predictably
            using var decrypt = new CryptoSymmetricDecryptAES();
            Assert.ThrowsAny<Exception>(() => decrypt.Decrypt(ciphertext, "password"));
        }
        [Fact]
        public void TruncatedBinaryFails() {
            // reject incomplete binary metadata
            var ciphertext = Convert.FromBase64String(BinaryVectorBase64);
            using var decrypt = new CryptoSymmetricDecryptAES();
            Assert.ThrowsAny<Exception>(() => decrypt.Decrypt(ciphertext.Take(30).ToArray(), "password"));
        }
        [Fact]
        public void OversizedHeaderFails() {
            // preserve the existing bounded header read
            using var decrypt = new CryptoSymmetricDecryptAES();
            Assert.ThrowsAny<Exception>(() => decrypt.Decrypt("aes:?version=" + new string('x', 600) + ",AAAA", "password"));
        }
        [Theory]
        [InlineData("aes:?saltLength=-1,")]
        [InlineData("aes:?saltLength=2000000,")]
        [InlineData("aes:?ivLength=-1,")]
        [InlineData("aes:?ivLength=2000,")]
        [InlineData("aes:?keySize=-1,")]
        [InlineData("aes:?blockSize=0,")]
        [InlineData("aes:?encoding=999,")]
        [InlineData("aes:?cipherMode=999,")]
        [InlineData("aes:?paddingMode=999,")]
        public void PathologicalHeaderValuesFailBeforeAllocation(string ciphertext) {
            // bound attacker-controlled values before allocation or setup
            using var decrypt = new CryptoSymmetricDecryptAES();
            Assert.ThrowsAny<Exception>(() => decrypt.Decrypt(ciphertext, "password"));
        }
        [Fact]
        public void InvalidIterationMetadataFails() {
            // reject zero iteration metadata before invoking PBKDF2
            using var decrypt = new CryptoSymmetricDecryptAES();
            Assert.Throws<FormatException>(() => decrypt.Decrypt("aes:," + Convert.ToBase64String(new byte[36]), "password"));
        }
        [Fact]
        public void WrongPasswordDoesNotRecoverKnownPlaintext() {
            // failure is observable but is not an authenticity guarantee
            using var decrypt = new CryptoSymmetricDecryptAES();
            try {
                Assert.NotEqual("compatibility plaintext", decrypt.Decrypt(Base64Vector, "wrong password"));
            } catch (CryptographicException) {
                // invalid padding is an expected wrong-password outcome
            }
        }
        [Theory]
        [InlineData(-1, 16, 1000, 0)]
        [InlineData(2000000, 16, 1000, 0)]
        [InlineData(16, -1, 1000, 0)]
        [InlineData(16, 16, 0, 0)]
        [InlineData(16, 16, 1000, -1)]
        public void InvalidEncryptionOptionsFailPredictably(int saltLength, int ivLength, int iterationsMin, int iterationsRandomRange) {
            // reject invalid local configuration before allocation or random selection
            using var encrypt = new CryptoSymmetricEncryptAES(new() {
                SaltLength = saltLength,
                IVLength = ivLength,
                IterationsMin = iterationsMin,
                IterationsRandomRange = iterationsRandomRange
            });
            Assert.ThrowsAny<ArgumentException>(() => encrypt.Encrypt("plaintext", "password"));
        }
    }
}
