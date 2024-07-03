
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using System.IO;

namespace DProjects.Db.Readers {

    [Protocol("auto", "")]
    [ProtocolExample("auto:", "")]
    public class DBReaderAutoFactory : IFactoryByUrlAndArgument<IDBReader, TextReader> {

        public IDBReader Create(string src, TextReader reader) {
            var leaveOpen = false;
            return new DBReaderAuto(reader, leaveOpen);
        }
    }

}
