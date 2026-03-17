
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("xmld", "")]
    [ProtocolExample("xmld:", "")]
    [ProtocolExample("xmld:?inferDataTypes=false", "")]
    public class DBReaderXmlDocumentsFactory : IFactoryByUrl<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderXmlDocuments.Settings>(src);
            return new DBReaderXmlDocuments(reader, leaveOpen, settings);
        }
    }

}
