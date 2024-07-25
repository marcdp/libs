
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;

using Microsoft.Extensions.DependencyInjection;

using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace DProjects.Fs {

    [Protocol("fs-dir", "")]
    [ProtocolUsage("fs-dir:///PATH")]
    [ProtocolExample("fs-dir:///path/to/dir", "")]
    public class FilesystemFsDirFactory(IFilesystem fs) : IFactoryByUrl<IFilesystem> {
        public IFilesystem Create(string src) {
            var url = new System.Uri(src);
            var init = UrlUtils.GetQueryValue<bool>(url.Query, "init");
            var fsMounter = new FilesystemMounter(true);
            fsMounter.Mount("/", fs, true, url.AbsolutePath);
            if (init) fsMounter.CreateDirectory("/");
            return fsMounter;
        }

    }

}
