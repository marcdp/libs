using DProjects.Utils;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Buffers.Text;
using System.ComponentModel;
using Xunit;

namespace DProjects.Crypto.Tests
{
    public class SymmetricTests {

        [Theory]
        [InlineData("123456 hello world", "1", "caesar:,234567!ifmmp!xpsme")]
        public void CaesarTest(string input, string password, string expected) {
            //encrypt
            using var algEncrypt = new CryptoSymmetricEncryptCaesar(new() {
                Header = true,
                Separator = ','
            });
            var computed = algEncrypt.Encrypt(input, password);
            Assert.Equal(expected, computed);
            //decrypt
            using var algDecrypt = new CryptoSymmetricDecryptCaesar(new() {
                Header = true,
                Separator = ','
            });
            Assert.Equal(input, algDecrypt.Decrypt(computed, password));    
        }


        [Theory]
        [InlineData("aes:?iterationsMin=1000&iterationsRandomRange=0&saltLength=0&iv=MTIzNDU2Nzg5MDEyMzQ1Ng==", "123456 hello world", "12345", "aes:?saltLength=0,MTIzNDU2Nzg5MDEyMzQ1NugDAAD7NekmPdsTB8zgBCcA637D0yHpdJYzxeOfblbSRbuWXg==")]
        public void AesTest(string algorithm, string input, string password, string expected) {
            using var algEncrypt = new CryptoSymmetricEncryptAESFactory().Create(algorithm);
            var computed = algEncrypt.Encrypt(input, password);
            Assert.Equal(expected, computed);
            //decrypt 
            using var algDecrypt = new CryptoSymmetricDecryptAESFactory().Create("aes:");
            Assert.Equal(input, algDecrypt.Decrypt(computed, password));
        }

    }

}