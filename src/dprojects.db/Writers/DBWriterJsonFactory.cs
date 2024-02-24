
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("json", "")]
    [ProtocolExample("json:", "")]
    public class DBWriterJsonFactory(TextWriter writer) : IFactoryByUrl<IDBWriter> {

        public IDBWriter Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterJson.Settings>(src);
            return new DBWriterJson(writer, leaveOpen, settings);
        }
    }

}
