
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("yaml", "")]
    [ProtocolExample("yaml:", "")]
    public class DBWriterYamlFactory : IFactoryByUrl<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterYaml.Settings>(src);
            return new DBWriterYaml(writer, leaveOpen, settings);
        }
    }

}
