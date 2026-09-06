using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Db.Readers {


    public class DBReaderRaw : IDBReader {


        //inner classes
        public class Settings {
            public char ColumnSeparator { get; set; }
            public Settings() {
                ColumnSeparator = '\t';
            }
        }


        //variables
        private TextReader mReader;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private string? mFirstLine;


        //constructor
        public DBReaderRaw(TextReader reader, bool leaveOpen, Settings settings) {
            mReader = reader;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mFirstLine = mReader.ReadLine();
            if (mFirstLine != null) {
                var values = mFirstLine.Split();
                while (mTable.Columns.Count < values.Length) mTable.Columns.Add("column" + mTable.Columns.Count);
            }
        }
        public DBReaderRaw(TextReader reader, bool leaveOpen) : this(reader, leaveOpen, new Settings()) {
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
            string? line = (mFirstLine != null ? mFirstLine : mReader.ReadLine());
            mFirstLine = null;
            if (line == null) return null;
            return ParseLine(line);
        }
        public bool Read(object?[] values) {
            var dbRow = Read();
            if (dbRow == null) return false;
            for (var i = 0; i < dbRow.Values.Length; i++) {
                values[i] = dbRow.Values[i];
            }
            return true;
        }
        public async Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            string? line = mFirstLine;
            if (line == null) {
                line = await mReader.ReadLineAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }
            mFirstLine = null;
            if (line == null) return null;
            return ParseLine(line);
        }
        public async Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            var dbRow = await ReadAsync(cancellationToken);
            if (dbRow == null) return false;
            for (var i = 0; i < dbRow.Values.Length; i++) {
                values[i] = dbRow.Values[i];
            }
            return true;
        }
        private DBRow ParseLine(string line) {
            var values = line.Split();
            while (values.Length > mTable.Columns.Count) {
                values[values.Length - 2] += mSettings.ColumnSeparator + values[values.Length - 1];
                System.Array.Resize(ref values, values.Length - 1);
            }
            return new DBRow(mTable, values);
        }
        public bool NextResult() {
            return false;
        }
        public Task<bool> NextResultAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(NextResult());
        }


    }


}
