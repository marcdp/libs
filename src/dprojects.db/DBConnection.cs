using DProjects.Db.Readers;
using DProjects.Db.Schema;
using DProjects.DataTypes;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Reflection;
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
        protected string mName;
        protected string mConnectionString;
        protected Type? mConnectionType;
        protected System.Data.Common.DbConnection mConnection;
        protected int mCommandTimeout;
        protected Stack<System.Data.Common.DbTransaction> mTransactions;
        protected bool mIsDisposed;
        protected bool mAvoidParametrizedQueries;
        protected bool mAvoidInitializeDBTableFromDataReader;


        //constructor
        public DBConnection(string name, string connectionString, System.Data.Common.DbConnection connection) {
            mName = name;
            mConnectionString = connectionString;
            mCommandTimeout = 0;
            mTransactions = new Stack<System.Data.Common.DbTransaction>();
            mIsDisposed = false;
            mConnection = connection;
        }
        public virtual void Dispose() {
            if (mTransactions.Count > 0) throw new Exception("There are pending transactions in connection \'" + mName + "\'.");
            if (!mIsDisposed) {
                mIsDisposed = true;
                if (mConnection != null) {
                    var connection = mConnection;
                    mConnection = null!;
                    connection.Close();
                    connection.Dispose();
                    Disposed?.Invoke(this);                    
                }
            }
        }


        //properties
        public string Name => mName; 
        public string ConnectionString => mConnectionString;
        public int CommandTimeout { get { return mCommandTimeout; } set { mCommandTimeout = value; } }
        public System.Data.Common.DbConnection Connection => mConnection;
        public bool IsOpen => (mConnection.State == System.Data.ConnectionState.Open);


        //DML methods
        #region "DML methods"
        public void Open() {
            if (mConnection.State == System.Data.ConnectionState.Open) return;
            mConnection.Open();
        }
        public async Task OpenAsync(CancellationToken cancellationToken = default) {
            if (mConnection.State == System.Data.ConnectionState.Open) return;
            await mConnection.OpenAsync(cancellationToken!);

        }
        public void Close() {
            if (mConnection != null) {
                mConnection.Close();
                mConnection.Dispose();
            }
        }
        public string ParseStatement(string sql, object?[]? parameters = null) {
            var command = CreateCommand(sql, parameters);
            var result = command.CommandText;
            command.Dispose();
            return result;
        }
        public System.Data.Common.DbCommand CreateCommand() {
            if (!IsOpen) Open();
            return Connection.CreateCommand();
        }
        public async Task<DbCommand> CreateCommandAsync(CancellationToken cancellationToken = default) {
            if (!IsOpen) await OpenAsync(cancellationToken);
            return Connection.CreateCommand();
        }
        protected virtual System.Data.Common.DbCommand CreateCommand(string sql, object?[]? parameters = null) {
            if (!IsOpen) Open();
            var command = Connection.CreateCommand();
            if (mAvoidParametrizedQueries) {
                command.CommandText = CreateCommandText(sql, parameters);
            } else {
                command.CommandText = CreateCommandTextWithParameters(command, sql, parameters);
            }
            if (mCommandTimeout != 0) command.CommandTimeout = mCommandTimeout;
            command.CommandType = System.Data.CommandType.Text;
            if (mTransactions.Count > 0) command.Transaction = mTransactions.Peek();
            return command;
        }
        protected virtual async Task<System.Data.Common.DbCommand> CreateCommandAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            if (!IsOpen) await OpenAsync(cancellationToken);
            var command = Connection.CreateCommand();
            if (mAvoidParametrizedQueries) {
                command.CommandText = CreateCommandText(sql, parameters);
            } else {
                command.CommandText = CreateCommandTextWithParameters(command, sql, parameters);
            }
            if (mTransactions.Count > 0) command.Transaction = mTransactions.Peek();
            command.CommandType = System.Data.CommandType.Text;
            if (mCommandTimeout != 0) command.CommandTimeout = mCommandTimeout;
            return command;
        }
        protected string CreateCommandText(string sql, object?[]? parameters = null) {
            if (parameters == null) return sql;
            int j = 0;
            int k = 0;
            var sb = new StringBuilder();
            try {
                //replaces the question marks (?) with the coded values of the parameters
                if (parameters.Length > 0) {
                    j = 0;
                    k = 0;
                    foreach (object? parameter in parameters) {
                        bool allowNull = false;
                        string value = "";
                        Type? type = null;
                        k = sql.IndexOf('?', j);
                        if (k == -1) throw new Exception("Error parsing sql statement, too many parameters.");
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
            } catch (ArgumentOutOfRangeException e) {
                throw new Exception("Error parsing sql statement \'" + sql + "\'.", e);
            } catch (Exception e) {
                throw new Exception("Error parsing sql statement \'" + sql + "\'.", e);
            }
            return sql;
        }
        protected string CreateCommandTextWithParameters(System.Data.Common.DbCommand command, string sql, object?[]? parameters = null) {
            int j = 0;
            int k = 0;
            try {
                command.CommandText = sql;
                if (parameters != null && parameters.Length > 0) {
                    var parameterPrefix = GetSqlParameterPrefix();
                    var sb = new StringBuilder();
                    j = 0;
                    k = 0;
                    foreach (object? parameter in parameters) {
                        k = sql.IndexOf('?', j);
                        if (k == -1) throw new Exception("Error parsing sql statement: too many parameters");
                        sb.Append(sql.Substring(j, k - j));
                        sb.Append(parameterPrefix + (command.Parameters.Count));
                        j = k + 1;
                        var dbParameter = command.CreateParameter();
                        dbParameter.ParameterName = parameterPrefix + (command.Parameters.Count);
                        if (parameter == null) {
                            dbParameter.Value = DBNull.Value;
                        } else {
                            dbParameter.Value = parameter;
                            var dbType = GetDbType(parameter);
                            if (dbType != System.Data.DbType.Object) dbParameter.DbType = dbType;
                        }
                        command.Parameters.Add(dbParameter);
                    }
                    sb.Append(sql.Substring(k + 1));
                    command.CommandText = sb.ToString();
                }
            } catch (ArgumentOutOfRangeException e) {
                throw new Exception("Error parsing sql statement \'" + sql + "\'.", e);
            } catch (Exception e) {
                throw new Exception("Error parsing sql statement \'" + sql + "\'.", e);
            }
            return command.CommandText;
        }
        private System.Data.DbType GetDbType(object? value) {
            //if (value is string) return System.Data.DbType.AnsiString;
            if (value is string) return System.Data.DbType.String;
            if (value is byte) return System.Data.DbType.Byte;
            if (value is bool) return System.Data.DbType.Boolean;
            if (value is Currency) return System.Data.DbType.Currency;
            if (value is DateTime) return System.Data.DbType.DateTime;
            if (value is DateTime) return System.Data.DbType.Date;
            if (value is decimal) return System.Data.DbType.Decimal;
            if (value is double) return System.Data.DbType.Double;
            if (value is Guid) return System.Data.DbType.Guid;
            if (value is Int16) return System.Data.DbType.Int16;
            if (value is Int32) return System.Data.DbType.Int32;
            if (value is Int64) return System.Data.DbType.Int64;
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
            //if (value is ) return System.Data.DbType.DateTimeOffset;
            if (value is byte[]) return System.Data.DbType.Binary;
            return System.Data.DbType.Object;
        }
        public virtual long ExecuteNonQuery(string sql, object?[]? parameters = null) {
            using var command = CreateCommand(sql, parameters);
            try {
                return command.ExecuteNonQuery();
            } catch (Exception e) {
                throw new Exception("Error in DBConnectionData.ExecuteNonQuery(\'" + command.CommandText + "\')", e);
            } 
        }
        public virtual async Task<long> ExecuteNonQueryAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            using var command = await CreateCommandAsync(sql, parameters, cancellationToken);
            try {
                return await command.ExecuteNonQueryAsync();
            } catch (Exception e) {
                throw new Exception($"Error in DBConnectionData.ExecuteNonQuery(\'{command.CommandText}\')", e);
            }
        }
        public virtual T ExecuteScalar<T>(string sql, object?[]? parameters = null) {
            using var command = CreateCommand(sql, parameters);
            try {
                var res = command.ExecuteScalar();
                return ConvertUtils.To<T>(res);
            } catch (Exception e) {
                throw new Exception("Error in DBConnectionData.ExecuteScalar(\'" + sql + "\')", e);
            }
        }
        public virtual async Task<T> ExecuteScalarAsync<T>(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            using var command = await CreateCommandAsync(sql, parameters, cancellationToken);
            try {
                var res = await command.ExecuteScalarAsync();
                return ConvertUtils.To<T>(res);
            } catch (Exception e) {
                throw new Exception("Error in DBConnectionData.ExecuteScalar(\'" + sql + "\')", e);
            }
        }
        public virtual IDBReader ExecuteReader(string sql, object?[]? parameters = null) {
            var command = CreateCommand(sql, parameters);
            try {
                var reader = command.ExecuteReader();
                return new DBReaderDbDataReader(reader, GetSqlAvoidCloseCommandForDBTable());
            } catch (Exception e) {
                throw new Exception("Error in DBConnectionData.ExecuteReader(\'" + sql + "\'), " + e.Message, e);
            }
        }

        public virtual DbDataReader ExecuteDbDataReader(string sql, object?[]? parameters = null) {
            var command = CreateCommand(sql, parameters);
            try {
                var reader = command.ExecuteReader();
                return reader;
            } catch (Exception e) {
                throw new Exception("Error in DBConnectionData.ExecuteReader(\'" + sql + "\'), " + e.Message, e);
            }
        }
        public virtual async Task<DbDataReader> ExecuteDbDataReaderAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            var command = await CreateCommandAsync(sql, parameters, cancellationToken);
            try {
                var reader = await command.ExecuteReaderAsync(cancellationToken);
                return reader;
            } catch (Exception e) {
                throw new Exception("Error in DBConnectionData.ExecuteReader(\'" + sql + "\'), " + e.Message, e);
            }
        }
        public virtual async Task<IDBReader> ExecuteReaderAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default) {
            var command = await CreateCommandAsync(sql, parameters, cancellationToken);
            try {
                var reader = await command.ExecuteReaderAsync(cancellationToken);
                var result = new DBReaderDbDataReader(reader, false, new DBReaderDbDataReader.Settings() { AvoidInitializeDBTableFromDataReader = mAvoidInitializeDBTableFromDataReader });
                return result;
            } catch (Exception e) {
                throw new Exception("Error in DBConnectionData.ExecuteReader(\'" + sql + "\'), " + e.Message, e);
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
            mTransactions.Push(Connection.BeginTransaction());
        }
        public virtual void CommitTrans() {
            mTransactions.Pop().Commit();
        }
        public virtual void RollBackTrans() {
            mTransactions.Pop().Rollback();
        }
        #endregion


        //DDL 
        #region "DDL"
        public DBSchemaDatabase GetSchema() {
            var all = new string[] { "*" };
            return GetSchema(all, all, all, all);
        }
        public DBSchemaDatabase GetSchema(string[] tableNames, string[] viewNames, string[] sequenceNames, string[] procedureNames) {
            var dbSchema = new DBSchemaDatabase();
            dbSchema.Name = Name;
            var tables = new List<DBSchemaTable>();
            foreach (var tableName in GetTableNames()) {
                var valid = false;
                foreach (var pattern in tableNames) {
                    if (pattern.Length>0 && StringUtils.Like(tableName, pattern)) valid = true;
                }
                if (valid) tables.Add(GetTableSchema(tableName));
            }
            dbSchema.Tables.AddRange(tables.ToArray());
            var views = new List<DBSchemaView>();
            foreach (var viewName in GetViewNames()) {
                var valid = false;
                foreach (var pattern in viewNames) {
                    if (pattern.Length > 0 && StringUtils.Like(viewName, pattern)) valid = true;
                }
                if (valid) views.Add(GetViewSchema(viewName));
            }
            dbSchema.Views.AddRange(views.ToArray());
            var sequences = new List<DBSchemaSequence>();
            foreach (var sequenceName in GetSequenceNames()) {
                var valid = false;
                foreach (var pattern in sequenceNames) {
                    if (pattern.Length > 0 && StringUtils.Like(sequenceName, pattern)) valid = true;
                }
                if (valid) sequences.Add(GetSequenceSchema(sequenceName));
            }
            dbSchema.Sequences.AddRange(sequences.ToArray());
            var procedures = new List<DBSchemaProcedure>();
            foreach (var procedureName in GetProcedureNames()) {
                var valid = false;
                foreach (var pattern in procedureNames) {
                    if (pattern.Length > 0 && StringUtils.Like(procedureName, pattern)) valid = true;
                }
                if (valid) procedures.Add(GetProcedureSchema(procedureName));
            }
            dbSchema.Procedures.AddRange(procedures.ToArray());
            return dbSchema;
        }

        //tables
        public virtual string[] GetTableNames() {
            throw new NotImplementedException();
        }
        public virtual bool ExistsTable(string table) {
            throw new NotImplementedException();
        }
        public virtual DBSchemaTable GetTableSchema(string table) {
            throw new NotImplementedException();
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
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "ALTER TABLE " + qb + table + qe + " ALTER " + column + " DROP DEFAULT ";
        }
        public virtual string GetSqlDropIndex(string table, string index) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "DROP INDEX " + qb + table + qe + "." + index;
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
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE " + qb + table + qe + " ALTER COLUMN " + qb + dBSchemaColumn.Name + qe);
            sql.Append(" ").Append(GetSqlTypeDefinition(dBSchemaColumn.DataType, dBSchemaColumn.Size, dBSchemaColumn.Precision, dBSchemaColumn.Scale));
            if (dBSchemaColumn.Null) {
                sql.Append(" NULL ");
            } else {
                sql.Append(" NOT NULL ");
            }
            return sql.ToString();
        }
        public virtual string GetSqlCreateDefault(string table, string column, string aDefault) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var sql = new StringBuilder();
            sql.Append("ALTER TABLE " + qb + table + qe + " ADD CONSTRAINT DF_" + table + "_" + column);
            if ("now".Equals(aDefault, StringComparison.OrdinalIgnoreCase)) {
                sql.Append(" DEFAULT ").Append(GetSqlDefaultNowExpression());
            } else if (aDefault.Length > 0) {
                sql.Append(" DEFAULT ").Append(aDefault);
            }
            sql.Append(" FOR " + column);
            return sql.ToString();
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
            throw new NotImplementedException("GetViewNames not implemented");
        }
        public virtual bool ExistsView(string name) {
            throw new NotImplementedException("ExistsView not implemented");
        }
        public virtual DBSchemaView GetViewSchema(string name) {
            var dbSchemaView = new DBSchemaView();
            dbSchemaView.Name = name;
            dbSchemaView.Description = "";
            dbSchemaView.Content = GetView(name);
            return dbSchemaView;
        }
        public virtual string GetView(string name) {
            throw new NotImplementedException("GetView not implemented");
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
            return [];
        }
        public virtual DBSchemaSequence GetSequenceSchema(string name) {
            throw new NotImplementedException("GetSequenceSchema not implemented");
        }
        public virtual bool ExistsSequence(string name) {
            throw new NotImplementedException("Exists sequence not implemented");
        }
        public virtual string GetSqlCreateSequence(DBSchemaSequence dbSchemaSequence) {
            var sql = new StringBuilder();
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            sql.AppendLine("CREATE SEQUENCE  " + qb + dbSchemaSequence.Name + qe);
            sql.AppendLine("    START WITH " + dbSchemaSequence.InitValue);
            sql.AppendLine("    INCREMENT BY " + dbSchemaSequence.IncrementBy);
            return sql.ToString();
        }
        public virtual string GetSqlAlterSequenceIncrement(DBSchemaSequence dbSchemaSequence) {
            var sql = new StringBuilder();
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            sql.AppendLine("ALTER SEQUENCE  " + qb + dbSchemaSequence.Name + qe);
            sql.AppendLine("    INCREMENT BY " + dbSchemaSequence.IncrementBy);
            return sql.ToString();
        }
        public virtual string GetSqlDropSequence(string sequence) {
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            return "DROP SEQUENCE " + qb + sequence + qe;
        }


        //procedures
        public virtual string[] GetProcedureNames() {
            return [];
        }
        public virtual string GetProcedure(string name) {
            throw new NotImplementedException();
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
            throw new Exception("Backup Not implemented");
        }
        public virtual void RestoreDb(string filename, string dbname) {
            throw new Exception("Restore Not implemented");
        }
        public virtual void CompactDb() {
            throw new Exception("Compact Not implemented");
        }
        public virtual bool ExistsDb(string dbname) {
            throw new Exception("ExistsDatabase Not implemented");
        }
        public virtual void CreateDb(string dbname) {
            throw new Exception("CreateDatabase Not implemented");
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
            var dbSchemaOld = GetSchema();
            var qb = GetSqlQualifierBegin();
            var qe = GetSqlQualifierEnd();
            var separator = GetSqlSeparator();
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
                        } else if (dbSchemaColumnOld.DataType != dbSchemaColumn.DataType || dbSchemaColumnOld.Size != dbSchemaColumn.Size || dbSchemaColumnOld.Precision != dbSchemaColumn.Precision || dbSchemaColumnOld.Scale != dbSchemaColumn.Scale || dbSchemaColumnOld.Default != dbSchemaColumn.Default || dbSchemaColumnOld.Null != dbSchemaColumn.Null) {
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
                    if (dbSchemaTableOld == null || dbSchemaPrimaryKeyOld == null || !dbSchemaPrimaryKeyOld.GetHash().Equals(dbSchemaTable.PrimaryKey.GetHash())) {
                        if (dbSchemaPrimaryKeyOld != null && !dbSchemaPrimaryKeyOld.GetHash().Equals(dbSchemaTable.PrimaryKey.GetHash())) {
                            var sqlDrop = GetSqlDropPrimaryKey(dbSchemaTable.Name, dbSchemaPrimaryKeyOld.Name);
                            logger.LogInformation(sqlDrop + separator);
                            if (applyChanges) ExecuteNonQuery(sqlDrop);
                        }
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
                    if (dbSchemaIndexOld == null || !dbSchemaIndexOld.GetHash().Equals(dbSchemaIndex.GetHash())) {
                        if (dbSchemaIndexOld != null) {
                            var sqlDrop = GetSqlDropIndex(dbSchemaTable.Name, dbSchemaIndex.Name);
                            logger.LogInformation(sqlDrop + separator);
                            if (applyChanges) ExecuteNonQuery(sqlDrop);
                        }
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
                    if (dbSchemaForeignKeyOld == null || !dbSchemaForeignKeyOld.GetHash().Equals(dbSchemaForeignKey.GetHash())) {
                        if (dbSchemaForeignKeyOld != null) {
                            var sqlDrop = GetSqlDropForeignKey(dbSchemaTable.Name, dbSchemaForeignKey.Name);
                            logger.LogInformation(sqlDrop + separator);
                            if (applyChanges) ExecuteNonQuery(sqlDrop);
                        }
                        var sql = GetSqlCreateForeignKey(dbSchemaTable.Name, dbSchemaForeignKey);
                        logger.LogInformation(sql + separator);
                        if (applyChanges) ExecuteNonQuery(sql);
                    }
                }
            }
            //views
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
            //sequences
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
            //procedures
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
            //remove invalid tables
            foreach (var dbSchemaTable in dbSchemaOld.Tables) {
                if (dbSchema.GetTable(dbSchemaTable.Name) == null) {
                    var sql = GetSqlDropTable(dbSchemaTable.Name);
                    logger.LogInformation(sql + separator);
                    if (applyChanges) ExecuteNonQuery(sql);
                }
            }
            //remove invalid table columns
            foreach (var dbSchemaTable in dbSchema.Tables) {
                var dbSchemaTableOld = dbSchemaOld.GetTable(dbSchemaTable.Name);
                if (dbSchemaTableOld != null) {
                    foreach (var dbSchemaColumnOld in dbSchemaTableOld.Columns) {
                        if (dbSchemaTable.GetColumn(dbSchemaColumnOld.Name) == null) {
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
                }
            }
            //remove invalid table foreign keys
            foreach (var dbSchemaTable in dbSchema.Tables) {
                var dbSchemaTableOld = dbSchemaOld.GetTable(dbSchemaTable.Name);
                if (dbSchemaTableOld != null) {
                    foreach (var dbSchemaForeignKeyOld in dbSchemaTableOld.ForeignKeys) {
                        if (dbSchemaTable.GetForeignKey(dbSchemaForeignKeyOld.Name) == null) {
                            var sql = GetSqlDropForeignKey(dbSchemaTable.Name, dbSchemaForeignKeyOld.Name);
                            logger.LogInformation(sql + separator);
                            if (applyChanges) ExecuteNonQuery(sql);
                        }
                    }
                }
            }
            //remove invalid table indexes
            foreach (var dbSchemaTable in dbSchema.Tables) {
                var dbSchemaTableOld = dbSchemaOld.GetTable(dbSchemaTable.Name);
                if (dbSchemaTableOld != null) {
                    foreach (var dbSchemaIndexOld in dbSchemaTableOld.Indexes) {
                        if (dbSchemaTable.GetIndex(dbSchemaIndexOld.Name) == null) {
                            if (dbSchemaTable.PrimaryKey == null || string.Join(",", dbSchemaIndexOld.Columns) != string.Join(",", dbSchemaTable.PrimaryKey.Columns)) {
                                var sql = GetSqlDropIndex(dbSchemaTable.Name, dbSchemaIndexOld.Name);
                                logger.LogInformation(sql + separator);
                                if (applyChanges) ExecuteNonQuery(sql);
                            }
                        }
                    }
                }
            }
            //remove invalid views
            foreach (var dbSchemaView in dbSchemaOld.Views) {
                if (dbSchema.GetView(dbSchemaView.Name) == null) {
                    var sql = GetSqlDropView(dbSchemaView.Name);
                    logger.LogInformation(sql + separator);
                    if (applyChanges) ExecuteNonQuery(sql);
                }
            }
            //remove invalid sequences
            foreach (var dbSchemaSequence in dbSchemaOld.Sequences) {
                if (dbSchema.GetSequence(dbSchemaSequence.Name) == null) {
                    var sql = GetSqlDropSequence(dbSchemaSequence.Name);
                    logger.LogInformation(sql + separator);
                    if (applyChanges) ExecuteNonQuery(sql);
                }
            }
            //remove invalid procedures
            foreach (var dbSchemaProcedure in dbSchemaOld.Procedures) {
                if (dbSchema.GetProcedure(dbSchemaProcedure.Name) == null) {
                    var sql = GetSqlDropProcedure(dbSchemaProcedure.Name);
                    logger.LogInformation(sql + separator);
                    if (applyChanges) ExecuteNonQuery(sql);
                }
            }
            //scripts
            foreach (var dbSchemaScript in dbSchema.Scripts) {
                if (applyChanges) {
                    var sql = dbSchemaScript.Content;
                    logger.LogInformation(sql.ToString() + separator);
                    if (applyChanges) ExecuteNonQuery(sql.ToString());
                }
            }
            //records
            foreach (var dbSchemaTable in dbSchema.Tables) {
                foreach (var dBSchemaRecord in dbSchemaTable.Records) {
                    if (dbSchemaTable.PrimaryKey is null) throw new Exception("Unable to insert record: primary key not found: " + dbSchemaTable.Name);
                    //check if row exists
                    var sSql = new StringBuilder();
                    var oArgs = new List<object?>();
                    sSql.Append("SELECT COUNT(*) FROM " + dbSchemaTable.Name + " WHERE ");
                    var index = 0;
                    foreach (var key in dBSchemaRecord.Keys) {
                        if (key != null && System.Array.IndexOf(dbSchemaTable.PrimaryKey.Columns, key) != -1) {
                            var dbSchemaColumn = dbSchemaTable.GetColumn(key.ToString() ?? "");
                            if (dbSchemaColumn == null) throw new Exception("Unable to insert record: column not found: " + dbSchemaTable.Name + "." + key.ToString());
                            sSql.Append((index > 0 ? " AND " : "") + dbSchemaColumn.Name + "=?");
                            var value = ConvertUtils.To(dBSchemaRecord[key.ToString()], dbSchemaColumn.GetNetDataType(), true);
                            oArgs.Add(value);
                            index++;
                        }
                    }
                    if (index == 0) {
                        //search for unique index
                        foreach (var dbSchemaIndex in dbSchemaTable.Indexes) {
                            if (dbSchemaIndex.Unique) {
                                foreach (var columnName in dbSchemaIndex.Columns) {
                                    var dbSchemaColumn = dbSchemaTable.GetColumn(columnName);
                                    if (dbSchemaColumn == null) throw new Exception("Unable to insert record: column not found: " + dbSchemaTable.Name + "." + columnName);
                                    sSql.Append((index > 0 ? " AND " : "") + dbSchemaColumn.Name + "=?");
                                    var value = ConvertUtils.To(dBSchemaRecord[columnName], dbSchemaColumn.GetNetDataType(), true);
                                    oArgs.Add(value);
                                    index++;
                                }
                                break;
                            }
                        }
                        if (index == 0) throw new Exception("Unable to insert record: unique index not found: " + dbSchemaTable.Name);
                    }
                    var count = ExecuteScalar<int>(sSql.ToString(), oArgs.ToArray()!);
                    if (count == 0) {
                        //insert
                        sSql = new StringBuilder();
                        oArgs = new List<object?>();
                        sSql.Append("INSERT INTO " + dbSchemaTable.Name + " (");
                        index = 0;
                        foreach (var key in dBSchemaRecord.Keys) {
                            if (key != null) sSql.Append((index++ > 0 ? "," : "") + key.ToString());
                        }
                        sSql.Append(") VALUES (");
                        index = 0;
                        foreach (var key in dBSchemaRecord.Keys) {
                            if (key != null) {
                                var dbSchemaColumn = dbSchemaTable.GetColumn(key.ToString() ?? "");
                                if (dbSchemaColumn == null) throw new Exception("Unable to insert record: column not found: " + dbSchemaTable.Name + "." + key.ToString());
                                sSql.Append((index > 0 ? "," : "") + "?");
                                var value = ConvertUtils.To(dBSchemaRecord[key.ToString()], dbSchemaColumn.GetNetDataType(), true);
                                oArgs.Add(value);
                                index++;
                            }
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
            return " TOP " + number + " ";
        }
        public virtual bool GetSqlSelectTopAtEnd() {
            return true;
        }
        public virtual string GetSqlSelectOffsetLimit(long offset, int length) {
            return " OFFSET " + offset + " ROWS FETCH NEXT " + length + " ROW ONLY";
        }
        public virtual string GetSqlLikeAllExpression() {
            return "%";
        }
        public virtual string GetSqlLikeOneExpression() {
            return "SELECT 1";
        }
        public virtual string GetSqlSelectTest() {
            return "SELECT 1";
        }
        public virtual string GetSqlSelectAutoincrement() {
            return "SELECT @@IDENTITY";
        }
        public virtual string GetSqlSeparator() {
            return ";";
        }
        public virtual string GetSqlAlias() {
            return "a";
        }
        public virtual string GetSqlDefaultNowExpression() {
            return "getDate()";
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
            if (type == typeof(System.Guid)) {
                return " uniqueidentifier NOT NULL DEFAULT newId()";
            } else if (type == typeof(System.Int64)) {
                return " BIGINT IDENTITY NOT NULL ";
            } else {
                return " INT IDENTITY NOT NULL ";
            }
        }
        public virtual string GetSqlTimeStampDefinition() {
            return "TIMESTAMP";
        }
        public virtual bool GetSqlAvoidCloseCommandForDBTable() {
            return false;
        }
        public virtual bool GetSqlRequireSpaceWhenNull() {
            return false;
        }
        public virtual string GetSqlEncodedLikeValue(string target) {
            string s = target.Trim();
            s = s.Replace("[", "[[]");
            s = s.Replace("á", "a");
            s = s.Replace("à", "a");
            s = s.Replace("Á", "a");
            s = s.Replace("À", "a");
            s = s.Replace("A", "a");
            s = s.Replace("a", "[aáàÀAÁ]");
            s = s.Replace("é", "e");
            s = s.Replace("è", "e");
            s = s.Replace("È", "e");
            s = s.Replace("É", "e");
            s = s.Replace("E", "e");
            s = s.Replace("e", "[eéèÉÈE]");
            s = s.Replace("í", "i");
            s = s.Replace("ï", "i");
            s = s.Replace("Í", "i");
            s = s.Replace("Ï", "i");
            s = s.Replace("I", "i");
            s = s.Replace("i", "[iíïÌÍI]");
            s = s.Replace("ó", "o");
            s = s.Replace("ò", "o");
            s = s.Replace("Ó", "o");
            s = s.Replace("Ò", "o");
            s = s.Replace("O", "o");
            s = s.Replace("o", "[oóòÒÓO]");
            s = s.Replace("ú", "u");
            s = s.Replace("ü", "u");
            s = s.Replace("Ú", "u");
            s = s.Replace("Ú", "u");
            s = s.Replace("Ü", "u");
            s = s.Replace("u", "[uúüÚÙU]");
            s = s.Replace("_", "[_]");
            return s;
        }
        public virtual string GetSqlTypeDefinition(DBSchemaDataType dataType, int size, int precision, int scale) {
            var result = new StringBuilder();
            result.Append(dataType.ToString().ToUpper());
            if (size > 0) {
                result.Append("(" + size + ")");
            } else if (size == 0 && (dataType == DBSchemaDataType.Varchar || dataType == DBSchemaDataType.Nvarchar || dataType == DBSchemaDataType.Varbinary)) {
                result.Append("(MAX)");
            } else if (precision > 0 || scale > 0) {
                result.Append("(" + precision + "," + scale + ")");
            }
            return result.ToString();
        }
        public virtual DBSchemaDataType GetDataTypeFromSqlDataTypeName(string dataTypeName, int length, int precision, int scale) {
            if (dataTypeName.Equals("bit", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Boolean.ToString();
            if (dataTypeName.Equals("text", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Varchar.ToString();
            if (dataTypeName.Equals("ntext", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Varchar.ToString();
            if (dataTypeName.Equals("varchar2", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Varchar.ToString();
            if (dataTypeName.Equals("number", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Numeric.ToString();
            if (dataTypeName.Equals("uniqueidentifier", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.UniqueIdentifier.ToString();
            if (System.Enum.TryParse<DBSchemaDataType>(dataTypeName, true, out DBSchemaDataType result)) {
                return result;
            }
            throw new NotImplementedException();
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
            } else if (type == typeof(Guid)) {
                return DBSchemaDataType.UniqueIdentifier;
            } else if (Nullable.GetUnderlyingType(type) != null) {
                return GetDataTypeFromNetDataTypeName(Nullable.GetUnderlyingType(type), length, precision, scale);
            } else if (type.IsEnum) {
                return DBSchemaDataType.Int;
            }
            throw new NotImplementedException();
        }
        public virtual string GetSqlEncodedValue(object? value, Type? type, bool allowNull) {
            if (type == null) {
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
            //} else if (type == typeof(DateTimeOffset)) {
            //    if (value is DateTimeOffset) value = ((DateTimeOffset)value).DateTime;
            //    if (value == null || (value == System.DBNull.Value)) {
            //        if (allowNull) {
            //            return "NULL";
            //        } else {
            //            return "'1/1/1'";
            //        }
            //    }
            //    if (System.Convert.ToDateTime(value).Equals(System.Convert.ToDateTime(null))) {
            //        if (allowNull) {
            //            return "NULL";
            //        } else {
            //            return "'1/1/1'";
            //        }
            //    }
            //    return "'" + System.Convert.ToDateTime(value).ToString(DateTimeUtils.DATETIME_ISO8601_MSZ) + "'";
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
                throw new Exception("Unimplemend sql data type \'" + type.FullName + "\'.");
            }
        }
        public virtual string GetSqlParameterPrefix() {
            return "@__p";
        }
        public virtual string GetSqlGetNextSequenceValue(string sequenceName) {
            return "SELECT NEXT VALUE FOR " + GetSqlQualifierBegin() + sequenceName + GetSqlQualifierEnd();
        }
        public virtual string GetSqlTempTablePrefix() {
            return "";
        }
        public virtual string GetSqlIfRowCountThrowError(int rowCount, int errorCode, string errorMessage) {
            var sql = new StringBuilder();
            sql.AppendLine("if @@ROWCOUNT = " + rowCount);
            sql.AppendLine("BEGIN");
            sql.AppendLine("    THROW " + errorCode + ", '" + errorMessage.Replace("'", "''") + "', 0;");
            sql.AppendLine("END;");
            return sql.ToString();
        }

        #endregion

    }


}
