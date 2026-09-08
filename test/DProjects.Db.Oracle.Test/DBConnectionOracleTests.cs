using DProjects.Db.Schema;

namespace DProjects.Db.Oracle.Tests {

    public class DBConnectionOracleTests {

        // tests
        [Fact]
        public void RetainedPrimitivesUseVerifiedOracleSyntax() {
            var connection = CreateConnectionWithoutProviderInitialization();

            Assert.Equal(":", connection.GetSqlParameterPrefix());
            Assert.Equal(":__p0", connection.GetSqlParameterPlaceholderForTest(0));
            Assert.Equal("\"", connection.GetSqlQualifierBegin());
            Assert.Equal("\"", connection.GetSqlQualifierEnd());
            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromSqlDataTypeName("VARCHAR2", 100, 0, 0));
            Assert.Equal(DBSchemaDataType.Numeric, connection.GetDataTypeFromSqlDataTypeName("NUMBER", 0, 12, 3));
            Assert.Equal(DBSchemaDataType.Double, connection.GetDataTypeFromSqlDataTypeName("FLOAT", 0, 0, 0));
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
