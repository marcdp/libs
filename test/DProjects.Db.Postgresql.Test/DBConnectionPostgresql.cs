using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;
using DProjects.Db.Schema;

namespace DProjects.Db.Postgresql.Tests {

    public class DBConnectionPostgresqlTypeMappingTests {

        // methods
        [Fact]
        public void TimestampWithoutTimeZoneMapsToPortableDateTime() {
            using var connection = new DProjects.Db.Postgresql.DBConnectionPostgresql("mapping", "Host=localhost;Database=mapping;Username=mapping;Password=mapping");
            Assert.Equal(DProjects.Db.Schema.DBSchemaDataType.DateTime, connection.GetDataTypeFromSqlDataTypeName("timestamp without time zone", 0, 0, 0));
        }
        [Theory]
        [InlineData(true, "DROP NOT NULL")]
        [InlineData(false, "SET NOT NULL")]
        public void AlterColumn_UsesSeparateTypeAndNullabilityStatements(bool nullable, string nullabilityClause) {
            using var connection = new DProjects.Db.Postgresql.DBConnectionPostgresql("generation", "Host=localhost;Database=generation;Username=generation;Password=generation");
            var column = new DBSchemaColumn("column") { DataType = DBSchemaDataType.Varchar, Size = 100, Null = nullable };

            var expected = "ALTER TABLE \"table\" ALTER COLUMN \"column\" TYPE VARCHAR(100);" + Environment.NewLine
                + "ALTER TABLE \"table\" ALTER COLUMN \"column\" " + nullabilityClause;

            Assert.Equal(expected, connection.GetSqlAlterColumn("table", column));
        }
        [Fact]
        public void PostgresqlAliasesRemainProviderOwned() {
            using var connection = new DProjects.Db.Postgresql.DBConnectionPostgresql("mapping", "Host=localhost;Database=mapping;Username=mapping;Password=mapping");

            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromSqlDataTypeName("text", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromSqlDataTypeName("character varying", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.UniqueIdentifier, connection.GetDataTypeFromSqlDataTypeName("uuid", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Json, connection.GetDataTypeFromSqlDataTypeName("json", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Jsonb, connection.GetDataTypeFromSqlDataTypeName("jsonb", 0, 0, 0));
        }
        [Fact]
        public void SqlPrimitives_UsePostgresqlDialectBehavior() {
            var connectionString = "Host=localhost;Database=generation;Username=generation;Password=generation";
            using var connection = new DProjects.Db.Postgresql.DBConnectionPostgresql("generation", connectionString);

            Assert.Equal("$", connection.GetSqlParameterPrefix());
            Assert.Equal(" LIMIT 5", connection.GetSqlSelectTop(5));
            Assert.True(connection.GetSqlSelectTopAtEnd());
            Assert.Equal(" LIMIT 5 OFFSET 10", connection.GetSqlSelectOffsetLimit(10, 5));
            Assert.Equal("CURRENT_TIMESTAMP", connection.GetSqlDefaultNowExpression());
            Assert.Equal("SELECT nextval('sequence')", connection.GetSqlGetNextSequenceValue("sequence"));
            Assert.Equal("\"", connection.GetSqlQualifierBegin());
            Assert.Equal("\"", connection.GetSqlQualifierEnd());
            Assert.Equal("TEXT", connection.GetSqlTypeDefinition(DBSchemaDataType.Varchar, 0, 0, 0));
            Assert.Equal("BYTEA", connection.GetSqlTypeDefinition(DBSchemaDataType.Varbinary, 0, 0, 0));
            Assert.Equal("DROP INDEX \"index\"", connection.GetSqlDropIndex("table", "index"));
            Assert.Equal("ALTER TABLE \"table\" ALTER COLUMN \"column\" SET DEFAULT 0", connection.GetSqlCreateDefault("table", "column", "0"));
            var sequence = new DBSchemaSequence() { Name = "sequence", InitValue = 10, IncrementBy = 2 };
            Assert.Contains("START 10", connection.GetSqlCreateSequence(sequence));
            Assert.Equal("ALTER SEQUENCE \"sequence\" INCREMENT BY 2", connection.GetSqlAlterSequenceIncrement(sequence));
            Assert.Equal("DROP SEQUENCE \"sequence\"", connection.GetSqlDropSequence("sequence"));
        }
    }

    [Trait("Category", "Integration")]
    public class DBConnectionPostgresqlTests : DBConnectionTests<DProjects.Db.Postgresql.DBConnectionPostgresql> {


        //constructor // user-secret:s3-bucket
        public DBConnectionPostgresqlTests() : base ("user-secret:postgresql_dev", typeof(DProjects.Db.Postgresql.Assembly).Assembly) {
        }
    }
}
