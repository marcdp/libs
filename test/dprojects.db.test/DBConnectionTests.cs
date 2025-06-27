using DProjects.Db.Schema;
using DProjects.Factories;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Sdk;

namespace DProjects.Db.Tests {

    public abstract class DBConnectionTests<T> where T : IDBConnection {

        //vars
        private readonly IDBConnection mDBConnection;


        //constructor
        public DBConnectionTests(string connectionString, System.Reflection.Assembly? assembly = null) {
            //use secrets
            if (connectionString.StartsWith("user-secret:")) {
                var key = connectionString.Substring(connectionString.IndexOf(":") + 1);
                var builder = new ConfigurationBuilder().AddUserSecrets<DBConnectionTests<T>>();
                var config = builder.Build();
                var secretProvider = config.Providers.First();
                secretProvider.TryGet(key, out connectionString);
            }
            //register connection factory
            var services = new ServiceCollection();
            services.AddFactoryByUrl<IDBConnection>(cfg => {
                cfg.AddFactoriesFromAssembly(typeof(T).Assembly);
            });
            var serviceProvider = services.BuildServiceProvider();
            var factory = serviceProvider.GetRequiredService<IFactoryByUrl<IDBConnection>>();
            mDBConnection = factory.Create(connectionString);
        }


        //tests
        [Fact]
        public async Task Open_ShouldOpenConnection() {
            // Open
            await mDBConnection.OpenAsync();
            Assert.Equal(System.Data.ConnectionState.Open, mDBConnection.Connection.State);
            // Close
            mDBConnection.Close();
            Assert.Equal(System.Data.ConnectionState.Closed, mDBConnection.Connection.State);
        }
        [Theory]
        [InlineData("SELECT ?", new object?[] { 1 }, "SELECT 1")]
        [InlineData("SELECT ?, ?, ?", new object?[] { 1, 2, 3 }, "SELECT 1, 2, 3")]
        [InlineData("SELECT * WHERE a=?", new object?[] { "hello'" }, "SELECT * WHERE a='hello'''")]
        [InlineData("SELECT * WHERE a=?", new object?[] { "hello''" }, "SELECT * WHERE a='hello'''''")]
        [InlineData("SELECT * WHERE a=?", new object?[] { "hello''a" }, "SELECT * WHERE a='hello''''a'")]
        public void ParseStatement(string sql, object[] parameters, string expected) {
            Assert.Equal(expected, mDBConnection.ParseStatement(sql, parameters));
        }
        [Fact]
        public void Exec_ShouldOpenConnection() {
            mDBConnection.ExecuteScalar<object>(mDBConnection.GetSqlSelectTest());
        }
        [Fact]
        public async Task ExecuteNonQuery_InTransaction() {
            var tableName = "test";
            //drop table
            if (mDBConnection.ExistsTable(tableName)) {
                await mDBConnection.ExecuteNonQueryAsync(mDBConnection.GetSqlDropTable(tableName));
            }
            //create table
            var dbSchemaTable = new DBSchemaTable();
            dbSchemaTable.Name = tableName;
            dbSchemaTable.Columns.Add(new DBSchemaColumn() { Name = "id", DataType = DBSchemaDataType.Int });
            dbSchemaTable.Columns.Add(new DBSchemaColumn() { Name = "name", DataType = DBSchemaDataType.Varchar, Size = 100 });
            dbSchemaTable.PrimaryKey = new DBSchemaPrimaryKey() {
                Name = "pk_" + tableName,
                Columns = ["id"]
            };
            await mDBConnection.ExecuteNonQueryAsync(mDBConnection.GetSqlCreateTable(dbSchemaTable));
            //insert ok 
            await mDBConnection.ExecuteNonQueryAsync("INSERT INTO " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd() + " values (?,?)", [1, "hello"]);
            Assert.Equal(1, mDBConnection.ExecuteScalar<int>("SELECT max(id) FROM " + tableName));
            //insert error
            mDBConnection.BeginTrans();
            try {
                await mDBConnection.ExecuteNonQueryAsync("INSERT INTO " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd() + " values (?,?)", [2, "byte"]);
                throw new Exception();
            } catch (Exception ex) {
                mDBConnection.RollBackTrans();
            }
            Assert.Equal(1, mDBConnection.ExecuteScalar<int>("SELECT max(id) FROM " + tableName));
            //insert N rows
            for (var i = 2; i < 100; i++) {
                await mDBConnection.ExecuteNonQueryAsync("INSERT INTO " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd() + " values (?,?)", [i, "hello"]);
            }
            //select top
            var offset = 10;
            var length = 5;
            var dbTable = await mDBConnection.ExecuteTableAsync("SELECT * FROM " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd() + " ORDER BY id " + mDBConnection.GetSqlSelectOffsetLimit(offset, length));
            Assert.Equal(length, dbTable.Rows.Count);
            Assert.Equal(offset + 1, dbTable.Rows[0].Get<int>("id", 0));
            Assert.Equal(offset + length, dbTable.Rows[length - 1].Get<int>("id", 0));
            //drop table
            await mDBConnection.ExecuteNonQueryAsync(mDBConnection.GetSqlDropTable(tableName));
        }
        [Fact]

