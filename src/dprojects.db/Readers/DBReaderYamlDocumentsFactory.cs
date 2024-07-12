
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("yamld", "")]
    [ProtocolExample("yamld:", "")]
    public class DBReaderYamlDocumentsFactory : IFactoryByUrlAndArgument<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderYamlDocuments.Settings>(src);
            return new DBReaderYamlDocuments(reader, leaveOpen, settings);
        }
    }

}
