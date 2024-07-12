
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
            var fsMounter = new FilesystemMounter(true);
            fsMounter.Mount("/", fs, true, url.AbsolutePath);
            return fsMounter;
        }

    }

}
