
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("json", "")]
    [ProtocolExample("json:", "")]
    public class DBWriterJsonFactory : IFactoryByUrlAndArgument<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterJson.Settings>(src);
            return new DBWriterJson(writer, leaveOpen, settings);
        }
    }

}
