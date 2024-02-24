using DProjects.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace DProjects.Db.Writers {


    public class DBWriterHtml : IDBWriter {


        //options
        public class Settings {
            public string DateTimeFormat { get; set; }
            public string TableClassName { get; set; }
            public Settings() {
                DateTimeFormat = DateTimeUtils.DATETIME_ISO8601_MS;
                TableClassName = "";
            }
        }


        //variables
        private TextWriter mWriter;
        private bool mTableHeaderWrited;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;


        //constructor
        public DBWriterHtml(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;

        }
        public DBWriterHtml(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) {
        }
        public void Dispose() {
            if (!mTableHeaderWrited) {
                mWriter.Write(GetTableHeader());
                mTableHeaderWrited = true;
            }
            mWriter.Write(GetTableFooter());
            mWriter.Flush();
            if (!mLeaveOpen) {
                mWriter.Dispose();
            }
        }
        public async ValueTask DisposeAsync() {
            if (!mTableHeaderWrited) {
                await mWriter.WriteAsync(GetTableHeader());
                mTableHeaderWrited = true;
            }
            await mWriter.WriteAsync(GetTableFooter());
            await mWriter.FlushAsync();
            if (!mLeaveOpen) {
                mWriter.Dispose();
            }
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
            if (!mTableHeaderWrited) {
                mWriter.Write(GetTableHeader());
                mTableHeaderWrited = true;
            }
            mWriter.WriteLine(GetTableRow(values));
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
            if (!mTableHeaderWrited) {
                await mWriter.WriteAsync(GetTableHeader());
                mTableHeaderWrited = true;
            }
            await mWriter.WriteLineAsync(GetTableRow(values));
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }

        //private 
        private string GetTableHeader() {
            var html = new StringBuilder();
            html.AppendLine("<table class=\"" + mSettings.TableClassName + "\">");
            html.Append ("<tr>");
            foreach (DBColumn dbColumn in mTable.Columns) {
                html.Append("<th>" + dbColumn.Name + "</th>");
            }
            html.AppendLine("</tr>");
            return html.ToString();
        }
        private string GetTableFooter() {
            return "</table>";
        }
        private string GetTableRow(object?[] values) {
            var line = new StringBuilder();
            line.Append("<tr>");
            var index = 0;
            foreach (DBColumn dbColumn in mTable.Columns) {
                line.Append("<td>");
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
                } else if (value is IDictionary) {
                    var dict = (IDictionary)value;
                    line.Append("<table>");
                    foreach (var key in dict.Keys) {
                        var subValue = dict[key];
                        line.Append("<tr><td>" + key + "</td><td>" + subValue + "</td></tr>");
                    }
                    line.Append("</table>");
                } else {
                    line.Append(ConvertUtils.ToSimpleString(value));
                }
                line.Append("</td>");
                index++;
            }
            line.Append("</tr>");
            return line.ToString();            
        }
    }


}
