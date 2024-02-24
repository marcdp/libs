using DProjects.Utils;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Db.Writers {


    public class DBWriterDomain : IDBWriter {


        //settings
        public class Settings {
            public Settings() {
            }
        }


        //variables
        private TextWriter mWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private int mCount;


        //constructor
        public DBWriterDomain(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
        }
        public DBWriterDomain(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) {
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
            mWriter.Write((mCount++ > 0 ? "|" : "") + GetRow(values));
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
            await mWriter.WriteAsync((mCount++ > 0 ? "|" : "") + GetRow(values));
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }

        //private methods
        public string GetRow(params object?[] values) {
            var line = new StringBuilder();
            line.Append(values[0]);
            line.Append("=");
            line.Append(UrlUtils.UrlEncode("" + values[1]));
            for (var index = 2; index < mTable.Columns.Count; index++) {
                var dbColumn = mTable.Columns[index];
                line.Append((index == 2 ? "?" : "&"));
                line.Append(dbColumn.Name);
                line.Append("=");
                line.Append(UrlUtils.UrlEncode(ConvertUtils.ToSimpleString(values[index])));
                index++;
            }
            return line.ToString();
        }
    }


}
