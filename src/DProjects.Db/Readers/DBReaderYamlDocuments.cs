using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using DProjects.Utils;

namespace DProjects.Db.Readers {


    public class DBReaderYamlDocuments : IDBReader {


        //inner classes
        public class Settings {
            public Settings() {
            }
        }


        //variables
        private TextReader mReader;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private DBRow? mFirstDBRow;


        //constructor
        public DBReaderYamlDocuments(TextReader reader, bool leaveOpen, Settings settings) {
            mReader = reader;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            if ("---".Equals(mReader.ReadLine())) {
                var yaml = ReadNextYamlDocument();
                if (yaml != null) {
                    var deserializer = new YamlDotNet.Serialization.Deserializer();
                    var dict = deserializer.Deserialize<IDictionary<object, object?>>(yaml);
                    if (dict != null) {
                        var values = new List<object?>();
                        foreach (var key in dict.Keys) {
                            var value = dict[key];
                            var type = (value == null ? typeof(string) : value.GetType());
                            mTable.Columns.Add(key.ToString(), type);
                            values.Add(value);
                        }
                        mFirstDBRow = new DBRow(mTable, values.ToArray());
                    }
                }
            }
        }
        public DBReaderYamlDocuments(TextReader reader, bool leaveOpen) : this(reader, leaveOpen, new Settings()) {
        }
        public void Dispose() {
            if (!mLeaveOpen) {
                mReader.Dispose();
            }
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
            if (mFirstDBRow != null) {
                var dbRow = mFirstDBRow;
                mFirstDBRow = null;
                return dbRow;
            } else {
                var yaml = ReadNextYamlDocument();
                if (yaml == null) return null;
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                var dict = deserializer.Deserialize<IDictionary<object, object?>>(yaml);
                if (dict == null) return null;
                var values = new List<object?>();
                foreach (var column in mTable.Columns) {
                    if (dict.TryGetValue(column.Name, out object? value)) {
                        values.Add(value);
                    } else {
                        values.Add(null);
                    }
                }
                return new DBRow(mTable, values.ToArray());
            }
        }
        public bool Read(object?[] values) {
            var dbRow = Read();
            if (dbRow == null) return false;
            for(var i = 0; i< dbRow.Values.Length; i++) {
                values[i] = dbRow.Values[i];
            }
            return true;
        }
        public async Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            if (mFirstDBRow != null) {                 
                var dbRow = mFirstDBRow;
                mFirstDBRow = null;
                return dbRow;
            } else {
                var yaml = await ReadNextYamlDocumentAsync(cancellationToken);
                if (yaml == null) return null;
                var deserializer = new YamlDotNet.Serialization.Deserializer();
                var dict = deserializer.Deserialize<IDictionary<object, object?>>(yaml);
                if (dict == null) return null;
                var values = new List<object?>();
                foreach (var column in mTable.Columns) {
                    if (dict.TryGetValue(column.Name, out object? value)) {
                        values.Add(value);
                    } else {
                        values.Add(null);
                    }
                }
                return new DBRow(mTable, values.ToArray());
            }
        }
        public async Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            var dbRow = await ReadAsync();
            if (dbRow == null) return false;
            for (var i = 0; i < dbRow.Values.Length; i++) {
                values[i] = dbRow.Values[i];
            }
            return true;
        }
        public bool NextResult() {
            return false;
        }
        public Task<bool> NextResultAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(NextResult());
        }


        //private
        private string? ReadNextYamlDocument() {
            var yaml = new StringBuilder();
            do {
                var line = mReader.ReadLine();
                if (line == null) break;
                if (line.Equals("---")) break;
                if (line.Equals("...")) break;
                yaml.AppendLine(line);
            } while (true);
            if (yaml.Length == 0) return null;
            return yaml.ToString();
        }
        private async Task<string?> ReadNextYamlDocumentAsync(CancellationToken cancellationToken) {
            var yaml = new StringBuilder();
            do {
                var line = await mReader.ReadLineAsync();
                if (line == null) break;
                if (line.Equals("---")) break;
                if (line.Equals("...")) break;
                yaml.AppendLine(line);
            } while (true);
            if (yaml.Length == 0) return null;
            return yaml.ToString();
        } 
    }


}
