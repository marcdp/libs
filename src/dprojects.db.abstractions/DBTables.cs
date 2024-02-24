using System;
using System.Collections;
using System.Collections.Generic;


namespace DProjects.Db {


    public class DBTables : IEnumerable<DBTable> {


        //variables
        private List<DBTable> mTables;


        //constructor
        public DBTables() {
            mTables = new List<DBTable>();
        }


        //properties
        public int Count {
            get {
                return mTables.Count;
            }
        }
        public DBTable? this[string name] {
            get {
                foreach (DBTable dbTable in mTables) {
                    if (dbTable.Name == name) {
                        return dbTable;
                    }
                }
                return null;
            }
        }
        public DBTable? this[int index] {
            get {
                if (index < 0 || index >= mTables.Count) {
                    throw new IndexOutOfRangeException();
                }
                return mTables[index];
            }
        }


        //methods
        public IEnumerator<DBTable> GetEnumerator() {
            return this.GetEnumerator1();
        }

        public IEnumerator<DBTable> GetEnumerator1() {
            return ((IEnumerable<DBTable>)mTables).GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return this.IEnumerable_GetEnumerator();
        }

        public IEnumerator IEnumerable_GetEnumerator() {
            return ((IEnumerable<DBTable>)mTables).GetEnumerator();
        }
        public void Add(DBTable objDBTable) {
            mTables.Add(objDBTable);
        }
        public void Remove(string name) {
            DBTable? dbTable = this[name];
            if (dbTable is null) return;
            Remove(dbTable);
        }
        public void Remove(DBTable dbTable) {
            for (int i = 0; i <= mTables.Count - 1; i++) {
                if (mTables[i] == dbTable) {
                    mTables.RemoveAt(i);
                    return;
                }
            }
        }


    }


}
