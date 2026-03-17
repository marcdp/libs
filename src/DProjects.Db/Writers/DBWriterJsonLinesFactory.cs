
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("jsonl", "")]
    [ProtocolExample("jsonl:", "")]
    public class DBWriterJsonLinesFactory : IFactoryByUrl<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterJsonLines.Settings>(src);
            return new DBWriterJsonLines(writer, leaveOpen, settings);
        }
    }

}
