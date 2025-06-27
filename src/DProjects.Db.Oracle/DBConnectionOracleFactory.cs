using DProjects.Factories;
using DProjects.Factories.Attributes;
using System;


namespace DProjects.Db.Oracle {

    [Protocol("oracle", "")]
    [ProtocolUsage("oracle:CONNECTION_STRING")]
    public class DBConnectionOracleFactory : IFactoryByUrl<IDBConnection> {
        public IDBConnection Create(string src) {
            var name = "";
            var connectionString = src.Substring(src.IndexOf(":") + 1);
            return new DBConnectionOracle(name, connectionString.Substring(connectionString.IndexOf(":") + 1));
        }

    }

}
