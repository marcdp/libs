using DProjects.Db.Schema;
using System;
using client = Oracle.ManagedDataAccess.Client;

namespace DProjects.Db.Oracle {

    public class DBConnectionOracle : DBConnection {

        // ctor
        public DBConnectionOracle(string name, string connectionString) : base(name, connectionString, new client.OracleConnection(connectionString)) {
        }

        // methods
        public override string GetSqlParameterPrefix() {
            return ":";
        }
        public override string GetSqlQualifierBegin() {
            return "\"";
        }
        public override string GetSqlQualifierEnd() {
            return "\"";
        }
        public override string GetSqlSelectTest() {
            return "SELECT 1 FROM DUAL";
        }
        public override string GetSqlTransactionStart() {
            throw Unsupported("Textual transaction start SQL");
        }
        public override string GetSqlTransactionWrap(string sql) {
            throw Unsupported("Textual transaction wrapping");
        }
        public override string GetSqlDefaultNowExpression() {
            return "CURRENT_TIMESTAMP";
        }
        public override string GetSqlTrueExpression() {
            throw Unsupported("Boolean SQL literals");
        }
        public override string GetSqlFalseExpression() {
            throw Unsupported("Boolean SQL literals");
        }
        public override DBSchemaDataType GetDataTypeFromSqlDataTypeName(string dataTypeName, int length, int precision, int scale) {
            if (dataTypeName.Equals("date", StringComparison.OrdinalIgnoreCase)) return DBSchemaDataType.DateTime;
            if (dataTypeName.Equals("timestamp", StringComparison.OrdinalIgnoreCase)) return DBSchemaDataType.Timestamp;
            if (dataTypeName.Equals("timestamp with time zone", StringComparison.OrdinalIgnoreCase)
                || dataTypeName.Equals("timestamp with local time zone", StringComparison.OrdinalIgnoreCase)) {
                throw Unsupported($"SQL data type '{dataTypeName}'");
            }
            if (dataTypeName.Equals("varchar2", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Varchar.ToString();
            if (dataTypeName.Equals("number", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Numeric.ToString();
            if (dataTypeName.Equals("float", StringComparison.OrdinalIgnoreCase)) dataTypeName = DBSchemaDataType.Double.ToString();
            return base.GetDataTypeFromSqlDataTypeName(dataTypeName, length, precision, scale);
        }
        public override string GetSqlSelectTop(int number) {
            throw Unsupported("Limited select SQL");
        }
        public override bool GetSqlSelectTopAtEnd() {
            throw Unsupported("Limited select SQL");
        }
        public override string GetSqlSelectOffsetLimit(long offset, int length) {
            throw Unsupported("Paged select SQL");
        }
        public override string GetSqlSelectAutoincrement() {
            throw Unsupported("Autoincrement value retrieval");
        }
        public override string GetSqlIdentityDefinition(Type type) {
            throw Unsupported("Identity-column SQL");
        }
        public override string GetSqlEncodedLikeValue(string target) {
            throw Unsupported("LIKE value encoding");
        }
        public override string GetSqlEncodedValue(object? value, Type? type, bool allowNull) {
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset) || type == typeof(bool) || type == typeof(Guid) || type == typeof(byte[])
                || type == typeof(TimeSpan) || (type == typeof(string) && value is string text && text.IndexOf('\0') >= 0)) {
                throw Unsupported($"SQL literal encoding for data type '{type.FullName}'");
            }
            return base.GetSqlEncodedValue(value, type, allowNull);
        }
        public override string GetSqlGetNextSequenceValue(string sequenceName) {
            throw Unsupported("Sequence value retrieval");
        }
        public override string GetSqlTempTablePrefix() {
            throw Unsupported("Temporary-table prefixes");
        }
        public override string GetSqlIfRowCountThrowError(int rowCount, int errorCode, string errorMessage) {
            throw Unsupported("Row-count error SQL");
        }
        public override string[] GetTableNames() {
            throw Unsupported("Table discovery");
        }
        public override bool ExistsTable(string table) {
            throw Unsupported("Table existence checks");
        }
        public override DBSchemaTable GetTableSchema(string table) {
            throw Unsupported("Table schema discovery");
        }
        public override string GetSqlCreateTable(DBSchemaTable dbSchemaTable, bool avoidCreatePrimaryKey = false, bool avoidCreateForeignKeys = false) {
            throw Unsupported("Table creation SQL");
        }
        public override string GetSqlCreatePrimaryKey(string table, DBSchemaPrimaryKey dbSchemaPrimaryKey) {
            throw Unsupported("Primary-key creation SQL");
        }
        public override string GetSqlCreateForeignKey(string table, DBSchemaForeignKey dbSchemaForeignKey) {
            throw Unsupported("Foreign-key creation SQL");
        }
        public override string GetSqlCreateIndex(string table, DBSchemaIndex dbSchemaIndex) {
            throw Unsupported("Index creation SQL");
        }
        public override string GetSqlCreateColumn(string table, DBSchemaColumn dbSchemaColumn) {
            throw Unsupported("Column creation SQL");
        }
        public override string GetSqlAlterColumn(string table, DBSchemaColumn dbSchemaColumn) {
            throw Unsupported("Column alteration SQL");
        }
        public override string GetSqlDropTable(string table) {
            throw Unsupported("Table removal SQL");
        }
        public override string GetSqlDropPrimaryKey(string table, string name) {
            throw Unsupported("Primary-key removal SQL");
        }
        public override string GetSqlDropForeignKey(string table, string name) {
            throw Unsupported("Foreign-key removal SQL");
        }
        public override string GetSqlDropIndex(string table, string index) {
            throw Unsupported("Index removal SQL");
        }
        public override string GetSqlDropColumn(string table, string column) {
            throw Unsupported("Column removal SQL");
        }
        public override string GetSqlDropDefault(string table, string column) {
            throw Unsupported("Default removal SQL");
        }
        public override string GetSqlCreateDefault(string table, string column, string aDefault) {
            throw Unsupported("Default creation SQL");
        }
        public override string GetSqlTypeDefinition(DBSchemaDataType dataType, int size, int precision, int scale) {
            throw Unsupported("Schema type generation");
        }
        public override string GetSqlCreateTempTable(string table, string select) {
            throw Unsupported("Temporary-table creation SQL");
        }
        public override string GetSqlDropTempTable(string table) {
            throw Unsupported("Temporary-table removal SQL");
        }
        public override string[] GetViewNames() {
            throw Unsupported("View discovery");
        }
        public override bool ExistsView(string view) {
            throw Unsupported("View existence checks");
        }
        public override string GetView(string view) {
            throw Unsupported("View definition discovery");
        }
        public override DBSchemaView GetViewSchema(string view) {
            throw Unsupported("View schema discovery");
        }
        public override string GetSqlCreateView(DBSchemaView dbSchemaView) {
            throw Unsupported("View creation SQL");
        }
        public override string GetSqlDropView(string view) {
            throw Unsupported("View removal SQL");
        }
        public override string[] GetSequenceNames() {
            throw Unsupported("Sequence discovery");
        }
        public override DBSchemaSequence GetSequenceSchema(string sequence) {
            throw Unsupported("Sequence schema discovery");
        }
        public override bool ExistsSequence(string sequence) {
            throw Unsupported("Sequence existence checks");
        }
        public override string GetSqlCreateSequence(DBSchemaSequence dbSchemaSequence) {
            throw Unsupported("Sequence creation SQL");
        }
        public override string GetSqlAlterSequenceIncrement(DBSchemaSequence dbSchemaSequence) {
            throw Unsupported("Sequence alteration SQL");
        }
        public override string GetSqlDropSequence(string sequence) {
            throw Unsupported("Sequence removal SQL");
        }
        public override string[] GetProcedureNames() {
            throw Unsupported("Procedure discovery");
        }
        public override string GetProcedure(string procedure) {
            throw Unsupported("Procedure definition discovery");
        }
        public override DBSchemaProcedure GetProcedureSchema(string procedure) {
            throw Unsupported("Procedure schema discovery");
        }
        public override string GetSqlCreateProcedure(DBSchemaProcedure dbSchemaProcedure) {
            throw Unsupported("Procedure creation SQL");
        }
        public override string GetSqlDropProcedure(string procedure) {
            throw Unsupported("Procedure removal SQL");
        }
        public override string BackupDb() {
            throw Unsupported("Database backup");
        }
        public override void RestoreDb(string filename, string name) {
            throw Unsupported("Database restore");
        }
        public override void CompactDb() {
            throw Unsupported("Database compaction");
        }
        public override bool ExistsDb(string name) {
            throw Unsupported("Database existence checks");
        }
        public override void CreateDb(string name) {
            throw Unsupported("Database creation");
        }

        // methods (private)
        protected override string GetSqlParameterName(int index) {
            return "__p" + index;
        }
        protected override string GetSqlParameterPlaceholder(int index) {
            return ":" + GetSqlParameterName(index);
        }
        private static NotSupportedException Unsupported(string capability) {
            return new NotSupportedException(capability + " is not supported by the Oracle database connection.");
        }
    }
}
