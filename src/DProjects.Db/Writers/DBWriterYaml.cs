using DProjects.Utils;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using DProjects.Text.Yaml;

namespace DProjects.Db.Writers {


    public class DBWriterYaml : IDBWriter {


        //inner classes
        public class Settings : YamlSerializerSettings {
            public bool Colorize { get; set; }
            public Settings() {
            }
        }


        //variables
        private TextWriter mWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private bool mFrontMatter;


        //constructor
        public DBWriterYaml(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mFrontMatter = settings.FrontMatter;
            settings.FrontMatter = false;
            if (mFrontMatter) {
                mWriter.WriteLine("---");
            }
        }
        public DBWriterYaml(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) { }
        public void Dispose() {
            if (mFrontMatter) {
                mWriter.WriteLine("---");
            }
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
            var result = new DProjects.Text.Yaml.YamlSerializer(mSettings).Serialize(new object[] { dict });
            if (mSettings.Colorize) {
                result = ConsoleUtils.ColorizeYaml(result);
            }
            return result;
        }
    }


}
