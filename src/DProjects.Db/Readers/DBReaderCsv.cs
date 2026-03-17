using DProjects.Text.Readers;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Db.Readers {


    public class DBReaderCsv : IDBReader {


        //inner classes
        public class Settings {
            public char Delimiter { get; set; }
            public string LineTerminator { get; set; }
            public char QuoteChar { get; set; }
            public bool DoubleQuote { get; set; }
            public bool Header { get; set; }
            public int SkipInitialRows { get; set; }
            public bool SkipInitialSpace { get; set; }
            public string NullSequence { get; set; }
            public string Comment { get; set; }
            public bool IgnoreComments { get; set; }
            public bool IgnoreEmptyLines { get; set; }
            public bool InferDataTypes { get; set; }
            public string EscapeChar { get; set; }
            public Settings() {
                Delimiter = ',';
                LineTerminator = "\r\n";
                QuoteChar = '"';
                DoubleQuote = true;
                Header = true;
                SkipInitialRows = 0; //skip first N rows
                SkipInitialSpace = true; //specifies how to interpret whitespace which immediately follows a delimiter; if false, it means that whitespace immediately after a delimiter should be treated as part of the following field.
                NullSequence = ""; //null sequence. Some possible value: Null = "\\N";
                Comment = "#";
                IgnoreComments = true;
                IgnoreEmptyLines = true;
                InferDataTypes = false;
                EscapeChar = "";
            }
        }
         

        //variables
        private readonly LineReader mReader;
        private readonly Settings mSettings;
        private DBTable? mTable;


        //constructor
        public DBReaderCsv(TextReader reader, bool leaveOpen = false, Settings? settings = null) {
            mReader = new LineReader(reader, leaveOpen);
            mSettings = settings ?? new Settings();
            
        }
        public void Dispose() {
            if (mReader != null) {
                mReader.Dispose();
            }
        }


        //methods sync
        public DBColumns GetColumns() {
            return GetTable().Columns;
        }
        public int GetColumnsCount() {
            return GetTable().Columns.Count;
        }
        public DBRow? Read() {
            var dbTable = GetTable();
            var values = ReadLine(dbTable, new List<string>());
            if (values == null) return null;
            return ValuesToDBRow(dbTable, values);
        }
        public bool Read(object?[] values) {
            throw new NotImplementedException();
        }
        public bool NextResult() {
            return false;
        }


        //methods async
        public async Task<DBColumns> GetColumnsAsync(CancellationToken cancellationToken = default) {
            return (await GetTableAsync(cancellationToken)).Columns;
        }        
        public async Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            var dbTable = await GetTableAsync(cancellationToken);
            var values = await ReadLineAsync(dbTable, new List<string>(), cancellationToken);
            if (values == null) return null;
            return ValuesToDBRow(dbTable,values);
        }
        public Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public Task<bool> NextResultAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(false);
        }


        //private methods sync
        private DBTable GetTable() {
            if (mTable != null) return mTable;
            mTable = new DBTable();
            //skip rows
            for (var i = 0; i < mSettings.SkipInitialRows; i++) {
                mReader.ReadLine();
            }
            //read first line
            if (mSettings.Header) {
                var line = mReader.ReadLine();
                if (line != null) {
                    foreach (string columnName in line.Split(mSettings.Delimiter)) {
                        var dbColumn = new DBColumn(UnquoteValue(columnName));
                        mTable.Columns.Add(dbColumn);
                    }

                }
            } else {
                var lines = new List<string>();
                var value = ReadLine(mTable, lines);
                if (value != null && value.Length > 0) {
                    for (var i = 0; i < value.Length; i++) {
                        var dbColumn = new DBColumn("column" + i);
                        mTable.Columns.Add(dbColumn);
                    }
                    foreach (var line in lines) {
                        mReader.PushBackLine(line);
                    }
                }
            }
            if (mSettings.InferDataTypes) InferDataTypes(mTable);
            return mTable;
        }
        private string[]? ReadLine(DBTable dbTable, List<string> linesReaded) {  //}, CancellationToken cancellationToken = default) {
            //read line
            var values = new List<string>();
            bool insideDoubleQuotes = false;
            bool continueReading = false;
            var previousValue = new StringBuilder();
            do {
                if (insideDoubleQuotes) previousValue.AppendLine();
                //read line
                var line = mReader.ReadLine();
                if (line == null) return null;
                linesReaded.Add(line);
                if (mSettings.IgnoreComments && line.StartsWith(mSettings.Comment) && previousValue.Length == 0 && !insideDoubleQuotes) {
                    //ignore comment
                    continueReading = true;
                } else if (mSettings.IgnoreEmptyLines && line.Length == 0 && previousValue.Length == 0 && !insideDoubleQuotes) {
                    //ignore line
                    continueReading = true;
                } else {
                    //read
                    continueReading = false;
                    for (int i = 0; i <= line.Length - 1; i++) {
                        char c = line[i];
                        char cNext = (i < line.Length - 1 ? line[i + 1] : ' ');
                        if (mSettings.EscapeChar.Length > 0 && mSettings.EscapeChar.Equals(c.ToString())) {
                            if (cNext == '\\') {
                                previousValue.Append("\\");
                            } else if (cNext == 'r') {
                                previousValue.Append("\r");
                            } else if (cNext == 'n') {
                                previousValue.Append("\n");
                            } else if (cNext == '"') {
                                previousValue.Append("\"");
                            }
                            i++;
                        } else if (c == mSettings.QuoteChar && previousValue.Length == 0 && mSettings.DoubleQuote) {
                            insideDoubleQuotes = !insideDoubleQuotes;
                        } else if (c == mSettings.QuoteChar && insideDoubleQuotes && cNext != mSettings.QuoteChar) {
                            insideDoubleQuotes = false;
                        } else if (c == mSettings.QuoteChar && insideDoubleQuotes && cNext == mSettings.QuoteChar) {
                            previousValue.Append(c);
                            i++;
                        } else if (c == mSettings.Delimiter && !insideDoubleQuotes) {
                            values.Add(previousValue.ToString());
                            previousValue.Clear();
                        } else {
                            previousValue.Append(c);
                        }
                    }
                }
            } while (insideDoubleQuotes || continueReading);
            values.Add(previousValue.ToString());
            //remove extra columns
            while (dbTable.Columns.Count != 0 && values.Count > dbTable.Columns.Count) values.RemoveAt(values.Count - 1);
            //add missing columns
            while (dbTable.Columns.Count != 0 && values.Count < dbTable.Columns.Count) values.Add("");
            //return
            return values.ToArray();
        }
        private void InferDataTypes(DBTable dbTable) {
            var lines = new List<string>();
            var values = new List<string[]>();
            var numberOfLines = 10;
            for (var i = 0; i < numberOfLines; i++) {
                var v = ReadLine(dbTable, lines);
                if (v == null) break;
                values.Add(v);
            }
            if (values.Count > 0) {
                var iColumn = 0;
                foreach (var dbColumn in dbTable.Columns) {
                    var types = new Dictionary<Type, int>();
                    foreach (var vs in values) {
                        string? v = vs[iColumn];
                        if (mSettings.NullSequence.Length > 0 && v.Equals(mSettings.NullSequence)) v = null;
                        if (v != null && v.Length != 0) {
                            var aux = StringUtils.InferDataType(v);
                            foreach (var type in aux) {
                                if (types.ContainsKey(type)) {
                                    types[type] = types[type] + 1;
                                } else {
                                    types.Add(type, 1);
                                }
                            }
                        }
                    }
                    if (types.Count == 0) {
                        dbColumn.DBType = typeof(string);
                    } else {
                        foreach (var type in types.Keys) {
                            if (types[type] == values.Count) {
                                dbColumn.DBType = type;
                                break;
                            }
                        }
                    }
                    iColumn++;
                }
            }
            foreach (var line in lines) {
                mReader.PushBackLine(line);
            }
        }

        //private async methods
        private async Task<DBTable> GetTableAsync(CancellationToken cancellationToken = default) {
            if (mTable != null) return mTable;
            mTable = new DBTable();
            //skip rows
            for (var i = 0; i < mSettings.SkipInitialRows; i++) {
                await mReader.ReadLineAsync();
            }
            //read first line
            if (mSettings.Header) {
                var line = await mReader.ReadLineAsync();
                if (line != null) {
                    foreach (string columnName in line.Split(mSettings.Delimiter)) {
                        var dbColumn = new DBColumn(UnquoteValue(columnName));
                        mTable.Columns.Add(dbColumn);
                    }

                }
            } else {
                var lines = new List<string>();
                var value = await ReadLineAsync(mTable, lines, cancellationToken);
                if (value != null && value.Length > 0) {
                    for (var i = 0; i < value.Length; i++) {
                        var dbColumn = new DBColumn("column" + i);
                        mTable.Columns.Add(dbColumn);
                    }
                    foreach (var line in lines) {
                        mReader.PushBackLine(line);
                    }
                }
            }
            if (mSettings.InferDataTypes) await InferDataTypesAsync(mTable);
            return mTable;
        }        
        private async Task<string[]?> ReadLineAsync(DBTable dbTable, List<string> linesReaded, CancellationToken cancellationToken = default) {
            //read line
            var values = new List<string>();
            bool insideDoubleQuotes = false;
            bool continueReading = false;
            var previousValue = new StringBuilder();
            do {
                if (insideDoubleQuotes) previousValue.AppendLine();
                //read line
                var line = await mReader.ReadLineAsync();
                if (line == null) return null;
                linesReaded.Add(line);
                if (mSettings.IgnoreComments && line.StartsWith(mSettings.Comment) && previousValue.Length == 0 && !insideDoubleQuotes) {
                    //ignore comment
                    continueReading = true;
                } else if (mSettings.IgnoreEmptyLines && line.Length == 0 && previousValue.Length == 0 && !insideDoubleQuotes) {
                    //ignore line
                    continueReading = true;
                } else {
                    //read
                    continueReading = false;
                    for (int i = 0; i <= line.Length - 1; i++) {
                        char c = line[i];
                        char cNext = (i < line.Length - 1 ? line[i + 1] : ' ');
                        if (mSettings.EscapeChar.Length > 0 && mSettings.EscapeChar.Equals(c.ToString())) {
                            if (cNext == '\\') {
                                previousValue.Append("\\");
                            } else if (cNext == 'r') {
                                previousValue.Append("\r");
                            } else if (cNext == 'n') {
                                previousValue.Append("\n");
                            } else if (cNext == '"') {
                                previousValue.Append("\"");
                            }
                            i++;
                        } else if (c == mSettings.QuoteChar && previousValue.Length == 0 && mSettings.DoubleQuote) {
                            insideDoubleQuotes = !insideDoubleQuotes;
                        } else if (c == mSettings.QuoteChar && insideDoubleQuotes && cNext != mSettings.QuoteChar) {
                            insideDoubleQuotes = false;
                        } else if (c == mSettings.QuoteChar && insideDoubleQuotes && cNext == mSettings.QuoteChar) {
                            previousValue.Append(c);
                            i++;
                        } else if (c == mSettings.Delimiter && !insideDoubleQuotes) {
                            values.Add(previousValue.ToString());
                            previousValue.Clear();
                        } else {
                            previousValue.Append(c);
                        }
                    }
                }
            } while (insideDoubleQuotes || continueReading);
            values.Add(previousValue.ToString());
            //remove extra columns
            while (dbTable.Columns.Count != 0 && values.Count > dbTable.Columns.Count) values.RemoveAt(values.Count - 1);
            //add missing columns
            while (dbTable.Columns.Count != 0 && values.Count < dbTable.Columns.Count) values.Add("");
            //return
            return values.ToArray();
        }
        private async Task InferDataTypesAsync(DBTable dbTable) {
            var lines = new List<string>();
            var values = new List<string[]>();
            var numberOfLines = 10;
            for (var i = 0; i < numberOfLines; i++) {
                var v = await ReadLineAsync(dbTable, lines);
                if (v == null) break;
                values.Add(v);
            }
            if (values.Count > 0) {
                var iColumn = 0;
                foreach (var dbColumn in dbTable.Columns) {
                    var types = new Dictionary<Type, int>();
                    foreach (var vs in values) {
                        string? v = vs[iColumn];
                        if (mSettings.NullSequence.Length > 0 && v.Equals(mSettings.NullSequence)) v = null;
                        if (v != null && v.Length != 0) {
                            var aux = StringUtils.InferDataType(v);
                            foreach (var type in aux) {
                                if (types.ContainsKey(type)) {
                                    types[type] = types[type] + 1;
                                } else {
                                    types.Add(type, 1);
                                }
                            }
                        }
                    }
                    if (types.Count == 0) {
                        dbColumn.DBType = typeof(string);
                    } else {
                        foreach (var type in types.Keys) {
                            if (types[type] == values.Count) {
                                dbColumn.DBType = type;
                                break;
                            }
                        }
                    }
                    iColumn++;
                }
            }
            foreach (var line in lines) {
                mReader.PushBackLine(line);
            }
        }


        //private methods
        private DBRow ValuesToDBRow(DBTable dbTable, string[] values) {
            DBRow dbRow = dbTable.NewRow();
            for (int i = 0; i < values.Length; i++) {
                string v = UnquoteValue(values[i]);
                Type dbType = dbTable.Columns[i].DBType;
                if (dbType == typeof(string)) {
                    dbRow[i] = (v.Equals(mSettings.NullSequence) && mSettings.NullSequence.Length > 0 ? null : v);
                } else if (dbType == typeof(decimal)) {
                    if (!string.IsNullOrEmpty(v)) {
                        if (v.ToLower().Equals("inf")) {
                            dbRow[i] = Double.PositiveInfinity;
                        } else if (v.ToLower().Equals("-inf")) {
                            dbRow[i] = Double.NegativeInfinity;
                        } else if (v.ToLower().Equals("nan")) {
                            dbRow[i] = Double.NaN;
                        } else {
                            dbRow[i] = decimal.Parse(v, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                } else if (dbType == typeof(double)) {
                    if (!string.IsNullOrEmpty(v)) {
                        if (v.ToLower().Equals("inf")) {
                            dbRow[i] = Double.PositiveInfinity;
                        } else if (v.ToLower().Equals("-inf")) {
                            dbRow[i] = Double.NegativeInfinity;
                        } else if (v.ToLower().Equals("nan")) {
                            dbRow[i] = Double.NaN;
                        } else {
                            dbRow[i] = Double.Parse(v, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);
                        }
                    }
                } else if (dbType == typeof(Single)) {
                    if (!string.IsNullOrEmpty(v)) {
                        if (v.ToLower().Equals("inf")) {
                            dbRow[i] = Single.PositiveInfinity;
                        } else if (v.ToLower().Equals("-inf")) {
                            dbRow[i] = Single.NegativeInfinity;
                        } else if (v.ToLower().Equals("nan")) {
                            dbRow[i] = Single.NaN;
                        } else {
                            dbRow[i] = Single.Parse(v, System.Globalization.NumberStyles.Any, System.Globalization.NumberFormatInfo.InvariantInfo);
                        }
                    }
                } else if (dbType == typeof(int)) {
                    if (!string.IsNullOrEmpty(v)) {
                        if (v.ToLower().Equals("inf")) {
                            dbRow[i] = Double.PositiveInfinity;
                        } else if (v.ToLower().Equals("-inf")) {
                            dbRow[i] = Double.NegativeInfinity;
                        } else if (v.ToLower().Equals("nan")) {
                            dbRow[i] = Double.NaN;
                        } else {
                            dbRow[i] = Int32.Parse(v, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                } else if (dbType == typeof(long)) {
                    if (!string.IsNullOrEmpty(v)) {
                        if (v.ToLower().Equals("inf")) {
                            dbRow[i] = Double.PositiveInfinity;
                        } else if (v.ToLower().Equals("-inf")) {
                            dbRow[i] = Double.NegativeInfinity;
                        } else if (v.ToLower().Equals("nan")) {
                            dbRow[i] = Double.NaN;
                        } else {
                            dbRow[i] = Int64.Parse(v, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                } else if (dbType == typeof(bool)) {
                    if (!string.IsNullOrEmpty(v)) {
                        dbRow[i] = (v.Equals("1") || v.Equals("true", StringComparison.OrdinalIgnoreCase));
                    }
                } else if (dbType == typeof(System.DateTime)) {
                    if (!string.IsNullOrEmpty(v)) {
                        dbRow[i] = DateTimeUtils.Parse(v);
                    }
                } else if (dbType == typeof(System.TimeSpan)) {
                    if (!string.IsNullOrEmpty(v)) {
                        dbRow[i] = System.Xml.XmlConvert.ToTimeSpan(v);
                    }
                } else if (dbType == typeof(System.Array)) {
                    if (!string.IsNullOrEmpty(v)) {
                        var o = JsonSerializer.Deserialize<System.Array>(v);
                        dbRow[i] = o;
                    }
                } else {
                    dbRow[i] = v;
                }
            }
            return dbRow;
        }        
        private string UnquoteValue(string value) {
            var result = value.Trim();
            if (mSettings.SkipInitialSpace && result.StartsWith(" ")) {
                result = result.TrimStart();
            }
            var quoteString = "" + mSettings.QuoteChar;
            if (result.StartsWith(quoteString) && result.EndsWith(quoteString) && result.Length > 1) {
                result = result.Substring(1, result.Length - 2);
                if (mSettings.DoubleQuote) result = result.Replace(mSettings.QuoteChar.ToString() + mSettings.QuoteChar.ToString(), mSettings.QuoteChar.ToString());
            }
            return result;
        }
        

    }


}
