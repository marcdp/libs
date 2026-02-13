using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("aes", "")]
    [ProtocolUsage("")]
    [ProtocolExample("aes:?fold=76", "")]
    [ProtocolExample("aes:?encoding=binary", "")]
    [ProtocolExample("aes:?encoding=base64&cipher=GCM&ivLength=12", "")]
    public class CryptoSymmetricEncryptAESFactory : IFactoryByUrl<ICryptoSymmetricEncrypt> {
        public ICryptoSymmetricEncrypt Create(string src) {
            return new CryptoSymmetricEncryptAES(UrlUtils.Deserialize<CryptoSymmetricEncryptAES.Options>(src, new() {
                ThrowExceptionIfPropertyNotFound = false
            }));
        }
    }

}
