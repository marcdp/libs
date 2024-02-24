using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Db {

    
    public class DBTable {

        //variables
        private string mName;
        private DBColumns mColumns;
        private DBRows mRows;
        private DBColumn[] mPrimaryKey;
        private Dictionary<string, object> mExtendedProperties;
        private bool mChanged;


        //constructor
        public DBTable() {
            mName = "";
            mColumns = new DBColumns(this);
            mRows = new DBRows(this);
            mPrimaryKey = [];
            mExtendedProperties = new Dictionary<string, object>();
        }
        public DBTable(string name) : this() {
            mName = name;
        }


        //properties
        public string Name {
            get {
                return mName;
            }
            set {
                mName = value;
            }
        }
        public DBColumns Columns {
            get {
                return mColumns;
            }
        }
        public DBRows Rows {
            get {
                return mRows;
            }
        }
        public DBColumn[] PrimaryKey {
            get {
                return mPrimaryKey;
            }
            set {
                mPrimaryKey = value;
            }
        }
        public Dictionary<string, object> ExtendedProperties {
            get {
                return mExtendedProperties;
            }
        }
        public bool HasChanges {
            get {
                return mChanged;
            }
        }


        //methods
        public DBRow NewRow() {
            return new DBRow(this);
        }
        public void ImportRow(DBRow row) {
            mRows.Add(row);
        }
        public DBRow[] Select(string expression) {
            throw new NotImplementedException();
        }
        public void AcceptChanges() {
            mChanged = false;
        }
        public void SetChanged() {
            mChanged = true;
        }


        //static
        public static DBTable FromDBReader(IDBReader dbReader) {
            var result = new DBTable();
            foreach(var dbColumn in dbReader.GetColumns()) {
                result.Columns.Add(dbColumn);
            }
            do {
                var dbRow = dbReader.Read();
                if (dbRow == null) break;
                result.Rows.Add(dbRow);
            } while (true);
            return result;
        }
        public static async Task<DBTable> FromDBReaderAsync(IDBReader dbReader, CancellationToken cancellationToken = default) {
            var result = new DBTable();
            foreach (var dbColumn in await dbReader.GetColumnsAsync(cancellationToken)) {
                result.Columns.Add(dbColumn);
            }
            do {
                var dbRow = await dbReader.ReadAsync(cancellationToken);
                if (dbRow == null) break;
                result.Rows.Add(dbRow);
            } while (true);
            return result;
        }

    }


}
