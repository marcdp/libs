using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using VO = System.Collections.Generic.IDictionary<string, object?>;

namespace DProjects.Db.Readers {


    public class DBReaderJson : IDBReader {


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
        private Queue<VO> mVOs;


        //constructor
        public DBReaderJson(TextReader reader, bool leaveOpen, Settings settings) {
            mReader = reader;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mVOs = new Queue<VO>();
            var json = mReader.ReadToEnd();
            if (json.StartsWith("[")) {
                foreach (var vo in JsonSerializer.Deserialize<VO[]>(json)!) {
                    mVOs.Enqueue(vo);
                }
            } else {
                mVOs.Enqueue(JsonSerializer.Deserialize<VO>(json)!);
            }
            if (mVOs.Count > 0) {
                var firstVO = mVOs.Peek();
                foreach (var key in firstVO.Keys) {
                    var value = firstVO[key];
                    var type = (value == null ? typeof(string) : value.GetType());
                    mTable.Columns.Add(key, type);
                }
            }
        }
        public DBReaderJson(TextReader reader, bool leaveOpen) : this(reader, leaveOpen, new Settings()) {
        }
        public void Dispose() {
            if (!mLeaveOpen && mReader != null) {
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
            if (mVOs.Count == 0) return null;
            var vo = mVOs.Dequeue();
            var values = new List<object?>();
            foreach (var column in mTable.Columns) {
                if (vo.TryGetValue(column.Name, out object? subValue)) {
                    values.Add(subValue);
                } else {
                    values.Add(null);
                }
            }
            return new DBRow(mTable, values.ToArray());
        }
        public bool Read(object?[] values) {
            var dbRow = Read();
            if (dbRow == null) return false;
            for (var i = 0; i < dbRow.Values.Length; i++) {
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
