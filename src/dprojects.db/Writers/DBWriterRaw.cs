using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Db.Writers {


    public class DBWriterRaw : IDBWriter {


        //options
        public class Settings {
            public string DateTimeFormat { get; set; }
            public char ColumnSeparator { get; set; }
            public Settings() {
                DateTimeFormat = DateTimeUtils.DATETIME_ISO8601_MS;
                ColumnSeparator = '\t';
            }
        }


        //variables
        private TextWriter mWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;


        //constructor
        public DBWriterRaw(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
        }
        public DBWriterRaw(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) {
        }
        public void Dispose() {
            if (!mLeaveOpen) {
                mWriter.Dispose();
            }
        }
        public ValueTask DisposeAsync() {
            Dispose();
            return new ValueTask();
        }


        //properties
        public DBColumns Columns => mTable.Columns;


        //sync methods
        public void Write(DBRow row) {
            Write(row.Values);
        }
        public void Write(IDictionary<string, object?> row) {
            Write(new DBRow(mTable, row).Values);
        }
        public void Write(params object?[] values) {
            mWriter.WriteLine(GetRow(values));
        }
        public void Flush() {
            mWriter.Flush();
        }

        //async methods
        public async Task WriteAsync(DBRow row) {
            await WriteAsync(row.Values);
        }
        public async Task WriteAsync(IDictionary<string, object?> row) {
            await WriteAsync(new DBRow(mTable, row).Values);
        }
        public async Task WriteAsync(params object?[] values) {
            await mWriter.WriteLineAsync(GetRow(values));
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }

        //private
        public string GetRow(params object?[] values) {
            var line = new StringBuilder();
            var index = 0;
            foreach (DBColumn dbColumn in mTable.Columns) {
                if (line.Length > 0) line.Append(mSettings.ColumnSeparator);
                if (values[index] != null) {
                    object? value = values[index];
                    if (value == null) {
                    } else if (value is bool) {
                        line.Append((bool)value ? "1" : "0");
                    } else if (value is short || value is int || value is long) {
                        line.Append((System.Convert.ToInt64(value)).ToString());
                    } else if (value is Single) {
                        line.Append((System.Convert.ToSingle(value)).ToString().Replace(",", "."));
                    } else if (value is double) {
                        line.Append((System.Convert.ToSingle(value)).ToString().Replace(",", "."));
                    } else if (value is DateTime) {
                        line.Append(((DateTime)value).ToString(mSettings.DateTimeFormat));
                    } else {
                        line.Append(ConvertUtils.ToSimpleString(value));
                    }
                }
                index++;
            }
            return line.ToString();
        }

    }


}
