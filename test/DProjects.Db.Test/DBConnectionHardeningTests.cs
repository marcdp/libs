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
            if (operation == CommandOperation.DbDataReader || operation == CommandOperation.Reader) Assert.True(connection.FakeConnection.LastCommand.IsDisposed);
        }
        [Fact]
        public void ExecuteReader_DisposeOwnsDataReaderAndCommandAndIsRepeatable() {
            using var connection = new TestDBConnection();

            var reader = connection.ExecuteReader("SELECT 1");
            var command = connection.FakeConnection.LastCommand!;
            var dataReader = command.Reader!;

            Assert.False(dataReader.IsDisposed);
            Assert.False(command.IsDisposed);
            reader.Dispose();
            Assert.True(dataReader.IsDisposed);
            Assert.True(command.IsDisposed);
            reader.Dispose();
        }
        [Fact]
        public async Task ExecuteReaderAsync_DisposeOwnsDataReaderAndCommand() {
            using var connection = new TestDBConnection();
            connection.FakeConnection.CancelReaderExecutionAsync = false;

            var reader = await connection.ExecuteReaderAsync("SELECT 1", cancellationToken: TestContext.Current.CancellationToken);
            var command = connection.FakeConnection.LastCommand!;
            var dataReader = command.Reader!;

            Assert.False(dataReader.IsDisposed);
            Assert.False(command.IsDisposed);
            reader.Dispose();
            Assert.True(dataReader.IsDisposed);
            Assert.True(command.IsDisposed);
        }
        [Fact]
        public void ExecuteDbDataReader_DisposeOwnsUnderlyingReaderAndCommand() {
            using var connection = new TestDBConnection();

            var reader = connection.ExecuteDbDataReader("SELECT 1");
            var command = connection.FakeConnection.LastCommand!;
            var underlyingReader = command.Reader!;

            Assert.False(underlyingReader.IsDisposed);
            Assert.False(command.IsDisposed);
            reader.Dispose();
            Assert.True(underlyingReader.IsDisposed);
            Assert.True(command.IsDisposed);
        }
        [Fact]
        public async Task ExecuteDbDataReaderAsync_DisposeOwnsUnderlyingReaderAndCommand() {
            using var connection = new TestDBConnection();
            connection.FakeConnection.CancelReaderExecutionAsync = false;

            var reader = await connection.ExecuteDbDataReaderAsync("SELECT 1", cancellationToken: TestContext.Current.CancellationToken);
            var command = connection.FakeConnection.LastCommand!;
            var underlyingReader = command.Reader!;

            Assert.False(underlyingReader.IsDisposed);
            Assert.False(command.IsDisposed);
            await reader.DisposeAsync();
            Assert.True(underlyingReader.IsDisposed);
            Assert.True(command.IsDisposed);
        }
        [Theory]
        [InlineData(ReaderOperation.Reader)]
        [InlineData(ReaderOperation.ReaderAsync)]
        [InlineData(ReaderOperation.DbDataReader)]
        [InlineData(ReaderOperation.DbDataReaderAsync)]
        public async Task ReaderExecutionFailure_DisposesCommand(ReaderOperation operation) {
            using var connection = new TestDBConnection();
            connection.FakeConnection.ReaderExecutionException = new InvalidOperationException("reader failed");
            connection.FakeConnection.CancelReaderExecutionAsync = false;

            if (operation == ReaderOperation.Reader) {
                Assert.Throws<Exception>(() => connection.ExecuteReader("SELECT 1"));
            } else if (operation == ReaderOperation.ReaderAsync) {
                await Assert.ThrowsAsync<Exception>(() => connection.ExecuteReaderAsync("SELECT 1", cancellationToken: TestContext.Current.CancellationToken));
            } else if (operation == ReaderOperation.DbDataReader) {
                Assert.Throws<Exception>(() => connection.ExecuteDbDataReader("SELECT 1"));
            } else {
                await Assert.ThrowsAsync<Exception>(() => connection.ExecuteDbDataReaderAsync("SELECT 1", cancellationToken: TestContext.Current.CancellationToken));
            }

            Assert.True(connection.FakeConnection.LastCommand!.IsDisposed);
        }
        [Fact]
        public async Task ReaderCommandConfigurationFailure_DisposesCommandForSyncAndAsyncPaths() {
            using var connection = new TestDBConnection();

            Assert.Throws<ArgumentException>(() => connection.ExecuteReader("SELECT ?"));
            Assert.True(connection.FakeConnection.LastCommand!.IsDisposed);

            await Assert.ThrowsAsync<ArgumentException>(() => connection.ExecuteReaderAsync("SELECT ?", cancellationToken: TestContext.Current.CancellationToken));
            Assert.True(connection.FakeConnection.LastCommand!.IsDisposed);
        }
        [Theory]
        [InlineData(ReaderOperation.Reader)]
        [InlineData(ReaderOperation.DbDataReader)]
        public void ReaderDisposalFailure_StillDisposesCommand(ReaderOperation operation) {
            using var connection = new TestDBConnection();
            connection.FakeConnection.ThrowOnReaderDispose = true;

            IDisposable reader = operation == ReaderOperation.Reader ? connection.ExecuteReader("SELECT 1") : connection.ExecuteDbDataReader("SELECT 1");

            Assert.Throws<InvalidOperationException>(() => reader.Dispose());
            Assert.True(connection.FakeConnection.LastCommand!.Reader!.IsDisposed);
            Assert.True(connection.FakeConnection.LastCommand.IsDisposed);
        }
        [Fact]
        public async Task ExecuteReader_SyncAndAsyncUseEquivalentInitializationSettings() {
            using var connection = new TestDBConnection() { AvoidInitializeDBTableFromDataReader = true };

            using var syncReader = connection.ExecuteReader("SELECT 1");
            Assert.Equal("value", syncReader.GetColumns()[0].Name);

            connection.FakeConnection.CancelReaderExecutionAsync = false;
            using var asyncReader = await connection.ExecuteReaderAsync("SELECT 1", cancellationToken: TestContext.Current.CancellationToken);
            Assert.Equal("value", asyncReader.GetColumns()[0].Name);
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
        public enum ReaderOperation {
            Reader,
            ReaderAsync,
            DbDataReader,
            DbDataReaderAsync
        }

        private sealed class TestDBConnection : DBConnection {

            // props
            public bool AvoidParametrizedQueries {
                get => mAvoidParametrizedQueries;
                set => mAvoidParametrizedQueries = value;
            }
            public bool AvoidInitializeDBTableFromDataReader {
                get => mAvoidInitializeDBTableFromDataReader;
                set => mAvoidInitializeDBTableFromDataReader = value;
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
            public bool CancelReaderExecutionAsync { get; set; } = true;
            public FakeDbCommand? LastCommand { get; private set; }
            public Exception? ReaderExecutionException { get; set; }
            public bool ThrowOnReaderDispose { get; set; }
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
            public bool IsDisposed { get; private set; }
            public FakeDbDataReader? Reader { get; private set; }
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
                if (((FakeDbConnection)mConnection!).ReaderExecutionException is Exception exception) throw exception;
                Reader = new FakeDbDataReader(((FakeDbConnection)mConnection!).ThrowOnReaderDispose);
                return Reader;
            }
            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) {
                CancellationToken = cancellationToken;
                var connection = (FakeDbConnection)mConnection!;
                if (connection.ReaderExecutionException is Exception exception) return Task.FromException<DbDataReader>(exception);
                if (connection.CancelReaderExecutionAsync) return Task.FromException<DbDataReader>(new OperationCanceledException(cancellationToken));
                Reader = new FakeDbDataReader(connection.ThrowOnReaderDispose);
                return Task.FromResult<DbDataReader>(Reader);
            }
            protected override void Dispose(bool disposing) {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        private sealed class FakeDbDataReader : DbDataReader {

            // vars
            private readonly bool mThrowOnDispose;

            // props
            public override object this[int ordinal] => 1;
            public override object this[string name] => 1;
            public override int Depth => 0;
            public override int FieldCount => 1;
            public override bool HasRows => true;
            public override bool IsClosed => IsDisposed;
            public bool IsDisposed { get; private set; }
            public override int RecordsAffected => 0;

            // ctor
            public FakeDbDataReader(bool throwOnDispose) {
                mThrowOnDispose = throwOnDispose;
            }

            // methods
            public override bool GetBoolean(int ordinal) {
                return true;
            }
            public override byte GetByte(int ordinal) {
                return 1;
            }
            public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) {
                return 0;
            }
            public override char GetChar(int ordinal) {
                return '1';
            }
            public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) {
                return 0;
            }
            public override string GetDataTypeName(int ordinal) {
                return "Int32";
            }
            public override DateTime GetDateTime(int ordinal) {
                return default;
            }
            public override decimal GetDecimal(int ordinal) {
                return 1;
            }
            public override double GetDouble(int ordinal) {
                return 1;
            }
            public override IEnumerator GetEnumerator() {
                return Array.Empty<object>().GetEnumerator();
            }
            public override Type GetFieldType(int ordinal) {
                return typeof(int);
            }
            public override float GetFloat(int ordinal) {
                return 1;
            }
            public override Guid GetGuid(int ordinal) {
                return Guid.Empty;
            }
            public override short GetInt16(int ordinal) {
                return 1;
            }
            public override int GetInt32(int ordinal) {
                return 1;
            }
            public override long GetInt64(int ordinal) {
                return 1;
            }
            public override string GetName(int ordinal) {
                return "value";
            }
            public override int GetOrdinal(string name) {
                return 0;
            }
            public override DataTable? GetSchemaTable() {
                throw new InvalidOperationException("Schema initialization should be avoided.");
            }
            public override string GetString(int ordinal) {
                return "1";
            }
            public override object GetValue(int ordinal) {
                return 1;
            }
            public override int GetValues(object[] values) {
                values[0] = 1;
                return 1;
            }
            public override bool IsDBNull(int ordinal) {
                return false;
            }
            public override bool NextResult() {
                return false;
            }
            public override bool Read() {
                return false;
            }

            // methods (private)
            protected override void Dispose(bool disposing) {
                IsDisposed = true;
                if (mThrowOnDispose) throw new InvalidOperationException("reader dispose failed");
                base.Dispose(disposing);
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
