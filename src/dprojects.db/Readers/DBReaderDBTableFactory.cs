
using DProjects.Factories;
using DProjects.Factories.Attributes;

namespace DProjects.Db.Readers {

    [Protocol("dbtable", "")]
    [ProtocolExample("dbtable:", "")]
    public class DBReaderDBTableFactory(DBTable dbTable) : IFactoryByUrl<IDBReader> {

        public IDBReader Create(string src) {
            return new DBReaderDBTable(dbTable);
        }
    }

}
