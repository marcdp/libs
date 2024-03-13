using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("caesar", "")]
    [ProtocolExample("caesar:", "")]
    public class CryptoSymmetricDecryptCaesarFactory : IFactoryByUrl<ICryptoSymmetricDecrypt> {
        public ICryptoSymmetricDecrypt Create(string src) {
            return new CryptoSymmetricDecryptCaesar(UrlUtils.Deserialize<CryptoSymmetricDecryptCaesar.Options>(src));
        }
    }

}
