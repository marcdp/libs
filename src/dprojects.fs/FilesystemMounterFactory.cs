
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System;
using System.Web;

namespace DProjects.Fs {

    [Protocol("mounter", "")]
    [ProtocolUsage("mounter:")]
    [ProtocolExample("mounter:?/=mem:", "")]
    [ProtocolExample("mounter:?/=mem:&/mypath=file:///path/to/dir", "")]
    public class FilesystemMounterFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<IFilesystem> {

        public IFilesystem Create(string src) {
            var url = new Uri(src);
            var isReadonly = UrlUtils.GetQueryValue<bool>(url.Query, "isReadonly");
            var result = new FilesystemMounter(isReadonly);
            var query = HttpUtility.ParseQueryString(url.Query);
            foreach (var path in query.AllKeys) {
                if (path != null && path.StartsWith("/")) {
                    var subUrl = query[path];
                    var prefix = query["prefix" + path] ?? "";
                    var subFs = fsFactory.Create(subUrl);
                    result.Mount(path, subFs, true, prefix);
                }
            }
            return result;
        }

    }

}
