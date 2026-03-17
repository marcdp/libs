using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("caesar", "")]
    [ProtocolExample("caesar:", "")]
    [ProtocolExample("aes:?fold=76", "")]
    [ProtocolExample("aes:?encoding=binary", "")]
    [ProtocolExample("aes:?encoding=base64&cipher=ECB&ivLength=32", "")]
    public class CryptoSymmetricEncryptCaesarFactory : IFactoryByUrl<ICryptoSymmetricEncrypt> {
        public ICryptoSymmetricEncrypt Create(string src) {
            return new CryptoSymmetricEncryptCaesar(UrlUtils.Deserialize<CryptoSymmetricEncryptCaesar.Options>(src, new() {
                ThrowExceptionIfPropertyNotFound = false
            }));
        }
    }

}
