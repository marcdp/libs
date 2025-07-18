using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;

namespace DProjects.Db.Sqlite.Tests
{
    public class DBConnectionSqliteTests : DBConnectionTests<DProjects.Db.Sqlite.DBConnectionSqlite> {


        //constructor // user-secret:s3-bucket
        public DBConnectionSqliteTests() : base ("user-secret:sqlite", typeof(DProjects.Db.Sqlite.Assembly).Assembly) {
        }
    }
}
