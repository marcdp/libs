using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace DProjects.Db.Writers {


    public class DBWriterPlain : IDBWriter {


        //enums
        public enum NewLineModes {
            Default,
            Remove
        }

        //settings
        public class Settings {
            public string DateTimeFormat { get; set; }
            public NewLineModes NewLineMode;
            public bool ColumnNames { get; set; }
            public bool Colorize { get; set; } = false;
            public Settings() {
                DateTimeFormat = DateTimeUtils.DATETIME_ISO8601;
                NewLineMode = NewLineModes.Default;
                ColumnNames = true;
            }
        }


        //variables
        private TextWriter mWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private List<int> mColWidths;
        private int mMaxColumnLength;
        private Settings mSettings;
        private bool mTableWrited;
        private bool mDisposed;


        //constructor
        public DBWriterPlain(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mColWidths = new List<int>();
            mMaxColumnLength = int.MaxValue;
            mSettings = settings;
        }
        public DBWriterPlain(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) {
        }
        public void Dispose() {
            if (!mDisposed) {
                WriteTable();
                mWriter.Flush();
                if (!mLeaveOpen) {
                    mWriter.Dispose();
                }
            }
            mDisposed = true;
        }
        public async ValueTask DisposeAsync() {
            if (!mDisposed) {
                await WriteTableAsync();
                await mWriter.FlushAsync();
                if (!mLeaveOpen) {
                    mWriter.Dispose();
                }
            }
            mDisposed = true;
        }
        

        //properties
        public DBColumns Columns => mTable.Columns;


        //sync methods
        public void Write(params object?[] values) {
            Write(new DBRow(mTable, values));
        }
        public void Write(IDictionary<string, object?> row) {
            Write(new DBRow(mTable, row));
        }
        public void Write(DBRow row) {
            if (!mTableWrited) {
                mTable.Rows.Add(row);
            } else {
                mWriter.WriteLine(GetRow(row));
            }
        }

        //async methods
        public async Task WriteAsync(params object?[] values) {
            await WriteAsync(new DBRow(mTable, values), default);
        }
        public async Task WriteAsync(IDictionary<string, object?> row, CancellationToken cancellationToken) {
            await WriteAsync(new DBRow(mTable, row), default);
        }
        public async Task WriteAsync(DBRow row, CancellationToken cancellationToken) {
            if (!mTableWrited) {
                mTable.Rows.Add(row);
            } else {
                await mWriter.WriteLineAsync(GetRow(row));
            }
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }


        //utils
        private void WriteTable() {
            if (!mTableWrited) {
                mWriter.Write(GetTableHeader());
                foreach (var row in mTable.Rows) {
                    mWriter.WriteLine(GetRow(row));
                }
                mTableWrited = true;
            }
        }
        private async Task WriteTableAsync() {
            if (!mTableWrited) {
                await mWriter.WriteAsync(GetTableHeader());
                foreach (var row in mTable.Rows) {
                    await mWriter.WriteLineAsync(GetRow(row));
                }
                mTableWrited = true;
            }
        }
        private string GetTableHeader() {
            var result = new StringBuilder();
            foreach (DBColumn dbColumn in mTable.Columns) {
                int cw = dbColumn.Name.Length;
                int align = 0;
                foreach (DBRow dbRow in mTable.Rows) {
                    string valueAsString = (dbRow.IsNull(dbColumn)) ? "" : (ToSimpleString(dbColumn, dbRow[dbColumn], ref align));
                    cw = Math.Max(cw, valueAsString.Length);
                }
                cw = Math.Min(cw, mMaxColumnLength);
                mColWidths.Add(cw);
            }
            if (mSettings.ColumnNames) {
                int index = 0;
                var columnNames = new StringBuilder();
                foreach (DBColumn dbColumn in mTable.Columns) {
                    columnNames.Append(string.Format("{0,-" + mColWidths[index] + "}", dbColumn.Name) + "  ");
                    index++;
                }
                if (mSettings.Colorize) {
                    columnNames = new StringBuilder(DProjects.Utils.Highlighters.HighlighterPlain.HighlightHeader(columnNames.ToString()));
                };
                result.AppendLine(columnNames.ToString());
                index = 0;
                var separator = new StringBuilder();
                foreach (DBColumn dbColumn in mTable.Columns) {
                    separator.Append(StringUtils.Space(mColWidths[index], '-') + "  ");
                    index++;
                }
                if (mSettings.Colorize) {
                    separator = new StringBuilder(DProjects.Utils.Highlighters.HighlighterPlain.HighlightSeparator(separator.ToString()));
                } 
                result.AppendLine(separator.ToString());
            }
            var res = result.ToString();
            //if (mSettings.Colorize) {
            //    res = DProjects.Utils.Highlighters.HighlighterPlain.HighlightHeader(res);
            //}
            return res;
        }
        private string GetRow(DBRow dbRow) {
            var line = new StringBuilder();
            var index = 0;
            foreach (DBColumn dbColumn in mTable.Columns) {
                var align = 0;
                if (ReflectionUtils.GetTypeIsNumeric(dbColumn.DBType)) align = 1;
                string valueAsString = (dbRow.IsNull(dbColumn)) ? "" : (ToSimpleString(dbColumn, dbRow[dbColumn], ref align));
                line.Append(string.Format("{0," + (align == 0 ? "-" : "") + mColWidths[index] + "}", valueAsString) + "  ");
                index++;
            }
            var res = line.ToString();
            if (mSettings.Colorize) {
                res = DProjects.Utils.Highlighters.HighlighterPlain.HighlightRow(res);
            }
            return res;
        }
        private string ToSimpleString(DBColumn dbColumn, object? aObject, ref int align) {
            string result = "";
            if (aObject == null) {
                result = "";
            } else if (aObject is bool) {
                result = ((bool)aObject ? "Y" : "N");
            } else if (aObject is Single) {
                result = (System.Convert.ToSingle(aObject)).ToString().Replace(",", ".");
            } else if (aObject is double) {
                result = (System.Convert.ToSingle(aObject)).ToString().Replace(",", ".");
            } else if (aObject is decimal) {
                result = (System.Convert.ToDecimal(aObject)).ToString().Replace(",", ".");
            } else if (aObject is short || aObject is int || aObject is long) {
                long value = System.Convert.ToInt64(aObject);
                if (dbColumn.Format == DBColumnFormat.Filesize) {
                    if (value == -1) return "";
                    return StringUtils.FormatSize(value, false, false);
                } else {
                    result = value.ToString();
                }
            } else if (aObject is DateTime) {
                DateTime value = System.Convert.ToDateTime(aObject);
                if (value == default) {
                    return "";
                }
                result = value.ToUniversalTime().ToString(mSettings.DateTimeFormat);
            } else if (aObject is string[]) {
                string[] value = (string[])aObject;
                result = string.Join(",", value);
            } else if (aObject is object[]) {
                var aux = new StringBuilder();
                foreach (var item in (object[])aObject) {
                    if (aux.Length > 0) aux.Append(",");
                    aux.Append((item == null ? "null" : item.ToString()));
                }
                result = aux.ToString();
            } else {
                //result = aObject.ToString() ?? "";
                result = ConvertUtils.ToSimpleString(aObject);
            }
            if (result != null && result.IndexOf('\n') != -1) {
                if (mSettings.NewLineMode == NewLineModes.Default) {
                    //result = result.Replace("\r\n", "\n"); //use \n always
                    result = result.Replace("\n", "\\n").Replace("\r", "\\r");
                } else if (mSettings.NewLineMode == NewLineModes.Remove) {
                    result = result.Replace("\r\n", " ").Replace("\n", " ");
                }
            }
            if (result != null && result.Length > mMaxColumnLength) {
                result = StringUtils.GetTextCutted(result, mMaxColumnLength, true);
            }
            return result ?? "";
        }
        public void Flush() {
            WriteTable();
            mWriter.Flush();
        }

        
    }


}
