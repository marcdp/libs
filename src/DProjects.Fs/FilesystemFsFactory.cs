
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

using Microsoft.Extensions.DependencyInjection;

using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace DProjects.Fs {

    [Protocol("fs", "")]
    [ProtocolUsage("fs:///PATH")]
    [ProtocolExample("fs:///path/to/dir", "")]
    public class FilesystemFsFactory(IFilesystem fs) : IFactoryByUrl<IFilesystem> {
        public IFilesystem Create(string src) {
            var url = new System.Uri(src);
            var init = UrlUtils.GetQueryValue<bool>(url.Query, "init");
            var fsMounter = new FilesystemMounter(true);
            fsMounter.Mount("/", fs, false, url.AbsolutePath);
            if (init) fsMounter.CreateDirectory("/");
            return fsMounter;
        }

    }

}
