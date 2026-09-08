using DProjects.Db.Schema;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging.Abstractions;

namespace DProjects.Db.Tests {

    public class DBConnectionHardeningTests {

        // tests
        [Fact]
        public void ParseStatement_ShouldRenderValuesAndValidatePlaceholderCount() {
            using var connection = new TestDBConnection();
            Assert.Equal("SELECT 1, 2", connection.ParseStatement("SELECT ?, ?", [1, 2]));
            Assert.Throws<ArgumentException>(() => connection.ParseStatement("SELECT ?, ?", [1]));
            Assert.Throws<ArgumentException>(() => connection.ParseStatement("SELECT ?", [1, 2]));
        }
        [Fact]
        public void ParseStatement_ShouldEncodeLiteralValues() {
            using var connection = new TestDBConnection();
            Assert.Equal("SELECT NULL", connection.ParseStatement("SELECT ?", [null]));
            Assert.Equal("SELECT NULL", connection.ParseStatement("SELECT ?", [DBNull.Value]));
            Assert.Equal("SELECT 'hello'''", connection.ParseStatement("SELECT ?", ["hello'"]));
        }
        [Fact]
        public void CommandTextConstruction_ShouldPreserveOriginalExceptionTypes() {
            using var literalConnection = new TestDBConnection();
            using var parameterConnection = new UnsupportedParameterDBConnection();

            Assert.Throws<NotSupportedException>(() => literalConnection.ParseStatement("SELECT ?", [new Version()]));
            Assert.Throws<NotSupportedException>(() => parameterConnection.ExecuteScalar<int>("SELECT ?", [1]));
            Assert.True(parameterConnection.FakeConnection.LastCommand!.IsDisposed);
            Assert.Throws<ArgumentException>(() => literalConnection.ParseStatement("SELECT ?, ?", [1]));
        }
        [Fact]
        public void Execution_ShouldUseNativeParametersInsteadOfRenderedLiterals() {
            using var connection = new TestDBConnection();

            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT ?", [123]));

            var command = connection.FakeConnection.LastCommand!;
            Assert.Equal("SELECT @__p0", command.CommandText);
            Assert.Single(command.Parameters.Cast<DbParameter>());
            Assert.Equal(123, command.Parameters[0].Value);
        }
        [Fact]
        public void Execution_ShouldBindSupportedValuesAndNormalizeNullAndProjectTimestamp() {
            using var connection = new TestDBConnection();
            var timestamp = new DProjects.DataTypes.Timestamp(123456789);
            var values = new object?[] { null, DBNull.Value, "hello'", 1, 2L, 3.5m, 4.5d, true, Guid.Empty, DateTime.UnixEpoch, DateTimeOffset.UnixEpoch, TimeSpan.FromSeconds(1), new byte[] { 1, 2 }, timestamp };

            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?", values));

            var command = connection.FakeConnection.LastCommand!;
            Assert.Equal(values.Length, command.Parameters.Count);
            Assert.Equal(DBNull.Value, command.Parameters[0].Value);
            Assert.Equal(DBNull.Value, command.Parameters[1].Value);
            Assert.Equal("hello'", command.Parameters[2].Value);
            Assert.Equal(timestamp.UnixMs, command.Parameters[13].Value);
            Assert.DoesNotContain("hello'", command.CommandText);
        }
        [Fact]
        public void BeginTrans_OnClosedConnectionAutoOpensAndNestedBeginFails() {
            using var connection = new TestDBConnection();

            connection.BeginTrans();

            Assert.Equal(ConnectionState.Open, connection.FakeConnection.State);
            Assert.Equal(1, connection.FakeConnection.OpenCallCount);
            Assert.Throws<InvalidOperationException>(() => connection.BeginTrans());
        }
        [Fact]
        public void CommandsUseActiveTransactionAndConnectionRemainsUsableAfterCommit() {
            using var connection = new TestDBConnection();
            connection.BeginTrans();
            var transaction = connection.FakeConnection.LastTransaction!;

            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT 1"));
            Assert.Same(transaction, connection.FakeConnection.LastCommand!.Transaction);

            connection.CommitTrans();

            Assert.True(transaction.WasCommitted);
            Assert.Equal(1, transaction.DisposeCallCount);
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT 1"));
            Assert.Null(connection.FakeConnection.LastCommand!.Transaction);
        }
        [Fact]
        public void RollBackTrans_RollsBackDisposesAndClearsTransaction() {
            using var connection = new TestDBConnection();
            connection.BeginTrans();
            var transaction = connection.FakeConnection.LastTransaction!;

            connection.RollBackTrans();

            Assert.True(transaction.WasRolledBack);
            Assert.Equal(1, transaction.DisposeCallCount);
            Assert.Throws<InvalidOperationException>(() => connection.RollBackTrans());
        }
        [Fact]
        public void TransactionCompletionWithoutActiveTransactionFailsExplicitly() {
            using var connection = new TestDBConnection();

            Assert.Throws<InvalidOperationException>(() => connection.CommitTrans());
            Assert.Throws<InvalidOperationException>(() => connection.RollBackTrans());
        }
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TransactionCompletionFailureStillDisposesAndClearsTransaction(bool commit) {
            using var connection = new TestDBConnection();
            connection.BeginTrans();
            var transaction = connection.FakeConnection.LastTransaction!;
            transaction.ThrowOnCommit = commit;
            transaction.ThrowOnRollback = !commit;

            Assert.Throws<InvalidOperationException>(() => {
                if (commit) connection.CommitTrans(); else connection.RollBackTrans();
            });

            Assert.Equal(1, transaction.DisposeCallCount);
            connection.BeginTrans();
        }
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TransactionOperationFailureRemainsPrimaryWhenDisposalAlsoFails(bool commit) {
            using var connection = new TestDBConnection();
            connection.BeginTrans();
            var transaction = connection.FakeConnection.LastTransaction!;
            var operationException = new InvalidOperationException(commit ? "commit failed" : "rollback failed");
            transaction.CommitException = commit ? operationException : null;
            transaction.RollbackException = commit ? null : operationException;
            transaction.DisposeException = new ApplicationException("dispose failed");

            var actualException = Assert.Throws<InvalidOperationException>(() => {
                if (commit) connection.CommitTrans(); else connection.RollBackTrans();
            });

            Assert.Same(operationException, actualException);
            Assert.Equal(1, transaction.DisposeCallCount);
            connection.BeginTrans();
        }
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TransactionDisposalFailureSurfacesWhenOperationSucceeds(bool commit) {
            using var connection = new TestDBConnection();
            connection.BeginTrans();
            var transaction = connection.FakeConnection.LastTransaction!;
            var disposalException = new ApplicationException("dispose failed");
            transaction.DisposeException = disposalException;

            var actualException = Assert.Throws<ApplicationException>(() => {
                if (commit) connection.CommitTrans(); else connection.RollBackTrans();
            });

            Assert.Same(disposalException, actualException);
            Assert.Equal(1, transaction.DisposeCallCount);
            connection.BeginTrans();
        }
        [Theory]
        [InlineData(ProviderFailureOperation.NonQuery)]
        [InlineData(ProviderFailureOperation.NonQueryAsync)]
        [InlineData(ProviderFailureOperation.Scalar)]
        [InlineData(ProviderFailureOperation.ScalarAsync)]
        [InlineData(ProviderFailureOperation.Reader)]
        [InlineData(ProviderFailureOperation.ReaderAsync)]
        [InlineData(ProviderFailureOperation.DbDataReader)]
        [InlineData(ProviderFailureOperation.DbDataReaderAsync)]
        public async Task Execution_PreservesProviderExceptionIdentityAndDisposesCommand(ProviderFailureOperation operation) {
            using var connection = new TestDBConnection();
            var expectedException = new InvalidOperationException("provider failure");
            var cancellationToken = TestContext.Current.CancellationToken;
            if (operation == ProviderFailureOperation.NonQuery || operation == ProviderFailureOperation.NonQueryAsync) connection.FakeConnection.NonQueryExecutionException = expectedException;
            if (operation == ProviderFailureOperation.Scalar || operation == ProviderFailureOperation.ScalarAsync) connection.FakeConnection.ScalarExecutionException = expectedException;
            if (operation == ProviderFailureOperation.Reader || operation == ProviderFailureOperation.ReaderAsync || operation == ProviderFailureOperation.DbDataReader || operation == ProviderFailureOperation.DbDataReaderAsync) connection.FakeConnection.ReaderExecutionException = expectedException;

            Exception actualException;
            if (operation == ProviderFailureOperation.NonQuery) actualException = Assert.Throws<InvalidOperationException>(() => connection.ExecuteNonQuery("SELECT 1"));
            else if (operation == ProviderFailureOperation.NonQueryAsync) actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ExecuteNonQueryAsync("SELECT 1", cancellationToken: cancellationToken));
            else if (operation == ProviderFailureOperation.Scalar) actualException = Assert.Throws<InvalidOperationException>(() => connection.ExecuteScalar<int>("SELECT 1"));
            else if (operation == ProviderFailureOperation.ScalarAsync) actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ExecuteScalarAsync<int>("SELECT 1", cancellationToken: cancellationToken));
            else if (operation == ProviderFailureOperation.Reader) actualException = Assert.Throws<InvalidOperationException>(() => connection.ExecuteReader("SELECT 1"));
            else if (operation == ProviderFailureOperation.ReaderAsync) actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ExecuteReaderAsync("SELECT 1", cancellationToken: cancellationToken));
            else if (operation == ProviderFailureOperation.DbDataReader) actualException = Assert.Throws<InvalidOperationException>(() => connection.ExecuteDbDataReader("SELECT 1"));
            else actualException = await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ExecuteDbDataReaderAsync("SELECT 1", cancellationToken: cancellationToken));

            Assert.Same(expectedException, actualException);
            Assert.True(connection.FakeConnection.LastCommand!.IsDisposed);
        }
        [Fact]
        public void ExecutionFailureRemainsPrimaryWhenCommandDisposalAlsoFails() {
            using var connection = new TestDBConnection();
            var expectedException = new InvalidOperationException("provider failure");
            connection.FakeConnection.NonQueryExecutionException = expectedException;
            connection.FakeConnection.CommandDisposeException = new ApplicationException("command dispose failed");

            var actualException = Assert.Throws<InvalidOperationException>(() => connection.ExecuteNonQuery("SELECT 1"));

            Assert.Same(expectedException, actualException);
            Assert.True(connection.FakeConnection.LastCommand!.IsDisposed);
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
                Assert.Throws<InvalidOperationException>(() => connection.ExecuteReader("SELECT 1"));
            } else if (operation == ReaderOperation.ReaderAsync) {
                await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ExecuteReaderAsync("SELECT 1", cancellationToken: TestContext.Current.CancellationToken));
            } else if (operation == ReaderOperation.DbDataReader) {
                Assert.Throws<InvalidOperationException>(() => connection.ExecuteDbDataReader("SELECT 1"));
            } else {
                await Assert.ThrowsAsync<InvalidOperationException>(() => connection.ExecuteDbDataReaderAsync("SELECT 1", cancellationToken: TestContext.Current.CancellationToken));
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
        public void Open_RepeatedOpenCloseAndReopenFollowLifecycleContract() {
            using var connection = new TestDBConnection();
            var physicalConnection = connection.FakeConnection;

            connection.Open();
            Assert.Equal(ConnectionState.Open, physicalConnection.State);
            Assert.Equal(1, physicalConnection.OpenCallCount);

            connection.Open();
            Assert.Equal(1, physicalConnection.OpenCallCount);

            connection.Close();
            Assert.Equal(ConnectionState.Closed, physicalConnection.State);
            Assert.Equal(1, physicalConnection.CloseCallCount);
            Assert.Equal(0, physicalConnection.DisposeCallCount);

            connection.Close();
            Assert.Equal(1, physicalConnection.CloseCallCount);

            connection.Open();
            Assert.Equal(ConnectionState.Open, physicalConnection.State);
            Assert.Equal(2, physicalConnection.OpenCallCount);
        }
        [Fact]
        public void Close_WithActiveTransactionDisposesClearsAndAllowsReuse() {
            using var connection = new TestDBConnection();
            connection.BeginTrans();
            var physicalConnection = connection.FakeConnection;
            var transaction = physicalConnection.LastTransaction!;

            connection.Close();

            Assert.Equal(1, transaction.DisposeCallCount);
            Assert.False(transaction.WasCommitted);
            Assert.Equal(ConnectionState.Closed, physicalConnection.State);
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT 1"));
            Assert.Equal(ConnectionState.Open, physicalConnection.State);
            Assert.Null(physicalConnection.LastCommand!.Transaction);
        }
        [Fact]
        public void Close_StillClosesAndClearsTransactionWhenTransactionDisposalFails() {
            using var connection = new TestDBConnection();
            connection.BeginTrans();
            var physicalConnection = connection.FakeConnection;
            var transaction = physicalConnection.LastTransaction!;
            var disposalException = new ApplicationException("transaction dispose failed");
            transaction.DisposeException = disposalException;

            var actualException = Assert.Throws<ApplicationException>(() => connection.Close());

            Assert.Same(disposalException, actualException);
            Assert.Equal(ConnectionState.Closed, physicalConnection.State);
            Assert.Equal(1, connection.ExecuteScalar<int>("SELECT 1"));
            Assert.Null(physicalConnection.LastCommand!.Transaction);
        }
        [Fact]
        public async Task PublicCreateCommand_SyncAndAsyncApplyTransactionTimeoutAndAutoOpen() {
            using var connection = new TestDBConnection() { CommandTimeout = 42 };
            connection.BeginTrans();
            var transaction = connection.FakeConnection.LastTransaction!;

            using (var command = connection.CreateCommand()) {
                Assert.Same(transaction, command.Transaction);
                Assert.Equal(42, command.CommandTimeout);
                Assert.Equal(string.Empty, command.CommandText);
            }
            connection.CommitTrans();
            connection.Close();
            using var cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSource.Token;

            using var asyncCommand = await connection.CreateCommandAsync(cancellationToken);
            Assert.Null(asyncCommand.Transaction);
            Assert.Equal(42, asyncCommand.CommandTimeout);
            Assert.Equal(ConnectionState.Open, connection.FakeConnection.State);
            Assert.Equal(cancellationToken, connection.FakeConnection.OpenCancellationToken);
        }
        [Fact]
        public void ExecuteAfterClose_AutomaticallyReopensConnection() {
            using var connection = new TestDBConnection();
            var physicalConnection = connection.FakeConnection;
            connection.Open();
            connection.Close();

            var result = connection.ExecuteScalar<int>("SELECT 1");

            Assert.Equal(1, result);
            Assert.Equal(ConnectionState.Open, physicalConnection.State);
            Assert.Equal(2, physicalConnection.OpenCallCount);
        }
        [Fact]
        public void Dispose_IsRepeatableAndDisposesPhysicalConnectionExactlyOnce() {
            var connection = new TestDBConnection();
            var physicalConnection = connection.FakeConnection;
            var disposedEventCount = 0;
            connection.Disposed += _ => disposedEventCount++;
            connection.Open();

            connection.Dispose();
            connection.Dispose();

            Assert.Equal(ConnectionState.Closed, physicalConnection.State);
            Assert.Equal(1, physicalConnection.CloseCallCount);
            Assert.Equal(1, physicalConnection.DisposeCallCount);
            Assert.Equal(1, disposedEventCount);
        }
        [Fact]
        public void CloseAndDispose_AreSafeInEitherOrder() {
            var closeThenDispose = new TestDBConnection();
            var firstPhysicalConnection = closeThenDispose.FakeConnection;
            closeThenDispose.Open();
            closeThenDispose.Close();
            closeThenDispose.Dispose();
            Assert.Equal(1, firstPhysicalConnection.DisposeCallCount);

            var disposeThenClose = new TestDBConnection();
            var secondPhysicalConnection = disposeThenClose.FakeConnection;
            disposeThenClose.Open();
            disposeThenClose.Dispose();
            disposeThenClose.Close();
            Assert.Equal(1, secondPhysicalConnection.DisposeCallCount);
        }
        [Fact]
        public void OperationsAfterDispose_ThrowObjectDisposedException() {
            var connection = new TestDBConnection();
            connection.Dispose();

            Assert.Throws<ObjectDisposedException>(() => connection.Open());
            Assert.Throws<ObjectDisposedException>(() => connection.CreateCommand());
            Assert.Throws<ObjectDisposedException>(() => connection.ExecuteNonQuery("SELECT 1"));
            Assert.Throws<ObjectDisposedException>(() => connection.ExecuteScalar<int>("SELECT 1"));
            Assert.Throws<ObjectDisposedException>(() => connection.ExecuteReader("SELECT 1"));
            Assert.Throws<ObjectDisposedException>(() => connection.BeginTrans());
            Assert.Throws<ObjectDisposedException>(() => connection.Connection);
            Assert.Throws<ObjectDisposedException>(() => connection.IsOpen);
        }
        [Fact]
        public void Dispose_WithActiveTransactionDisposesTransactionAndConnection() {
            var connection = new TestDBConnection();
            var physicalConnection = connection.FakeConnection;
            connection.Open();
            connection.BeginTrans();
            var transaction = physicalConnection.LastTransaction!;

            connection.Dispose();

            Assert.Equal(1, transaction.DisposeCallCount);
            Assert.False(transaction.WasCommitted);
            Assert.Equal(1, physicalConnection.DisposeCallCount);
        }
        [Fact]
        public void UnsupportedOperations_ShouldUseSpecificExceptions() {
            using var connection = new TestDBConnection();
            Assert.Throws<NotSupportedException>(() => connection.GetTableNames());
            Assert.Throws<NotSupportedException>(() => connection.GetSequenceNames());
            Assert.Throws<NotSupportedException>(() => connection.GetProcedureNames());
            Assert.Throws<NotSupportedException>(() => connection.BackupDb());
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("unsupported", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromNetDataTypeName(typeof(Version)));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlEncodedValue(new Version(), typeof(Version), false));
            Assert.Throws<NotSupportedException>(() => new DBTable().Select("id = 1"));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((DBSchemaDataType)int.MaxValue).GetNetDataType());
            Assert.Throws<ArgumentOutOfRangeException>(() => ((DBSchemaDataType)int.MaxValue).GetDbType());
        }
        [Fact]
        public void ProviderSpecificSqlPrimitives_AreUnsupportedByDialectNeutralBase() {
            using var connection = new TestDBConnection();

            Assert.Throws<NotSupportedException>(() => connection.GetSqlSelectTop(1));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlSelectTopAtEnd());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlSelectOffsetLimit(0, 1));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlSelectAutoincrement());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlIdentityDefinition(typeof(int)));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlDefaultNowExpression());
            Assert.Throws<NotSupportedException>(() => connection.GetSqlTypeDefinition(DBSchemaDataType.Varchar, 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlDropDefault("table", "column"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlDropIndex("table", "index"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlAlterColumn("table", new DBSchemaColumn("column")));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlCreateDefault("table", "column", "0"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlCreateSequence(new DBSchemaSequence()));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlAlterSequenceIncrement(new DBSchemaSequence()));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlDropSequence("sequence"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlGetNextSequenceValue("sequence"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlIfRowCountThrowError(0, 50000, "error"));
            Assert.Throws<NotSupportedException>(() => connection.GetSqlEncodedLikeValue("value"));
        }
        [Fact]
        public void BaseTypeNameMapping_RecognizesPortableNamesCaseInsensitively() {
            using var connection = new TestDBConnection();

            Assert.Equal(DBSchemaDataType.Timestamp, connection.GetDataTypeFromSqlDataTypeName("Timestamp", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Timestamp, connection.GetDataTypeFromSqlDataTypeName("timestamp", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Timestamp, connection.GetDataTypeFromSqlDataTypeName("TIMESTAMP", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromSqlDataTypeName("Varchar", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromSqlDataTypeName("varchar", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromSqlDataTypeName("VARCHAR", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.DateTime, connection.GetDataTypeFromSqlDataTypeName("DateTime", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.DateTime, connection.GetDataTypeFromSqlDataTypeName("datetime", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.DateTime, connection.GetDataTypeFromSqlDataTypeName("DATETIME", 0, 0, 0));
            Assert.Equal(DBSchemaDataType.UniqueIdentifier, connection.GetDataTypeFromSqlDataTypeName("uniqueidentifier", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("bit", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("text", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("ntext", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("varchar2", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("number", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("character varying", 0, 0, 0));
            Assert.Throws<NotSupportedException>(() => connection.GetDataTypeFromSqlDataTypeName("rowversion", 0, 0, 0));
        }
        [Fact]
        public void LikeExpressions_UseSqlWildcardSemantics() {
            using var connection = new TestDBConnection();

            Assert.Equal("%", connection.GetSqlLikeAllExpression());
            Assert.Equal("_", connection.GetSqlLikeOneExpression());
        }
        [Fact]
        public void ImplementedEmptyCapabilityCanBeDistinguishedFromUnsupportedCapability() {
            using var unsupported = new TestDBConnection();
            using var implemented = new EmptyCapabilityDBConnection();

            Assert.Throws<NotSupportedException>(() => unsupported.GetSequenceNames());
            Assert.Empty(implemented.GetSequenceNames());
        }
        [Fact]
        public void GetSchema_TablesOnly_DoesNotInspectUnrequestedCategories() {
            using var connection = new SchemaTrackingDBConnection() { TableNames = ["customers"] };

            var schema = connection.GetSchema(["*"], [], [], []);

            Assert.Equal(1, connection.GetTableNamesCallCount);
            Assert.Equal(0, connection.GetViewNamesCallCount);
            Assert.Equal(0, connection.GetSequenceNamesCallCount);
            Assert.Equal(0, connection.GetProcedureNamesCallCount);
            Assert.Equal("customers", Assert.Single(schema.Tables).Name);
            Assert.Empty(schema.Views);
            Assert.Empty(schema.Sequences);
            Assert.Empty(schema.Procedures);
        }
        [Fact]
        public void GetSchema_EmptyScope_DoesNotInspectAnyCategory() {
            using var connection = new SchemaTrackingDBConnection();

            var schema = connection.GetSchema([], [], [], []);

            Assert.Equal(0, connection.GetTableNamesCallCount);
            Assert.Equal(0, connection.GetViewNamesCallCount);
            Assert.Equal(0, connection.GetSequenceNamesCallCount);
            Assert.Equal(0, connection.GetProcedureNamesCallCount);
            Assert.Empty(schema.Tables);
            Assert.Empty(schema.Views);
            Assert.Empty(schema.Sequences);
            Assert.Empty(schema.Procedures);
        }
        [Fact]
        public void GetSchema_RequestedUnsupportedCategory_PropagatesNotSupportedException() {
            using var connection = new SchemaTrackingDBConnection() { ProcedureNamesSupported = false };

            Assert.Throws<NotSupportedException>(() => connection.GetSchema([], [], [], ["*"]));

            Assert.Equal(1, connection.GetProcedureNamesCallCount);
        }
        [Fact]
        public void GetSchema_MultiplePatterns_PreserveLikeFilteringSemantics() {
            using var connection = new SchemaTrackingDBConnection() { TableNames = ["customers", "audit_log", "orders", "event1"] };

            var schema = connection.GetSchema(["customer?", "*_log", "event#"], [], [], []);

            Assert.Equal(["customers", "audit_log", "event1"], schema.Tables.Select(table => table.Name));
            Assert.Equal(["customers", "audit_log", "event1"], connection.TableSchemaNames);
        }
        [Fact]
        public void GetSchema_NullFilters_ThrowArgumentNullExceptionBeforeInspectingCapabilities() {
            using var connection = new SchemaTrackingDBConnection();

            Assert.Equal("tableNames", Assert.Throws<ArgumentNullException>(() => connection.GetSchema(null!, [], [], [])).ParamName);
            Assert.Equal("viewNames", Assert.Throws<ArgumentNullException>(() => connection.GetSchema([], null!, [], [])).ParamName);
            Assert.Equal("sequenceNames", Assert.Throws<ArgumentNullException>(() => connection.GetSchema([], [], null!, [])).ParamName);
            Assert.Equal("procedureNames", Assert.Throws<ArgumentNullException>(() => connection.GetSchema([], [], [], null!)).ParamName);
            Assert.Equal(0, connection.GetTableNamesCallCount);
            Assert.Equal(0, connection.GetViewNamesCallCount);
            Assert.Equal(0, connection.GetSequenceNamesCallCount);
            Assert.Equal(0, connection.GetProcedureNamesCallCount);
        }
        [Fact]
        public void GetSchema_WithoutFilters_RemainsAFullSnapshot() {
            using var connection = new SchemaTrackingDBConnection() { ProcedureNamesSupported = false };

            Assert.Throws<NotSupportedException>(() => connection.GetSchema());

            Assert.Equal(1, connection.GetTableNamesCallCount);
            Assert.Equal(1, connection.GetViewNamesCallCount);
            Assert.Equal(1, connection.GetSequenceNamesCallCount);
            Assert.Equal(1, connection.GetProcedureNamesCallCount);
        }
        [Fact]
        public void ApplySchemaChanges_WithoutOptionalTargets_OnlyInspectsTables() {
            using var connection = new SchemaTrackingDBConnection() { ProcedureNamesSupported = false };

            connection.ApplySchemaChanges(new DBSchemaDatabase(), false, NullLogger<IDBConnection>.Instance);

            Assert.Equal(1, connection.GetTableNamesCallCount);
            Assert.Equal(0, connection.GetViewNamesCallCount);
            Assert.Equal(0, connection.GetSequenceNamesCallCount);
            Assert.Equal(0, connection.GetProcedureNamesCallCount);
        }
        [Fact]
        public void ApplySchemaChanges_WithUnsupportedProcedureTarget_PropagatesNotSupportedException() {
            using var connection = new SchemaTrackingDBConnection() { ProcedureNamesSupported = false };
            var schema = new DBSchemaDatabase();
            schema.Procedures.Add(new DBSchemaProcedure() { Name = "refresh_data", Content = "SELECT 1" });

            Assert.Throws<NotSupportedException>(() => connection.ApplySchemaChanges(schema, false, NullLogger<IDBConnection>.Instance));

            Assert.Equal(1, connection.GetProcedureNamesCallCount);
        }
        [Fact]
        public void ApplySchemaChanges_ExistingUnmanagedProcedure_IsNotInspectedOrDropped() {
            using var connection = new SchemaTrackingDBConnection() { ProcedureNames = ["existing_procedure"], ProcedureNamesSupported = true };

            connection.ApplySchemaChanges(new DBSchemaDatabase(), false, NullLogger<IDBConnection>.Instance);

            Assert.Equal(0, connection.GetProcedureNamesCallCount);
            Assert.Equal(0, connection.GetSqlDropProcedureCallCount);
        }
        [Fact]
        public void TypeMappings_AreSemanticallyConsistent() {
            using var connection = new TestDBConnection();

            Assert.Equal(typeof(DateTime), DBSchemaDataType.Timestamp.GetNetDataType());
            Assert.Equal(DbType.DateTime, DBSchemaDataType.Timestamp.GetDbType());
            Assert.Equal(DBSchemaDataType.Varchar, connection.GetDataTypeFromNetDataTypeName(typeof(string)));
            Assert.Equal(DBSchemaDataType.Boolean, connection.GetDataTypeFromNetDataTypeName(typeof(bool)));
            Assert.Equal(DBSchemaDataType.Int, connection.GetDataTypeFromNetDataTypeName(typeof(int)));
            Assert.Equal(DBSchemaDataType.Bigint, connection.GetDataTypeFromNetDataTypeName(typeof(long)));
            Assert.Equal(DBSchemaDataType.Decimal, connection.GetDataTypeFromNetDataTypeName(typeof(decimal)));
            Assert.Equal(DBSchemaDataType.UniqueIdentifier, connection.GetDataTypeFromNetDataTypeName(typeof(Guid)));
            Assert.Equal(DBSchemaDataType.DateTime, connection.GetDataTypeFromNetDataTypeName(typeof(DateTime)));
            Assert.Equal(DBSchemaDataType.DateTime, connection.GetDataTypeFromNetDataTypeName(typeof(DateTimeOffset)));
            Assert.Equal(DBSchemaDataType.Varbinary, connection.GetDataTypeFromNetDataTypeName(typeof(byte[])));
            Assert.Equal(DBSchemaDataType.Bigint, connection.GetDataTypeFromNetDataTypeName(typeof(DProjects.DataTypes.Timestamp)));
            Assert.Equal(DBSchemaDataType.Time, connection.GetDataTypeFromNetDataTypeName(typeof(TimeSpan)));
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
        public enum ProviderFailureOperation {
            NonQuery,
            NonQueryAsync,
            Scalar,
            ScalarAsync,
            Reader,
            ReaderAsync,
            DbDataReader,
            DbDataReaderAsync
        }

        private class TestDBConnection : DBConnection {

            // props
            public bool AvoidInitializeDBTableFromDataReader {
                get => mAvoidInitializeDBTableFromDataReader;
                set => mAvoidInitializeDBTableFromDataReader = value;
            }
            public FakeDbConnection FakeConnection => (FakeDbConnection)Connection;

            // ctor
            public TestDBConnection() : base("test", "test", new FakeDbConnection()) {
            }
        }

        private sealed class EmptyCapabilityDBConnection : TestDBConnection {

            // methods
            public override string[] GetSequenceNames() {
                return [];
            }
        }

        private sealed class SchemaTrackingDBConnection : TestDBConnection {

            // props
            public int GetProcedureNamesCallCount { get; private set; }
            public int GetSequenceNamesCallCount { get; private set; }
            public int GetSqlDropProcedureCallCount { get; private set; }
            public int GetTableNamesCallCount { get; private set; }
            public int GetViewNamesCallCount { get; private set; }
            public string[] ProcedureNames { get; set; } = [];
            public bool ProcedureNamesSupported { get; set; } = true;
            public string[] SequenceNames { get; set; } = [];
            public string[] TableNames { get; set; } = [];
            public List<string> TableSchemaNames { get; } = [];
            public string[] ViewNames { get; set; } = [];

            // methods
            public override string[] GetTableNames() {
                GetTableNamesCallCount++;
                return TableNames;
            }
            public override DBSchemaTable GetTableSchema(string table) {
                TableSchemaNames.Add(table);
                return new DBSchemaTable() { Name = table };
            }
            public override string[] GetViewNames() {
                GetViewNamesCallCount++;
                return ViewNames;
            }
            public override DBSchemaView GetViewSchema(string view) {
                return new DBSchemaView() { Name = view };
            }
            public override string[] GetSequenceNames() {
                GetSequenceNamesCallCount++;
                return SequenceNames;
            }
            public override DBSchemaSequence GetSequenceSchema(string sequence) {
                return new DBSchemaSequence() { Name = sequence };
            }
            public override string[] GetProcedureNames() {
                GetProcedureNamesCallCount++;
                if (!ProcedureNamesSupported) throw new NotSupportedException("Procedure enumeration is not supported by this test connection.");
                return ProcedureNames;
            }
            public override DBSchemaProcedure GetProcedureSchema(string procedure) {
                return new DBSchemaProcedure() { Name = procedure };
            }
            public override string GetSqlDropProcedure(string procedure) {
                GetSqlDropProcedureCallCount++;
                return "DROP PROCEDURE " + procedure;
            }
        }

        private sealed class UnsupportedParameterDBConnection : TestDBConnection {

            // methods (private)
            protected override string GetSqlParameterPlaceholder(int index) {
                throw new NotSupportedException("Parameter placeholders are not supported by this test connection.");
            }
        }

        private sealed class FakeDbConnection : DbConnection {

            // vars
            private ConnectionState mState;

            // props
            public bool CancelReaderExecutionAsync { get; set; } = true;
            public int CloseCallCount { get; private set; }
            public Exception? CommandDisposeException { get; set; }
            public int DisposeCallCount { get; private set; }
            public FakeDbCommand? LastCommand { get; private set; }
            public FakeDbTransaction? LastTransaction { get; private set; }
            public Exception? NonQueryExecutionException { get; set; }
            public CancellationToken OpenCancellationToken { get; private set; }
            public int OpenCallCount { get; private set; }
            public Exception? ReaderExecutionException { get; set; }
            public Exception? ScalarExecutionException { get; set; }
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
                if (mState == ConnectionState.Closed) throw new InvalidOperationException("connection is already closed");
                CloseCallCount++;
                mState = ConnectionState.Closed;
            }
            public override void Open() {
                if (mState == ConnectionState.Open) throw new InvalidOperationException("connection is already open");
                OpenCallCount++;
                mState = ConnectionState.Open;
            }
            public override Task OpenAsync(CancellationToken cancellationToken) {
                OpenCancellationToken = cancellationToken;
                Open();
                return Task.CompletedTask;
            }

            // methods (private)
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) {
                LastTransaction = new FakeDbTransaction(this, isolationLevel);
                return LastTransaction;
            }
            protected override DbCommand CreateDbCommand() {
                LastCommand = new FakeDbCommand(this);
                return LastCommand;
            }
            protected override void Dispose(bool disposing) {
                if (disposing) {
                    DisposeCallCount++;
                    mState = ConnectionState.Closed;
                }
            }
        }

        private sealed class FakeDbTransaction : DbTransaction {

            // vars
            private readonly DbConnection mConnection;
            private readonly IsolationLevel mIsolationLevel;

            // props
            public int DisposeCallCount { get; private set; }
            public override IsolationLevel IsolationLevel => mIsolationLevel;
            public bool WasCommitted { get; private set; }
            public bool WasRolledBack { get; private set; }
            public Exception? CommitException { get; set; }
            public Exception? DisposeException { get; set; }
            public Exception? RollbackException { get; set; }
            public bool ThrowOnCommit { get; set; }
            public bool ThrowOnRollback { get; set; }
            protected override DbConnection DbConnection => mConnection;

            // ctor
            public FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel) {
                mConnection = connection;
                mIsolationLevel = isolationLevel;
            }

            // methods
            public override void Commit() {
                if (CommitException != null) throw CommitException;
                if (ThrowOnCommit) throw new InvalidOperationException("commit failed");
                WasCommitted = true;
            }
            public override void Rollback() {
                if (RollbackException != null) throw RollbackException;
                if (ThrowOnRollback) throw new InvalidOperationException("rollback failed");
                WasRolledBack = true;
            }

            // methods (private)
            protected override void Dispose(bool disposing) {
                if (disposing) DisposeCallCount++;
                if (disposing && DisposeException != null) throw DisposeException;
                base.Dispose(disposing);
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
                if (((FakeDbConnection)mConnection!).NonQueryExecutionException is Exception exception) throw exception;
                return 1;
            }
            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) {
                CancellationToken = cancellationToken;
                if (((FakeDbConnection)mConnection!).NonQueryExecutionException is Exception exception) return Task.FromException<int>(exception);
                return Task.FromException<int>(new OperationCanceledException(cancellationToken));
            }
            public override object? ExecuteScalar() {
                if (((FakeDbConnection)mConnection!).ScalarExecutionException is Exception exception) throw exception;
                return 1;
            }
            public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) {
                CancellationToken = cancellationToken;
                if (((FakeDbConnection)mConnection!).ScalarExecutionException is Exception exception) return Task.FromException<object?>(exception);
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
                if (disposing && ((FakeDbConnection)mConnection!).CommandDisposeException is Exception exception) throw exception;
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
