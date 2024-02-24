
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("plain", "")]
    [ProtocolExample("plain:", "")]
    public class DBWriterPlainFactory(TextWriter writer) : IFactoryByUrl<IDBWriter> {

        public IDBWriter Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterPlain.Settings>(src);
            return new DBWriterPlain(writer, leaveOpen, settings);
        }
    }

}
