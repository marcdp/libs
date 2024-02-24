
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("xmld", "")]
    [ProtocolExample("xmld:", "")]
    public class DBWriterXmlDocumentsFactory(TextWriter writer) : IFactoryByUrl<IDBWriter> {

        public IDBWriter Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterXmlDocuments.Settings>(src);
            return new DBWriterXmlDocuments(writer, leaveOpen, settings);
        }
    }

}
