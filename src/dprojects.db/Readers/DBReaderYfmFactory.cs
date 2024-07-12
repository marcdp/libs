
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("yfm", "")]
    [ProtocolExample("yfm:", "")]
    public class DBReaderYfmFactory : IFactoryByUrlAndArgument<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            var settings = UrlUtils.Deserialize<DBReaderYfm.Settings>(src);
            return new DBReaderYfm(reader, leaveOpen, settings);
        }
    }

}
