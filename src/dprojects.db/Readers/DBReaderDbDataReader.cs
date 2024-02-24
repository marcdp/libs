using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Db.Readers {


    public class DBReaderDbDataReader : IDBReader {


        //inner classes
        public class Settings {
            public Settings() {
            }
        }


        //variables
        private DbDataReader mReader;
        private bool mLeaveOpen;
        private DBTable? mTable;
        private Settings mSettings;
        private bool mNoResults;


        //constructor
        public DBReaderDbDataReader(DbDataReader reader, bool leaveOpen = false, Settings? settings = null) {
            if (settings == null) settings = new Settings();
            mReader = reader;
            mLeaveOpen = leaveOpen;
            mSettings = settings;            
        }
        public void Dispose() {
            if (!mLeaveOpen && mReader != null) {
                mReader.Dispose();
            }
        }


        //properties
        public DBColumns Columns {
            get {
                if (mTable == null) mTable = InitializeDBTableFromDataReader();
                return mTable.Columns;
            }
        }


        //methods
        public DBColumns GetColumns() {
            if (mTable == null) mTable = InitializeDBTableFromDataReader();
            return mTable.Columns;
        }
        public int GetColumnsCount() {
            return mReader.FieldCount;
        }
        public Task<DBColumns> GetColumnsAsync(CancellationToken cancellationToken = default) {
            if (mTable == null) mTable = InitializeDBTableFromDataReader();
            return Task.FromResult(mTable.Columns);
        }
        public DBRow? Read() {
            if (mTable == null) mTable = InitializeDBTableFromDataReader();
            if (mNoResults) {
                var dbRow = mTable.NewRow();
                dbRow[0] = mReader.RecordsAffected;
                mNoResults = false;
                return dbRow;
            } else if (mReader.Read()) {
                var dbRow = mTable.NewRow();
                for (var i = 0; i < mReader.FieldCount; i++) {
                    if (mReader.IsDBNull(i)) {
                        dbRow[i] = null;
                    } else {
                        object value = mReader.GetValue(i);
                        if (value is UInt64 && mTable.Columns[i].DBType == typeof(bool)) {
                            value = (System.Convert.ToUInt64(value) == 0) ? false : true;
                        }
                        dbRow[i] = value;
                    }
                }
                return dbRow;
            }
            return null;
        }
        public bool Read(object?[] values) {
            throw new NotImplementedException();
        }
        public async Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            if (mTable == null) mTable = InitializeDBTableFromDataReader();
            if (mNoResults) {
                var dbRow = mTable.NewRow();
                dbRow[0] = mReader.RecordsAffected;
                mNoResults = false;
                return dbRow;
            } else if (await mReader.ReadAsync(cancellationToken)) {
                var dbRow = mTable.NewRow();
                for (var i = 0; i < mReader.FieldCount; i++) {
                    if (mReader.IsDBNull(i)) {
                        dbRow[i] = null;
                    } else {
                        object value = mReader.GetValue(i);
                        if (value is UInt64 && mTable.Columns[i].DBType == typeof(bool)) {
                            value = (System.Convert.ToUInt64(value) == 0) ? false : true;
                        }
                        dbRow[i] = value;
                    }
                }
                return dbRow;
            }
            return null;
        }
        public async Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            if (!(await mReader.ReadAsync(cancellationToken))) return false;
            mReader.GetValues(values);
            for(var i = 0;i < mReader.FieldCount; i++) {
                if (values[i] == DBNull.Value) values[i] = null;
            }
            return true;
        }
        public bool NextResult() {
            var result = mReader.NextResult();
            if (result) mTable = null;
            return result;
        }
        public async Task<bool> NextResultAsync(CancellationToken cancellationToken = default) {
            var result = await mReader.NextResultAsync(cancellationToken);
            if (result) mTable = null;
            return result;
        }


        //private 
        private DBTable InitializeDBTableFromDataReader() {
            var dbTable = new DBTable();
            //init table columns
            var schema = mReader.GetSchemaTable();
            if (schema != null) {
                //A query returning records was executed
                //DataTable underlineSchema = null;
                for (var i = 0; i < mReader.FieldCount; i++) {
                    //se situa en la row del schema correspondiente
                    var dbRow = schema.Rows[i];
                    //Create a column name that is unique in the data table
                    string columnName = dbRow["ColumnName"]?.ToString() ?? "";
                    //Add the column definition to the data table
                    DBColumn dbColumn = new DBColumn(columnName, (Type)(dbRow["DataType"]));
                    if ((Type)dbRow["DataType"] == typeof(string)) {
                        dbColumn.MaxLength = Convert.ToInt32(dbRow["ColumnSize"]);
                        if (dbColumn.MaxLength < 0) dbColumn.MaxLength = int.MaxValue;
                    }
                    if (dbColumn.DBType == typeof(UInt64) && System.Convert.ToInt32(dbRow["Columnsize"]) == 1) {
                        dbColumn.DBType = typeof(bool);
                    }
                    dbColumn.ReadOnly = Convert.ToBoolean(dbRow["IsReadOnly"]);
                    dbColumn.Required = !Convert.ToBoolean(dbRow["AllowDBNull"]);
                    if (dbRow.Table.Columns.Contains("IsAutoIncrement")) {
                        dbColumn.AutoIncrement = Convert.ToBoolean(dbRow["IsAutoIncrement"]);
                    }
                    if (dbRow.Table.Columns.Contains("DefaultValue")) {
                        if (!dbColumn.AutoIncrement) {
                            if (!(dbColumn.DBType == typeof(byte[]))) {
                                if (!(dbRow["DefaultValue"] == System.DBNull.Value) && !(dbRow["DefaultValue"] == null)) {
                                    dbColumn.DefaultValue = dbRow["DefaultValue"];
                                }
                            }
                        }
                    }
                    if (dbColumn.AutoIncrement) dbColumn.ReadOnly = true;
                    if (dbRow.IsNull("IsUnique")) {
                        dbColumn.Unique = false;
                    } else {
                        dbColumn.Unique = (bool)dbRow["IsUnique"];
                    }
                    dbTable.Columns.Add(dbColumn);
                    if (!dbRow.IsNull("IsKey") && Convert.ToBoolean(dbRow["IsKey"])) {
                        DBColumn[] array = dbTable.PrimaryKey;
                        Array.Resize(ref array, array.Length + 1);
                        array[array.Length - 1] = dbColumn;
                        dbTable.PrimaryKey = array;
                    }
                }
            } else {
                //No records were returned
                mNoResults = true;
                dbTable.Columns.Add("RowsAffected", typeof(long));
            }
            return dbTable;

        }

    }


}
