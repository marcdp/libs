using DProjects.Utils;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Buffers.Text;
using Xunit;

namespace DProjects.Crypto.Tests
{
    public class KeyDerivationTests {

        [Theory]
        [InlineData("123456", 1000, 16, KeyDerivationPrf.HMACSHA256, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, "6E3849111BE8D3763422666E93C55")]
        [InlineData("123456123456123456", 10000, 32, KeyDerivationPrf.HMACSHA256, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 }, "775ABF1EAA90A867FF3617CD96C9D2A6CA4D11FF7E6C78DEB76068521D1539EB")]
        public void PBKDF2Test(string input, int iterations, int keyLength, KeyDerivationPrf prf, byte[] salt, string expected) {
            using var alg = new CryptoKeyDerivationPBKDF2(new() {
                Iterations = iterations,
                KeyLength = keyLength,
                Prf = prf
            });
            Assert.Equal(expected, HexUtils.Hex(alg.Derive(input, salt)));
        }

        [Theory]
        [InlineData(1, 16, KeyDerivationPrf.HMACSHA1)]
        [InlineData(2, 24, KeyDerivationPrf.HMACSHA256)]
        [InlineData(1000, 32, KeyDerivationPrf.HMACSHA512)]
        public void PBKDF2ExplicitOptionsAreDeterministic(int iterations, int keyLength, KeyDerivationPrf prf) {
            // preserve deterministic parameter semantics across supported PRFs and lengths
            var options = new CryptoKeyDerivationPBKDF2.Options() { Iterations = iterations, KeyLength = keyLength, Prf = prf };
            using var first = new CryptoKeyDerivationPBKDF2(options);
            using var second = new CryptoKeyDerivationPBKDF2(options);
            var salt = new byte[] { 1, 2, 3, 4 };
            Assert.Equal(first.Derive("password", salt), second.Derive("password", salt));
            Assert.Equal(keyLength, first.Derive("password", salt).Length);
        }
        [Theory]
        [InlineData(0, 16)]
        [InlineData(-1, 16)]
        [InlineData(1000, 0)]
        [InlineData(1000, -1)]
        public void PBKDF2InvalidNumericOptionsFail(int iterations, int keyLength) {
            // retain deterministic rejection of invalid PBKDF2 parameters
            using var algorithm = new CryptoKeyDerivationPBKDF2(new() { Iterations = iterations, KeyLength = keyLength });
            Assert.ThrowsAny<ArgumentException>(() => algorithm.Derive("password", new byte[] { 1, 2, 3 }));
        }
         

    }

}
