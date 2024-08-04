
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("xmld", "")]
    [ProtocolExample("xmld:", "")]
    public class DBWriterXmlDocumentsFactory : IFactoryByUrl<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterXmlDocuments.Settings>(src);
            return new DBWriterXmlDocuments(writer, leaveOpen, settings);
        }
    }

}
