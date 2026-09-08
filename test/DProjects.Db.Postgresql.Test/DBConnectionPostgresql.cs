using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;
using DProjects.Db.Schema;
using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Db.Postgresql.Tests {

    public class DBConnectionPostgresqlTypeMappingTests {

        // methods
        [Theory]
        [InlineData("bytea", DBSchemaDataType.Varbinary)]
        [InlineData("character", DBSchemaDataType.Char)]
        [InlineData("character varying", DBSchemaDataType.Varchar)]
        [InlineData("double precision", DBSchemaDataType.Double)]
        [InlineData("timestamp without time zone", DBSchemaDataType.DateTime)]
        [InlineData("text", DBSchemaDataType.Varchar)]
        [InlineData("uuid", DBSchemaDataType.UniqueIdentifier)]
        [InlineData("json", DBSchemaDataType.Json)]
        [InlineData("jsonb", DBSchemaDataType.Jsonb)]
        public void PostgreSqlTypeAliasesMapToPortableTypes(string sqlType, DBSchemaDataType expected) {
            using var connection = CreateConnection();

            Assert.Equal(expected, connection.GetDataTypeFromSqlDataTypeName(sqlType, 0, 0, 0));
        }
        [Theory]
        [InlineData(DBSchemaDataType.Char, 10, 0, 0, "CHAR(10)")]
        [InlineData(DBSchemaDataType.Char, 0, 0, 0, "CHAR")]
        [InlineData(DBSchemaDataType.Varchar, 100, 0, 0, "VARCHAR(100)")]
        [InlineData(DBSchemaDataType.Varchar, 0, 0, 0, "TEXT")]
        [InlineData(DBSchemaDataType.Nchar, 10, 0, 0, "CHAR(10)")]
        [InlineData(DBSchemaDataType.Nchar, 0, 0, 0, "CHAR")]
        [InlineData(DBSchemaDataType.Nvarchar, 100, 0, 0, "VARCHAR(100)")]
        [InlineData(DBSchemaDataType.Nvarchar, 0, 0, 0, "TEXT")]
        [InlineData(DBSchemaDataType.Binary, 0, 0, 0, "BYTEA")]
        [InlineData(DBSchemaDataType.Binary, 100, 0, 0, "BYTEA")]
        [InlineData(DBSchemaDataType.Varbinary, 0, 0, 0, "BYTEA")]
        [InlineData(DBSchemaDataType.Varbinary, 100, 0, 0, "BYTEA")]
        [InlineData(DBSchemaDataType.Numeric, 0, 0, 0, "NUMERIC")]
        [InlineData(DBSchemaDataType.Numeric, 0, 12, 3, "NUMERIC(12,3)")]
        [InlineData(DBSchemaDataType.Decimal, 0, 0, 0, "DECIMAL")]
        [InlineData(DBSchemaDataType.Decimal, 0, 12, 3, "DECIMAL(12,3)")]
        [InlineData(DBSchemaDataType.Smallint, 0, 0, 0, "SMALLINT")]
        [InlineData(DBSchemaDataType.TinyInt, 0, 0, 0, "SMALLINT")]
        [InlineData(DBSchemaDataType.Int, 0, 0, 0, "INTEGER")]
        [InlineData(DBSchemaDataType.Bigint, 0, 0, 0, "BIGINT")]
        [InlineData(DBSchemaDataType.Float, 0, 0, 0, "REAL")]
        [InlineData(DBSchemaDataType.Real, 0, 0, 0, "DOUBLE PRECISION")]
        [InlineData(DBSchemaDataType.Double, 0, 0, 0, "DOUBLE PRECISION")]
        [InlineData(DBSchemaDataType.Boolean, 0, 0, 0, "BOOLEAN")]
        [InlineData(DBSchemaDataType.Date, 0, 0, 0, "DATE")]
        [InlineData(DBSchemaDataType.DateTime, 0, 0, 0, "TIMESTAMP WITHOUT TIME ZONE")]
        [InlineData(DBSchemaDataType.Time, 0, 0, 0, "TIME")]
        [InlineData(DBSchemaDataType.Timestamp, 0, 0, 0, "TIMESTAMP WITHOUT TIME ZONE")]
        [InlineData(DBSchemaDataType.Interval, 0, 0, 0, "INTERVAL")]
        [InlineData(DBSchemaDataType.UniqueIdentifier, 0, 0, 0, "UUID")]
        [InlineData(DBSchemaDataType.Json, 0, 0, 0, "JSON")]
        [InlineData(DBSchemaDataType.Jsonb, 0, 0, 0, "JSONB")]
        public void GetSqlTypeDefinition_MapsEverySupportedPortableType(
            DBSchemaDataType dataType, int size, int precision, int scale, string expected) {
            using var connection = CreateConnection();

            Assert.Equal(expected, connection.GetSqlTypeDefinition(dataType, size, precision, scale));
        }
        [Theory]
        [InlineData(DBSchemaDataType.None)]
        [InlineData((DBSchemaDataType)int.MaxValue)]
        public void GetSqlTypeDefinition_RejectsUnsupportedPortableTypes(DBSchemaDataType dataType) {
            using var connection = CreateConnection();

            Assert.Throws<NotSupportedException>(() => connection.GetSqlTypeDefinition(dataType, 0, 0, 0));
        }
        [Theory]
        [InlineData(DBSchemaDataType.Numeric)]
        [InlineData(DBSchemaDataType.Decimal)]
        public void GetSqlTypeDefinition_RejectsScaleWithoutPrecision(DBSchemaDataType dataType) {
            using var connection = CreateConnection();

            Assert.Throws<ArgumentOutOfRangeException>(() => connection.GetSqlTypeDefinition(dataType, 0, 0, 2));
        }
        [Theory]
        [InlineData(DBSchemaDataType.DateTime, DBSchemaDataType.Timestamp)]
        [InlineData(DBSchemaDataType.Timestamp, DBSchemaDataType.DateTime)]
        [InlineData(DBSchemaDataType.Char, DBSchemaDataType.Nchar)]
        [InlineData(DBSchemaDataType.Nchar, DBSchemaDataType.Char)]
        [InlineData(DBSchemaDataType.Varchar, DBSchemaDataType.Nvarchar)]
        [InlineData(DBSchemaDataType.Nvarchar, DBSchemaDataType.Varchar)]
        [InlineData(DBSchemaDataType.Binary, DBSchemaDataType.Varbinary)]
        [InlineData(DBSchemaDataType.Varbinary, DBSchemaDataType.Binary)]
        [InlineData(DBSchemaDataType.Smallint, DBSchemaDataType.TinyInt)]
        [InlineData(DBSchemaDataType.TinyInt, DBSchemaDataType.Smallint)]
        [InlineData(DBSchemaDataType.Real, DBSchemaDataType.Double)]
        [InlineData(DBSchemaDataType.Double, DBSchemaDataType.Real)]
        public void SchemaDataTypeEquivalence_RecognizesPostgresqlTypeCollapses(DBSchemaDataType actual, DBSchemaDataType expected) {
            using var connection = new TestableDBConnectionPostgresql();

            Assert.True(connection.AreSchemaDataTypesEquivalentForTest(actual, expected));
        }
        [Theory]
        [InlineData(DBSchemaDataType.Float, DBSchemaDataType.Double)]
        [InlineData(DBSchemaDataType.Int, DBSchemaDataType.Bigint)]
        [InlineData(DBSchemaDataType.Date, DBSchemaDataType.DateTime)]
        [InlineData(DBSchemaDataType.Json, DBSchemaDataType.Jsonb)]
        public void SchemaDataTypeEquivalence_PreservesDistinctPostgresqlTypes(DBSchemaDataType actual, DBSchemaDataType expected) {
            using var connection = new TestableDBConnectionPostgresql();

            Assert.False(connection.AreSchemaDataTypesEquivalentForTest(actual, expected));
        }
        [Theory]
        [InlineData(DBSchemaDataType.DateTime, DBSchemaDataType.Timestamp, 0)]
        [InlineData(DBSchemaDataType.Varchar, DBSchemaDataType.Nvarchar, 100)]
        [InlineData(DBSchemaDataType.Smallint, DBSchemaDataType.TinyInt, 0)]
        public void ApplySchemaChanges_DoesNotAlterProviderCanonicalizedTypes(DBSchemaDataType discovered, DBSchemaDataType desired, int size) {
            using var connection = new TestableDBConnectionPostgresql() { DiscoveredDataType = discovered, ColumnSize = size };
            var schema = new DBSchemaDatabase();
            var table = new DBSchemaTable() { Name = "records" };
            table.Columns.Add(new DBSchemaColumn("value") { DataType = desired, Size = size });
            schema.Tables.Add(table);

            connection.ApplySchemaChanges(schema, false, NullLogger<IDBConnection>.Instance);

            Assert.Equal(0, connection.GetSqlAlterColumnCallCount);
        }
        [Theory]
        [InlineData(true, "DROP NOT NULL")]
        [InlineData(false, "SET NOT NULL")]
        public void AlterColumn_UsesSeparateTypeAndNullabilityStatements(bool nullable, string nullabilityClause) {
            using var connection = CreateConnection();
            var column = new DBSchemaColumn("column") { DataType = DBSchemaDataType.Varchar, Size = 100, Null = nullable };

            var expected = "ALTER TABLE \"table\" ALTER COLUMN \"column\" TYPE VARCHAR(100);" + Environment.NewLine
                + "ALTER TABLE \"table\" ALTER COLUMN \"column\" " + nullabilityClause;

            Assert.Equal(expected, connection.GetSqlAlterColumn("table", column));
        }
        [Fact]
        public void AlterColumn_UsesPostgresqlDateTimeTypeMapping() {
            using var connection = CreateConnection();
            var column = new DBSchemaColumn("created") { DataType = DBSchemaDataType.DateTime, Null = false };

            var sql = connection.GetSqlAlterColumn("table", column);

            Assert.Contains("TYPE TIMESTAMP WITHOUT TIME ZONE", sql);
            Assert.DoesNotContain("DATETIME", sql);
        }
        [Fact]
        public void GetSqlTypeDefinition_DoesNotLeakPortableVendorTypeNames() {
            using var connection = CreateConnection();

            var definitions = new[] {
                connection.GetSqlTypeDefinition(DBSchemaDataType.DateTime, 0, 0, 0),
                connection.GetSqlTypeDefinition(DBSchemaDataType.TinyInt, 0, 0, 0),
                connection.GetSqlTypeDefinition(DBSchemaDataType.Nchar, 10, 0, 0),
                connection.GetSqlTypeDefinition(DBSchemaDataType.Nvarchar, 100, 0, 0),
                connection.GetSqlTypeDefinition(DBSchemaDataType.Binary, 10, 0, 0),
                connection.GetSqlTypeDefinition(DBSchemaDataType.Varbinary, 10, 0, 0),
                connection.GetSqlTypeDefinition(DBSchemaDataType.Double, 0, 0, 0),
                connection.GetSqlTypeDefinition(DBSchemaDataType.UniqueIdentifier, 0, 0, 0)
            };

            Assert.DoesNotContain(definitions, definition => definition is "DATETIME" or "TINYINT" or "NVARCHAR" or "NCHAR" or "VARBINARY" or "BINARY"
                or "DOUBLE" or "UNIQUEIDENTIFIER" or "VARCHAR(MAX)" or "NVARCHAR(MAX)");
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

        // methods (private)
        private static DProjects.Db.Postgresql.DBConnectionPostgresql CreateConnection() {
            return new DProjects.Db.Postgresql.DBConnectionPostgresql("mapping", "Host=localhost;Database=mapping;Username=mapping;Password=mapping");
        }

        private sealed class TestableDBConnectionPostgresql : DProjects.Db.Postgresql.DBConnectionPostgresql {

            // props
            public int ColumnSize { get; set; }
            public DBSchemaDataType DiscoveredDataType { get; set; }
            public int GetSqlAlterColumnCallCount { get; private set; }

            // ctor
            public TestableDBConnectionPostgresql() : base("test", "Host=localhost;Database=test;Username=test;Password=test") {
            }

            // methods
            public bool AreSchemaDataTypesEquivalentForTest(DBSchemaDataType actual, DBSchemaDataType expected) {
                return AreSchemaDataTypesEquivalent(actual, expected);
            }
            public override string[] GetTableNames() {
                return ["records"];
            }
            public override DBSchemaTable GetTableSchema(string table) {
                var schema = new DBSchemaTable() { Name = table };
                schema.Columns.Add(new DBSchemaColumn("value") { DataType = DiscoveredDataType, Size = ColumnSize });
                return schema;
            }
            public override string GetSqlAlterColumn(string table, DBSchemaColumn dBSchemaColumn) {
                GetSqlAlterColumnCallCount++;
                return base.GetSqlAlterColumn(table, dBSchemaColumn);
            }
        }
    }

    [Trait("Category", "Integration")]
    public class DBConnectionPostgresqlTests : DBConnectionTests<DProjects.Db.Postgresql.DBConnectionPostgresql> {


        //constructor // user-secret:s3-bucket
        public DBConnectionPostgresqlTests() : base ("user-secret:postgresql_dev", typeof(DProjects.Db.Postgresql.Assembly).Assembly) {
        }
    }
}
