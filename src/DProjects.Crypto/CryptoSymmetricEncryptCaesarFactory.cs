using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("caesar", "")]
    [ProtocolExample("caesar:", "")]
    public class CryptoSymmetricEncryptCaesarFactory : IFactoryByUrl<ICryptoSymmetricEncrypt> {
        public ICryptoSymmetricEncrypt Create(string src) {
            return new CryptoSymmetricEncryptCaesar(UrlUtils.Deserialize<CryptoSymmetricEncryptCaesar.Options>(src, new() {
                ThrowExceptionIfPropertyNotFound = false
            }));
        }
    }

}
