
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("jsonl", "")]
    [ProtocolExample("jsonl:", "")]
    public class DBWriterJsonLinesFactory(TextWriter writer) : IFactoryByUrl<IDBWriter> {

        public IDBWriter Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterJsonLines.Settings>(src);
            return new DBWriterJsonLines(writer, leaveOpen, settings);
        }
    }

}
