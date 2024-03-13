using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("sha1", "")]
    public class CryptoHashSHA1Factory : IFactoryByUrl<ICryptoHash> {
        public ICryptoHash Create(string src) {
            return new CryptoHashSHA1(UrlUtils.Deserialize<CryptoHashSHA1.Options>(src));
        }
    }


}
