using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

namespace DProjects.Crypto {
    [Protocol("sha512", "")]
    public class CryptoHashSHA512Factory : IFactoryByUrl<ICryptoHash> {
        public ICryptoHash Create(string src) {
            return new CryptoHashSHA512(UrlUtils.Deserialize<CryptoHashSHA512.Options>(src, new() {
                ThrowExceptionIfPropertyNotFound = false
            }));
        }
    }


}
