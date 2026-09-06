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
        object? IDictionary<string, object?>.this[string key] { get => this[key]; set => this[key] = value; }
        ICollection<string> IDictionary<string, object?>.Keys {
            get {
                var keys = new string[mTable.Columns.Count];
                for (var index = 0; index < keys.Length; index++) {
                    keys[index] = mTable.Columns[index].Name;
                }
                return keys;
            }
        }
        ICollection<object?> IDictionary<string, object?>.Values => (object?[])mValues.Clone();
        int ICollection<KeyValuePair<string, object?>>.Count => mValues.Length;
        bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => true;
        void IDictionary<string, object?>.Add(string key, object? value) {
            throw FixedSchemaException();
        }
        void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) {
            throw FixedSchemaException();
        }
        void ICollection<KeyValuePair<string, object?>>.Clear() {
            throw FixedSchemaException();
        }
        bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item) {
            var index = mTable.Columns.GetColumnIndex(item.Key);
            return index != -1 && EqualityComparer<object?>.Default.Equals(this[index], item.Value);
        }
        bool IDictionary<string, object?>.ContainsKey(string key) {
            return mTable.Columns.GetColumnIndex(key) != -1;
        }
        void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex) {
            if (array == null) throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0) throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < mValues.Length) {
                throw new ArgumentException("The destination array has insufficient capacity.", nameof(array));
            }
            for (var index = 0; index < mValues.Length; index++) {
                array[arrayIndex + index] = new KeyValuePair<string, object?>(mTable.Columns[index].Name, mValues[index]);
            }
        }
        IEnumerator<KeyValuePair<string, object?>> IEnumerable<KeyValuePair<string, object?>>.GetEnumerator() {
            return GetDictionaryEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return GetDictionaryEnumerator();
        }
        bool IDictionary<string, object?>.Remove(string key) {
            throw FixedSchemaException();
        }
        bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) {
            throw FixedSchemaException();
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
        IEnumerator<KeyValuePair<string, object?>> GetDictionaryEnumerator() {
            for (var index = 0; index < mValues.Length; index++) {
                yield return new KeyValuePair<string, object?>(mTable.Columns[index].Name, mValues[index]);
            }
        }
        static NotSupportedException FixedSchemaException() {
            return new NotSupportedException("DBRow has a fixed schema defined by its table.");
        }



    }


}
