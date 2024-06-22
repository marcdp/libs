using System;

using DProjects.Utils;
using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Fs {

    [Protocol("mem", "")]
    [ProtocolUsage("mem:")]
    [ProtocolExample("mem:", "")]
    public class FilesystemMemFactory : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) { 
            var url = new Uri(src);
            var isReadonly = UrlUtils.GetQueryValue<bool>(url.Query, "isReadonly", false);
            var autoFlush = UrlUtils.GetQueryValue<bool>(url.Query, "autoFlush", false);
            return new FilesystemMem(isReadonly, autoFlush);
        }

    }

}
