
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("domain", "")]
    [ProtocolExample("domain:", "")]
    public class DBWriterDomainFactory : IFactoryByUrlAndArgument<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterDomain.Settings>(src);
            return new DBWriterDomain(writer, leaveOpen, settings);
        }
    }

}