        // create a simple table from schema
        public async Task CreateSimpleTableFromSchema() {
            var tableName = "test";
            //drop table
            if (mDBConnection.ExistsTable(tableName)) {
                await mDBConnection.ExecuteNonQueryAsync(mDBConnection.GetSqlDropTable(tableName));
            }
            //create table
            var dbSchemaTable = new DBSchemaTable();
            dbSchemaTable.Name = tableName;
            dbSchemaTable.Columns.Add(new DBSchemaColumn() { Name = "id", DataType = DBSchemaDataType.Int });
            dbSchemaTable.Columns.Add(new DBSchemaColumn() { Name = "name", DataType = DBSchemaDataType.Varchar, Size = 100 });
            dbSchemaTable.PrimaryKey = new DBSchemaPrimaryKey() {
                Name = "pk_" + tableName,
                Columns = ["id"]
            };
            await mDBConnection.ExecuteNonQueryAsync(mDBConnection.GetSqlCreateTable(dbSchemaTable));
            //insert ok 
            await mDBConnection.ExecuteNonQueryAsync("INSERT INTO " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd() + " values (?,?)", [1, "hello"]);
            Assert.Equal(1, mDBConnection.ExecuteScalar<int>("SELECT max(id) FROM " + tableName));
            //insert error
            mDBConnection.BeginTrans();
            try {
                await mDBConnection.ExecuteNonQueryAsync("INSERT INTO " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd() + " values (?,?)", [2, "byte"]);
                throw new Exception();
            } catch (Exception ex) {
                mDBConnection.RollBackTrans();
            }
            Assert.Equal(1, mDBConnection.ExecuteScalar<int>("SELECT max(id) FROM " + tableName));
            //insert N rows
            for (var i = 2; i < 100; i++) {
                await mDBConnection.ExecuteNonQueryAsync("INSERT INTO " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd() + " values (?,?)", [i, "hello"]);
            }
            //select top
            var offset = 10;
            var length = 5;
            var dbTable = await mDBConnection.ExecuteTableAsync("SELECT * FROM " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd() + " ORDER BY id " + mDBConnection.GetSqlSelectOffsetLimit(offset, length));
            Assert.Equal(length, dbTable.Rows.Count);
            // delete rows
            await mDBConnection.ExecuteNonQueryAsync("DELETE FROM " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd());
            // check if table is empty
            dbTable = await mDBConnection.ExecuteTableAsync("SELECT * FROM " + mDBConnection.GetSqlQualifierBegin() + tableName + mDBConnection.GetSqlQualifierEnd());
            Assert.Equal(0, dbTable.Rows.Count);
            // drop table
            await mDBConnection.ExecuteNonQueryAsync(mDBConnection.GetSqlDropTable(tableName));
        }
    }
}
