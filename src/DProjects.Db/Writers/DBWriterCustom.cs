using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace DProjects.Db.Writers {


    public class DBWriterCustom : IDBWriter {


        //delegate
        public delegate string WriteDelegate(DBRow row);


        //options
        public class Settings {
            public Settings() {
            }
        }


        //variables
        private TextWriter mWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private WriteDelegate mWriteDelegate;


        //constructor
        public DBWriterCustom(TextWriter writer, bool leaveOpen, WriteDelegate writeDelegate, Settings settings) {
            mWriter = writer;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mWriteDelegate = writeDelegate;
        }
        public DBWriterCustom(TextWriter writer, bool leaveOpen, WriteDelegate writeDelegate) : this(writer, leaveOpen, writeDelegate, new Settings()) {
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
            mWriter.WriteLine(mWriteDelegate(row));
        }
        public void Write(IDictionary<string, object?> row) {
            mWriter.WriteLine(mWriteDelegate(new DBRow(mTable, row)));
        }
        public void Write(params object?[] values) {
            mWriter.WriteLine(mWriteDelegate(new DBRow(mTable, values)));
        }
        public void Flush() {
            mWriter.Flush();
        }

        //async methods
        public async Task WriteAsync(DBRow row, CancellationToken cancellationToken) {
            await mWriter.WriteLineAsync(mWriteDelegate(row));
        }
        public async Task WriteAsync(IDictionary<string, object?> row, CancellationToken cancellationToken) {
            await mWriter.WriteLineAsync(mWriteDelegate(new DBRow(mTable, row)));
        }
        public async Task WriteAsync(params object?[] values) {
            await mWriter.WriteLineAsync(mWriteDelegate(new DBRow(mTable, values)));
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }

    }


}
