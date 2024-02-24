
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("domain", "")]
    [ProtocolExample("domain:", "")]
    public class DBWriterDomainFactory(TextWriter writer) : IFactoryByUrl<IDBWriter> {

        public IDBWriter Create(string src) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterDomain.Settings>(src);
            return new DBWriterDomain(writer, leaveOpen, settings);
        }
    }

}
