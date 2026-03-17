using DProjects.Utils;
using System;
using System.Collections;
using System.Collections.Generic;


namespace DProjects.Db {


    public class DBRows : IEnumerable<DBRow> {


        //variables
        private readonly DBTable mTable;
        private readonly List<DBRow> mRows;


        //constructor
        public DBRows(DBTable dbTable) {
            mTable = dbTable;
            mRows = new List<DBRow>();
        }


        //properties
        public int Count {
            get {
                return mRows.Count;
            }
        }
        public DBRow this[int index] {
            get {
                if (index < 0 || index >= mRows.Count) {
                    throw new IndexOutOfRangeException();
                }
                return mRows[index];
            }
        }


        //methods
        public IEnumerator<DBRow> GetEnumerator() {
            return this.GetEnumerator1();
        }

        public IEnumerator<DBRow> GetEnumerator1() {
            return ((IEnumerable<DBRow>)mRows).GetEnumerator();
        }
        IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.IEnumerable_GetEnumerator();
        }

        public IEnumerator IEnumerable_GetEnumerator() {
            return ((IEnumerable<DBRow>)mRows).GetEnumerator();
        }
        public void Add(DBRow dbRow) {
            if (dbRow.Table == mTable) {
                mRows.Add(dbRow);
            } else {
                var values = new List<object?>();
                foreach (DBColumn dbColumn in mTable.Columns) {
                    if (dbRow.Table.Columns.Contains(dbColumn.Name)) {
                        if (dbRow.IsNull(dbColumn.Name)) {
                            values.Add(null);
                        } else {
                            values.Add(dbRow[dbColumn.Name]);
                        }
                    } else {
                        values.Add(null);
                    }
                }
                this.Add(values.ToArray());
            }
            mTable.SetChanged();
        }
        public void Add(IDictionary<string, object?> dict) {
            var dbRow = new DBRow(mTable, dict);
            mRows.Add(dbRow);
            mTable.SetChanged();
        }
        public void Add(params object?[] values) {
            var dbRow = new DBRow(mTable);
            for (int i = 0; i <= values.Length - 1; i++) {
                dbRow[i] = values[i];
            }
            Add(dbRow);
            mTable.SetChanged();
        }
        public void Remove(DBRow dbRow) {
            for (int i = 0; i <= mRows.Count - 1; i++) {
                if (mRows[i] == dbRow) {
                    mRows.RemoveAt(i);
                    mTable.SetChanged();
                    return;
                }
            }
        }
        public void RemoveAt(int index) {
            if (index < 0 || index >= mRows.Count) {
                throw new IndexOutOfRangeException();
            }
            mRows.RemoveAt(index);
            mTable.SetChanged();
        }
        public DBRow? Find(object value) {
            if (mTable.PrimaryKey.Length == 1) {
                foreach (var dbRow in mRows) {
                    object? objValueA = dbRow[mTable.PrimaryKey[0]];
                    if (value.Equals(objValueA)) {
                        return dbRow;
                    }
                }
            }
            return null;
        }
        public void Sort(string expression) {
            List<string> columns = new List<string>();
            List<int> directions = new List<int>();
            foreach (string aux in expression.Split(',')) {
                string part = aux.Trim();
                string column = part;
                int direction = 1;
                if (column.IndexOf(" ") != -1) {
                    if (column.ToLower().EndsWith(" asc")) {
                        column = column.Substring(0, column.Length - 4).Trim();
                        direction = 1;
                    } else if (column.ToLower().EndsWith(" desc")) {
                        column = column.Substring(0, column.Length - 4).Trim();
                        direction = -1;
                    } else {
                        throw new Exception("Unable to sort: invalid order by expression: " + column);
                    }
                }
                if (!mTable.Columns.Contains(column)) {
                    throw new Exception("Unable to sort: column not found: " + column);
                }
                columns.Add(column);
                directions.Add(direction);
            }
            mRows.Sort((DBRow rbRowA, DBRow dbRowB) => {
                for (int i = 0; i <= columns.Count - 1; i++) {
                    string column = columns[i];
                    int direction = directions[i];
                    object? valueA = rbRowA[column];
                    object? valueB = dbRowB[column];
                    int subresult = 0;
                    if (valueA == null && valueB != null) {
                        subresult = 1;
                    } else if (valueB == null && valueA != null) {
                        subresult = -1;
                    } else if (valueB == null && valueA == null) {
                        subresult = 0;
                    } else if (valueA != null && valueB != null && valueA.GetType() == valueB.GetType()) {
                        if (valueA.GetType() == typeof(short)) {
                            subresult = System.Convert.ToInt16(valueA).CompareTo(System.Convert.ToInt16(valueB));
                        } else if (valueA.GetType() == typeof(int)) {
                            subresult = System.Convert.ToInt32(valueA).CompareTo(System.Convert.ToInt32(valueB));
                        } else if (valueA.GetType() == typeof(long)) {
                            subresult = System.Convert.ToInt64(valueA).CompareTo(System.Convert.ToInt64(valueB));
                        } else if (valueA.GetType() == typeof(Single)) {
                            subresult = System.Convert.ToSingle(valueA).CompareTo(System.Convert.ToSingle(valueB));
                        } else if (valueA.GetType() == typeof(double)) {
                            subresult = System.Convert.ToDouble(valueA).CompareTo(System.Convert.ToDouble(valueB));
                        } else if (valueA.GetType() == typeof(bool)) {
                            subresult = System.Convert.ToBoolean(valueA).CompareTo(System.Convert.ToBoolean(valueB));
                        } else if (valueA.GetType() == typeof(DateTime)) {
                            subresult = System.Convert.ToDateTime(valueA).CompareTo(System.Convert.ToDateTime(valueB));
                        } else if (valueA.GetType() == typeof(decimal)) {
                            subresult = System.Convert.ToDecimal(valueA).CompareTo(System.Convert.ToDecimal(valueB));
                        } else {
                            subresult = (valueA.ToString() ?? "").CompareTo(valueB.ToString());
                        }
                    } else {
                        subresult = String.Compare(valueA?.ToString() ?? "", valueB?.ToString() ?? "");
                    }
                    if (subresult != 0) {
                        return subresult * direction;
                    }
                }
                return 0;
            }
            );
        }

    }


}
