
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("plain", "")]
    [ProtocolExample("plain:", "")]
    [ProtocolExample("plain:", "")]
    public class DBReaderPlainFactory(TextReader reader) : IFactoryByUrl<IDBReader> {

        public IDBReader Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderPlain.Settings>(src);
            return new DBReaderPlain(reader, leaveOpen, settings);
        }
    }

}
