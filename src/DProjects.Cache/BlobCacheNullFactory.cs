
using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Cache {

    [Protocol("null", "")]
    public class BlobCacheNullFactory : IFactoryByUrl<IBlobCache> {
        public IBlobCache Create(string src) {
            return new BlobCacheNull();
        }

    }

}
