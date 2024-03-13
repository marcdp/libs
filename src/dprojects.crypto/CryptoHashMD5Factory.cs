using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("md5", "")]
    public class CryptoHashMD5Factory : IFactoryByUrl<ICryptoHash> {
        public ICryptoHash Create(string src) {
            return new CryptoHashMD5(UrlUtils.Deserialize<CryptoHashMD5.Options>(src));
        }
    }


}
