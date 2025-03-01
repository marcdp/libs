
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
            var path = url.AbsolutePath;
            var init = UrlUtils.GetQueryValue(url.Query, "init", false);
            if (DProjects.Utils.EnvironmentUtils.IsWindows()) {
                path = path[1] + ":" + path.Substring(2).Replace('/', System.IO.Path.DirectorySeparatorChar);
            }
            var filesystem = filesystemFactory.Create(path + (init ? "?init=true" : ""));
            return new BlobCacheFsDir(filesystem, "/", logger);
        }

    }

}
