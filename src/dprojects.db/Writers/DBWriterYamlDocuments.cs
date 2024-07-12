using DProjects.Utils;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using DProjects.Text.Yaml;

namespace DProjects.Db.Writers {


    public class DBWriterYamlDocuments : IDBWriter {


        //inner classes
        public class Settings : YamlSerializerSettings {
            public enum StreamSeparators {
                Default
            }
            public Settings() {
                StreamSeparator = StreamSeparators.Default;
                //FrontMatter = true;
                //ContentPropertyNames = ["content"];
            }
            public StreamSeparators StreamSeparator { get; set; }
        }


        //variables
        private TextWriter mWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        

        //constructor
        public DBWriterYamlDocuments(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
        }
        public DBWriterYamlDocuments(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) { }
        public void Dispose() {
            mWriter.WriteLine("...");
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
            mWriter.Write(GetRow(values));
        }
        public void Flush() {
            mWriter.Flush();
        }


        //async methods
        public async Task WriteAsync(DBRow row, CancellationToken cancellationToken) {
            await WriteAsync(row.Values);
        }
        public async Task WriteAsync(IDictionary<string, object?> row, CancellationToken cancellationToken) {
            await WriteAsync(new DBRow(mTable, row).Values, cancellationToken);
        }
        public async Task WriteAsync(params object?[] values) {
            await mWriter.WriteAsync(GetRow(values));
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }


        //private methods
        public string GetRow(object?[] values) {
            int index = 0;
            var dict = new Dictionary<string, object?>();
            foreach (var column in mTable.Columns) {
                dict[column.Name] = values[index++];
            }
            var result = new System.Text.StringBuilder();
            if (mSettings.StreamSeparator == Settings.StreamSeparators.Default) {
                result.AppendLine("---");
            }
            result.Append(new DProjects.Text.Yaml.YamlSerializer(mSettings).Serialize(dict));
            return result.ToString();
        }
    }


}
