
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.Runtime.InteropServices;

namespace DProjects.Fs {

    [Protocol("temp", "")]
    [ProtocolUsage("temp:")]
    [ProtocolExample("temp:", "")]
    public class FilesystemTempFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var file = UrlUtils.GetQueryValue<bool>(url.Query, "file");
            var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString());
            return new FilesystemTemp(path, file);
        }

    }
    

}
