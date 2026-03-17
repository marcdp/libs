
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("markdown", "")]
    [ProtocolExample("markdown:", "")]
    public class DBWriterMarkdownFactory : IFactoryByUrl<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterMarkdown.Settings>(src);
            return new DBWriterMarkdown(writer, leaveOpen, settings);
        }
    }

}
