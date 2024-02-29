
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("html", "")]
    [ProtocolExample("html:", "")]
    public class DBWriterHtmlFactory : IFactoryByUrlAndArgument<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterHtml.Settings>(src);
            return new DBWriterHtml(writer, leaveOpen, settings);
        }
    }

}
