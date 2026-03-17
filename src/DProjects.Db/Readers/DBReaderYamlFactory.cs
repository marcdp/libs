
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("yaml", "")]
    [ProtocolExample("yaml:", "")]
    public class DBReaderYamlFactory : IFactoryByUrl<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderYaml.Settings>(src);
            return new DBReaderYaml(reader, leaveOpen, settings);
        }
    }

}
