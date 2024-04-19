
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;

using Microsoft.Extensions.Logging;


namespace DProjects.Cache {

    [Protocol("fs-dir", "")]
    [ProtocolExample("fs-dir:/path/to/dir", "")]
    public class BlobCacheFsDirFactory(IFilesystem filesystem, ILogger<IFilesystem> logger) : IFactoryByUrl<IBlobCache> {
        public IBlobCache Create(string src) {
            var url = new System.Uri(src);
            return new BlobCacheFsDir(filesystem, url.AbsolutePath, logger);
        }

    }

}
