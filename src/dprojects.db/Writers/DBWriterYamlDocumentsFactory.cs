
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("yamld", "")]
    [ProtocolExample("yamld:", "")]
    public class DBWriterYamlDocumentsFactory : IFactoryByUrlAndArgument<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterYamlDocuments.Settings>(src);
            return new DBWriterYamlDocuments(writer, leaveOpen, settings);
        }
    }

}
