
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Web;

namespace DProjects.Fs {

    [Protocol("union", "")]
    [ProtocolUsage("union:")]
    [ProtocolExample("union:?fs=mem:", "")]
    [ProtocolExample("union:?fs=mem:&fs=file:///d&fs=file:///c", "")]
    public class FilesystemUnionFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var isReadonly = UrlUtils.GetQueryValue<bool>(url.Query, "isReadonly");
            var result = new FilesystemMounter(isReadonly);
            var query = HttpUtility.ParseQueryString(url.Query);
            var filesystems = new List<IFilesystem>();
            foreach (var subUrl in query.GetValues("fs")) {
                filesystems.Add(fsFactory.Create(subUrl));
            }
            return new FilesystemUnion(filesystems.ToArray(), isReadonly);
        }

    }

}
