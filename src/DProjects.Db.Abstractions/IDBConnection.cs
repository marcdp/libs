using DProjects.Db.Schema;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DProjects.Db {

    /// <summary>Represents a reusable provider-specific database connection wrapper.</summary>
    /// <remarks>
    /// Calling <see cref="IDisposable.Dispose"/> permanently ends the wrapper lifetime and releases owned resources. Disposal is idempotent; operations requiring
    /// a live wrapper subsequently throw <see cref="ObjectDisposedException"/>. SQL-generation methods are provider-specific, and unsupported operations may
    /// throw <see cref="NotSupportedException"/>.
    /// </remarks>
    public interface IDBConnection : IDisposable {

        // props
        /// <summary>Gets the logical connection name.</summary>
        string Name { get; }
        /// <summary>Gets the connection string used to create the underlying connection.</summary>
        string ConnectionString { get; }
        /// <summary>Gets or sets the timeout, in seconds, applied to commands created by this wrapper.</summary>
        int CommandTimeout { get; set; }
        /// <summary>Gets advanced, non-owning access to the underlying connection.</summary>
        /// <remarks>The caller must not dispose or replace the returned connection.</remarks>
        DbConnection Connection { get; }
        /// <summary>Gets whether the underlying connection is currently open.</summary>
        bool IsOpen { get; }

        // methods
        /// <summary>Opens the underlying connection if necessary. Calling this when already open is safe.</summary>
        void Open();
        /// <summary>Asynchronously opens the underlying connection if necessary. Calling this when already open is safe.</summary>
        Task OpenAsync(CancellationToken cancellationToken = default);
        /// <summary>Closes the physical connection while leaving the wrapper reusable.</summary>
        /// <remarks>Any active transaction is disposed without commit. Later explicit or execution-triggered opening remains valid.</remarks>
        void Close();

        /// <summary>Executes a query and materializes its result, automatically opening the connection when necessary.</summary>
        /// <remarks><c>?</c> is the portable input placeholder. Its count must exactly match <paramref name="parameters"/>; execution binds native parameters.</remarks>
        DBTable ExecuteTable(string sql, object?[]? parameters = null);
        /// <summary>Asynchronously executes a query and materializes its result, automatically opening the connection when necessary.</summary>
        /// <remarks><c>?</c> is the portable input placeholder. Its count must exactly match <paramref name="parameters"/>; execution binds native parameters.</remarks>
        Task<DBTable> ExecuteTableAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        /// <summary>Executes a query and returns a reader, automatically opening the connection when necessary.</summary>
        /// <remarks>
        /// The reader owns the internally-created command; disposing it releases both. Portable <c>?</c> placeholders are bound as native parameters and must
        /// match the parameter count.
        /// </remarks>
        IDBReader ExecuteReader(string sql, object?[]? parameters = null);
        /// <summary>Executes a query and returns a data-reader wrapper, automatically opening the connection when necessary.</summary>
        /// <remarks>
        /// Disposing the returned wrapper disposes its underlying reader and internally-created command. Portable <c>?</c> placeholders are bound as native
        /// parameters and must match the parameter count.
        /// </remarks>
        DbDataReader ExecuteDbDataReader(string sql, object?[]? parameters = null);
        /// <summary>Asynchronously executes a query and returns a data-reader wrapper, automatically opening the connection when necessary.</summary>
        /// <remarks>
        /// Disposing the returned wrapper disposes its underlying reader and internally-created command. Portable <c>?</c> placeholders are bound as native
        /// parameters and must match the parameter count.
        /// </remarks>
        Task<DbDataReader> ExecuteDbDataReaderAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        /// <summary>Asynchronously executes a query and returns a reader, automatically opening the connection when necessary.</summary>
        /// <remarks>
        /// The reader owns the internally-created command; disposing it releases both. Portable <c>?</c> placeholders are bound as native parameters and must
        /// match the parameter count.
        /// </remarks>
        Task<IDBReader> ExecuteReaderAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        /// <summary>Executes a statement and returns its affected-row count, automatically opening the connection when necessary.</summary>
        /// <remarks><c>?</c> is the portable input placeholder. Its count must exactly match <paramref name="parameters"/>; execution binds native parameters.</remarks>
        long ExecuteNonQuery(string sql, object?[]? parameters = null);
        /// <summary>Asynchronously executes a statement and returns its affected-row count, automatically opening the connection when necessary.</summary>
        /// <remarks><c>?</c> is the portable input placeholder. Its count must exactly match <paramref name="parameters"/>; execution binds native parameters.</remarks>
        Task<long> ExecuteNonQueryAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        /// <summary>Executes a query and converts its first scalar value, automatically opening the connection when necessary.</summary>
        /// <remarks><c>?</c> is the portable input placeholder. Its count must exactly match <paramref name="parameters"/>; execution binds native parameters.</remarks>
        T ExecuteScalar<T>(string sql, object?[]? parameters = null);
        /// <summary>Asynchronously executes a query and converts its first scalar value, automatically opening the connection when necessary.</summary>
        /// <remarks><c>?</c> is the portable input placeholder. Its count must exactly match <paramref name="parameters"/>; execution binds native parameters.</remarks>
        Task<T> ExecuteScalarAsync<T>(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        /// <summary>Returns the provider's current generated identity value, automatically opening the connection when necessary.</summary>
        long ExecuteIdentity();
        /// <summary>Asynchronously returns the provider's current generated identity value, automatically opening the connection when necessary.</summary>
        Task<long> ExecuteIdentityAsync(CancellationToken cancellationToken = default);
        /// <summary>Creates and configures a command, automatically opening the connection when necessary.</summary>
        /// <remarks>The caller owns and must dispose the command. The active transaction and <see cref="CommandTimeout"/> are applied.</remarks>
        DbCommand CreateCommand();
        /// <summary>Asynchronously creates and configures a command, automatically opening the connection when necessary.</summary>
        /// <remarks>The caller owns and must dispose the command. The active transaction and <see cref="CommandTimeout"/> are applied.</remarks>
        Task<DbCommand> CreateCommandAsync(CancellationToken cancellationToken = default);
        /// <summary>Renders encoded literal SQL for diagnostics or logging.</summary>
        /// <remarks>This is not the executable parameterization path and must not replace native parameter binding. Placeholder and parameter counts must match.</remarks>
        string ParseStatement(string sql, object?[]? parameters = null);

        /// <summary>Begins the single supported active transaction, automatically opening the connection when necessary.</summary>
        /// <exception cref="InvalidOperationException">A transaction is already active.</exception>
        /// <exception cref="ObjectDisposedException">The wrapper has been disposed.</exception>
        void BeginTrans();
        /// <summary>Commits, clears, and disposes the active transaction.</summary>
        /// <exception cref="InvalidOperationException">No transaction is active.</exception>
        /// <exception cref="ObjectDisposedException">The wrapper has been disposed.</exception>
        void CommitTrans();
        /// <summary>Rolls back, clears, and disposes the active transaction.</summary>
        /// <exception cref="InvalidOperationException">No transaction is active.</exception>
        /// <exception cref="ObjectDisposedException">The wrapper has been disposed.</exception>
        void RollBackTrans();

        /// <summary>Gets a full snapshot by requesting every defined schema category.</summary>
        /// <exception cref="NotSupportedException">The provider cannot supply a requested category or full snapshot.</exception>
        DBSchemaDatabase GetSchema();
        /// <summary>Gets a schema snapshot filtered independently by category.</summary>
        /// <remarks>
        /// An empty array omits a category, <c>["*"]</c> requests every object, and other entries are patterns. A requested unsupported category throws
        /// <see cref="NotSupportedException"/>.
        /// </remarks>
        DBSchemaDatabase GetSchema(string[] tableNames, string[] viewNames, string[] sequenceNames, string[] procedureNames);
        /// <summary>Evaluates or applies SQL that reconciles the current schema with <paramref name="dbSchema"/>.</summary>
        /// <remarks>
        /// Tables are always managed, so an empty target table collection can remove existing tables. Views, sequences, and procedures are managed only when
        /// their target collection is non-empty; empty means unmanaged, not delete all. When <paramref name="applyChanges"/> is false, planned SQL is logged but
        /// not executed; when true, changes are executed. No atomicity guarantee is implied.
        /// </remarks>
        void ApplySchemaChanges(DBSchemaDatabase dbSchema, bool applyChanges, ILogger<IDBConnection> logger);

        // ddl table
        string[] GetTableNames();
        bool ExistsTable(string table);
        DBSchemaTable GetTableSchema(string table);
        string GetSqlCreateTable(DBSchemaTable dbSchemaTable, bool avoidCreatePrimaryKey = false, bool avoidCreateForeignKeys = false);
        string GetSqlCreatePrimaryKey(string table, DBSchemaPrimaryKey dbSchemaPrimaryKey);
        string GetSqlCreateForeignKey(string table, DBSchemaForeignKey dbSchemaForeignKey);
        string GetSqlCreateIndex(string table, DBSchemaIndex dbSchemaIndex);
        string GetSqlCreateColumn(string table, DBSchemaColumn dBSchemaColumn);
        string GetSqlAlterColumn(string table, DBSchemaColumn dBSchemaColumn);
        string GetSqlDropTable(string table);
        string GetSqlDropPrimaryKey(string table, string name);
        string GetSqlDropForeignKey(string table, string name);
        string GetSqlDropIndex(string table, string index);
        string GetSqlDropColumn(string table, string column);
        string GetSqlDropDefault(string table, string column);
        string GetSqlCreateDefault(string table, string column, string aDefault);
        DBSchemaDataType GetDataTypeFromNetDataTypeName(Type type, int length = 0, int precision = 0, int scale = 0);
        string GetSqlCreateTempTable(string table, string select);
        string GetSqlDropTempTable(string table);

        // ddl sequence
        /// <summary>Gets sequence names.</summary>
        /// <exception cref="NotSupportedException">The provider does not support sequence discovery.</exception>
        string[] GetSequenceNames();
        DBSchemaSequence GetSequenceSchema(string sequence);
        bool ExistsSequence(string sequence);
        string GetSqlCreateSequence(DBSchemaSequence dbSchemaSequence);
        string GetSqlAlterSequenceIncrement(DBSchemaSequence dbSchemaSequence);
        string GetSqlDropSequence(string sequence);

        // ddl view
        string[] GetViewNames();
        bool ExistsView(string view);
        string GetSqlDropView(string view);
        DBSchemaView GetViewSchema(string view);
        string GetView(string view);
        string GetSqlCreateView(DBSchemaView dbSchemaView);

        // ddl procedure
        /// <summary>Gets stored-procedure names.</summary>
        /// <exception cref="NotSupportedException">The provider does not support procedure discovery.</exception>
        string[] GetProcedureNames();
        string GetProcedure(string name);
        DBSchemaProcedure GetProcedureSchema(string procedure);
        string GetSqlCreateProcedure(DBSchemaProcedure dbSchemaProcedure);
        string GetSqlDropProcedure(string procedure);

        // ddl database
        /// <summary>Creates a provider-specific database backup.</summary>
        /// <exception cref="NotSupportedException">The provider does not support this operation.</exception>
        string BackupDb();
        /// <summary>Restores a provider-specific database backup.</summary>
        /// <exception cref="NotSupportedException">The provider does not support this operation.</exception>
        void RestoreDb(string filename, string name);
        /// <summary>Compacts the database using provider-specific behavior.</summary>
        /// <exception cref="NotSupportedException">The provider does not support this operation.</exception>
        void CompactDb();
        /// <summary>Determines whether a database exists using provider-specific behavior.</summary>
        /// <exception cref="NotSupportedException">The provider does not support this operation.</exception>
        bool ExistsDb(string name);
        /// <summary>Creates a database using provider-specific behavior.</summary>
        /// <exception cref="NotSupportedException">The provider does not support this operation.</exception>
        void CreateDb(string name);

        // sql generation
        string GetSqlTransactionWrap(string sql);
        string GetSqlTransactionStart();
        string GetSqlTransactionCommit();
        string GetSqlTransactionRollBack();
        string GetSqlSelectTop(int number);
        bool GetSqlSelectTopAtEnd();
        string GetSqlSelectOffsetLimit(long offset, int length);
        string GetSqlLikeAllExpression();
        string GetSqlLikeOneExpression();
        string GetSqlSelectTest();
        string GetSqlSelectAutoincrement();
        string GetSqlSeparator();
        string GetSqlAlias();
        string GetSqlDefaultNowExpression();
        string GetSqlTrueExpression();
        string GetSqlFalseExpression();
        string GetSqlIdentityDefinition(Type type);
        string GetSqlQualifierBegin();
        string GetSqlQualifierEnd();
        string GetSqlEncodedLikeValue(string target);
        bool GetSqlAvoidCloseCommandForDBTable();
        bool GetSqlRequireSpaceWhenNull();
        /// <summary>Translates a portable schema type to the current provider's native SQL type definition.</summary>
        /// <remarks>Translation need not preserve one-to-one enum identity because a provider may canonicalize several portable types to the same storage semantics.</remarks>
        /// <exception cref="NotSupportedException">The provider does not support the requested type.</exception>
        string GetSqlTypeDefinition(DBSchemaDataType dataType, int size, int precision, int scale);
        string GetSqlEncodedValue(object? value, Type? type, bool bAllowNull);
        string GetSqlParameterPrefix();
        string GetSqlGetNextSequenceValue(string sequenceName);
        string GetSqlTempTablePrefix();
        string GetSqlIfRowCountThrowError(int rowCount, int errorCode, string errorMessage);
    }
}
