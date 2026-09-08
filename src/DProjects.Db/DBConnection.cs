using DProjects.Db.Readers;
using DProjects.Db.Schema;
using DProjects.DataTypes;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DProjects.Log;
using Microsoft.Extensions.Logging;

namespace DProjects.Db {


    public abstract class DBConnection : IDBConnection {


        //events
        public delegate void DisposedEventHandler(DBConnection sender);
        public event DisposedEventHandler? Disposed;


        //variables
        protected readonly string mName;
        protected readonly string mConnectionString;
        protected readonly Type? mConnectionType;
        protected System.Data.Common.DbConnection mConnection;
        protected int mCommandTimeout;
        protected System.Data.Common.DbTransaction? mTransaction;
        protected bool mIsDisposed;
        protected bool mAvoidInitializeDBTableFromDataReader;


        //constructor
        public DBConnection(string name, string connectionString, System.Data.Common.DbConnection connection) {
            mName = name;
            mConnectionString = connectionString;
            mCommandTimeout = 0;
            mIsDisposed = false;
            mConnection = connection;
        }
        /// <summary>Permanently ends this wrapper's lifetime and releases its owned transaction and connection resources.</summary>
        /// <remarks>This method is idempotent. Operations requiring a live wrapper subsequently throw <see cref="ObjectDisposedException"/>.</remarks>
        public virtual void Dispose() {
            if (mIsDisposed) return;
            mIsDisposed = true;
            Exception? disposalException = null;
            // dispose the active transaction without committing it
            if (mTransaction != null) {
                var transaction = mTransaction;
                try {
                    transaction.Dispose();
                } catch (Exception exception) {
                    disposalException = disposalException ?? exception;
                } finally {
                    mTransaction = null;
                }
            }
            // close and dispose the physical connection after dependent resources
            try {
                if (mConnection.State != System.Data.ConnectionState.Closed) mConnection.Close();
            } catch (Exception exception) {
                disposalException = disposalException ?? exception;
            }
            try {
                mConnection.Dispose();
            } catch (Exception exception) {
                disposalException = disposalException ?? exception;
            }
            try {
                Disposed?.Invoke(this);
            } catch (Exception exception) {
                disposalException = disposalException ?? exception;
            }
            if (disposalException != null) ExceptionDispatchInfo.Capture(disposalException).Throw();
        }


        //properties
        public string Name => mName; 
        public string ConnectionString => mConnectionString;
        public int CommandTimeout { get { return mCommandTimeout; } set { mCommandTimeout = value; } }
        public System.Data.Common.DbConnection Connection {
            get {
                ThrowIfDisposed();
                return mConnection;
            }
        }
        public bool IsOpen {
            get {
                ThrowIfDisposed();
                return mConnection.State == System.Data.ConnectionState.Open;
            }
        }


