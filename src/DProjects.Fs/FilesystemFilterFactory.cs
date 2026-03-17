
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Web;

namespace DProjects.Fs {

    [Protocol("filter", "")]
    [ProtocolUsage("filter:FSURL![?exclude=PATH][&exclude=PATTERN][&include=PATH][&include=PATTERN]")]
    [ProtocolExample("filter:file:///path/to/directory!/?exclude=/dir2", "")]
    [ProtocolExample("filter:/path/to/directory!/?exclude=*.txt", "")]
    [ProtocolExample("filter:/path/to/directory!/?exclude=*&include=/folder/*.txt", "")]
    [ProtocolExample("filter:/path/to/directory!/?exclude=*&include=/folder1&include=/folder2", "")]
    public class FilesystemFilterFactory(IFactoryByUrl<IFilesystem> fsFactory) : IFactoryByUrl<IFilesystem> {
        public IFilesystem Create(string src) {
            if (src.IndexOf("!") == -1) src += "!";
            var innerUrl = src.Substring(7, src.LastIndexOf("!") - 7);
            var bangPath = src.Substring(src.LastIndexOf("!") + 1);
            var bangQuery = "";
            if (bangPath.IndexOf("?") != -1) {
                bangQuery = bangPath.Substring(bangPath.IndexOf("?"));
                bangPath = bangPath.Substring(0, bangPath.IndexOf("?"));
            }
            if (bangPath.Length == 0) bangPath = "/";
            bangPath = UrlUtils.UrlDecode(bangPath);
            var filters = new List<FilesystemFilter.Filter>();
            var parameters = HttpUtility.ParseQueryString(bangQuery);
            foreach (var key in parameters.AllKeys) {
                if (key != null) {
                    foreach (var value in parameters.GetValues(key)) {
                        if (key == "exclude") {
                            filters.Add(new FilesystemFilter.Filter() { Type = FilesystemFilter.FilterType.Exclude, Value = value });
                        } else if (key == "include") {
                            filters.Add(new FilesystemFilter.Filter() { Type = FilesystemFilter.FilterType.Include, Value = value });
                        }
                    }
                }
            }
            var filesystem = fsFactory.Create(innerUrl);
            return new FilesystemFilter(filesystem, filters.ToArray(), System.StringComparison.CurrentCultureIgnoreCase);
        }

    }

}
