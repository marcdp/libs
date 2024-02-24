
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("raw", "")]
    [ProtocolExample("raw:", "")]
    [ProtocolExample("raw:?columnSeparator=.", "")]
    public class DBReaderRawFactory(TextReader reader) : IFactoryByUrl<IDBReader> {

        public IDBReader Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderRaw.Settings>(src);
            return new DBReaderRaw(reader, leaveOpen, settings);
        }
    }

}
