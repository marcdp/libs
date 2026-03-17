
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Writers {

    [Protocol("yfm", "")]
    [ProtocolExample("yfm:", "")]
    public class DBWriterYfmFactory : IFactoryByUrl<IDBWriter, TextWriter> {

        public IDBWriter Create(string src, TextWriter writer) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBWriterYfm.Settings>(src);
            return new DBWriterYfm(writer, leaveOpen, settings);
        }
    }

}
