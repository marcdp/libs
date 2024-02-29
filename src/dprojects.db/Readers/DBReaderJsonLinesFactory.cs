
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("jsonl", "")]
    [ProtocolExample("jsonl:", "")]
    public class DBReaderJsonLinesFactory : IFactoryByUrlAndArgument<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderJsonLines.Settings>(src);
            return new DBReaderJsonLines(reader, leaveOpen, settings);
        }
    }

}