        //DML methods
        #region "DML methods"
        public void Open() {
            ThrowIfDisposed();
            if (mConnection.State == System.Data.ConnectionState.Open) return;
            mConnection.Open();
        }
        public async Task OpenAsync(CancellationToken cancellationToken = default) {
            ThrowIfDisposed();
            if (mConnection.State == System.Data.ConnectionState.Open) return;
            await mConnection.OpenAsync(cancellationToken!);

        }
        public void Close() {
            if (mIsDisposed) return;
            Exception? cleanupException = null;
            var transaction = mTransaction;
            mTransaction = null;
            if (transaction != null) {
                try {
                    transaction.Dispose();
                } catch (Exception exception) {
                    cleanupException = exception;
                }
            }
            try {
                if (mConnection.State != System.Data.ConnectionState.Closed) mConnection.Close();
            } catch (Exception exception) {
                cleanupException = cleanupException ?? exception;
            }
            if (cleanupException != null) ExceptionDispatchInfo.Capture(cleanupException).Throw();
        }
        public string ParseStatement(string sql, object?[]? parameters = null) {
            ThrowIfDisposed();
            return CreateCommandText(sql, parameters);
        }
        public System.Data.Common.DbCommand CreateCommand() {
            if (!IsOpen) Open();
            var command = Connection.CreateCommand();
            try {
                ConfigureCommand(command);
                return command;
            } catch (Exception exception) {
                DisposePreservingPrimaryException(command, exception);
                throw;
            }
        }
        public async Task<DbCommand> CreateCommandAsync(CancellationToken cancellationToken = default) {
            if (!IsOpen) await OpenAsync(cancellationToken);
            var command = Connection.CreateCommand();
            try {
                ConfigureCommand(command);
                return command;
            } catch (Exception exception) {
                DisposePreservingPrimaryException(command, exception);
                throw;
            }
        }
        protected virtual System.Data.Common.DbCommand CreateCommand(string sql, object?[]? parameters = null) {
            if (!IsOpen) Open();
            var command = Connection.CreateCommand();
            try {
                command.CommandText = CreateCommandTextWithParameters(command, sql, parameters);
                ConfigureCommand(command);
                command.CommandType = System.Data.CommandType.Text;
                return command;
            } catch (Exception exception) {
                DisposePreservingPrimaryException(command, exception);
                throw;
            }
        }
        protected virtual async Task<System.Data.Common.DbCommand> CreateCommandAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            if (!IsOpen) await OpenAsync(cancellationToken);
            var command = Connection.CreateCommand();
            try {
                command.CommandText = CreateCommandTextWithParameters(command, sql, parameters);
                ConfigureCommand(command);
                command.CommandType = System.Data.CommandType.Text;
                return command;
            } catch (Exception exception) {
                DisposePreservingPrimaryException(command, exception);
                throw;
            }
        }
        protected string CreateCommandText(string sql, object?[]? parameters = null) {
            ValidateParameterCount(sql, parameters);
            if (parameters == null) return sql;
            int j = 0;
            int k = 0;
            var sb = new StringBuilder();
            // replaces the question marks with encoded parameter values
            if (parameters.Length > 0) {
                j = 0;
                k = 0;
                foreach (object? parameter in parameters) {
                    bool allowNull = false;
                    string value = "";
                    Type? type = null;
                    k = sql.IndexOf('?', j);
                    sb.Append(sql.Substring(j, k - j));
                    if (parameter == null) {
                        type = null;
                    } else {
                        type = parameter.GetType();
                    }
                    allowNull = (parameter == null) || (parameter == System.DBNull.Value) || (parameter is DateTime && System.Convert.ToDateTime(parameter).Equals(System.Convert.ToDateTime(null)));
                    value = GetSqlEncodedValue(parameter, type, allowNull);
                    sb.Append(value);
                    j = k + 1;
                }
                sb.Append(sql.Substring(k + 1));
                sql = sb.ToString();
            }
            return sql;
        }
        protected string CreateCommandTextWithParameters(System.Data.Common.DbCommand command, string sql, object?[]? parameters = null) {
            ValidateParameterCount(sql, parameters);
            int j = 0;
            int k = 0;
            command.CommandText = sql;
            // replaces the question marks with native parameter placeholders
            if (parameters != null && parameters.Length > 0) {
                var sb = new StringBuilder();
                j = 0;
                k = 0;
                foreach (object? parameter in parameters) {
                    k = sql.IndexOf('?', j);
                    sb.Append(sql.Substring(j, k - j));
                    sb.Append(GetSqlParameterPlaceholder(command.Parameters.Count));
                    j = k + 1;
                    var dbParameter = command.CreateParameter();
                    dbParameter.ParameterName = GetSqlParameterName(command.Parameters.Count);
                    if (parameter == null) {
                        dbParameter.Value = DBNull.Value;
                    } else {
                        dbParameter.Value = GetDbParameterValue(parameter);
                        var dbType = GetDbType(parameter);
                        if (dbType != System.Data.DbType.Object) dbParameter.DbType = dbType;
                    }
                    command.Parameters.Add(dbParameter);
                }
                sb.Append(sql.Substring(k + 1));
                command.CommandText = sb.ToString();
            }
            return command.CommandText;
        }
        private static void ValidateParameterCount(string sql, object?[]? parameters) {
            if (sql == null) throw new ArgumentNullException(nameof(sql));
            var placeholderCount = 0;
            foreach (var character in sql) {
                if (character == '?') placeholderCount++;
            }
            var parameterCount = parameters?.Length ?? 0;
            if (placeholderCount != parameterCount) throw new ArgumentException($"SQL contains {placeholderCount} placeholders but {parameterCount} parameters were provided.", nameof(parameters));
        }
        private System.Data.DbType GetDbType(object? value) {
            //if (value is string) return System.Data.DbType.AnsiString;
            if (value is string) return System.Data.DbType.String;
            if (value is byte) return System.Data.DbType.Byte;
            if (value is bool) return System.Data.DbType.Boolean;
            if (value is Currency) return System.Data.DbType.Currency;
            if (value is DateTime) return System.Data.DbType.DateTime;
            if (value is DateTimeOffset) return System.Data.DbType.DateTimeOffset;
            if (value is decimal) return System.Data.DbType.Decimal;
            if (value is double) return System.Data.DbType.Double;
            if (value is Guid) return System.Data.DbType.Guid;
            if (value is Int16) return System.Data.DbType.Int16;
            if (value is Int32) return System.Data.DbType.Int32;
            if (value is Int64) return System.Data.DbType.Int64;
            if (value is Timestamp) return System.Data.DbType.Int64;
            //if (value is ) return System.Data.DbType.SByte;
            if (value is float) return System.Data.DbType.Single;
            if (value is TimeSpan) return System.Data.DbType.Time;
            //if (value is ) return System.Data.DbType.UInt16;
            //if (value is ) return System.Data.DbType.UInt32;
            //if (value is ) return System.Data.DbType.UInt64;
            //if (value is ) return System.Data.DbType.VarNumeric;
            //if (value is ) return System.Data.DbType.AnsiStringFixedLength;
            if (value is char) return System.Data.DbType.StringFixedLength;
            if (value is System.Xml.XmlDocument) return System.Data.DbType.Xml;
            //if (value is ) return System.Data.DbType.DateTime2;
            if (value is byte[]) return System.Data.DbType.Binary;
            return System.Data.DbType.Object;
        }
        public virtual long ExecuteNonQuery(string sql, object?[]? parameters = null) {
            var command = CreateCommand(sql, parameters);
            Exception? executionException = null;
            try {
                return command.ExecuteNonQuery();
            } catch (Exception exception) {
                executionException = exception;
                throw;
            } finally {
                DisposePreservingPrimaryException(command, executionException);
            }
        }
        public virtual async Task<long> ExecuteNonQueryAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            var command = await CreateCommandAsync(sql, parameters, cancellationToken);
            Exception? executionException = null;
            try {
                return await command.ExecuteNonQueryAsync(cancellationToken);
            } catch (Exception exception) {
                executionException = exception;
                throw;
            } finally {
                DisposePreservingPrimaryException(command, executionException);
            }
        }
        public virtual T ExecuteScalar<T>(string sql, object?[]? parameters = null) {
            var command = CreateCommand(sql, parameters);
            Exception? executionException = null;
            try {
                var res = command.ExecuteScalar();
                return ConvertUtils.To<T>(res);
            } catch (Exception exception) {
                executionException = exception;
                throw;
            } finally {
                DisposePreservingPrimaryException(command, executionException);
            }
        }
        public virtual async Task<T> ExecuteScalarAsync<T>(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            var command = await CreateCommandAsync(sql, parameters, cancellationToken);
            Exception? executionException = null;
            try {
                var res = await command.ExecuteScalarAsync(cancellationToken);
                return ConvertUtils.To<T>(res);
            } catch (Exception exception) {
                executionException = exception;
                throw;
            } finally {
                DisposePreservingPrimaryException(command, executionException);
            }
        }
        public virtual IDBReader ExecuteReader(string sql, object?[]? parameters = null) {
            var command = CreateCommand(sql, parameters);
            DbDataReader? reader = null;
            try {
                reader = command.ExecuteReader();
                return new DBReaderDbDataReader(reader, command, new DBReaderDbDataReader.Settings() { AvoidInitializeDBTableFromDataReader = mAvoidInitializeDBTableFromDataReader });
            } catch (Exception exception) {
                if (reader != null) DisposePreservingPrimaryException(reader, exception);
                DisposePreservingPrimaryException(command, exception);
                throw;
            }
        }

        public virtual DbDataReader ExecuteDbDataReader(string sql, object?[]? parameters = null) {
            var command = CreateCommand(sql, parameters);
            DbDataReader? reader = null;
            try {
                reader = command.ExecuteReader();
                return new OwnedDbDataReader(reader, command);
            } catch (Exception exception) {
                if (reader != null) DisposePreservingPrimaryException(reader, exception);
                DisposePreservingPrimaryException(command, exception);
                throw;
            }
        }
        public virtual async Task<DbDataReader> ExecuteDbDataReaderAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            var command = await CreateCommandAsync(sql, parameters, cancellationToken);
            DbDataReader? reader = null;
            try {
                reader = await command.ExecuteReaderAsync(cancellationToken);
                return new OwnedDbDataReader(reader, command);
            } catch (Exception exception) {
                if (reader != null) DisposePreservingPrimaryException(reader, exception);
                DisposePreservingPrimaryException(command, exception);
                throw;
            }
        }
        public virtual async Task<IDBReader> ExecuteReaderAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            var command = await CreateCommandAsync(sql, parameters, cancellationToken);
            DbDataReader? reader = null;
            try {
                reader = await command.ExecuteReaderAsync(cancellationToken);
                var result = new DBReaderDbDataReader(reader, command, new DBReaderDbDataReader.Settings() { AvoidInitializeDBTableFromDataReader = mAvoidInitializeDBTableFromDataReader });
                return result;
            } catch (Exception exception) {
                if (reader != null) DisposePreservingPrimaryException(reader, exception);
                DisposePreservingPrimaryException(command, exception);
                throw;
            }
        }
        public virtual DBTable ExecuteTable(string sql, object?[]? parameters = null) {
            using (var dbReader = ExecuteReader(sql, parameters)) {
                return DBTable.FromDBReader(dbReader);
            }
        }
        public virtual async Task<DBTable> ExecuteTableAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            using (var dbReaderAsync = await ExecuteReaderAsync(sql, parameters, cancellationToken)) {
                return await DBTable.FromDBReaderAsync(dbReaderAsync, cancellationToken);
            }
        }
        public virtual long ExecuteIdentity() {
            return ExecuteScalar<long>(GetSqlSelectAutoincrement());
        }
        public virtual async Task<long> ExecuteIdentityAsync(CancellationToken cancellationToken = default) {
            return await ExecuteScalarAsync<long>(GetSqlSelectAutoincrement(), [], cancellationToken);
        }
        public virtual void BeginTrans() {
            ThrowIfDisposed();
            if (mTransaction != null) throw new InvalidOperationException("A transaction is already active.");
            if (!IsOpen) Open();
            mTransaction = Connection.BeginTransaction();
        }
        public virtual void CommitTrans() {
            ThrowIfDisposed();
            CompleteTransaction(transaction => transaction.Commit(), "No transaction is active to commit.");
        }
        public virtual void RollBackTrans() {
            ThrowIfDisposed();
            CompleteTransaction(transaction => transaction.Rollback(), "No transaction is active to roll back.");
        }
        #endregion


        //DDL 
        #region "DDL"
        public DBSchemaDatabase GetSchema() {
            var all = new string[] { "*" };
            return GetSchema(all, all, all, all);
        }
        public DBSchemaDatabase GetSchema(string[] tableNames, string[] viewNames, string[] sequenceNames, string[] procedureNames) {
            if (tableNames == null) throw new ArgumentNullException(nameof(tableNames));
            if (viewNames == null) throw new ArgumentNullException(nameof(viewNames));
            if (sequenceNames == null) throw new ArgumentNullException(nameof(sequenceNames));
            if (procedureNames == null) throw new ArgumentNullException(nameof(procedureNames));
            var dbSchema = new DBSchemaDatabase();
            dbSchema.Name = Name;
            if (tableNames.Length > 0) {
                var tables = new List<DBSchemaTable>();
                foreach (var tableName in GetTableNames()) {
                    if (MatchesAny(tableName, tableNames)) tables.Add(GetTableSchema(tableName));
                }
                dbSchema.Tables.AddRange(tables.ToArray());
            }
            if (viewNames.Length > 0) {
                var views = new List<DBSchemaView>();
                foreach (var viewName in GetViewNames()) {
                    if (MatchesAny(viewName, viewNames)) views.Add(GetViewSchema(viewName));
                }
                dbSchema.Views.AddRange(views.ToArray());
            }
            if (sequenceNames.Length > 0) {
                var sequences = new List<DBSchemaSequence>();
                foreach (var sequenceName in GetSequenceNames()) {
                    if (MatchesAny(sequenceName, sequenceNames)) sequences.Add(GetSequenceSchema(sequenceName));
                }
                dbSchema.Sequences.AddRange(sequences.ToArray());
            }
            if (procedureNames.Length > 0) {
                var procedures = new List<DBSchemaProcedure>();
                foreach (var procedureName in GetProcedureNames()) {
                    if (MatchesAny(procedureName, procedureNames)) procedures.Add(GetProcedureSchema(procedureName));
                }
                dbSchema.Procedures.AddRange(procedures.ToArray());
            }
            return dbSchema;
        }

        //tables
        public virtual string[] GetTableNames() {
            throw new NotSupportedException("Table enumeration is not supported by this database connection.");
        }
        public virtual bool ExistsTable(string table) {
            throw new NotSupportedException("Table existence checks are not supported by this database connection.");
        }
        public virtual DBSchemaTable GetTableSchema(string table) {
            throw new NotSupportedException("Table schema retrieval is not supported by this database connection.");
        }
        public virtual string GetSqlCreateTable(DBSchemaTable dbSchemaTable, bool avoidCreatePrimaryKey = false, bool avoidCreateForeignKeys = false) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.AppendLine("CREATE TABLE " + qb + dbSchemaTable.Name + qe + " (");
            int i = 0;
            foreach (var column in dbSchemaTable.Columns) {
                if (i > 0) sql.Append("    ,"); else sql.Append("     ");
                sql.Append(qb + column.Name + qe + " ");
                if (column.IsAutoincrement) {
                    sql.Append(GetSqlIdentityDefinition(column.GetNetDataType()));
                } else {
                    sql.Append(GetSqlTypeDefinition(column.DataType, column.Size, column.Precision, column.Scale));
                    if (!string.IsNullOrEmpty(column.Default)) {
                        if (column.Default != null && column.Default.Equals("now", StringComparison.OrdinalIgnoreCase)) {
                            sql.Append(" DEFAULT " + GetSqlDefaultNowExpression());
                        } else {
                            sql.Append(" DEFAULT " + column.Default);
                        }
                    }
                    if (!string.IsNullOrEmpty(column.Collation)) {
                        sql.Append(" COLLATE " + column.Collation);
                    }
                    if (column.Null) {
                        sql.Append(" ");
                    } else {
                        sql.Append(" NOT NULL ");
                    }
                }
                sql.AppendLine();
                i++;
            }
            if (!avoidCreatePrimaryKey) {
                if (dbSchemaTable.PrimaryKey != null) {                
                    sql.Append("    ,CONSTRAINT " + dbSchemaTable.PrimaryKey.Name + " PRIMARY KEY (");
                    for (var j = 0; j < dbSchemaTable.PrimaryKey.Columns.Length; j++) sql.Append((j > 0 ? "," : "") + qb + dbSchemaTable.PrimaryKey.Columns[j] + qe);
                    sql.Append(")");
                    sql.AppendLine();
                }
            }
            if (!avoidCreateForeignKeys) {
                foreach (var dbForeignKey in dbSchemaTable.ForeignKeys) {
                    sql.Append("    ,CONSTRAINT " + dbForeignKey.Name + " FOREIGN KEY (");
                    for (var j = 0; j < dbForeignKey.Columns.Length; j++) sql.Append((j > 0 ? "," : "") + qb + dbForeignKey.Columns[j] + qe);
                    sql.Append(") REFERENCES ");
                    sql.Append(qb + dbForeignKey.RefTable + qe);
                    sql.Append(" (");
                    for (var j = 0; j < dbForeignKey.RefColumns.Length; j++) sql.Append((j > 0 ? "," : "") + qb + dbForeignKey.RefColumns[j] + qe);
                    sql.Append(") ");
                    if (dbForeignKey.OnDelete == DBSchemaOnDeleteRule.Cascade) {
                        sql.Append(" ON DELETE CASCADE ");
                    } else if (dbForeignKey.OnDelete == DBSchemaOnDeleteRule.SetNull) {
                        sql.Append(" ON DELETE SET NULL ");
                    } else if (dbForeignKey.OnDelete == DBSchemaOnDeleteRule.SetDefault) {
                        sql.Append(" ON DELETE SET DEFAULT");
                    }
                    if (dbForeignKey.OnUpdate == DBSchemaOnUpdateRule.Cascade) {
                        sql.Append(" ON UPDATE CASCADE ");
                    } else if (dbForeignKey.OnUpdate == DBSchemaOnUpdateRule.SetNull) {
                        sql.Append(" ON UPDATE SET NULL ");
                    } else if (dbForeignKey.OnUpdate == DBSchemaOnUpdateRule.SetDefault) {
                        sql.Append(" ON UPDATE SET DEFAULT");
                    }
                    sql.AppendLine();
                }
            }
            sql.Append(")");
            return sql.ToString();
        }
        public virtual string GetSqlCreatePrimaryKey(string table, DBSchemaPrimaryKey dbSchemaPrimaryKey) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE " + qb + table + qe + " ADD CONSTRAINT " + dbSchemaPrimaryKey.Name + " PRIMARY KEY (");
            for (var j = 0; j < dbSchemaPrimaryKey.Columns.Length; j++) sql.Append((j > 0 ? "," : "") + qb + dbSchemaPrimaryKey.Columns[j] + qe);
            sql.Append(")");
            return sql.ToString();
        }
        public virtual string GetSqlCreateForeignKey(string table, DBSchemaForeignKey dbSchemaForeignKey) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE " + qb + table + qe + " ADD CONSTRAINT " + dbSchemaForeignKey.Name + " FOREIGN KEY (");
            for (var j = 0; j < dbSchemaForeignKey.Columns.Length; j++) sql.Append((j > 0 ? "," : "") + qb + dbSchemaForeignKey.Columns[j] + qe);
            sql.Append(") REFERENCES ");
            sql.Append(qb + dbSchemaForeignKey.RefTable + qe);
            sql.Append(" (");
            for (var j = 0; j < dbSchemaForeignKey.RefColumns.Length; j++) sql.Append((j > 0 ? "," : "") + qb + dbSchemaForeignKey.RefColumns[j] + qe);
            sql.Append(") ");
            if (dbSchemaForeignKey.OnDelete == DBSchemaOnDeleteRule.Cascade) {
                sql.Append(" ON DELETE CASCADE");
            } else if (dbSchemaForeignKey.OnDelete == DBSchemaOnDeleteRule.SetNull) {
                sql.Append(" ON DELETE SET NULL");
            } else if (dbSchemaForeignKey.OnDelete == DBSchemaOnDeleteRule.SetDefault) {
                sql.Append(" ON DELETE SET DEFAULT");
            }
            if (dbSchemaForeignKey.OnUpdate == DBSchemaOnUpdateRule.Cascade) {
                sql.Append(" ON UPDATE CASCADE");
            } else if (dbSchemaForeignKey.OnUpdate == DBSchemaOnUpdateRule.SetNull) {
                sql.Append(" ON UPDATE SET NULL");
            } else if (dbSchemaForeignKey.OnUpdate == DBSchemaOnUpdateRule.SetDefault) {
                sql.Append(" ON UPDATE SET DEFAULT");
            }
            return sql.ToString();
        }
        public virtual string GetSqlCreateIndex(string table, DBSchemaIndex dbSchemaIndex) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.Append("CREATE " + (dbSchemaIndex.Unique ? "UNIQUE" : "") + " INDEX " + dbSchemaIndex.Name + " ON " + qb + table + qe + " (");
            for (var j = 0; j < dbSchemaIndex.Columns.Length; j++) sql.Append((j > 0 ? "," : "") + qb + dbSchemaIndex.Columns[j] + qe);
            sql.Append(")");
            return sql.ToString();
        }
        public virtual string GetSqlDropTable(string table) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "DROP TABLE " + qb + table + qe;
        }
        public virtual string GetSqlDropPrimaryKey(string table, string name) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "ALTER TABLE " + qb + table + qe + " DROP CONSTRAINT " + name;
        }
        public virtual string GetSqlDropForeignKey(string table, string name) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "ALTER TABLE " + qb + table + qe + " DROP CONSTRAINT " + name;
        }
        public virtual string GetSqlDropColumn(string table, string column) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "ALTER TABLE " + qb + table + qe + " DROP COLUMN " + column;
        }
        public virtual string GetSqlDropDefault(string table, string column) {
            throw new NotSupportedException("Default constraint removal is not supported by this database connection.");
        }
        public virtual string GetSqlDropIndex(string table, string index) {
            throw new NotSupportedException("Index removal is not supported by this database connection.");
        }
        public virtual string GetSqlCreateColumn(string table, DBSchemaColumn dBSchemaColumn) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE " + qb + table + qe + " ADD " + qb + dBSchemaColumn.Name + qe);
            sql.Append(" ").Append(GetSqlTypeDefinition(dBSchemaColumn.DataType, dBSchemaColumn.Size, dBSchemaColumn.Precision, dBSchemaColumn.Scale));
            if (dBSchemaColumn.Null) {
                sql.Append(" NULL ");
            } else {
                sql.Append(" NOT NULL ");
            }
            if (dBSchemaColumn.Default != null && "now".Equals(dBSchemaColumn.Default, StringComparison.CurrentCultureIgnoreCase)) {
                sql.Append(" DEFAULT ").Append(GetSqlDefaultNowExpression());
            } else if (dBSchemaColumn.Default != null && dBSchemaColumn.Default != "") {
                sql.Append(" DEFAULT ").Append(dBSchemaColumn.Default);
            } else if (!dBSchemaColumn.Null) {
                if (dBSchemaColumn.Default == null) {
                    var netType = DProjects.Db.Schema.DBSchemaDataTypeModule.GetNetDataType(dBSchemaColumn.DataType);
                    if (dBSchemaColumn.DataType == DBSchemaDataType.Date || dBSchemaColumn.DataType == DBSchemaDataType.DateTime) {
                        sql.Append(" DEFAULT ").Append(GetSqlEncodedValue(DateTime.Now, DateTime.Now.GetType(), dBSchemaColumn.Null));
                    } else {
                        var aDefault = netType.IsValueType ? Activator.CreateInstance(netType) : null;
                        sql.Append(" DEFAULT ").Append(GetSqlEncodedValue(aDefault, netType, dBSchemaColumn.Null));
                    }

                } else if (dBSchemaColumn.Default.Equals("now", StringComparison.CurrentCultureIgnoreCase)) {
                    sql.Append(" DEFAULT ").Append(GetSqlEncodedValue(DateTime.Now, DateTime.Now.GetType(), dBSchemaColumn.Null));
                }
            }
            return sql.ToString();
        }
        public virtual string GetSqlAlterColumn(string table, DBSchemaColumn dBSchemaColumn) {
            throw new NotSupportedException("Column alteration is not supported by this database connection.");
        }
        public virtual string GetSqlCreateDefault(string table, string column, string aDefault) {
            throw new NotSupportedException("Default constraint creation is not supported by this database connection.");
        }
        public virtual string GetSqlTransactionWrap(string sql) {
            return GetSqlTransactionStart() + GetSqlSeparator() + System.Environment.NewLine + sql + (sql.EndsWith(GetSqlSeparator() + System.Environment.NewLine) ? "" : GetSqlSeparator() + System.Environment.NewLine)  + GetSqlTransactionCommit();
        }
        public virtual string GetSqlTransactionStart() {
            return "BEGIN TRANSACTION";
        }
        public string GetSqlTransactionCommit() {
            return "COMMIT";
        }
        public string GetSqlTransactionRollBack() {
            return "ROLLBACK";
        }
        public virtual string GetSqlCreateTempTable(string table, string select) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "CREATE TEMP TABLE " + qb + table + qe + " AS " + select;
        }
        public virtual string GetSqlDropTempTable(string table) {
            return GetSqlDropTable(table);
        }

        //views
        public virtual string[] GetViewNames() {
            throw new NotSupportedException("View enumeration is not supported by this database connection.");
        }
        public virtual bool ExistsView(string name) {
            throw new NotSupportedException("View existence checks are not supported by this database connection.");
        }
        public virtual DBSchemaView GetViewSchema(string name) {
            var dbSchemaView = new DBSchemaView();
            dbSchemaView.Name = name;
            dbSchemaView.Description = "";
            dbSchemaView.Content = GetView(name);
            return dbSchemaView;
        }
        public virtual string GetView(string name) {
            throw new NotSupportedException("View retrieval is not supported by this database connection.");
        }
        public virtual string GetSqlDropView(string name) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "DROP VIEW " + qb + name + qe;
        }
        public virtual string GetSqlCreateView(DBSchemaView dbSchemaView) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "CREATE VIEW " + qb + dbSchemaView.Name + qe + " AS " + dbSchemaView.Content.Trim();
        }


        //sequences
        public virtual string[] GetSequenceNames() {
            throw new NotSupportedException("Sequence enumeration is not supported by this database connection.");
        }
        public virtual DBSchemaSequence GetSequenceSchema(string name) {
            throw new NotSupportedException("Sequence schema retrieval is not supported by this database connection.");
        }
        public virtual bool ExistsSequence(string name) {
            throw new NotSupportedException("Sequence existence checks are not supported by this database connection.");
        }
        public virtual string GetSqlCreateSequence(DBSchemaSequence dbSchemaSequence) {
            throw new NotSupportedException("Sequence creation is not supported by this database connection.");
        }
        public virtual string GetSqlAlterSequenceIncrement(DBSchemaSequence dbSchemaSequence) {
            throw new NotSupportedException("Sequence alteration is not supported by this database connection.");
        }
        public virtual string GetSqlDropSequence(string sequence) {
            throw new NotSupportedException("Sequence removal is not supported by this database connection.");
        }


        //procedures
        public virtual string[] GetProcedureNames() {
            throw new NotSupportedException("Procedure enumeration is not supported by this database connection.");
        }
        public virtual string GetProcedure(string name) {
            throw new NotSupportedException("Procedure retrieval is not supported by this database connection.");
        }
        public virtual DBSchemaProcedure GetProcedureSchema(string name) {
            var dbSchemaProcedure = new DBSchemaProcedure();
            dbSchemaProcedure.Name = name;
            dbSchemaProcedure.Description = "";
            dbSchemaProcedure.Content = GetProcedure(name);
            return dbSchemaProcedure;
        }
        public virtual string GetSqlCreateProcedure(DBSchemaProcedure dbSchemaProcedure) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.Append("CREATE PROCEDURE " + qb + dbSchemaProcedure.Name + qe + " ");
            sql.Append(dbSchemaProcedure.Content);
            return sql.ToString();
        }
        public virtual string GetSqlDropProcedure(string procedure) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "DROP PROCEDURE " + qb + procedure + qe;
        }

        //backup
        public virtual string BackupDb() {
            throw new NotSupportedException("Database backup is not supported by this database connection.");
        }
        public virtual void RestoreDb(string filename, string dbname) {
            throw new NotSupportedException("Database restore is not supported by this database connection.");
        }
        public virtual void CompactDb() {
            throw new NotSupportedException("Database compaction is not supported by this database connection.");
        }
        public virtual bool ExistsDb(string dbname) {
            throw new NotSupportedException("Database existence checks are not supported by this database connection.");
        }
        public virtual void CreateDb(string dbname) {
            throw new NotSupportedException("Database creation is not supported by this database connection.");
        }
        protected string ConverFullTypeNameToTypeName(string aFullTypeName) {
            if (aFullTypeName.LastIndexOf(".") > -1) {
                return aFullTypeName.Substring(aFullTypeName.LastIndexOf(".") + 1).Replace("[]", "()");
            } else {
                return aFullTypeName.Replace("[]", "()");
            }
        }

        //schema
        public void ApplySchemaChanges(DBSchemaDatabase dbSchema, bool applyChanges, ILogger<IDBConnection> logger) {
            var manageViews = dbSchema.Views.Count > 0;
            var manageSequences = dbSchema.Sequences.Count > 0;
            var manageProcedures = dbSchema.Procedures.Count > 0;
            var all = new string[] { "*" };
            var dbSchemaOld = GetSchema(all, manageViews ? all : Array.Empty<string>(), manageSequences ? all : Array.Empty<string>(), manageProcedures ? all : Array.Empty<string>());
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var separator = GetSqlSeparator();
            var droppedForeignKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var droppedIndexes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var droppedPrimaryKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            // removes foreign keys before the constraints, columns, and tables they depend on
            foreach (var dbSchemaTableOld in dbSchemaOld.Tables) {
                var dbSchemaTable = dbSchema.GetTable(dbSchemaTableOld.Name);
                if (dbSchemaTable == null) continue;
                foreach (var dbSchemaForeignKeyOld in dbSchemaTableOld.ForeignKeys) {
                    var dbSchemaForeignKey = dbSchemaTable.GetForeignKey(dbSchemaForeignKeyOld.Name);
                    var referencedTableOld = dbSchemaOld.GetTable(dbSchemaForeignKeyOld.RefTable);
                    var referencedTable = dbSchema.GetTable(dbSchemaForeignKeyOld.RefTable);
                    var localColumnRemoved = Array.Exists(dbSchemaForeignKeyOld.Columns, column => dbSchemaTable.GetColumn(column) == null);
                    var referencedColumnRemoved = referencedTable != null && Array.Exists(dbSchemaForeignKeyOld.RefColumns, column => referencedTable.GetColumn(column) == null);
                    var referencedPrimaryKeyChanged = referencedTableOld?.PrimaryKey != null && (referencedTable?.PrimaryKey == null
                        || !referencedTableOld.PrimaryKey.GetHash().Equals(referencedTable.PrimaryKey.GetHash()));
                    if (dbSchemaForeignKey == null || !dbSchemaForeignKeyOld.GetHash().Equals(dbSchemaForeignKey.GetHash()) || referencedTable == null
                        || localColumnRemoved || referencedColumnRemoved || referencedPrimaryKeyChanged) {
                        var sql = GetSqlDropForeignKey(dbSchemaTableOld.Name, dbSchemaForeignKeyOld.Name);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                        droppedForeignKeys.Add(GetSchemaObjectKey(dbSchemaTableOld.Name, dbSchemaForeignKeyOld.Name));
                    }
                }
            }
            // removes indexes before columns they depend on
            foreach (var dbSchemaTableOld in dbSchemaOld.Tables) {
                var dbSchemaTable = dbSchema.GetTable(dbSchemaTableOld.Name);
                if (dbSchemaTable == null) continue;
                foreach (var dbSchemaIndexOld in dbSchemaTableOld.Indexes) {
                    var dbSchemaIndex = dbSchemaTable.GetIndex(dbSchemaIndexOld.Name);
                    var indexedColumnRemoved = Array.Exists(dbSchemaIndexOld.Columns, column => dbSchemaTable.GetColumn(column) == null);
                    if (dbSchemaIndex == null || !dbSchemaIndexOld.GetHash().Equals(dbSchemaIndex.GetHash()) || indexedColumnRemoved) {
                        var sql = GetSqlDropIndex(dbSchemaTableOld.Name, dbSchemaIndexOld.Name);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                        droppedIndexes.Add(GetSchemaObjectKey(dbSchemaTableOld.Name, dbSchemaIndexOld.Name));
                    }
                }
            }
            // removes primary keys explicitly before dependent column changes
            foreach (var dbSchemaTableOld in dbSchemaOld.Tables) {
                var dbSchemaTable = dbSchema.GetTable(dbSchemaTableOld.Name);
                if (dbSchemaTable == null || dbSchemaTableOld.PrimaryKey == null) continue;
                if (dbSchemaTable.PrimaryKey == null || !dbSchemaTableOld.PrimaryKey.GetHash().Equals(dbSchemaTable.PrimaryKey.GetHash())) {
                    var sql = GetSqlDropPrimaryKey(dbSchemaTableOld.Name, dbSchemaTableOld.PrimaryKey.Name);
                    logger.LogInformation(sql + separator);
                    if (applyChanges) ExecuteNonQuery(sql);
                    droppedPrimaryKeys.Add(dbSchemaTableOld.Name);
                }
            }
            // removes columns only after their dependent schema objects are gone
            foreach (var dbSchemaTable in dbSchema.Tables) {
                var dbSchemaTableOld = dbSchemaOld.GetTable(dbSchemaTable.Name);
                if (dbSchemaTableOld == null) continue;
                foreach (var dbSchemaColumnOld in dbSchemaTableOld.Columns) {
                    if (dbSchemaTable.GetColumn(dbSchemaColumnOld.Name) != null) continue;
                    if (dbSchemaColumnOld.Default != null) {
                        var sqlDrop = GetSqlDropDefault(dbSchemaTable.Name, dbSchemaColumnOld.Name);
                        logger.LogInformation(sqlDrop + separator);
                        if (applyChanges) ExecuteNonQuery(sqlDrop);
                    }
                    var sql = GetSqlDropColumn(dbSchemaTable.Name, dbSchemaColumnOld.Name);
                    logger.LogInformation(sql + separator);
                    if (applyChanges) ExecuteNonQuery(sql);
                }
            }
            // removes tables after foreign keys from surviving tables have been removed
            foreach (var dbSchemaTableOld in dbSchemaOld.Tables) {
                if (dbSchema.GetTable(dbSchemaTableOld.Name) != null) continue;
                var sql = GetSqlDropTable(dbSchemaTableOld.Name);
                logger.LogInformation(sql + separator);
                if (applyChanges) ExecuteNonQuery(sql);
            }
            //tables
            foreach (var dbSchemaTable in dbSchema.Tables) {
                var dbSchemaTableOld = dbSchemaOld.GetTable(dbSchemaTable.Name);
                if (dbSchemaTableOld == null) {
                    //new table
                    var sql = GetSqlCreateTable(dbSchemaTable, true, true);
                    logger.LogInformation(sql.ToString() + separator);
                    if (applyChanges) {
                        ExecuteNonQuery(sql.ToString());
                        //add
                        var aux = new List<DBSchemaTable>(dbSchemaOld.Tables);
                        aux.Add(GetTableSchema(dbSchemaTable.Name));
                        dbSchemaOld.Tables.Clear();
                        dbSchemaOld.Tables.AddRange(aux.ToArray());
                    }
                } else {
                    //modify table
                    var modified = false;
                    foreach (var dbSchemaColumn in dbSchemaTable.Columns) {
                        var dbSchemaColumnOld = dbSchemaTableOld.GetColumn(dbSchemaColumn.Name);
                        if (dbSchemaColumnOld == null) {
                            //create
                            var sql = GetSqlCreateColumn(dbSchemaTable.Name, dbSchemaColumn);
                            logger.LogInformation(sql + separator);
                            if (applyChanges) ExecuteNonQuery(sql);
                            modified = true;
                        } else if (!AreSchemaColumnTypesEquivalent(dbSchemaColumnOld, dbSchemaColumn) || dbSchemaColumnOld.Default != dbSchemaColumn.Default
                            || dbSchemaColumnOld.Null != dbSchemaColumn.Null) {
                            //change
                            if (dbSchemaColumnOld.Default != null && dbSchemaColumnOld.Default != dbSchemaColumn.Default) {
                                var sqlDrop = GetSqlDropDefault(dbSchemaTable.Name, dbSchemaColumn.Name);
                                logger.LogInformation(sqlDrop + separator);
                                if (applyChanges) ExecuteNonQuery(sqlDrop);
                            }
                            if (dbSchemaColumnOld.Null != dbSchemaColumn.Null) {
                                //var sqlDrop = GetSqlAlterColumn(dbSchemaTable.Name, dbSchemaColumn);
                                //logger.LogInformation(sqlDrop + separator);
                                //if (applyChanges) ExecuteNonQuery(sqlDrop);
                            }
                            var sql = GetSqlAlterColumn(dbSchemaTable.Name, dbSchemaColumn);
                            logger.LogInformation(sql + separator);
                            if (applyChanges) ExecuteNonQuery(sql);
                            if (dbSchemaColumn.Default != null && dbSchemaColumnOld.Default != dbSchemaColumn.Default) {
                                var sqlDrop = GetSqlCreateDefault(dbSchemaTable.Name, dbSchemaColumn.Name, dbSchemaColumn.Default);
                                logger.LogInformation(sqlDrop + separator);
                                if (applyChanges) ExecuteNonQuery(sqlDrop);
                            }
                            modified = true;
                        }
                    }
                    //add
                    if (modified) {
                        for (var i = 0; i < dbSchemaOld.Tables.Count; i++) {
                            if (dbSchemaOld.Tables[i].Name == dbSchemaTable.Name) dbSchemaOld.Tables[i] = GetTableSchema(dbSchemaTable.Name);
                        }
                    }
                }
            }
            //primary keys
            foreach (var dbSchemaTable in dbSchema.Tables) {
                if (dbSchemaTable.PrimaryKey != null) {
                    var dbSchemaTableOld = dbSchemaOld.GetTable(dbSchemaTable.Name);
                    var dbSchemaPrimaryKeyOld = (dbSchemaTableOld != null ? dbSchemaTableOld.PrimaryKey : null);
                    if (dbSchemaTableOld == null || dbSchemaPrimaryKeyOld == null || droppedPrimaryKeys.Contains(dbSchemaTable.Name)
                        || !dbSchemaPrimaryKeyOld.GetHash().Equals(dbSchemaTable.PrimaryKey.GetHash())) {
                        var sql = GetSqlCreatePrimaryKey(dbSchemaTable.Name, dbSchemaTable.PrimaryKey);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //creates indexes
            foreach (var dbSchemaTable in dbSchema.Tables) {
                var dbSchemaTableOld = dbSchemaOld.GetTable(dbSchemaTable.Name);
                foreach (var dbSchemaIndex in dbSchemaTable.Indexes) {
                    var dbSchemaIndexOld = (dbSchemaTableOld != null ? dbSchemaTableOld.GetIndex(dbSchemaIndex.Name) : null);
                    if (dbSchemaIndexOld == null || droppedIndexes.Contains(GetSchemaObjectKey(dbSchemaTable.Name, dbSchemaIndex.Name))
                        || !dbSchemaIndexOld.GetHash().Equals(dbSchemaIndex.GetHash())) {
                        var sql = GetSqlCreateIndex(dbSchemaTable.Name, dbSchemaIndex);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //foreign keys
            foreach (var dbSchemaTable in dbSchema.Tables) {
                var dbSchemaTableOld = dbSchemaOld.GetTable(dbSchemaTable.Name);
                foreach (var dbSchemaForeignKey in dbSchemaTable.ForeignKeys) {
                    var dbSchemaForeignKeyOld = (dbSchemaTableOld != null ? dbSchemaTableOld.GetForeignKey(dbSchemaForeignKey.Name) : null);
                    if (dbSchemaForeignKeyOld == null || droppedForeignKeys.Contains(GetSchemaObjectKey(dbSchemaTable.Name, dbSchemaForeignKey.Name))
                        || !dbSchemaForeignKeyOld.GetHash().Equals(dbSchemaForeignKey.GetHash())) {
                        var sql = GetSqlCreateForeignKey(dbSchemaTable.Name, dbSchemaForeignKey);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //views
            if (manageViews) {
                foreach (var dbSchemaView in dbSchema.Views) {
                    var dbSchemaViewExisting = dbSchemaOld.GetView(dbSchemaView.Name);
                    if (dbSchemaViewExisting == null || !dbSchemaViewExisting.Content.Trim().Equals(dbSchemaView.Content.Trim())) {
                        if (dbSchemaViewExisting != null) {
                            var sqlDrop = GetSqlDropView(dbSchemaViewExisting.Name);
                            logger.LogInformation(sqlDrop + separator);
                            if (applyChanges) ExecuteNonQuery(sqlDrop);
                        }
                        var sql = GetSqlCreateView(dbSchemaView);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //sequences
            if (manageSequences) {
                foreach (var dbSchemaSequence in dbSchema.Sequences) {
                    var dbSchemaSequenceOld = dbSchemaOld.GetSequence(dbSchemaSequence.Name);
                    if (dbSchemaSequenceOld == null) {
                        var sql = GetSqlCreateSequence(dbSchemaSequence);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    } else if (dbSchemaSequenceOld.IncrementBy != dbSchemaSequence.IncrementBy) {
                        var sql = GetSqlAlterSequenceIncrement(dbSchemaSequence);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //procedures
            if (manageProcedures) {
                foreach (var dbSchemaProcedure in dbSchema.Procedures) {
                    var dbSchemaProcedureOld = dbSchemaOld.GetProcedure(dbSchemaProcedure.Name);
                    if (dbSchemaProcedureOld == null || !dbSchemaProcedureOld.Content.Trim().Equals(dbSchemaProcedure.Content.Trim())) {
                        if (dbSchemaProcedureOld != null) {
                            var sqlDrop = GetSqlDropProcedure(dbSchemaProcedureOld.Name);
                            logger.LogInformation(sqlDrop + separator);
                            if (applyChanges) ExecuteNonQuery(sqlDrop);
                        }
                        var sql = GetSqlCreateProcedure(dbSchemaProcedure);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //remove invalid views
            if (manageViews) {
                foreach (var dbSchemaView in dbSchemaOld.Views) {
                    if (dbSchema.GetView(dbSchemaView.Name) == null) {
                        var sql = GetSqlDropView(dbSchemaView.Name);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //remove invalid sequences
            if (manageSequences) {
                foreach (var dbSchemaSequence in dbSchemaOld.Sequences) {
                    if (dbSchema.GetSequence(dbSchemaSequence.Name) == null) {
                        var sql = GetSqlDropSequence(dbSchemaSequence.Name);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //remove invalid procedures
            if (manageProcedures) {
                foreach (var dbSchemaProcedure in dbSchemaOld.Procedures) {
                    if (dbSchema.GetProcedure(dbSchemaProcedure.Name) == null) {
                        var sql = GetSqlDropProcedure(dbSchemaProcedure.Name);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //scripts
            foreach (var dbSchemaScript in dbSchema.Scripts) {
                var sql = dbSchemaScript.Content;
                logger.LogInformation(sql + separator);
                if (applyChanges) ExecuteNonQuery(sql);
            }
            //records
            foreach (var dbSchemaTable in dbSchema.Tables) {
                foreach (var dBSchemaRecord in dbSchemaTable.Records) {
                    if (dbSchemaTable.PrimaryKey is null) throw new InvalidOperationException("Unable to insert record: primary key not found: " + dbSchemaTable.Name);
                    //check if row exists
                    var sSql = new StringBuilder();
                    var oArgs = new List<object?>();
                    sSql.Append("SELECT COUNT(*) FROM " + qb + dbSchemaTable.Name + qe + " WHERE ");
                    var index = 0;
                    var primaryKeyColumns = new List<DBSchemaColumn>();
                    var primaryKeyUsable = dbSchemaTable.PrimaryKey.Columns.Length > 0;
                    foreach (var columnName in dbSchemaTable.PrimaryKey.Columns) {
                        var dbSchemaColumn = dbSchemaTable.GetColumn(columnName);
                        if (dbSchemaColumn == null) throw new InvalidOperationException("Unable to insert record: column not found: " + dbSchemaTable.Name + "." + columnName);
                        primaryKeyColumns.Add(dbSchemaColumn);
                        if (!ContainsRecordKey(dBSchemaRecord, columnName)) primaryKeyUsable = false;
                    }
                    if (primaryKeyUsable) {
                        foreach (var dbSchemaColumn in primaryKeyColumns) {
                            sSql.Append((index > 0 ? " AND " : "") + qb + dbSchemaColumn.Name + qe + "=?");
                            var value = ConvertUtils.To(dBSchemaRecord[dbSchemaColumn.Name], dbSchemaColumn.GetNetDataType(), true);
                            oArgs.Add(value);
                            index++;
                        }
                    }
                    if (index == 0) {
                        //search for unique index
                        foreach (var dbSchemaIndex in dbSchemaTable.Indexes) {
                            if (dbSchemaIndex.Unique) {
                                var uniqueColumns = new List<DBSchemaColumn>();
                                var uniqueIndexUsable = dbSchemaIndex.Columns.Length > 0;
                                foreach (var columnName in dbSchemaIndex.Columns) {
                                    var dbSchemaColumn = dbSchemaTable.GetColumn(columnName);
                                    if (dbSchemaColumn == null) throw new InvalidOperationException("Unable to insert record: column not found: " + dbSchemaTable.Name + "." + columnName);
                                    uniqueColumns.Add(dbSchemaColumn);
                                    if (!ContainsRecordKey(dBSchemaRecord, columnName)) uniqueIndexUsable = false;
                                }
                                if (!uniqueIndexUsable) continue;
                                foreach (var dbSchemaColumn in uniqueColumns) {
                                    sSql.Append((index > 0 ? " AND " : "") + qb + dbSchemaColumn.Name + qe + "=?");
                                    var value = ConvertUtils.To(dBSchemaRecord[dbSchemaColumn.Name], dbSchemaColumn.GetNetDataType(), true);
                                    oArgs.Add(value);
                                    index++;
                                }
                                break;
                            }
                        }
                        if (index == 0) throw new InvalidOperationException("Unable to insert record: unique index not found: " + dbSchemaTable.Name);
                    }
                    var count = ExecuteScalar<int>(sSql.ToString(), oArgs.ToArray()!);
                    if (count == 0) {
                        //insert
                        sSql = new StringBuilder();
                        oArgs = new List<object?>();
                        var recordColumns = new List<DBSchemaColumn>();
                        foreach (var key in dBSchemaRecord.Keys) {
                            if (key != null) {
                                var dbSchemaColumn = dbSchemaTable.GetColumn(key.ToString() ?? "");
                                if (dbSchemaColumn == null) throw new InvalidOperationException("Unable to insert record: column not found: " + dbSchemaTable.Name + "." + key.ToString());
                                recordColumns.Add(dbSchemaColumn);
                            }
                        }
                        sSql.Append("INSERT INTO " + qb + dbSchemaTable.Name + qe + " (");
                        index = 0;
                        foreach (var dbSchemaColumn in recordColumns) {
                            sSql.Append((index++ > 0 ? "," : "") + qb + dbSchemaColumn.Name + qe);
                        }
                        sSql.Append(") VALUES (");
                        index = 0;
                        foreach (var dbSchemaColumn in recordColumns) {
                            sSql.Append((index > 0 ? "," : "") + "?");
                            var value = ConvertUtils.To(dBSchemaRecord[dbSchemaColumn.Name], dbSchemaColumn.GetNetDataType(), true);
                            oArgs.Add(value);
                            index++;
                        }
                        sSql.Append(")");
                        logger.LogInformation(ParseStatement(sSql.ToString(), oArgs.ToArray()) + separator);
                        if (applyChanges) ExecuteNonQuery(sSql.ToString(), oArgs.ToArray());
                    }
                }
            }

        }
        #endregion


        //format 
        #region "format" 
        public virtual string GetSqlSelectTop(int number) {
            throw new NotSupportedException("Limited select syntax is not supported by this database connection.");
        }
        public virtual bool GetSqlSelectTopAtEnd() {
            throw new NotSupportedException("Limited select syntax is not supported by this database connection.");
        }
        public virtual string GetSqlSelectOffsetLimit(long offset, int length) {
            throw new NotSupportedException("Paged select syntax is not supported by this database connection.");
        }
        public virtual string GetSqlLikeAllExpression() {
            return "%";
        }
        public virtual string GetSqlLikeOneExpression() {
            return "_";
        }
        public virtual string GetSqlSelectTest() {
            return "SELECT 1";
        }
        public virtual string GetSqlSelectAutoincrement() {
            throw new NotSupportedException("Autoincrement value retrieval is not supported by this database connection.");
        }
        public virtual string GetSqlSeparator() {
            return ";";
        }
        public virtual string GetSqlAlias() {
            return "a";
        }
        public virtual string GetSqlDefaultNowExpression() {
            throw new NotSupportedException("Current timestamp defaults are not supported by this database connection.");
        }
        /// <summary>Gets the legacy generic timestamp SQL definition.</summary>
        /// <returns><c>TIMESTAMP</c>.</returns>
        [Obsolete("GetSqlTimeStampDefinition is ambiguous. Use GetSqlTypeDefinition(DBSchemaDataType.Timestamp, ...) instead.")]
        public virtual string GetSqlTimeStampDefinition() {
            return "TIMESTAMP";
        }
        public virtual string GetSqlTrueExpression() {
            return "1";
        }
        public virtual string GetSqlFalseExpression() {
            return "0";
        }
        public virtual string GetSqlQualifierBegin() {
            return "";
        }
        public virtual string GetSqlQualifierEnd() {
            return "";
        }
        public virtual string GetSqlIdentityDefinition(Type type) {
            throw new NotSupportedException("Identity columns are not supported by this database connection.");
        }
        public virtual bool GetSqlAvoidCloseCommandForDBTable() {
            return false;
        }
        public virtual bool GetSqlRequireSpaceWhenNull() {
            return false;
        }
        public virtual string GetSqlEncodedLikeValue(string target) {
            throw new NotSupportedException("LIKE value encoding is not supported by this database connection.");
        }
        public virtual string GetSqlTypeDefinition(DBSchemaDataType dataType, int size, int precision, int scale) {
            var result = new StringBuilder();
            result.Append(dataType.ToString().ToUpper());
            if (size > 0) {
                result.Append("(" + size + ")");
            } else if (size == 0 && (dataType == DBSchemaDataType.Varchar || dataType == DBSchemaDataType.Nvarchar || dataType == DBSchemaDataType.Varbinary)) {
                throw new NotSupportedException("Unbounded SQL data type rendering is not supported by this database connection.");
            } else if (precision > 0 || scale > 0) {
                result.Append("(" + precision + "," + scale + ")");
            }
            return result.ToString();
        }
        public virtual DBSchemaDataType GetDataTypeFromSqlDataTypeName(string dataTypeName, int length, int precision, int scale) {
            if (System.Enum.TryParse<DBSchemaDataType>(dataTypeName, true, out DBSchemaDataType result)) {
                return result;
            }
            throw new NotSupportedException($"SQL data type '{dataTypeName}' is not supported.");
        }
        public virtual DBSchemaDataType GetDataTypeFromNetDataTypeName(Type type, int length = 0, int precision = 0, int scale = 0) {
            if (length == int.MaxValue) length = 0;
            if (type == typeof(string)) {
                return DBSchemaDataType.Varchar;
            } else if (type == typeof(short)) {
                return DBSchemaDataType.Smallint;
            } else if (type == typeof(byte)) {
                return DBSchemaDataType.TinyInt;
            } else if (type == typeof(char)) {
                return DBSchemaDataType.Char;
            } else if (type == typeof(int)) {
                return DBSchemaDataType.Int;
            } else if (type == typeof(long)) {
                return DBSchemaDataType.Bigint;
            } else if (type == typeof(float)) {
                return DBSchemaDataType.Float;
            } else if (type == typeof(double)) {
                return DBSchemaDataType.Double;
            } else if (type == typeof(decimal)) {
                return DBSchemaDataType.Decimal;
            } else if (type == typeof(bool)) {
                return DBSchemaDataType.Boolean;
            } else if (type == typeof(byte[])) {
                return DBSchemaDataType.Varbinary;
            } else if (type == typeof(DateTime)) {
                return DBSchemaDataType.DateTime;
            } else if (type == typeof(DateTimeOffset)) {
                return DBSchemaDataType.DateTime;
            } else if (type == typeof(Guid)) {
                return DBSchemaDataType.UniqueIdentifier;
            } else if (type == typeof(Timestamp)) {
                return DBSchemaDataType.Bigint;
            } else if (type == typeof(TimeSpan)) {
                return DBSchemaDataType.Time;
            } else if (Nullable.GetUnderlyingType(type) != null) {
                return GetDataTypeFromNetDataTypeName(Nullable.GetUnderlyingType(type), length, precision, scale);
            } else if (type.IsEnum) {
                return DBSchemaDataType.Int;
            }
            throw new NotSupportedException($".NET data type '{type.FullName}' is not supported.");
        }
        public virtual string GetSqlEncodedValue(object? value, Type? type, bool allowNull) {
            if (value == DBNull.Value) {
                return "NULL";
            } else if (type == null) {
                if (allowNull) {
                    return "NULL";
                } else {
                    return "''";
                }
            } else if (type == typeof(string)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "''";
                    }
                }
                var result = (value.ToString() ?? "").Replace("'", "''");
                if (result.IndexOf(char.ConvertFromUtf32(0)) != -1) {
                    string nullText = char.ConvertFromUtf32(0);
                    result = result.Replace(nullText, "' + 0x00 + '");
                }
                return "'" + result + "'";
            } else if (type == typeof(char)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "NULL";
                    }
                }
                return "'" + (value.ToString() ?? "").Replace("'", "''") + "'";
            } else if (type == typeof(DateTime)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "'1/1/1'";
                    }
                }
                if (System.Convert.ToDateTime(value).Equals(System.Convert.ToDateTime(null))) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "'1/1/1'";
                    }
                }
                return "'" + System.Convert.ToDateTime(value).ToString(DateTimeUtils.DATETIME_ISO8601) + "'";
            } else if (type == typeof(DateTimeOffset)) {
                if (value is DateTimeOffset) value = ((DateTimeOffset)value).DateTime;
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "'1/1/1'";
                    }
                }
                if (System.Convert.ToDateTime(value).Equals(System.Convert.ToDateTime(null))) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "'1/1/1'";
                    }
                }
                return "'" + System.Convert.ToDateTime(value).ToString(DateTimeUtils.DATETIME_ISO8601_MSZ) + "'";
            } else if (type == typeof(bool)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                if (System.Convert.ToBoolean(value)) {
                    return "1";
                } else {
                    return "0";
                }
            } else if (type == typeof(short)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                return System.Convert.ToInt16(value).ToString();
            } else if (type == typeof(int)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                return System.Convert.ToInt32(value).ToString();
            } else if (type == typeof(int[])) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "";
                    }
                }
                StringBuilder sb = new StringBuilder();
                sb.Append("(");
                int i = 0;
                foreach (int valuePart in ((int[])value)) {
                    if (i > 0) {
                        sb.Append(",");
                    }
                    sb.Append(valuePart);
                    i++;
                }
                sb.Append(")");
                return sb.ToString();
            } else if (type == typeof(long)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                return System.Convert.ToInt64(value).ToString();
            } else if (type == typeof(double)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                return System.Convert.ToDouble(value).ToString().Replace(",", ".");
            } else if (type == typeof(float)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                return System.Convert.ToSingle(value).ToString().Replace(",", ".");
            } else if (type == typeof(decimal)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                return System.Convert.ToDecimal(value).ToString().Replace(",", ".");
            } else if (type == typeof(string[])) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "";
                    }
                }
                var sb = new StringBuilder();
                sb.Append("(");
                int i = 0;
                foreach (string valuePart in ((string[])value)) {
                    if (i > 0) {
                        sb.Append(",");
                    }
                    sb.Append(this.GetSqlEncodedValue(valuePart, typeof(string), false));
                    i++;
                }
                sb.Append(")");
                return sb.ToString();
            } else if (type == typeof(byte[])) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0x";
                    }
                }
                var sb = new StringBuilder();
                sb.Append("0x").Append(BitConverter.ToString((byte[])value).Replace("-", ""));
                return sb.ToString();
            } else if (type == typeof(byte)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                var b = ConvertUtils.To<byte>(value);
                int i = b;
                return i.ToString();
            } else if (type.GetTypeInfo().IsEnum) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                return System.Convert.ToInt32(value).ToString();
            } else if (type == typeof(TimeSpan)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "'00:00:00'";
                    }
                }
                if (value is TimeSpan) {
                    return "'" + value.ToString() + "'";
                } else {
                    var valueAsTimeSpan = TimeSpan.Parse(value.ToString() ?? "");
                    return "'" + valueAsTimeSpan.ToString() + "'";
                }
            } else if (type == typeof(Timestamp)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "0";
                    }
                }
                if (value is Timestamp ts) {
                    return ts.UnixMs.ToString();
                } else {
                    var valueAsTimestamp = Timestamp.Parse(value.ToString() ?? "");
                    return valueAsTimestamp.UnixMs.ToString();
                }
            } else if (type == typeof(Guid)) {
                if (value == null || (value == System.DBNull.Value)) {
                    if (allowNull) {
                        return "NULL";
                    } else {
                        return "''";
                    }
                }
                return this.GetSqlEncodedValue(value.ToString() ?? "", typeof(string), allowNull);
            } else {
                throw new NotSupportedException("SQL encoding for data type '" + type.FullName + "' is not supported.");
            }
        }
        public virtual string GetSqlParameterPrefix() {
            return "@__p";
        }
        public virtual string GetSqlGetNextSequenceValue(string sequenceName) {
            throw new NotSupportedException("Sequence value retrieval is not supported by this database connection.");
        }
        public virtual string GetSqlTempTablePrefix() {
            return "";
        }
        public virtual string GetSqlIfRowCountThrowError(int rowCount, int errorCode, string errorMessage) {
            throw new NotSupportedException("Row-count error SQL is not supported by this database connection.");
        }

        // methods (private)
        private static bool MatchesAny(string name, string[] patterns) {
            foreach (var pattern in patterns) {
                if (pattern.Length > 0 && StringUtils.Like(name, pattern)) return true;
            }
            return false;
        }
        private void ConfigureCommand(DbCommand command) {
            command.Transaction = mTransaction;
            if (mCommandTimeout != 0) command.CommandTimeout = mCommandTimeout;
        }
        private static void DisposePreservingPrimaryException(IDisposable resource, Exception? primaryException) {
            try {
                resource.Dispose();
            } catch when (primaryException != null) {
                // preserve the primary operation exception
            }
        }
        protected virtual string GetSqlParameterName(int index) {
            return GetSqlParameterPrefix() + index;
        }
        protected virtual string GetSqlParameterPlaceholder(int index) {
            return GetSqlParameterName(index);
        }
        /// <summary>Determines whether discovered and desired column type metadata has equivalent provider storage semantics.</summary>
        protected virtual bool AreSchemaColumnTypesEquivalent(DBSchemaColumn actual, DBSchemaColumn expected) {
            return actual.DataType == expected.DataType && actual.Size == expected.Size && actual.Precision == expected.Precision && actual.Scale == expected.Scale;
        }
        private static bool ContainsRecordKey(DBSchemaRecord record, string columnName) {
            return Array.FindIndex(record.AllKeys, key => key != null && key.Equals(columnName, StringComparison.OrdinalIgnoreCase)) >= 0;
        }
        private static string GetSchemaObjectKey(string table, string name) {
            return table + "\0" + name;
        }
        private static object GetDbParameterValue(object value) {
            if (value == DBNull.Value) return DBNull.Value;
            if (value is Timestamp timestamp) return timestamp.UnixMs;
            return value;
        }
        private void CompleteTransaction(Action<DbTransaction> operation, string missingTransactionMessage) {
            var transaction = mTransaction ?? throw new InvalidOperationException(missingTransactionMessage);
            Exception? operationException = null;
            try {
                operation(transaction);
            } catch (Exception exception) {
                operationException = exception;
            } finally {
                mTransaction = null;
                DisposePreservingPrimaryException(transaction, operationException);
            }
            if (operationException != null) ExceptionDispatchInfo.Capture(operationException).Throw();
        }
        private void ThrowIfDisposed() {
            if (mIsDisposed) throw new ObjectDisposedException(GetType().FullName);
        }

        #endregion

    }


}
