using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {

    [Protocol("bcrypt", "")]
    [ProtocolUsage("bcrypt:")]
    public class CryptoHashBCRYPTFactory : IFactoryByUrl<ICryptoHash> {
        public ICryptoHash Create(string src) {
            return new CryptoHashBCRYPT(UrlUtils.Deserialize<CryptoHashBCRYPT.Options>(src));
        }
    }


}
