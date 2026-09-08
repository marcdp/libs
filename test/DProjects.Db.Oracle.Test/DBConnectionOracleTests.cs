using DProjects.Db.Schema;

namespace DProjects.Db.Oracle.Tests {

    public class DBConnectionOracleTests {

        // tests
        [Fact]
        public void RetainedPrimitivesUseVerifiedOracleSyntax() {
            var connection = CreateConnectionWithoutProviderInitialization();

            Assert.Equal(":", connection.GetSqlParameterPrefix());
            Assert.Equal(":__p0", connection.GetSqlParameterPlaceholderForTest(0));
            Assert.Equal("SELECT 1 FROM DUAL", connection.GetSqlSelectTest());
            Assert.Equal("CURRENT_TIMESTAMP", connection.GetSqlDefaultNowExpression());
            Assert.Equal("%", connection.GetSqlLikeAllExpression());
            Assert.Equal("_", connection.GetSqlLikeOneExpression());
            Assert.Equal(";", connection.GetSqlSeparator());
            Assert.Equal("a", connection.GetSqlAlias());
            Assert.Equal("COMMIT", connection.GetSqlTransactionCommit());
            Assert.Equal("ROLLBACK", connection.GetSqlTransactionRollBack());
            Assert.Equal("\"", connection.GetSqlQualifierBegin());
            Assert.Equal("\"", connection.GetSqlQualifierEnd());
            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromSqlDataTypeName("VARCHAR2", 100, 0, 0));
            Assert.Equal(DBSchemaDataType.Numeric, connection.GetDataTypeFromSqlDataTypeName("NUMBER", 0, 12, 3));
            Assert.Equal(DBSchemaDataType.Double, connection.GetDataTypeFromSqlDataTypeName("FLOAT", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.DateTime, connection.GetDataTypeFromSqlDataTypeName("DATE", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Timestamp, connection.GetDataTypeFromSqlDataTypeName("TIMESTAMP", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("TIMESTAMP WITH TIME ZONE", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("TIMESTAMP WITH LOCAL TIME ZONE", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlTransactionStart());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlTransactionWrap("SELECT 1 FROM DUAL"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlTrueExpression());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlFalseExpression());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlSelectTop(1));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlSelectTopAtEnd());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlSelectOffsetLimit(0, 1));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlSelectAutoincrement());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlIdentityDefinition(typeof(int)));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlEncodedLikeValue("value"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlGetNextSequenceValue("sequence"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlTempTablePrefix());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlIfRowCountThrowError(0, 1, "error"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlEncodedValue(DateTime.UtcNow, typeof(DateTime), false));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlEncodedValue(DateTimeOffset.UtcNow, typeof(DateTimeOffset), false));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlEncodedValue(true, typeof(bool), false));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlEncodedValue(Guid.Empty, typeof(Guid), false));
            Assert.Equal("42", connection.GetSqlEncodedValue(42, typeof(int), false));
        }
        [Fact]
        public void CopiedOrUnverifiedSchemaCapabilitiesAreExplicitlyUnsupported() {
            var connection = CreateConnectionWithoutProviderInitialization();

            Assert.Throws<NotSupportedException>(() => connection.GetTableNames());
            Assert.Throws<NotSupportedException>(() => connection.GetTableSchema("records"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlTypeDefinition(DBSchemaDataType.Varchar, 100, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetViewNames());
            Assert.Throws<NotSupportedException>(() => connection.GetSequenceNames());
            Assert.Throws<NotSupportedException>(() => connection.GetProcedureNames());
            Assert.Throws<NotSupportedException>(() => connection.BackupDb());
            Assert.Throws<NotSupportedException>(() => connection.RestoreDb("backup", "database"));
            Assert.Throws<NotSupportedException>(() => connection.CompactDb());
            Assert.Throws<NotSupportedException>(() => connection.ExistsDb("database"));
            Assert.Throws<NotSupportedException>(() => connection.CreateDb("database"));
        }

        private sealed class TestableDBConnectionOracle : DProjects.Db.Oracle.DBConnectionOracle {

            // ctor
            public TestableDBConnectionOracle() : base("test", "Data Source=test;User Id=test;Password=test") {
            }

            // methods
            public string GetSqlParameterPlaceholderForTest(int index) {
                return GetSqlParameterPlaceholder(index);
            }
        }

        // methods (private)
        private static TestableDBConnectionOracle CreateConnectionWithoutProviderInitialization() {
            // isolates SQL-generation tests from the legacy provider package's runtime initialization
            return (TestableDBConnectionOracle)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(TestableDBConnectionOracle));
        }
    }
}
