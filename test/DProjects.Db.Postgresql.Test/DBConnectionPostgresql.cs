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
        [InlineData("bpchar", DBSchemaDataType.Char)]
        [InlineData("double precision", DBSchemaDataType.Double)]
        [InlineData("float4", DBSchemaDataType.Float)]
        [InlineData("float8", DBSchemaDataType.Double)]
        [InlineData("int2", DBSchemaDataType.Smallint)]
        [InlineData("int4", DBSchemaDataType.Int)]
        [InlineData("int8", DBSchemaDataType.Bigint)]
        [InlineData("numeric", DBSchemaDataType.Numeric)]
        [InlineData("decimal", DBSchemaDataType.Decimal)]
        [InlineData("real", DBSchemaDataType.Float)]
        [InlineData("time without time zone", DBSchemaDataType.Time)]
        [InlineData("timestamp", DBSchemaDataType.DateTime)]
        [InlineData("varchar", DBSchemaDataType.Varchar)]
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
        [InlineData(DBSchemaDataType.Numeric, DBSchemaDataType.Decimal)]
        [InlineData(DBSchemaDataType.Decimal, DBSchemaDataType.Numeric)]
        public void SchemaDataTypeEquivalence_RecognizesPostgresqlTypeCollapses(DBSchemaDataType actual, DBSchemaDataType expected) {
            using var connection = new TestableDBConnectionPostgresql();

            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = actual }, new DBSchemaColumn() { DataType = expected }));
        }
        [Theory]
        [InlineData(DBSchemaDataType.Float, DBSchemaDataType.Double)]
        [InlineData(DBSchemaDataType.Int, DBSchemaDataType.Bigint)]
        [InlineData(DBSchemaDataType.Date, DBSchemaDataType.DateTime)]
        [InlineData(DBSchemaDataType.Json, DBSchemaDataType.Jsonb)]
        public void SchemaDataTypeEquivalence_PreservesDistinctPostgresqlTypes(DBSchemaDataType actual, DBSchemaDataType expected) {
            using var connection = new TestableDBConnectionPostgresql();

            Assert.False(connection.AreSchemaColumnTypesEquivalentForTest(new DBSchemaColumn() { DataType = actual }, new DBSchemaColumn() { DataType = expected }));
        }
        [Fact]
        public void SchemaColumnTypeEquivalence_IgnoresByteaSizeButPreservesMeaningfulFacets() {
            using var connection = new TestableDBConnectionPostgresql();

            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(
                new DBSchemaColumn() { DataType = DBSchemaDataType.Binary, Size = 100 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Varbinary, Size = 0 }));
            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(
                new DBSchemaColumn() { DataType = DBSchemaDataType.Char, Size = 1 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Nchar, Size = 0 }));
            Assert.True(connection.AreSchemaColumnTypesEquivalentForTest(
                new DBSchemaColumn() { DataType = DBSchemaDataType.Timestamp, Precision = 6 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.DateTime, Precision = 0 }));
            Assert.False(connection.AreSchemaColumnTypesEquivalentForTest(
                new DBSchemaColumn() { DataType = DBSchemaDataType.Varchar, Size = 100 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Nvarchar, Size = 200 }));
            Assert.False(connection.AreSchemaColumnTypesEquivalentForTest(
                new DBSchemaColumn() { DataType = DBSchemaDataType.Numeric, Precision = 12, Scale = 3 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Decimal, Precision = 18, Scale = 3 }));
            Assert.False(connection.AreSchemaColumnTypesEquivalentForTest(
                new DBSchemaColumn() { DataType = DBSchemaDataType.Numeric, Precision = 12, Scale = 3 },
                new DBSchemaColumn() { DataType = DBSchemaDataType.Decimal, Precision = 12, Scale = 4 }));
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
        [InlineData(DBSchemaDataType.Numeric, 0, 12, 3, DBSchemaDataType.Decimal, 0, 12, 3, 0)]
        [InlineData(DBSchemaDataType.Varbinary, 0, 0, 0, DBSchemaDataType.Binary, 100, 0, 0, 0)]
        [InlineData(DBSchemaDataType.Varchar, 100, 0, 0, DBSchemaDataType.Varchar, 200, 0, 0, 1)]
        public void ApplySchemaChanges_UsesPostgresqlColumnSemantics(DBSchemaDataType discoveredType, int discoveredSize, int discoveredPrecision,
            int discoveredScale, DBSchemaDataType desiredType, int desiredSize, int desiredPrecision, int desiredScale, int expectedAlterCount) {
            using var connection = new TestableDBConnectionPostgresql() {
                DiscoveredDataType = discoveredType,
                ColumnSize = discoveredSize,
                ColumnPrecision = discoveredPrecision,
                ColumnScale = discoveredScale
            };
            var schema = new DBSchemaDatabase();
            var table = new DBSchemaTable() { Name = "records" };
            table.Columns.Add(new DBSchemaColumn("value") { DataType = desiredType, Size = desiredSize, Precision = desiredPrecision, Scale = desiredScale });
            schema.Tables.Add(table);

            connection.ApplySchemaChanges(schema, false, NullLogger<IDBConnection>.Instance);

            Assert.Equal(expectedAlterCount, connection.GetSqlAlterColumnCallCount);
        }
        [Fact]
        public void MetadataMaterialization_PreservesIndexColumnsAndDoesNotDuplicatePrimaryKey() {
            using var connection = new TestableDBConnectionPostgresql();
            var table = new DBSchemaTable() { Name = "orders", PrimaryKey = new DBSchemaPrimaryKey() { Name = "orders_pkey", Columns = ["id"] } };
            var metadata = CreateMetadataTable(["index_name", "is_unique", "column_name", "ordinal_position"],
                ["ix_customer", false, "customer_id", 1],
                ["ux_reference", true, "tenant_id", 1],
                ["ux_reference", true, "reference", 2]);

            connection.PopulateIndexesForTest(table, metadata);

            Assert.Equal(2, table.Indexes.Count);
            Assert.Equal(["customer_id"], table.Indexes[0].Columns);
            Assert.False(table.Indexes[0].Unique);
            Assert.Equal(["tenant_id", "reference"], table.Indexes[1].Columns);
            Assert.True(table.Indexes[1].Unique);
            Assert.DoesNotContain(table.Indexes, index => index.Name == "orders_pkey");
        }
        [Fact]
        public void MetadataMaterialization_PreservesCompositeForeignKeyPairingAndActions() {
            using var connection = new TestableDBConnectionPostgresql();
            var table = new DBSchemaTable() { Name = "orders" };
            var metadata = CreateMetadataTable(
                ["constraint_name", "local_column", "referenced_table", "referenced_column", "ordinal_position", "delete_action", "update_action"],
                ["fk_order_customer", "tenant_id", "customers", "tenant_id", 1, "c", "a"],
                ["fk_order_customer", "customer_id", "customers", "id", 2, "c", "a"]);

            connection.PopulateForeignKeysForTest(table, metadata);

            var foreignKey = Assert.Single(table.ForeignKeys);
            Assert.Equal(["tenant_id", "customer_id"], foreignKey.Columns);
            Assert.Equal("customers", foreignKey.RefTable);
            Assert.Equal(["tenant_id", "id"], foreignKey.RefColumns);
            Assert.Equal(DBSchemaOnDeleteRule.Cascade, foreignKey.OnDelete);
            Assert.Equal(DBSchemaOnUpdateRule.NoAction, foreignKey.OnUpdate);
        }
        [Theory]
        [InlineData("hash", false, false, false, false, false, false, false, "non-btree access method")]
        [InlineData("btree", true, false, false, false, false, false, false, "expression keys")]
        [InlineData("btree", false, true, false, false, false, false, false, "partial predicate")]
        [InlineData("btree", false, false, true, false, false, false, false, "INCLUDE columns")]
        [InlineData("btree", false, false, false, true, false, false, false, "NULLS NOT DISTINCT")]
        [InlineData("btree", false, false, false, false, true, false, false, "non-default ordering")]
        [InlineData("btree", false, false, false, false, false, true, false, "non-default operator class")]
        [InlineData("btree", false, false, false, false, false, false, true, "non-default collation")]
        public void UnsupportedIndexMetadata_ThrowsWithIndexAndReason(string accessMethod, bool expressions, bool partial, bool includeColumns,
            bool nullsNotDistinct, bool ordering, bool operatorClass, bool collation, string reason) {
            using var connection = new TestableDBConnectionPostgresql();
            var metadata = CreateMetadataTable(
                ["index_name", "access_method", "has_expressions", "is_partial", "has_include_columns", "is_exclusion", "is_invalid",
                    "nulls_not_distinct", "has_nondefault_ordering", "has_nondefault_operator_class", "has_nondefault_collation"],
                ["ix_advanced", accessMethod, expressions, partial, includeColumns, false, false, nullsNotDistinct, ordering, operatorClass, collation]);

            var exception = Assert.Throws<NotSupportedException>(() => connection.ValidateSupportedIndexesForTest("orders", metadata));

            Assert.Contains("orders", exception.Message);
            Assert.Contains("ix_advanced", exception.Message);
            Assert.Contains(reason, exception.Message);
        }
        [Theory]
        [InlineData("archive", "s", false, false, "a", "a", "cross-schema reference")]
        [InlineData("public", "f", false, false, "a", "a", "MATCH mode")]
        [InlineData("public", "s", true, false, "a", "a", "deferrable semantics")]
        [InlineData("public", "s", false, false, "r", "a", "RESTRICT action")]
        public void UnsupportedForeignKeyMetadata_ThrowsWithConstraintAndReason(string referencedSchema, string matchType, bool deferrable,
            bool initiallyDeferred, string deleteAction, string updateAction, string reason) {
            using var connection = new TestableDBConnectionPostgresql();
            var metadata = CreateMetadataTable(
                ["constraint_name", "referenced_schema", "match_type", "is_deferrable", "is_initially_deferred", "delete_action", "update_action"],
                ["fk_advanced", referencedSchema, matchType, deferrable, initiallyDeferred, deleteAction, updateAction]);

            var exception = Assert.Throws<NotSupportedException>(() => connection.ValidateSupportedForeignKeysForTest("orders", "public", metadata));

            Assert.Contains("orders", exception.Message);
            Assert.Contains("fk_advanced", exception.Message);
            Assert.Contains(reason, exception.Message);
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
        private static DBTable CreateMetadataTable(string[] columns, params object?[][] rows) {
            var table = new DBTable();
            foreach (var column in columns) table.Columns.Add(column);
            foreach (var values in rows) table.Rows.Add(new DBRow(table, values));
            return table;
        }

        private sealed class TestableDBConnectionPostgresql : DProjects.Db.Postgresql.DBConnectionPostgresql {

            // props
            public int ColumnSize { get; set; }
            public int ColumnPrecision { get; set; }
            public int ColumnScale { get; set; }
            public DBSchemaDataType DiscoveredDataType { get; set; }
            public int GetSqlAlterColumnCallCount { get; private set; }

            // ctor
            public TestableDBConnectionPostgresql() : base("test", "Host=localhost;Database=test;Username=test;Password=test") {
            }

            // methods
            public bool AreSchemaColumnTypesEquivalentForTest(DBSchemaColumn actual, DBSchemaColumn expected) {
                return AreSchemaColumnTypesEquivalent(actual, expected);
            }
            public void PopulateIndexesForTest(DBSchemaTable table, DBTable metadata) {
                PopulateIndexes(table, metadata);
            }
            public void PopulateForeignKeysForTest(DBSchemaTable table, DBTable metadata) {
                PopulateForeignKeys(table, metadata);
            }
            public void ValidateSupportedIndexesForTest(string table, DBTable metadata) {
                ValidateSupportedIndexes(table, metadata);
            }
            public void ValidateSupportedForeignKeysForTest(string table, string schema, DBTable metadata) {
                ValidateSupportedForeignKeys(table, schema, metadata);
            }
            public override string[] GetTableNames() {
                return ["records"];
            }
            public override DBSchemaTable GetTableSchema(string table) {
                var schema = new DBSchemaTable() { Name = table };
                schema.Columns.Add(new DBSchemaColumn("value") {
                    DataType = DiscoveredDataType,
                    Size = ColumnSize,
                    Precision = ColumnPrecision,
                    Scale = ColumnScale
                });
                return schema;
            }
            public override string GetSqlAlterColumn(string table, DBSchemaColumn dBSchemaColumn) {
                GetSqlAlterColumnCallCount++;
                return base.GetSqlAlterColumn(table, dBSchemaColumn);
            }
        }
    }

    [Trait("Category", "Integration")]
    [Trait("Category", "CIProviderContract")]
    public class DBConnectionPostgresqlSchemaDiscoveryContractTests {

        // methods
        [Fact]
        public void GetTableSchema_DiscoversRealPostgresqlKeysAndIndexesWithoutDuplicatingPrimaryKey() {
            var connectionString = Environment.GetEnvironmentVariable("DPROJECTS_POSTGRESQL_CI")
                ?? throw new InvalidOperationException("DPROJECTS_POSTGRESQL_CI is required for the PostgreSQL provider contract.");
            var suffix = Guid.NewGuid().ToString("N");
            var parentTable = "schema_parent_" + suffix;
            var childTable = "schema_child_" + suffix;
            var parentPrimaryKey = "pk_parent_" + suffix;
            var childPrimaryKey = "pk_child_" + suffix;
            var normalIndex = "ix_parent_code_" + suffix;
            var compositeIndex = "ux_external_ref_" + suffix;
            var normalForeignKey = "fk_parent_code_" + suffix;
            var compositeForeignKey = "fk_parent_key_" + suffix;
            using var connection = new DProjects.Db.Postgresql.DBConnectionPostgresql("ci-provider-contract", connectionString);
            try {
                // creates an isolated schema using every production discovery category under contract
                connection.ExecuteNonQuery($"CREATE TABLE \"{parentTable}\" (\"tenant_id\" INTEGER NOT NULL, \"id\" INTEGER NOT NULL, \"code\" INTEGER NOT NULL, CONSTRAINT \"{parentPrimaryKey}\" PRIMARY KEY (\"tenant_id\", \"id\"), CONSTRAINT \"uq_code_{suffix}\" UNIQUE (\"code\"))");
                connection.ExecuteNonQuery($"CREATE TABLE \"{childTable}\" (\"child_id\" INTEGER NOT NULL, \"tenant_id\" INTEGER NOT NULL, \"parent_id\" INTEGER NOT NULL, \"parent_code\" INTEGER NULL, \"external_ref\" VARCHAR(40) NOT NULL, CONSTRAINT \"{childPrimaryKey}\" PRIMARY KEY (\"child_id\"), CONSTRAINT \"{normalForeignKey}\" FOREIGN KEY (\"parent_code\") REFERENCES \"{parentTable}\" (\"code\") ON DELETE SET NULL ON UPDATE CASCADE, CONSTRAINT \"{compositeForeignKey}\" FOREIGN KEY (\"tenant_id\", \"parent_id\") REFERENCES \"{parentTable}\" (\"tenant_id\", \"id\") ON DELETE CASCADE ON UPDATE NO ACTION)");
                connection.ExecuteNonQuery($"CREATE INDEX \"{normalIndex}\" ON \"{childTable}\" (\"parent_code\")");
                connection.ExecuteNonQuery($"CREATE UNIQUE INDEX \"{compositeIndex}\" ON \"{childTable}\" (\"tenant_id\", \"external_ref\")");

                var schema = connection.GetTableSchema(childTable);

                Assert.Equal(["child_id", "tenant_id", "parent_id", "parent_code", "external_ref"], schema.Columns.Select(column => column.Name));
                Assert.NotNull(schema.PrimaryKey);
                Assert.Equal(childPrimaryKey, schema.PrimaryKey.Name);
                Assert.Equal(["child_id"], schema.PrimaryKey.Columns);
                Assert.DoesNotContain(schema.Indexes, index => index.Name == childPrimaryKey);
                var discoveredNormalIndex = Assert.Single(schema.Indexes, index => index.Name == normalIndex);
                Assert.False(discoveredNormalIndex.Unique);
                Assert.Equal(["parent_code"], discoveredNormalIndex.Columns);
                var discoveredCompositeIndex = Assert.Single(schema.Indexes, index => index.Name == compositeIndex);
                Assert.True(discoveredCompositeIndex.Unique);
                Assert.Equal(["tenant_id", "external_ref"], discoveredCompositeIndex.Columns);
                var discoveredNormalForeignKey = Assert.Single(schema.ForeignKeys, foreignKey => foreignKey.Name == normalForeignKey);
                Assert.Equal(["parent_code"], discoveredNormalForeignKey.Columns);
                Assert.Equal(parentTable, discoveredNormalForeignKey.RefTable);
                Assert.Equal(["code"], discoveredNormalForeignKey.RefColumns);
                Assert.Equal(DBSchemaOnDeleteRule.SetNull, discoveredNormalForeignKey.OnDelete);
                Assert.Equal(DBSchemaOnUpdateRule.Cascade, discoveredNormalForeignKey.OnUpdate);
                var discoveredCompositeForeignKey = Assert.Single(schema.ForeignKeys, foreignKey => foreignKey.Name == compositeForeignKey);
                Assert.Equal(["tenant_id", "parent_id"], discoveredCompositeForeignKey.Columns);
                Assert.Equal(parentTable, discoveredCompositeForeignKey.RefTable);
                Assert.Equal(["tenant_id", "id"], discoveredCompositeForeignKey.RefColumns);
                Assert.Equal(DBSchemaOnDeleteRule.Cascade, discoveredCompositeForeignKey.OnDelete);
                Assert.Equal(DBSchemaOnUpdateRule.NoAction, discoveredCompositeForeignKey.OnUpdate);
            } finally {
                connection.ExecuteNonQuery($"DROP TABLE IF EXISTS \"{childTable}\"");
                connection.ExecuteNonQuery($"DROP TABLE IF EXISTS \"{parentTable}\"");
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
