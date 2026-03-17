using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Db.Readers {


    public class DBReaderYaml : IDBReader {


        //inner classes
        public class Settings {
            public Settings() {
            }
        }


        //variables
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private Queue<Dictionary<string, object?>> mRows;

        //constructor
        public DBReaderYaml(TextReader reader, bool leaveOpen, Settings settings) {
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mRows = new Queue<Dictionary<string, object?>>();
            var yaml = reader.ReadToEnd();
            if (!leaveOpen) reader.Dispose();
            //deserialize
            var deserializer = new YamlDotNet.Serialization.Deserializer();
            var yamlObject = deserializer.Deserialize<object[]>(yaml);
            foreach (Dictionary<object, object> dict in deserializer.Deserialize<object[]>(yaml)) {
                var d = new Dictionary<string, object?>();
                foreach (var key in dict.Keys) {
                    var value = dict[key];
                    var type = (value == null ? typeof(string) : value.GetType());
                    d[key.ToString()] = value;
                }
                mRows.Enqueue(d);
            }
            //get columns
            foreach(var key in mRows.Peek().Keys) {
                var value = mRows.Peek()[key];
                var type = (value == null ? typeof(string) : value.GetType());
                mTable.Columns.Add(key, type);
            }
        }
        public DBReaderYaml(TextReader reader, bool leaveOpen) : this(reader, leaveOpen, new Settings()) {
        }
        public void Dispose() {
        }


        //methods
        public DBColumns GetColumns() {
            return mTable.Columns;
        }
        public int GetColumnsCount() {
            return mTable.Columns.Count;
        }
        public Task<DBColumns> GetColumnsAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(mTable.Columns);
        }
        public DBRow? Read() {
            if (mRows.Count == 0) return null;
            var dict = mRows.Dequeue();
            var dbRow = mTable.NewRow();
            foreach (var key in dict.Keys) {
                dbRow[key] = dict[key];
            }
            return dbRow;
        }
        public bool Read(object?[] values) {
            var dbRow = Read();
            if (dbRow is null) return false;
            for (int i = 0; i < dbRow.Values.Length; i++) {
                values[i] = dbRow.Values[i];
            }
            return true;
        }
        public Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(Read());
        }
        public Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            return Task.FromResult(Read(values));
        }
        public bool NextResult() {
            return false;
        }
        public Task<bool> NextResultAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(NextResult());
        }

    }

}
