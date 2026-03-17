
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

using System;
using System.Linq;

namespace DProjects.Fs.Http {

    [Protocol("http", "")]
    [ProtocolUsage("http://server/path/to/share")]
    [ProtocolExample("http://127.0.0.1:8092/", "")]
    [ProtocolExample("http://127.0.0.1:8092/?authScheme=none", "")]
    [ProtocolExample("http://127.0.0.1:8092/?authScheme=basic", "")]
    [ProtocolExample("http://127.0.0.1:8092/?authScheme=hmac", "")]
    [ProtocolExample("http://127.0.0.1:8092/?isReadonly=true", "")]
    public class FilesystemHttpFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var maxFileUploadSize = UrlUtils.GetQueryValue<int>(url.Query, "maxFileUploadSize", 4 * 1024 * 1024);
            var isReadonly = UrlUtils.GetQueryValue<bool>(url.Query, "isReadonly", false); 
            var authScheme = UrlUtils.GetQueryValue<FilesystemHttp.AuthSchemes>(url.Query, "authScheme", FilesystemHttp.AuthSchemes.Hmac);    
            return new FilesystemHttp(url, maxFileUploadSize, authScheme, isReadonly);
        }

    }
     
}
