using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Db.Readers {


    public class DBReaderPlain : IDBReader {


        //inner classes
        public class Settings {
            public Settings() {
            }
        }


        //variables
        private TextReader mReader;
        private bool mLeaveOpen;
        private DBTable mTable;
        private List<int> mColWidths;
        private int mLineLength;
        private Settings mSettings;


        //constructor
        public DBReaderPlain(TextReader reader, bool leaveOpen, Settings settings) {
            mReader = reader;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mColWidths = new List<int>();
            string? line = mReader.ReadLine();
            if (line != null) {
                mLineLength = line.Length;
                mTable = new DBTable();
                //parse columns
                while (line.IndexOf("  ") != -1) {
                    line = line.Replace("  ", "|");
                }
                while (line.IndexOf("||") != -1) {
                    line = line.Replace("||", "|");
                }
                foreach (string column in line.Split('|')) {
                    if (column.Trim().Length > 0) {
                        mTable.Columns.Add(new DBColumn(column.Trim()));
                    }
                }
                //read columns length
                line = mReader.ReadLine();
                if (line != null) {
                    string[] aux = line.Replace("  ", "|").Split('|');
                    for (int i = 0; i <= mTable.Columns.Count - 1; i++) {
                        mColWidths.Add(aux[i].Trim().Length);
                    }
                }
            }
        }
        public DBReaderPlain(TextReader reader, bool leaveOpen) : this(reader, leaveOpen, new Settings()) {
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
            string? line = null;
            do {
                line = (line == null ? mReader.ReadLine() : line + "\n" + mReader.ReadLine());
                if (line == null) return null;
            } while (line.Length < mLineLength);
            List<string> values = new List<string>();
            int i = 0;
            foreach (int colWidth in mColWidths) {
                string value = line.Substring(i, colWidth);
                value = value.Trim();
                i += colWidth + 2;
                values.Add(value);
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
        public async Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            string? line = null;
            do {
                cancellationToken.ThrowIfCancellationRequested();
                var nextLine = await mReader.ReadLineAsync();
                cancellationToken.ThrowIfCancellationRequested();
                if (nextLine == null) return null;
                line = (line == null ? nextLine : line + "\n" + nextLine);
            } while (line.Length < mLineLength);
            List<string> values = new List<string>();
            int i = 0;
            foreach (int colWidth in mColWidths) {
                string value = line.Substring(i, colWidth);
                value = value.Trim();
                i += colWidth + 2;
                values.Add(value);
            }
            return new DBRow(mTable, values.ToArray());
        }
        public async Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            var dbRow = await ReadAsync(cancellationToken);
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
