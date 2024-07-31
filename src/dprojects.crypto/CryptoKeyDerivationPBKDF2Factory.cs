using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("pbkdf2", "")]
    [ProtocolExample("pbkdf2://?[iterations=XX][&keyLength=32]", "")]
    public class CryptoKeyDerivationPBKDF2Factory : IFactoryByUrl<ICryptoKeyDerivation> {
        public ICryptoKeyDerivation Create(string src) {
            return new CryptoKeyDerivationPBKDF2(UrlUtils.Deserialize<CryptoKeyDerivationPBKDF2.Options>(src, new() {
                 ThrowExceptionIfPropertyNotFound = false 
            }));
        }
    }

}
