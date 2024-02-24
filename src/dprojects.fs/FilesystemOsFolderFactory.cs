
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.Runtime.InteropServices;

namespace DProjects.Fs {

    [Protocol("os-folder", "")]
    [ProtocolUsage("os-folder:OS_FOLDER")]
    [ProtocolExample("os-folder://Desktop", "")]
    [ProtocolExample("os-folder://Desktop/my-folder?isReadonly=true", "")]
    [ProtocolExample("os-folder://MyDocuments", "")]
    [ProtocolExample("os-folder://ApplicationData", "")]
    [ProtocolExample("os-folder://LocalApplicationData", "")]
    public class FilesystemOsFolderFactory() : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var isReadonly = UrlUtils.ParseQueryString<bool>(url.Query, "isReadonly");
            System.Environment.SpecialFolder specialFolder;            
            if (!System.Enum.TryParse(url.Host, true, out specialFolder)) throw new NotImplementedException("Unable to create filesystem: invalid os special folder: " + url.Host);
            var absolutePath = System.IO.Path.Combine(System.Environment.GetFolderPath(specialFolder), UrlUtils.UrlDecode(url.AbsolutePath).Substring(1).Replace('/', System.IO.Path.DirectorySeparatorChar));
            return new FilesystemLocal(absolutePath, isReadonly, false, false);
        }

    }

}
