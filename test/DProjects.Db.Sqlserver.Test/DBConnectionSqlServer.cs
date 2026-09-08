using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;
using DProjects.Db.Schema;

namespace DProjects.Db.SqlServer.Tests {

    public class DBConnectionSqlServerSqlGenerationTests {

        // methods
        [Theory]
        [InlineData(DBSchemaDataType.Char, 10, 0, 0, "CHAR(10)")]
        [InlineData(DBSchemaDataType.Varchar, 0, 0, 0, "VARCHAR(MAX)")]
        [InlineData(DBSchemaDataType.Varchar, int.MaxValue, 0, 0, "VARCHAR(MAX)")]
        [InlineData(DBSchemaDataType.Nchar, 10, 0, 0, "NCHAR(10)")]
        [InlineData(DBSchemaDataType.Nvarchar, 0, 0, 0, "NVARCHAR(MAX)")]
        [InlineData(DBSchemaDataType.Binary, 8, 0, 0, "BINARY(8)")]
        [InlineData(DBSchemaDataType.Varbinary, 0, 0, 0, "VARBINARY(MAX)")]
        [InlineData(DBSchemaDataType.Numeric, 0, 12, 3, "NUMERIC(12,3)")]
        [InlineData(DBSchemaDataType.Decimal, 0, 18, 4, "DECIMAL(18,4)")]
        [InlineData(DBSchemaDataType.Smallint, 0, 0, 0, "SMALLINT")]
        [InlineData(DBSchemaDataType.TinyInt, 0, 0, 0, "TINYINT")]
        [InlineData(DBSchemaDataType.Int, 0, 0, 0, "INT")]
        [InlineData(DBSchemaDataType.Bigint, 0, 0, 0, "BIGINT")]
        [InlineData(DBSchemaDataType.Float, 0, 0, 0, "FLOAT(24)")]
        [InlineData(DBSchemaDataType.Real, 0, 0, 0, "FLOAT(53)")]
        [InlineData(DBSchemaDataType.Double, 0, 0, 0, "FLOAT(53)")]
        [InlineData(DBSchemaDataType.Boolean, 0, 0, 0, "BIT")]
        [InlineData(DBSchemaDataType.Date, 0, 0, 0, "DATE")]
        [InlineData(DBSchemaDataType.DateTime, 0, 0, 0, "DATETIME")]
        [InlineData(DBSchemaDataType.Time, 0, 0, 0, "TIME")]
        [InlineData(DBSchemaDataType.Timestamp, 0, 0, 0, "DATETIME2")]
        [InlineData(DBSchemaDataType.UniqueIdentifier, 0, 0, 0, "UNIQUEIDENTIFIER")]
        public void GetSqlTypeDefinition_MapsEverySupportedPortableType(DBSchemaDataType dataType, int size, int precision, int scale, string expected) {
            using var connection = CreateConnection();

            Assert.Equal(expected, connection.GetSqlTypeDefinition(dataType, size, precision, scale));
        }
        [Theory]
        [InlineData(DBSchemaDataType.None)]
        [InlineData(DBSchemaDataType.Interval)]
        [InlineData(DBSchemaDataType.Json)]
        [InlineData(DBSchemaDataType.Jsonb)]
        public void GetSqlTypeDefinition_RejectsUnsupportedPortableTypes(DBSchemaDataType dataType) {
            using var connection = CreateConnection();

            Assert.Throws<NotSupportedException>(() => connection.GetSqlTypeDefinition(dataType, 0, 0, 0));
        }
        [Theory]
        [InlineData("datetime", DBSchemaDataType.DateTime)]
        [InlineData("smalldatetime", DBSchemaDataType.DateTime)]
        [InlineData("datetime2", DBSchemaDataType.Timestamp)]
        [InlineData("real", DBSchemaDataType.Float)]
        [InlineData("float", DBSchemaDataType.Double)]
        [InlineData("timestamp", DBSchemaDataType.Varbinary)]
        [InlineData("rowversion", DBSchemaDataType.Varbinary)]
        public void SqlServerTypeNamesMapToDeliberatePortableTypes(string sqlType, DBSchemaDataType expected) {
            using var connection = CreateConnection();

            Assert.Equal(expected, connection.GetDataTypeFromSqlDataTypeName(sqlType, 0, 0, 0));
        }
        [Fact]
        public void SchemaColumnTypeEquivalence_CanonicalizesSqlServerFacets() {
            using var connection = new TestableDBConnectionSqlServer();

            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = DBSchemaDataType.Real },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Double }));
            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = DBSchemaDataType.Numeric, Precision = 12, Scale = 3 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Decimal, Precision = 12, Scale = 3 }));
            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = DBSchemaDataType.Decimal, Precision = 18, Scale = 0 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Numeric, Precision = 0, Scale = 0 }));
            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = DBSchemaDataType.Varchar, Size = int.MaxValue },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Varchar, Size = 0 }));
            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = DBSchemaDataType.Char, Size = 1 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Char, Size = 0 }));
            Assert.False(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = DBSchemaDataType.Float },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Double }));
            Assert.False(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = DBSchemaDataType.Decimal, Precision = 12, Scale = 3 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Numeric, Precision = 12, Scale = 4 }));
        }
        [Theory]
        [InlineData(DBSchemaDataType.Timestamp, DBSchemaDataType.Timestamp, 0, 0, 0, 0)]
        [InlineData(DBSchemaDataType.Double, DBSchemaDataType.Real, 0, 0, 0, 0)]
        [InlineData(DBSchemaDataType.Varchar, DBSchemaDataType.Varchar, 0, int.MaxValue, 0, 0)]
        [InlineData(DBSchemaDataType.Decimal, DBSchemaDataType.Numeric, 0, 0, 12, 3)]
        public void ApplySchemaChanges_DoesNotAlterProviderCanonicalizedTypes(DBSchemaDataType discovered, DBSchemaDataType desired, int desiredSize,
            int discoveredSize, int precision, int scale) {
            using var connection = new TestableDBConnectionSqlServer() {
                DiscoveredDataType = discovered,
                DesiredSize = discoveredSize,
                Precision = precision,
                Scale = scale
            };
            var schema = new DBSchemaDatabase();
            var table = new DBSchemaTable() { Name = "records" };
            table.Columns.Add(new DBSchemaColumn("value") { DataType = desired, Size = desiredSize, Precision = precision, Scale = scale });
            schema.Tables.Add(table);

            connection.ApplySchemaChanges(schema, false, Microsoft.Extensions.Logging.Abstractions.NullLogger<IDBConnection>.Instance);

            Assert.Equal(0, connection.GetSqlAlterColumnCallCount);
        }
        [Fact]
        public void PopulateIndexes_FiltersPrimaryKeyBackingIndex() {
            using var connection = new TestableDBConnectionSqlServer();
            var table = new DBSchemaTable() { Name = "orders" };
            table.Columns.Add(new DBSchemaColumn("id"));
            table.Columns.Add(new DBSchemaColumn("code"));
            var metadata = CreateMetadataTable(["index_name", "index_description", "index_keys"],
                ["pk_orders", "clustered, unique, primary key", "id"],
                ["ix_code", "nonclustered", "code"]);

            connection.PopulateIndexesForTest(table, metadata);

            var index = Assert.Single(table.Indexes);
            Assert.Equal("ix_code", index.Name);
            Assert.Equal(["code"], index.Columns);
        }
        [Fact]
        public void SqlPrimitives_PreserveSqlServerDialectBehavior() {
            using var connection = CreateConnection();

            Assert.Equal(" TOP 5 ", connection.GetSqlSelectTop(5));
            Assert.True(connection.GetSqlSelectTopAtEnd());
            Assert.Equal(" OFFSET 10 ROWS FETCH NEXT 5 ROW ONLY", connection.GetSqlSelectOffsetLimit(10, 5));
            Assert.Equal("SELECT @@IDENTITY", connection.GetSqlSelectAutoincrement());
            Assert.Equal("getDate()", connection.GetSqlDefaultNowExpression());
            Assert.Equal(" INT IDENTITY NOT NULL ", connection.GetSqlIdentityDefinition(typeof(int)));
            Assert.Equal(" BIGINT IDENTITY NOT NULL ", connection.GetSqlIdentityDefinition(typeof(long)));
            Assert.Equal(" uniqueidentifier NOT NULL DEFAULT newId()", connection.GetSqlIdentityDefinition(typeof(Guid)));
            Assert.Equal("VARCHAR(MAX)", connection.GetSqlTypeDefinition(DBSchemaDataType.Varchar, 0, 0, 0));
            Assert.Equal("NVARCHAR(MAX)", connection.GetSqlTypeDefinition(DBSchemaDataType.Nvarchar, 0, 0, 0));
            Assert.Equal("VARBINARY(MAX)", connection.GetSqlTypeDefinition(DBSchemaDataType.Varbinary, 0, 0, 0));
            Assert.Equal("DROP INDEX [table].index", connection.GetSqlDropIndex("table", "index"));
            Assert.Contains("ADD CONSTRAINT DF_table_column DEFAULT 0 FOR column", connection.GetSqlCreateDefault("table", "column", "0"));
            var sequence = new DBSchemaSequence() { Name = "sequence", InitValue = 10, IncrementBy = 2 };
            Assert.Contains("START WITH 10", connection.GetSqlCreateSequence(sequence));
            Assert.Contains("INCREMENT BY 2", connection.GetSqlAlterSequenceIncrement(sequence));
            Assert.Equal("DROP SEQUENCE [sequence]", connection.GetSqlDropSequence("sequence"));
            Assert.Equal("SELECT NEXT VALUE FOR [sequence]", connection.GetSqlGetNextSequenceValue("sequence"));
            Assert.Equal("[", connection.GetSqlQualifierBegin());
            Assert.Equal("]", connection.GetSqlQualifierEnd());
            Assert.Contains("@@ROWCOUNT", connection.GetSqlIfRowCountThrowError(0, 50000, "error"));
            Assert.Contains("THROW", connection.GetSqlIfRowCountThrowError(0, 50000, "error"));
            Assert.Equal("[aáàÀAÁ][_]", connection.GetSqlEncodedLikeValue("a_"));
        }
        [Fact]
        public void RowVersionNamesMapToBinaryWhilePortableTimestampRendersAsDateTime() {
            using var connection = CreateConnection();

            Assert.Equal(DBSchemaDataType.Varbinary, connection.GetDataTypeFromSqlDataTypeName("timestamp", 8, 0, 0));
            Assert.Equal(DBSchemaDataType.Varbinary, connection.GetDataTypeFromSqlDataTypeName("rowversion", 8, 0, 0));
            Assert.Equal("DATETIME2", connection.GetSqlTypeDefinition(DBSchemaDataType.Timestamp, 0, 0, 0));
            Assert.NotEqual("TIMESTAMP", connection.GetSqlTypeDefinition(DBSchemaDataType.Timestamp, 0, 0, 0));
        }
        [Fact]
        public void SqlServerAliasesRemainProviderOwnedAndProcedureEnumerationIsUnsupported() {
            using var connection = CreateConnection();

            Assert.Equal(DBSchemaDataType.Boolean, connection.GetDataTypeFromSqlDataTypeName("bit", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromSqlDataTypeName("text", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Nvarchar, connection.GetDataTypeFromSqlDataTypeName("ntext", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.UniqueIdentifier, connection.GetDataTypeFromSqlDataTypeName("uniqueidentifier", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetProcedureNames());
        }

        // methods (private)
        private static DProjects.Db.SqlServer.DBConnectionSqlServer CreateConnection() {
            var connectionString = "Server=localhost;Database=generation;Trusted_Connection=True;TrustServerCertificate=True";
            return new DProjects.Db.SqlServer.DBConnectionSqlServer("generation", connectionString);
        }
        private static DBTable CreateMetadataTable(string[] columns, params object?[][] rows) {
            var table = new DBTable();
            foreach (var column in columns) table.Columns.Add(column);
            foreach (var values in rows) table.Rows.Add(new DBRow(table, values));
            return table;
        }

        private sealed class TestableDBConnectionSqlServer : DProjects.Db.SqlServer.DBConnectionSqlServer {

            // props
            public DBSchemaDataType DiscoveredDataType { get; set; }
            public int DesiredSize { get; set; }
            public int Precision { get; set; }
            public int Scale { get; set; }
            public int GetSqlAlterColumnCallCount { get; private set; }

            // ctor
            public TestableDBConnectionSqlServer() : base("test", "Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True") {
            }

            // methods
            public bool AreSchemaColumnTypesEquivalentForTest(DBSchemaColumn actual, DBSchemaColumn expected) {
                return AreSchemaColumnTypesEquivalent(actual, expected);
            }
            public void PopulateIndexesForTest(DBSchemaTable table, DBTable metadata) {
                PopulateIndexes(table, metadata);
            }
            public override string[] GetTableNames() {
                return ["records"];
            }
            public override DBSchemaTable GetTableSchema(string table) {
                var schema = new DBSchemaTable() { Name = table };
                schema.Columns.Add(new DBSchemaColumn("value") { DataType = DiscoveredDataType, Size = DesiredSize, Precision = Precision, Scale = Scale });
                return schema;
            }
            public override string GetSqlAlterColumn(string table, DBSchemaColumn dBSchemaColumn) {
                GetSqlAlterColumnCallCount++;
                return base.GetSqlAlterColumn(table, dBSchemaColumn);
            }
        }
    }

    [Trait("Category", "Integration")]
    public class DBConnectionSqlServerTests : DBConnectionTests<DProjects.Db.SqlServer.DBConnectionSqlServer> {


        //constructor
        public DBConnectionSqlServerTests() : base("sqlserver:Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True;") {
        }  
    }
}
