
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("raw", "")]
    [ProtocolExample("raw:", "")]
    public class DBWriterRawFactory(TextWriter writer) : IFactoryByUrl<IDBWriter> {

        public IDBWriter Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterRaw.Settings>(src);
            return new DBWriterRaw(writer, leaveOpen, settings);
        }
    }

}
