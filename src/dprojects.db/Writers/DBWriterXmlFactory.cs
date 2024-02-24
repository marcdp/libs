
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("xml", "")]
    [ProtocolExample("xml:?dateTimeFormat=&indent=false&omitXmlDeclaration=true&indentChars=...", "")]
    public class DBWriterXmlFactory(TextWriter writer) : IFactoryByUrl<IDBWriter> {

        public IDBWriter Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterXml.Settings>(src);
            return new DBWriterXml(writer, leaveOpen, settings);
        }
    }

}
