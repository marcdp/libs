
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("jsonl", "")]
    [ProtocolExample("jsonl:", "")]
    public class DBReaderJsonLinesFactory(TextReader reader) : IFactoryByUrl<IDBReader> {

        public IDBReader Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderJsonLines.Settings>(src);
            return new DBReaderJsonLines(reader, leaveOpen, settings);
        }
    }

}
