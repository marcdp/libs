using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Db.Readers {


    public class DBReaderJsonLines : IDBReader {


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
        private string? mFirstLine;


        //constructor
        public DBReaderJsonLines(TextReader reader, bool leaveOpen, Settings settings) {
            mReader = reader;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mFirstLine = mReader.ReadLine();
            if (mFirstLine != null) {
                var vo = JsonSerializer.Deserialize<IDictionary<string, object?>>(mFirstLine);
                if (vo != null) {
                    foreach (var key in vo.Keys) {
                        var value = vo[key];
                        var type = (value == null ? typeof(string) : value.GetType());
                        mTable.Columns.Add(key, type);
                    }
                }
            }
        }
        public DBReaderJsonLines(TextReader reader, bool leaveOpen) : this(reader, leaveOpen, new Settings()) {
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
            var line = (mFirstLine != null ? mFirstLine : mReader.ReadLine());
            mFirstLine = null;
            if (line == null) return null;
            var vo = JsonSerializer.Deserialize<IDictionary<string, object?>>(line);
            if (vo == null) return null;
            var values = new List<object?>();
            foreach (var column in mTable.Columns) {
                if (vo.TryGetValue(column.Name, out object? value)) {
                    values.Add(value);
                } else {
                    values.Add(null);
                }
            }
            return new DBRow(mTable, values.ToArray());
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
            var line = (mFirstLine != null ? mFirstLine : await mReader.ReadLineAsync());
            mFirstLine = null;
            if (line == null) return null;
            var vo = JsonSerializer.Deserialize<IDictionary<string, object?>>(line);
            if (vo == null) return null;
            var values = new List<object?>();
            foreach (var column in mTable.Columns) {
                if (vo.TryGetValue(column.Name, out object? value)) {
                    values.Add(value);
                } else {
                    values.Add(null);
                }
            }
            return new DBRow(mTable, values.ToArray());
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


    }


}
