using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;

namespace DProjects.Db.Postgresql.Tests {

    [Trait("Category", "Integration")]
    public class DBConnectionPostgresqlTests : DBConnectionTests<DProjects.Db.Postgresql.DBConnectionPostgresql> {


        //constructor // user-secret:s3-bucket
        public DBConnectionPostgresqlTests() : base ("user-secret:postgresql_dev", typeof(DProjects.Db.Postgresql.Assembly).Assembly) {
        }
    }
}
