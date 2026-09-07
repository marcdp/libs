using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;

namespace DProjects.Db.Postgresql.Tests {

    public class DBConnectionPostgresqlTypeMappingTests {

        // methods
        [Fact]
        public void TimestampWithoutTimeZoneMapsToPortableDateTime() {
            using var connection = new DProjects.Db.Postgresql.DBConnectionPostgresql("mapping", "Host=localhost;Database=mapping;Username=mapping;Password=mapping");
            Assert.Equal(DProjects.Db.Schema.DBSchemaDataType.DateTime, connection.GetDataTypeFromSqlDataTypeName("timestamp without time zone", 0, 0, 0));
        }
    }

    [Trait("Category", "Integration")]
    public class DBConnectionPostgresqlTests : DBConnectionTests<DProjects.Db.Postgresql.DBConnectionPostgresql> {


        //constructor // user-secret:s3-bucket
        public DBConnectionPostgresqlTests() : base ("user-secret:postgresql_dev", typeof(DProjects.Db.Postgresql.Assembly).Assembly) {
        }
    }
}
