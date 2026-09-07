using DProjects.Db.Schema;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace DProjects.Db.Tests {

    public class DBConnectionHardeningTests {

        // tests
        [Fact]
        public void ParseStatement_ShouldValidatePlaceholderCountForBothCommandModes() {
            using var connection = new TestDBConnection();
            Assert.Equal("SELECT @__p0, @__p1", connection.ParseStatement("SELECT ?, ?", [1, 2]));
            Assert.Throws<ArgumentException>(() => connection.ParseStatement("SELECT ?, ?", [1]));
            Assert.Throws<ArgumentException>(() => connection.ParseStatement("SELECT ?", [1, 2]));
            connection.AvoidParametrizedQueries = true;
            Assert.Throws<ArgumentException>(() => connection.ParseStatement("SELECT ?, ?", [1]));
            Assert.Throws<ArgumentException>(() => connection.ParseStatement("SELECT ?", [1, 2]));
        }
        [Fact]
        public void ParseStatement_ShouldEncodeLiteralValues() {
            using var connection = new TestDBConnection() { AvoidParametrizedQueries = true };
            Assert.Equal("SELECT NULL", connection.ParseStatement("SELECT ?", [null]));
            Assert.Equal("SELECT NULL", connection.ParseStatement("SELECT ?", [DBNull.Value]));
            Assert.Equal("SELECT 'hello'''", connection.ParseStatement("SELECT ?", ["hello'"]));
        }
        [Theory]
        [InlineData(CommandOperation.NonQuery)]
        [InlineData(CommandOperation.Scalar)]
        [InlineData(CommandOperation.DbDataReader)]
        [InlineData(CommandOperation.Reader)]
        public async Task AsyncExecution_ShouldForwardAndPreserveCancellation(CommandOperation operation) {
            using var connection = new TestDBConnection();
            var cancellationToken = TestContext.Current.CancellationToken;
            await Assert.ThrowsAsync<OperationCanceledException>(async () => {
                if (operation == CommandOperation.NonQuery) {
                    await connection.ExecuteNonQueryAsync("SELECT 1", cancellationToken: cancellationToken);
                } else if (operation == CommandOperation.Scalar) {
                    await connection.ExecuteScalarAsync<object>("SELECT 1", cancellationToken: cancellationToken);
                } else if (operation == CommandOperation.DbDataReader) {
                    await connection.ExecuteDbDataReaderAsync("SELECT 1", cancellationToken: cancellationToken);
                } else {
                    await connection.ExecuteReaderAsync("SELECT 1", cancellationToken: cancellationToken);
                }
            });
            Assert.Equal(cancellationToken, connection.FakeConnection.LastCommand!.CancellationToken);
        }
        [Fact]
        public void UnsupportedOperations_ShouldUseSpecificExceptions() {
            using var connection = new TestDBConnection();
            Assert.Throws<NotSupportedException>(() => connection.GetTableNames());
            Assert.Throws<NotSupportedException>(() => connection.BackupDb());
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("unsupported", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromNetDataTypeName(typeof(Version)));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlEncodedValue(new Version(), typeof(Version), false));
            Assert.Throws<NotSupportedException>(() => new DBTable().Select("id = 1"));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((DBSchemaDataType)int.MaxValue).GetNetDataType());
            Assert.Throws<ArgumentOutOfRangeException>(() => ((DBSchemaDataType)int.MaxValue).GetDbType());
        }

        public enum CommandOperation {
            NonQuery,
            Scalar,
            DbDataReader,
            Reader
        }

        private sealed class TestDBConnection : DBConnection {

            // props
            public bool AvoidParametrizedQueries {
                get => mAvoidParametrizedQueries;
                set => mAvoidParametrizedQueries = value;
            }
            public FakeDbConnection FakeConnection => (FakeDbConnection)Connection;

            // ctor
            public TestDBConnection() : base("test", "test", new FakeDbConnection()) {
            }
        }

        private sealed class FakeDbConnection : DbConnection {

            // vars
            private ConnectionState mState;

            // props
            public FakeDbCommand? LastCommand { get; private set; }
            [AllowNull]
            public override string ConnectionString { get; set; } = "";
            public override string Database => "test";
            public override string DataSource => "test";
            public override string ServerVersion => "1";
            public override ConnectionState State => mState;

            // methods
            public override void ChangeDatabase(string databaseName) {
            }
            public override void Close() {
                mState = ConnectionState.Closed;
            }
            public override void Open() {
                mState = ConnectionState.Open;
            }

            // methods (private)
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) {
                throw new NotSupportedException();
            }
            protected override DbCommand CreateDbCommand() {
                LastCommand = new FakeDbCommand(this);
                return LastCommand;
            }
        }

        private sealed class FakeDbCommand : DbCommand {

            // vars
            private readonly FakeDbParameterCollection mParameters = new();
            private DbConnection? mConnection;
            private DbTransaction? mTransaction;

            // props
            public CancellationToken CancellationToken { get; private set; }
            [AllowNull]
            public override string CommandText { get; set; } = "";
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            protected override DbConnection? DbConnection { get => mConnection; set => mConnection = value; }
            protected override DbParameterCollection DbParameterCollection => mParameters;
            protected override DbTransaction? DbTransaction { get => mTransaction; set => mTransaction = value; }

            // ctor
            public FakeDbCommand(DbConnection connection) {
                mConnection = connection;
            }

            // methods
            public override void Cancel() {
            }
            public override int ExecuteNonQuery() {
                throw new NotSupportedException();
            }
            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) {
                CancellationToken = cancellationToken;
                return Task.FromException<int>(new OperationCanceledException(cancellationToken));
            }
            public override object? ExecuteScalar() {
                throw new NotSupportedException();
            }
            public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) {
                CancellationToken = cancellationToken;
                return Task.FromException<object?>(new OperationCanceledException(cancellationToken));
            }
            public override void Prepare() {
            }

            // methods (private)
            protected override DbParameter CreateDbParameter() {
                return new FakeDbParameter();
            }
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
                throw new NotSupportedException();
            }
            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) {
                CancellationToken = cancellationToken;
                return Task.FromException<DbDataReader>(new OperationCanceledException(cancellationToken));
            }
        }

        private sealed class FakeDbParameter : DbParameter {

            // props
            public override DbType DbType { get; set; }
            public override ParameterDirection Direction { get; set; }
            public override bool IsNullable { get; set; }
            [AllowNull]
            public override string ParameterName { get; set; } = "";
            [AllowNull]
            public override string SourceColumn { get; set; } = "";
            public override object? Value { get; set; }
            public override bool SourceColumnNullMapping { get; set; }
            public override int Size { get; set; }

            // methods
            public override void ResetDbType() {
            }
        }

        private sealed class FakeDbParameterCollection : DbParameterCollection {

            // vars
            private readonly List<DbParameter> mParameters = [];

            // props
            public override int Count => mParameters.Count;
            public override object SyncRoot => ((ICollection)mParameters).SyncRoot;

            // methods
            public override int Add(object value) {
                mParameters.Add((DbParameter)value);
                return mParameters.Count - 1;
            }
            public override void AddRange(Array values) {
                foreach (var value in values) Add(value!);
            }
            public override void Clear() {
                mParameters.Clear();
            }
            public override bool Contains(object value) {
                return mParameters.Contains((DbParameter)value);
            }
            public override bool Contains(string value) {
                return IndexOf(value) >= 0;
            }
            public override void CopyTo(Array array, int index) {
                ((ICollection)mParameters).CopyTo(array, index);
            }
            public override IEnumerator GetEnumerator() {
                return mParameters.GetEnumerator();
            }
            public override int IndexOf(object value) {
                return mParameters.IndexOf((DbParameter)value);
            }
            public override int IndexOf(string parameterName) {
                return mParameters.FindIndex(parameter => parameter.ParameterName == parameterName);
            }
            public override void Insert(int index, object value) {
                mParameters.Insert(index, (DbParameter)value);
            }
            public override void Remove(object value) {
                mParameters.Remove((DbParameter)value);
            }
            public override void RemoveAt(int index) {
                mParameters.RemoveAt(index);
            }
            public override void RemoveAt(string parameterName) {
                var index = IndexOf(parameterName);
                if (index >= 0) RemoveAt(index);
            }

            // methods (private)
            protected override DbParameter GetParameter(int index) {
                return mParameters[index];
            }
            protected override DbParameter GetParameter(string parameterName) {
                return mParameters[IndexOf(parameterName)];
            }
            protected override void SetParameter(int index, DbParameter value) {
                mParameters[index] = value;
            }
            protected override void SetParameter(string parameterName, DbParameter value) {
                var index = IndexOf(parameterName);
                if (index < 0) Add(value); else SetParameter(index, value);
            }
        }
    }
}
