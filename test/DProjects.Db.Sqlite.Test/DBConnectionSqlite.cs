using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;

namespace DProjects.Db.Sqlite.Tests
{
    [Trait("Category", "Integration")]
    public class DBConnectionSqliteTests : DBConnectionTests<DProjects.Db.Sqlite.DBConnectionSqlite> {

        //constructor 
        public DBConnectionSqliteTests() : base ("user-secret:sqlite", typeof(DProjects.Db.Sqlite.Assembly).Assembly) {
        }
    }
}
