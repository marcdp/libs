using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Log.Storage {

    [Protocol("fs-dir", "")]
    [ProtocolUsage("fs-dir://PATH-TO-DIR")]
    [ProtocolExample("fs-dir://PATH-TO-DIR", "")]
    [ProtocolExample("fs-dir://PATH-TO-DIR?format=json&filePattern=application-*.log&recursive=false", "")]
    [ProtocolExample("fs-dir://PATH-TO-DIR?format=rat", "")]
    [ProtocolExample("fs-dir://PATH-TO-DIR?format=classic", "")]
    public class LogStorageFsDirFactory(IFilesystem filesystem, IFactoryByUrl<ILogStorageEntryDeserializer> logStorageEntryDeserializer) : IFactoryByUrl<ILogStorage> {

        public ILogStorage Create(string src) {
            var url = new Uri(src);
            var filePattern = UrlUtils.GetQueryValue(url.Query, "filePattern", "");
            var fileName = UrlUtils.GetQueryValue(url.Query, "fileName", "");
            var fileExtension = UrlUtils.GetQueryValue(url.Query, "fileExtension", ".log");
            var recursive = UrlUtils.GetQueryValue(url.Query, "recursive", false);
            var deserializer = logStorageEntryDeserializer.Create(UrlUtils.GetQueryValue(url.Query, "format", "auto"));
            if (!string.IsNullOrWhiteSpace(filePattern)) return new LogStorageFsDir(filesystem, url.AbsolutePath, filePattern, recursive, deserializer);
            return new LogStorageFsDir(filesystem, url.AbsolutePath, fileName, fileExtension, recursive, deserializer);
        }

    }

}


