using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading;

using DProjects.Utils;

namespace DProjects.Db.Writers {


    public class DBWriterJsonLines : IDBWriter {

        //inner classes
        public class Settings {
            public bool Colorize { get; set; } = false;
            public bool Indent { get; set; } = false;
            public Settings() {
            }
        }


        //variables
        private TextWriter mWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private JsonSerializerOptions mOptions;


        //constructor
        public DBWriterJsonLines(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mOptions = new System.Text.Json.JsonSerializerOptions() { 
                WriteIndented = mSettings.Indent,
                Converters = {
                    new System.Text.Json.Serialization.JsonStringEnumConverter()
                }
            };
        }
        public DBWriterJsonLines(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) {
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


        //methods
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
        public async Task WriteAsync(DBRow row, CancellationToken cancellationToken) {
            await WriteAsync(row.Values);
        }
        public async Task WriteAsync(IDictionary<string, object?> row, CancellationToken cancellationToken) {
            await WriteAsync(new DBRow(mTable, row).Values, default);
        }
        public async Task WriteAsync(params object?[] values) {
            await mWriter.WriteLineAsync(GetRow(values));
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }


        //private
        public string GetRow(params object?[] values) {
            var aux = new Dictionary<string, object?>();
            int index = 0;
            foreach (var column in mTable.Columns) {
                aux[column.Name] = values[index++];
            }
            var result = JsonSerializer.Serialize(aux, mOptions);
            if (mSettings.Colorize) {
                result = ConsoleUtils.ColorizeJson(result);
            }
            return result;
        }

    }


}
