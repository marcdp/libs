
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("xml", "")]
    [ProtocolExample("xml:", "")]
    public class DBReaderXmlFactory(TextReader reader) : IFactoryByUrl<IDBReader> {

        public IDBReader Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderXml.Settings>(src);
            return new DBReaderXml(reader, leaveOpen, settings);
        }
    }

}
