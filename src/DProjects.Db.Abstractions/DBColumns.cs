using DProjects.Utils;
using System;
using System.Collections;
using System.Collections.Generic;


namespace DProjects.Db {


    public class DBColumns : IEnumerable<DBColumn> {


        //variabless
        private DBTable mTable;
        private List<DBColumn> mColumns;


        //constructor
        public DBColumns(DBTable table) {
            mTable = table;
            mColumns = new List<DBColumn>();
        }


        //properties
        public int Count {
            get {
                return mColumns.Count;
            }
        }
        public DBColumn this[string name] {
            get {
                return this[GetColumnIndex(name)];
            }
        }
        public DBColumn this[int index] {
            get {
                if (index < 0 || index > mColumns.Count - 1) {
                    throw new IndexOutOfRangeException();
                }
                return mColumns[index];
            }
        }


        //methods
        public bool Contains(string name) {
            return GetColumnIndex(name) != -1;
        }
        public void Add(DBColumns columns) {
            foreach (var column in columns) {
                Add(column.Clone());
            }
        }
        public DBColumn Add(string name) {
            return Add(new DBColumn(name, typeof(string)));
        }
        public DBColumn Add(string name, Type datatype) {
            return Add(new DBColumn(name, datatype));
        }
        public DBColumn Add(string name, Type datatype, DBColumnFormat format) {
            return Add(new DBColumn(name, datatype, format));
        }
        public DBColumn Add(DBColumn dbColumn) {
            mColumns.Add(dbColumn);
            return dbColumn;
        }
        public void Add(DBColumn[] dbColumns) {
            foreach (var column in dbColumns) {
                mColumns.Add(column);
            }
        }
        public void Clear() {
            while (mColumns.Count > 0) {
                Remove(mColumns[0].Name);
            }
        }
        public void Remove(string name) {
            int index = GetColumnIndex(name);
            if (!(index == -1)) {
                mColumns.RemoveAt(index);
                if (mTable != null) {
                    foreach (DBRow objDBRow in mTable.Rows) {
                        objDBRow.RemoveColumn(index);
                    }
                }
            }
        }
        public int GetColumnIndex(string name) {
            int i = 0;
            foreach (DBColumn dbColumn in mColumns) {
                if (StringUtils.Equals(mColumns[i].Name, name)) {
                    return i;
                }
                i++;
            }
            return -1;
        }
        public IEnumerator<DBColumn> GetEnumerator() {
            return this.GetEnumerator1();
        }

        public IEnumerator<DBColumn> GetEnumerator1() {
            return mColumns.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return this.IEnumerable_GetEnumerator();
        }

        public IEnumerator IEnumerable_GetEnumerator() {
            return mColumns.GetEnumerator();
        }


    }


}
