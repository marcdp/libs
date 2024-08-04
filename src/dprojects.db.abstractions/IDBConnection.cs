using DProjects.Db.Schema;
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace DProjects.Db {


    public interface IDBConnection : IDisposable {


        //propiedades
        string Name { get; }
        string ConnectionString { get; }
        int CommandTimeout { get; set; }
        System.Data.Common.DbConnection Connection { get; }
        bool IsOpen { get; }

        // open / close
        void Open();
        Task OpenAsync(CancellationToken cancellationToken = default);
        void Close();

        //DML
        DBTable ExecuteTable(string sql, object?[]? parameters = null);
        Task<DBTable> ExecuteTableAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        IDBReader ExecuteReader(string sql, object?[]? parameters = null);
        Task<IDBReader> ExecuteReaderAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        long ExecuteNonQuery(string sql, object?[]? parameters = null);
        Task<long> ExecuteNonQueryAsync(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        T ExecuteScalar<T>(string sql, object?[]? parameters = null);
        Task<T> ExecuteScalarAsync<T>(string sql, object?[]? parameters = null, CancellationToken cancellationToken = default);
        long ExecuteIdentity();
        Task<long> ExecuteIdentityAsync(CancellationToken cancellationToken = default);        
        DbCommand CreateCommand();
        Task<DbCommand> CreateCommandAsync(CancellationToken cancellationToken = default);
        string ParseStatement(string sql, object?[]? parameters = null);

        //Transaction
        void BeginTrans();
        void CommitTrans();
        void RollBackTrans();

        //DDL Schema
        DBSchemaDatabase GetSchema();
        DBSchemaDatabase GetSchema(string[] tableNames, string[] viewNames, string[] sequenceNames, string[] procedureNames);
        void ApplySchemaChanges(DBSchemaDatabase dbSchema, bool applyChanges, ILogger<IDBConnection> logger);

        //DDL Table
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
        //DDL Sequence
        string[] GetSequenceNames();
        DBSchemaSequence GetSequenceSchema(string sequence);
        bool ExistsSequence(string sequence);
        string GetSqlCreateSequence(DBSchemaSequence dbSchemaSequence);
        string GetSqlAlterSequenceIncrement(DBSchemaSequence dbSchemaSequence);
        string GetSqlDropSequence(string sequence);
        //DDL view
        string[] GetViewNames();
        bool ExistsView(string view);
        string GetSqlDropView(string view);
        DBSchemaView GetViewSchema(string view);
        string GetView(string view);
        string GetSqlCreateView(DBSchemaView dbSchemaView);
        //DDL Procedures
        string[] GetProcedureNames();
        string GetProcedure(string name);
        DBSchemaProcedure GetProcedureSchema(string procedure);
        string GetSqlCreateProcedure(DBSchemaProcedure dbSchemaProcedure);
        string GetSqlDropProcedure(string procedure);
        //DDL DB
        string BackupDb();
        void RestoreDb(string filename, string name);
        void CompactDb();
        bool ExistsDb(string name);
        void CreateDb(string name);
        //DML Transaction
        string GetSqlTransactionWrap(string sql);
        string GetSqlTransactionStart();
        string GetSqlTransactionCommit();
        string GetSqlTransactionRollBack();

        //format        
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
        string GetSqlTypeDefinition(DBSchemaDataType dataType, int size, int precision, int scale);
        string GetSqlEncodedValue(object? value, Type? type, bool bAllowNull);
        string GetSqlParameterPrefix();
        string GetSqlGetNextSequenceValue(string sequenceName);
        string GetSqlTempTablePrefix();
        string GetSqlIfRowCountThrowError(int rowCount, int errorCode, string errorMessage);
    }


}
