using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Fs;
using DProjects.Utils;
using System;

namespace DProjects.Log.Storage {

    [Protocol("fs-file", "")]
    [ProtocolUsage("fs-file://PATH-TO-FILE")]
    [ProtocolExample("fs-file://PATH-TO-FILE", "")]
    [ProtocolExample("fs-file://PATH-TO-FILE?format=json", "")]
    [ProtocolExample("fs-file://PATH-TO-FILE?format=rat", "")]
    [ProtocolExample("fs-file://PATH-TO-FILE?format=classic", "")]
    public class LogStorageFsFileFactory(IFilesystem filesystem, IFactoryByUrl<ILogStorageEntryDeserializer> logStorageEntryDeserializer) : IFactoryByUrl<ILogStorage> {

        public ILogStorage Create(string src) {
            var url = new Uri(src);
            var format = UrlUtils.GetQueryValue(url.Query, "format", "");
            var deserializer = logStorageEntryDeserializer.Create(format);
            return new LogStorageFsFile(filesystem, url.AbsolutePath, deserializer);
        }
         
    }

}


