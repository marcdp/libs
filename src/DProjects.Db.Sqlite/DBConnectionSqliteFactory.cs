using DProjects.Factories;
using DProjects.Factories.Attributes;
using System;


namespace DProjects.Db.Sqlite {

    [Protocol("sqlite", "")]
    [ProtocolUsage("sqlite:CONNECTION_STRING")]
    [ProtocolExample("sqlite:/path/to/file;", "")]
    public class DBConnectionSqliteFactory : IFactoryByUrl<IDBConnection> {
        public IDBConnection Create(string src) {
            var name = "";
            var connectionString = src.Substring(src.IndexOf(":") + 1);
            return new DBConnectionSqlite(name, connectionString.Substring(connectionString.IndexOf(":") + 1));
        }

    }

}
