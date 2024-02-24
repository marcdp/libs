
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("html", "")]
    [ProtocolExample("html:", "")]
    public class DBWriterHtmlFactory(TextWriter writer) : IFactoryByUrl<IDBWriter> {

        public IDBWriter Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterHtml.Settings>(src);
            return new DBWriterHtml(writer, leaveOpen, settings);
        }
    }

}
