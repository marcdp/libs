using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DProjects.Db.Writers {


    public class DBWriterCsv : IDBWriter {

        //RFC 4180
        //mimetype: text/csv; charset=utf8,header=present

        //options
        public class Settings {
            public char Delimiter { get; set; }
            public string LineTerminator { get; set; }
            public char QuoteChar { get; set; }
            public bool QuoteHeaders { get; set; }
            public bool Header { get; set; }
            public string NullSequence { get; set; }
            public string DateTimeFormat { get; set; }
            public string EscapeChar { get; set; }
            public Settings() {
                Delimiter = ',';
                LineTerminator = "\r\n";
                Header = true;
                QuoteChar = '"';
                QuoteHeaders = true;
                NullSequence = ""; //null values. For example: Null = "\\N";
                EscapeChar = "";
                DateTimeFormat = DateTimeUtils.DATETIME_ISO8601_MS;
            }
        }


        //variables
        protected TextWriter mWriter;
        protected bool mLeaveOpen;
        protected DBTable mTable;
        protected bool mColumnNamesWrited;
        protected Settings mSettings;


        //constructor
        public DBWriterCsv(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mWriter.NewLine = settings.LineTerminator;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            mColumnNamesWrited = false;
        }
        public DBWriterCsv(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) {
        }
        public virtual void Dispose() {
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
            if (mSettings.Header && !mColumnNamesWrited) {
                mWriter.WriteLine(GetColumnNamesString());
                mColumnNamesWrited = true;
            }
            mWriter.WriteLine(GetRowString(values));
        }
        public void Flush() {
            mWriter.Flush();
        }

        //async methods
        public async Task WriteAsync(params object?[] values) {
            if (mSettings.Header && !mColumnNamesWrited) {
                await mWriter.WriteLineAsync(GetColumnNamesString());
                mColumnNamesWrited = true;
            }
            await mWriter.WriteLineAsync(GetRowString(values));
        }
        public async Task WriteAsync(DBRow row) {
            await WriteAsync(row.Values);
        }
        public async Task WriteAsync(IDictionary<string, object?> row) {
            await WriteAsync(new DBRow(mTable, row).Values);
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }


        //private
        private string GetColumnNamesString() {
            var header = new StringBuilder();
            foreach (DBColumn dbColumn in mTable.Columns) {
                if (header.Length > 0) header.Append(mSettings.Delimiter);
                if (mSettings.QuoteHeaders) header.Append('"');
                header.Append(dbColumn.Name);
                if (mSettings.QuoteHeaders) header.Append('"');
            }
            return header.ToString();
        }
        private string GetRowString(object?[] values) {
            //write row
            var line = new StringBuilder();
            var index = 0;
            foreach (DBColumn dbColumn in mTable.Columns) {
                if (line.Length > 0) line.Append(mSettings.Delimiter);
                object? value = values[index];
                if (dbColumn.DBType == typeof(bool)) {
                    if (value != null) {
                        line.Append((bool)value ? "true" : "false");
                    } else {
                        line.Append(mSettings.NullSequence);
                    }
                } else if (dbColumn.DBType == typeof(short) || dbColumn.DBType == typeof(int) || dbColumn.DBType == typeof(long)) {
                    if (value != null) {
                        line.Append(value.ToString());
                    } else {
                        line.Append(mSettings.NullSequence);
                    }
                } else if (dbColumn.DBType == typeof(float) || dbColumn.DBType == typeof(double)) {
                    if (value != null) {
                        if (value is double && (double)value == Double.PositiveInfinity) {
                            line.Append("INF");
                        } else if (value is double && (double)value == Double.NegativeInfinity) {
                            line.Append("-INF");
                        } else if (value is double && (double)value == Double.NaN) {
                            line.Append("NaN");
                        } else if (value is double) {
                            line.Append(((double)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        } else if (value is float) {
                            line.Append(((float)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                    } else {
                        line.Append(mSettings.NullSequence);
                    }
                } else if (dbColumn.DBType == typeof(decimal)) {
                    if (value != null) {
                        if (value is double) {
                            if ((double)value == Double.PositiveInfinity) {
                                line.Append("INF");
                            } else if ((double)value == Double.NegativeInfinity) {
                                line.Append("-INF");
                            } else if ((double)value == Double.NaN) {
                                line.Append("NaN");
                            } else {
                                line.Append(((double)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                            }
                        } else {
                            line.Append(((decimal)value).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        }
                    } else {
                        line.Append(mSettings.NullSequence);
                    }
                } else if (dbColumn.DBType == typeof(object)) {
                    var json = JsonSerializer.Serialize(value);
                    line.Append("\"" + json.Replace("\"", "\"\"") + "\"");
                } else if (dbColumn.DBType == typeof(System.Array)) {
                    var json = JsonSerializer.Serialize(value);
                    line.Append("\"" + json.Replace("\"", "\"\"") + "\"");
                } else if (dbColumn.DBType == typeof(System.DateTime)) {
                    if (value != null) {
                        if (dbColumn.Format == DBColumnFormat.Date) {
                            line.Append(QuoteValue(((DateTime)value).ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_DATE)));
                        } else if (dbColumn.Format == DBColumnFormat.Time) {
                            line.Append(QuoteValue(((DateTime)value).ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_TIME)));
                        } else {
                            line.Append(QuoteValue(((DateTime)value).ToUniversalTime().ToString(mSettings.DateTimeFormat)));
                        }
                    } else {
                        line.Append(mSettings.NullSequence);
                    }
                } else if (dbColumn.DBType == typeof(System.TimeSpan)) {
                    if (value != null) {
                        line.Append(QuoteValue(System.Xml.XmlConvert.ToString((TimeSpan)value)));
                    } else {
                        line.Append(mSettings.NullSequence);
                    }
                } else {
                    if (value != null) {
                        line.Append(QuoteValue(ConvertUtils.ToSimpleString(value)));
                    } else {
                        line.Append(mSettings.NullSequence);
                    }
                }
                index++;
            }
            return line.ToString();
        }
        private string QuoteValue(string value) {
            var result = new StringBuilder(value.Length + 2);
            var useDoubleQuotes = false;
            if (value.Length == 0 || value.IndexOfAny(new char[] { mSettings.Delimiter, mSettings.QuoteChar, CharUtils.CHAR_CR, CharUtils.CHAR_LF }) != -1) useDoubleQuotes = true;
            if (mSettings.EscapeChar.Length > 0) {
                if (value.IndexOf(mSettings.EscapeChar) != -1) value = value.Replace(mSettings.EscapeChar, mSettings.EscapeChar + mSettings.EscapeChar);
                if (value.IndexOf(CharUtils.CHAR_CR) != -1) value = value.Replace(CharUtils.CHAR_CR.ToString(), mSettings.EscapeChar + "r");
                if (value.IndexOf(CharUtils.CHAR_LF) != -1) value = value.Replace(CharUtils.CHAR_LF.ToString(), mSettings.EscapeChar + "n");
                if (value.IndexOf(CharUtils.CHAR_TAB) != -1) value = value.Replace(CharUtils.CHAR_TAB.ToString(), mSettings.EscapeChar + "t");
                if (value.IndexOf(mSettings.QuoteChar) != -1) value = value.Replace(mSettings.QuoteChar.ToString(), mSettings.EscapeChar + mSettings.QuoteChar);
            } else {
                if (value.IndexOf(mSettings.QuoteChar) != -1) value = value.Replace(mSettings.QuoteChar.ToString(), mSettings.QuoteChar.ToString() + mSettings.QuoteChar.ToString());
            }
            if (useDoubleQuotes) result.Append(mSettings.QuoteChar);
            result.Append(value);
            if (useDoubleQuotes) result.Append(mSettings.QuoteChar);
            return result.ToString();
        }

    }


}
