
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;

using Microsoft.Extensions.Logging;


namespace DProjects.Cache {

    [Protocol("file", "")]
    [ProtocolExample("file:/path/to/dir", "")]
    public class BlobCacheFileFactory(IFactoryByUrl<IFilesystem> filesystemFactory, ILogger<IFilesystem> logger) : IFactoryByUrl<IBlobCache> {
        public IBlobCache Create(string src) {
            var url = new System.Uri(src);
            var path = url.LocalPath;
            var init = UrlUtils.GetQueryValue(url.Query, "init", false);
            var filesystem = filesystemFactory.Create(path + (init ? "?init=true" : ""));
            return new BlobCacheFsDir(filesystem, "/", logger);
        }

    }

}