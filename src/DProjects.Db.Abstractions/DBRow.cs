using DProjects.Utils;
using System;
using System.Collections;
using System.Collections.Generic;


namespace DProjects.Db {


    public class DBRow : IDictionary<string, object?> {


        //variables
        private DBTable mTable;
        private object?[] mValues;


        //constructor
        public DBRow(DBTable dbTable) {
            mTable = dbTable;
            mValues = new object?[dbTable.Columns.Count];
        }
        public DBRow(DBTable dbTable, params object?[] values) {
            mTable = dbTable;
            mValues = new object?[dbTable.Columns.Count];
            for (int i = 0; i <= values.Length - 1; i++) {
                mValues[i] = values[i];
            }
        }
        public DBRow(DBTable dbTable, IDictionary<string, object?> values) {
            mTable = dbTable;
            mValues = new object?[dbTable.Columns.Count];
            foreach (var value in values) {
                this[value.Key] = value.Value;
            }
        }


        //properties
        public object? this[int index] {
            get {
                if (index < 0 || index >= mValues.Length) {
                    throw new IndexOutOfRangeException();
                }
                return mValues[index];
            }
            set {
                if (index < 0 || index >= mValues.Length) {
                    throw new IndexOutOfRangeException();
                }
                mValues[index] = value;
                mTable.SetChanged();
            }
        }
        public object? this[string name] {
            get {
                var index = mTable.Columns.GetColumnIndex(name);
                return this[index];
            }
            set {
                this[mTable.Columns.GetColumnIndex(name)] = value;
            }
        }
        public object? this[DBColumn dbColumn] {
            get {
                return this[dbColumn.Name];
            }
            set {
                this[dbColumn.Name] = value;
            }
        }

        public DBTable Table {
            get {
                return mTable;
            }
        }
        public object?[] Values {
            get {
                return mValues;
            }
        }

        //methods
        public T Get<T>(string name, T defaultValue) {
            var value = this[name];
            if (value == null) return defaultValue;
            return ConvertUtils.To<T>(value);
        }
        public T GetAs<T>(int index) {
            return ConvertUtils.To<T>(this[index]);
        }
        public T GetAs<T>(string name) {
            return ConvertUtils.To<T>(this[name]);
        }
        public T GetAs<T>(DBColumn dbColumn) {
            return ConvertUtils.To<T>(this[dbColumn]);
        }
        public bool IsNull(string name) {
            object? value = this[name];
            return (value == null);
        }
        public bool IsNull(int index) {
            object? value = this[index];
            return (value == null);
        }
        public bool IsNull(DBColumn dbColumn) {
            object? value = this[dbColumn.Name];
            return (value == null);
        }
        public void RemoveColumn(int index) {
            var o = new List<object?>(mValues);
            o.RemoveAt(index);
            mValues = o.ToArray();
        }



        /* IDictionary<string.object> */
        object? IDictionary<string, object?>.this[string key] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        ICollection<string> IDictionary<string, object?>.Keys => throw new NotImplementedException();
        ICollection<object?> IDictionary<string, object?>.Values => throw new NotImplementedException();
        int ICollection<KeyValuePair<string, object?>>.Count => throw new NotImplementedException();
        bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => throw new NotImplementedException();
        void IDictionary<string, object?>.Add(string key, object? value) {
            throw new NotImplementedException();
        }
        void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) {
            throw new NotImplementedException();
        }
        void ICollection<KeyValuePair<string, object?>>.Clear() {
            throw new NotImplementedException();
        }
        bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item) {
            throw new NotImplementedException();
        }
        bool IDictionary<string, object?>.ContainsKey(string key) {
            throw new NotImplementedException();
        }
        void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) {
            throw new NotImplementedException();
        }
        IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() {
            return ToDict().GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }
        bool IDictionary<string, object?>.Remove(string key) {
            throw new NotImplementedException();
        }
        bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) {
            throw new NotImplementedException();
        }
        bool IDictionary<string, object?>.TryGetValue(string key, out object? value) {
            var index = mTable.Columns.GetColumnIndex(key);
            if (index == -1) {
                value = null;
                return false;
            } else {
                value = this[index];
                return true;
            }
        }
        Dictionary<string, object?> ToDict() {
            var dict = new Dictionary<string, object?>();
            int index = 0;
            foreach(var value in mValues) {
                dict[mTable.Columns[index++].Name] = value;
            }
            return dict;
        }



    }


}
