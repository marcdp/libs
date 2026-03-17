using DProjects.Factories;
using DProjects.Factories.Attributes;
using System;


namespace DProjects.Db.SqlServer {

    [Protocol("sqlserver", "")]
    [ProtocolUsage("sqlserver:CONNECTION_STRING")]
    [ProtocolExample("sqlserver:Server=.;Initial Catalog=MyDB;User Id=MyLogin;Password=MyPwd;", "")]
    public class DBConnectionSqlServerFactory : IFactoryByUrl<IDBConnection> {
        public IDBConnection Create(string src) {
            var name = "";
            var connectionString = src.Substring(src.IndexOf(":") + 1);
            return new DBConnectionSqlServer(name, connectionString.Substring(connectionString.IndexOf(":") + 1));
        }

    }

}
