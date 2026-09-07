using System.Data.Common;
using System.Data;
using DProjects.Db.Tests;
using DProjects.Db.Schema;

namespace DProjects.Db.Sqlite.Tests
{
    [Trait("Category", "Integration")]
    public class DBConnectionSqliteTests : DBConnectionTests<DProjects.Db.Sqlite.DBConnectionSqlite> {

        //constructor 
        public DBConnectionSqliteTests() : base ("user-secret:sqlite", typeof(DProjects.Db.Sqlite.Assembly).Assembly) {
        }
    }

    public class DBConnectionSqlitePortableContractTests {

        // methods
        [Fact]
        public void SqlPrimitives_UseSqliteDialectBehavior() {
            using var connection = CreateConnection();

            Assert.Equal(" LIMIT 5", connection.GetSqlSelectTop(5));
            Assert.True(connection.GetSqlSelectTopAtEnd());
            Assert.Equal(" LIMIT 5 OFFSET 10", connection.GetSqlSelectOffsetLimit(10, 5));
            Assert.Equal("CURRENT_TIMESTAMP", connection.GetSqlDefaultNowExpression());
            Assert.Equal("TEXT", connection.GetSqlTypeDefinition(DBSchemaDataType.Varchar, 0, 0, 0));
            Assert.Equal("BLOB", connection.GetSqlTypeDefinition(DBSchemaDataType.Varbinary, 0, 0, 0));
            Assert.Equal("DROP INDEX index", connection.GetSqlDropIndex("table", "index"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlCreateDefault("table", "column", "0"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlAlterColumn("table", new DBSchemaColumn("column")));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlCreateSequence(new DBSchemaSequence()));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlGetNextSequenceValue("sequence"));
        }
        [Fact]
        public async Task Lifecycle_OpenCloseReopenAndAutoOpenExecution() {
            using var connection = CreateConnection();
            connection.Open();
            Assert.True(connection.IsOpen);
            connection.Close();
            Assert.False(connection.IsOpen);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            Assert.True(connection.IsOpen);
            connection.Close();
            Assert.Equal(1L, connection.ExecuteScalar<long>("SELECT 1"));
            Assert.True(connection.IsOpen);
        }
        [Fact]
        public async Task BasicExecution_SupportsSyncAndAsyncPaths() {
            using var connection = CreateConnection();
            Assert.Equal(0, connection.ExecuteNonQuery("CREATE TABLE portable_test (id INTEGER NOT NULL, value TEXT NULL)"));
            Assert.Equal(1, connection.ExecuteNonQuery("INSERT INTO portable_test (id, value) VALUES (?, ?)", [1, "one"]));
            Assert.Equal(1L, connection.ExecuteScalar<long>("SELECT COUNT(*) FROM portable_test"));
            using (var reader = connection.ExecuteReader("SELECT id FROM portable_test")) Assert.NotNull(reader.Read());
            Assert.Equal(1, await connection.ExecuteNonQueryAsync("INSERT INTO portable_test (id, value) VALUES (?, ?)", [2, "two"], TestContext.Current.CancellationToken));
            Assert.Equal(2L, await connection.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM portable_test", cancellationToken: TestContext.Current.CancellationToken));
            using var asyncReader = await connection.ExecuteReaderAsync("SELECT id FROM portable_test ORDER BY id", cancellationToken: TestContext.Current.CancellationToken);
            Assert.NotNull(await asyncReader.ReadAsync(TestContext.Current.CancellationToken));
        }
        [Fact]
        public void Parameters_AreBoundNativelyForStringsNullApostrophesAndMultipleValues() {
            using var connection = CreateConnection();
            connection.ExecuteNonQuery("CREATE TABLE portable_parameters (id INTEGER NOT NULL, value TEXT NULL)");
            connection.ExecuteNonQuery("INSERT INTO portable_parameters (id, value) VALUES (?, ?)", [1, "hello'"]);
            connection.ExecuteNonQuery("INSERT INTO portable_parameters (id, value) VALUES (?, ?)", [2, null]);
            Assert.Equal("hello'", connection.ExecuteScalar<string>("SELECT value FROM portable_parameters WHERE id=?", [1]));
            Assert.Equal(1L, connection.ExecuteScalar<long>("SELECT COUNT(*) FROM portable_parameters WHERE value IS NULL AND id=?", [2]));
            Assert.Equal("SELECT 'hello'''", connection.ParseStatement("SELECT ?", ["hello'"]));
        }
        [Fact]
        public void Transactions_CommitAndRollback() {
            using var connection = CreateConnection();
            connection.ExecuteNonQuery("CREATE TABLE portable_transactions (id INTEGER NOT NULL)");
            connection.BeginTrans();
            connection.ExecuteNonQuery("INSERT INTO portable_transactions (id) VALUES (?)", [1]);
            connection.CommitTrans();
            Assert.Equal(1L, connection.ExecuteScalar<long>("SELECT COUNT(*) FROM portable_transactions"));
            connection.BeginTrans();
            connection.ExecuteNonQuery("INSERT INTO portable_transactions (id) VALUES (?)", [2]);
            connection.RollBackTrans();
            Assert.Equal(1L, connection.ExecuteScalar<long>("SELECT COUNT(*) FROM portable_transactions"));
        }

        // methods (private)
        private static DProjects.Db.Sqlite.DBConnectionSqlite CreateConnection() {
            return new DProjects.Db.Sqlite.DBConnectionSqlite("portable", "Data Source=:memory:");
        }
    }
}
