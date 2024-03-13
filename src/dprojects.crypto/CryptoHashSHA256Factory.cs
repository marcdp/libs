using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("sha256", "")]
    public class CryptoHashSHA256Factory : IFactoryByUrl<ICryptoHash> {
        public ICryptoHash Create(string src) {
            return new CryptoHashSHA256(UrlUtils.Deserialize<CryptoHashSHA256.Options>(src));
        }
    }


}
