
using DProjects.Factories;
using DProjects.Factories.Attributes;
using System;
using System.Linq;

namespace DProjects.Fs.Http {

    [Protocol("http", "")]
    [ProtocolUsage("http://server/path/to/share")]
    [ProtocolExample("http://127.0.0.1:8092/", "")]
    public class FilesystemHttpFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            int maxFileUploadSize = 4 * 1024 * 1024;
            var isReadonly = false;
            return new FilesystemHttp(url, maxFileUploadSize, isReadonly);
        }

    }
     
}
