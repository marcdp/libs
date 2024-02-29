
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("raw", "")]
    [ProtocolExample("raw:", "")]
    public class DBWriterRawFactory : IFactoryByUrlAndArgument<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterRaw.Settings>(src);
            return new DBWriterRaw(writer, leaveOpen, settings);
        }
    }

}
