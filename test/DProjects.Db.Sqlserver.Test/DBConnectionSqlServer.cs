using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;
using DProjects.Db.Schema;

namespace DProjects.Db.SqlServer.Tests {

    public class DBConnectionSqlServerSqlGenerationTests {

        // methods
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
    }

    [Trait("Category", "Integration")]
    public class DBConnectionSqlServerTests : DBConnectionTests<DProjects.Db.SqlServer.DBConnectionSqlServer> {


        //constructor
        public DBConnectionSqlServerTests() : base("sqlserver:Server=.;Database=test;Trusted_Connection=True;TrustServerCertificate=True;") {
        }  
    }
}
