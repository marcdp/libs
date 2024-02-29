
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("json", "")]
    [ProtocolExample("json:", "")]
    public class DBReaderJsonFactory : IFactoryByUrlAndArgument<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderJson.Settings>(src);
            return new DBReaderJson(reader, leaveOpen, settings);
        }
    }

}
