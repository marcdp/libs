using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Vault {

    [Protocol("fs-file", "")]
    [ProtocolUsage("fs-file://PATH-TO-FILE")]
    [ProtocolExample("fs-file:///var/log/file.log?autoFlush=true&useWriterThread=true&logFormatter=rat&level=debug", "")]
    public class VaultXmlFactory(IFilesystem filesystem) : IFactoryByUrl<IVault> {

        public IVault Create(string src) {
            var url = new Uri(src);
            //var truncate = UrlUtils.GetQueryValue(url.Query, "truncate", false);
            //var autoFlush = UrlUtils.GetQueryValue(url.Query, "autoFlush", false);
            //var useWriterThread = UrlUtils.GetQueryValue(url.Query, "useWriterThread", true);
            //var logFormatter = logFormatterFactory.Create(UrlUtils.GetQueryValue(url.Query, "format", "json"));
            //var level = UrlUtils.GetQueryValue(url.Query, "level", LogLevel.Information);
            var password = "";
            return new VaultXml(filesystem, url.AbsolutePath, password);
        }

    }

}


