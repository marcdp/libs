
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("raw", "")]
    [ProtocolExample("raw:", "")]
    [ProtocolExample("raw:?columnSeparator=.", "")]
    public class DBReaderRawFactory : IFactoryByUrl<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderRaw.Settings>(src);
            return new DBReaderRaw(reader, leaveOpen, settings);
        }
    }

}
