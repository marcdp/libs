using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;

namespace DProjects.Crypto.Tests {

    public class FactoryTests {

        // methods
        [Theory]
        [InlineData("md5:", typeof(CryptoHashMD5))]
        [InlineData("sha1:", typeof(CryptoHashSHA1))]
        [InlineData("sha256:", typeof(CryptoHashSHA256))]
        [InlineData("sha512:", typeof(CryptoHashSHA512))]
        [InlineData("bcrypt:", typeof(CryptoHashBCRYPT))]
        public void HashProtocolsResolveThroughAssemblyDiscovery(string url, Type expectedType) {
            // verify public hash protocol dispatch
            using var provider = CreateProvider();
            using var algorithm = provider.GetRequiredService<IFactoryByUrl<ICryptoHash>>().Create(url);
            Assert.Equal(expectedType, algorithm.GetType());
        }
        [Fact]
        public void ConfigurableProtocolsApplyDocumentedOptionNames() {
            // verify option names used by public examples are accepted and observable
            using var provider = CreateProvider();
            using var keyDerivation = provider.GetRequiredService<IFactoryByUrl<ICryptoKeyDerivation>>().Create("pbkdf2:?iterations=2&keyLength=16&prf=HMACSHA512");
            Assert.Equal(16, keyDerivation.Derive("password", new byte[] { 1, 2, 3 }).Length);
            using var encrypt = provider.GetRequiredService<IFactoryByUrl<ICryptoSymmetricEncrypt>>().Create("aes:?iterationsMin=1000&iterationsRandomRange=0&saltLength=12&cipherMode=CBC&ivLength=16");
            var ciphertext = encrypt.Encrypt("factory options", "password");
            Assert.StartsWith("aes:?saltLength=12,", ciphertext);
            using var decrypt = provider.GetRequiredService<IFactoryByUrl<ICryptoSymmetricDecrypt>>().Create("aes:");
            Assert.Equal("factory options", decrypt.Decrypt(ciphertext, "password"));
        }
        [Fact]
        public void CaesarProtocolsResolveAndRoundTrip() {
            // verify symmetric protocol dispatch for the compatibility transform
            using var provider = CreateProvider();
            using var encrypt = provider.GetRequiredService<IFactoryByUrl<ICryptoSymmetricEncrypt>>().Create("caesar:");
            using var decrypt = provider.GetRequiredService<IFactoryByUrl<ICryptoSymmetricDecrypt>>().Create("caesar:");
            Assert.IsType<CryptoSymmetricEncryptCaesar>(encrypt);
            Assert.IsType<CryptoSymmetricDecryptCaesar>(decrypt);
            Assert.Equal("factory", decrypt.Decrypt(encrypt.Encrypt("factory", "1"), "1"));
        }
        [Fact]
        public void UnknownOptionsRemainIgnoredByV1Factories() {
            // pin intentional common URL-deserializer behavior
            Assert.IsType<CryptoHashSHA256>(new CryptoHashSHA256Factory().Create("sha256:?unknown=value"));
            Assert.IsType<CryptoSymmetricEncryptAES>(new CryptoSymmetricEncryptAESFactory().Create("aes:?unknown=value"));
        }
        [Theory]
        [InlineData("aes:?saltLength=invalid")]
        [InlineData("pbkdf2:?iterations=invalid")]
        public void MalformedOptionValuesFailPredictably(string url) {
            // conversion failures are surfaced rather than silently defaulted
            Assert.ThrowsAny<Exception>(() => url.StartsWith("aes:") ? new CryptoSymmetricEncryptAESFactory().Create(url) : new CryptoKeyDerivationPBKDF2Factory().Create(url));
        }


        // methods (private)
        private static ServiceProvider CreateProvider() {
            var services = new ServiceCollection();
            services.AddFactoryByUrl<ICryptoHash>(configuration => configuration.AddFactoriesFromAssembly<DProjects.Crypto.Assembly>());
            services.AddFactoryByUrl<ICryptoKeyDerivation>(configuration => configuration.AddFactoriesFromAssembly<DProjects.Crypto.Assembly>());
            services.AddFactoryByUrl<ICryptoSymmetricEncrypt>(configuration => configuration.AddFactoriesFromAssembly<DProjects.Crypto.Assembly>());
            services.AddFactoryByUrl<ICryptoSymmetricDecrypt>(configuration => configuration.AddFactoriesFromAssembly<DProjects.Crypto.Assembly>());
            return services.BuildServiceProvider();
        }
    }
}
