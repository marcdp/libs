
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.Runtime.InteropServices;

namespace DProjects.Fs {

    [Protocol("file", "")]
    [ProtocolUsage("file://")]
    [ProtocolExample("file:///D:/path/to/dir", "")]
    public class FilesystemLocalFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var isReadonly = UrlUtils.GetQueryValue<bool>(url.Query, "isReadonly");
            var create = UrlUtils.GetQueryValue<bool>(url.Query, "create");
            var file = UrlUtils.GetQueryValue<bool>(url.Query, "file");
            var absolutePath = UrlUtils.UrlDecode(url.AbsolutePath);
            if (url.Host == "") {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    string aux = absolutePath;
                    if (absolutePath.StartsWith("/")) {
                        aux = absolutePath.Substring(1);
                    }
                    if (aux.Length > 1 && aux[1] == '/') {
                        aux = aux.Substring(0, 1) + ":" + aux.Substring(1);
                    } else if (aux.Length == 1) {
                        aux += ":/";
                    }
                    aux = aux.Replace('/', System.IO.Path.DirectorySeparatorChar);
                    if (!System.IO.Path.IsPathRooted(aux)) {
                        aux = System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), aux));
                    }
                    return new FilesystemLocal(aux, isReadonly, create, file);
                } else {
                    return new FilesystemLocal(absolutePath, isReadonly, create, file);
                }
            } else {
                return new FilesystemLocal("\\\\" + url.Host + absolutePath.Replace("/", "\\"), isReadonly, create, file);
            }
        }

    }

}
