using DProjects.Factories;
using DProjects.Factories.Attributes;
using System;


namespace DProjects.Db.Postgresql {

    [Protocol("postgresql", "")]
    [ProtocolUsage("postgresql:CONNECTION_STRING")]
    [ProtocolExample("postgresql:Server=.;Initial Catalog=MyDB;User Id=MyLogin;Password=MyPwd;", "")]
    public class DBConnectionPostgresqlFactory : IFactoryByUrl<IDBConnection> {
        public IDBConnection Create(string src) {
            var name = "";
            var connectionString = src.Substring(src.IndexOf(":") + 1);
            return new DBConnectionPostgresql(name, connectionString.Substring(connectionString.IndexOf(":") + 1));
        }

    }

}
