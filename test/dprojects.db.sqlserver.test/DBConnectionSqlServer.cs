using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;

namespace DProjects.Db.SqlServer.Tests
{
    public class DBConnectionSqlServerTests : DBConnectionTests<DProjects.Db.SqlServer.DBConnectionSqlServer> {


        //constructor
        public DBConnectionSqlServerTests() : base("sqlserver:Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True;") {
        }  
    }
}
