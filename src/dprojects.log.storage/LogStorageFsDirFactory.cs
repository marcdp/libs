using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Log.Storage {

    [Protocol("fs-dir", "")]
    [ProtocolUsage("fs-dir://PATH-TO-DIR")]
    [ProtocolExample("fs-dir://PATH-TO-DIR", "")]
    [ProtocolExample("fs-dir://PATH-TO-DIR?format=json&filename=YYYY-MM-DD&fileExtension=.log&recursive=false", "")]
    [ProtocolExample("fs-dir://PATH-TO-DIR?format=rat", "")]
    [ProtocolExample("fs-dir://PATH-TO-DIR?format=classic", "")]
    public class LogStorageFsDirFactory(IFilesystem filesystem, IFactoryByUrl<ILogStorageEntryDeserializer> logStorageEntryDeserializer) : IFactoryByUrl<ILogStorage> {

        public ILogStorage Create(string src) {
            var url = new Uri(src);
            var fileName = UrlUtils.GetQueryValue(url.Query, "fileName", "*.log");
            var fileExtension = UrlUtils.GetQueryValue(url.Query, "fileExtension", ".log");
            var recursive = UrlUtils.GetQueryValue(url.Query, "recursive", false);
            var deserializer = logStorageEntryDeserializer.Create(UrlUtils.GetQueryValue(url.Query, "format", "auto"));
            return new LogStorageFsDir(filesystem, url.AbsolutePath, fileName, fileExtension, recursive, deserializer);
        }
         
    }

}


