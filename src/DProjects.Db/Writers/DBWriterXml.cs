using DProjects.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Xml;
using System.Xml.Linq;

namespace DProjects.Db.Writers {


    public class DBWriterXml : IDBWriter {


        //options
        public class Settings {
            public string DateTimeFormat { get; set; }
            public bool Indent { get; set; }
            public bool OmitXmlDeclaration { get; set; }
            public string IndentChars { get; set; }
            public Settings() {
                DateTimeFormat = DateTimeUtils.DATETIME_ISO8601_MS;
                OmitXmlDeclaration = false;
                Indent = true;
                IndentChars = "    ";
            }
        }


        //variables
        private XmlWriter mXmlWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private bool mColumnsWrited;


        //constructor
        public DBWriterXml(TextWriter writer, bool leaveOpen, Settings settings) {
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = settings.Indent;
            xmlWriterSettings.IndentChars = settings.IndentChars;
            xmlWriterSettings.OmitXmlDeclaration = settings.OmitXmlDeclaration;
            xmlWriterSettings.CloseOutput = !leaveOpen;
            xmlWriterSettings.Async = true;
            mXmlWriter = XmlWriter.Create(writer, xmlWriterSettings);
            mTable = new DBTable();
            mSettings = settings;
            mLeaveOpen = leaveOpen;
            mXmlWriter.WriteStartDocument();
            mXmlWriter.WriteStartElement("table");
        }
        public DBWriterXml(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) {
        }
        public void Dispose() {
            if (!mColumnsWrited) WriteColumns();
            mXmlWriter.WriteEndElement();
            mXmlWriter.WriteEndElement();
            mXmlWriter.WriteEndDocument();
            mXmlWriter.Flush();
            mXmlWriter.Dispose();
        }
        public async ValueTask DisposeAsync() {
            if (!mColumnsWrited) await WriteColumnsAsync();
            await mXmlWriter.WriteEndElementAsync();
            await mXmlWriter.WriteEndElementAsync();
            await mXmlWriter.WriteEndDocumentAsync();
            await mXmlWriter.FlushAsync();
            mXmlWriter.Dispose();
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
            if (!mColumnsWrited) WriteColumns();
            mXmlWriter.WriteStartElement("row");
            var index = 0;
            foreach (DBColumn dbColumn in mTable.Columns) {
                mXmlWriter.WriteStartElement(dbColumn.Name);
                object? value = values[index];
                if (value == null) {
                } else if (value is DateTime) {
                    mXmlWriter.WriteValue(((DateTime)value).ToString(mSettings.DateTimeFormat));
                } else if (value is IDictionary) {
                    mXmlWriter.WriteValue(ConvertUtils.ToSimpleString(value));
                } else {
                    mXmlWriter.WriteValue(value);
                }
                mXmlWriter.WriteEndElement();
                index++;
            }
            mXmlWriter.WriteEndElement();
        }
        private void WriteColumns() {
            mXmlWriter.WriteStartElement("columns");
            foreach (var dbColumn in mTable.Columns) {
                mXmlWriter.WriteStartElement("column");
                if (dbColumn.AutoIncrement) mXmlWriter.WriteAttributeString("auto-increment", "true");
                mXmlWriter.WriteAttributeString("name", dbColumn.Name);
                mXmlWriter.WriteAttributeString("dbtype", dbColumn.DBType.FullName);
                if (dbColumn.DefaultValue is { }) mXmlWriter.WriteAttributeString("default-value", dbColumn.DefaultValue.ToString());
                if (!string.IsNullOrEmpty(dbColumn.Description)) mXmlWriter.WriteAttributeString("description", dbColumn.Description);
                if (dbColumn.Format != DBColumnFormat.None) mXmlWriter.WriteAttributeString("format", dbColumn.Format.ToString());
                if (dbColumn.MaxLength != 0) mXmlWriter.WriteAttributeString("max-length", dbColumn.MaxLength.ToString());
                if (dbColumn.MinLength != 0) mXmlWriter.WriteAttributeString("min-length", dbColumn.MinLength.ToString());
                if (dbColumn.ReadOnly) mXmlWriter.WriteAttributeString("readonly", "true");
                if (dbColumn.Required) mXmlWriter.WriteAttributeString("required", "true");
                if (!string.IsNullOrEmpty(dbColumn.Title)) mXmlWriter.WriteAttributeString("title", dbColumn.Title);
                if (dbColumn.Unique) mXmlWriter.WriteAttributeString("unique", "true");
                mXmlWriter.WriteEndElement();
            }
            mXmlWriter.WriteEndElement();
            mXmlWriter.WriteStartElement("rows");
            mColumnsWrited = true;

        }
        public void Flush() {
            mXmlWriter.Flush();
        }

        //async methods
        public async Task WriteAsync(DBRow row, CancellationToken cancellationToken) {
            await WriteAsync(row.Values);
        }
        public async Task WriteAsync(IDictionary<string, object?> row, CancellationToken cancellationToken) {
            await WriteAsync(new DBRow(mTable, row).Values, default);
        }
        public async Task WriteAsync(params object?[] values) {
            if (!mColumnsWrited) await WriteColumnsAsync();
            mXmlWriter.WriteStartElement("row");
            var index = 0;
            foreach (DBColumn dbColumn in mTable.Columns) {
                mXmlWriter.WriteStartElement(dbColumn.Name);
                object? value = values[index];
                if (value == null) {
                } else if (value is DateTime) {
                    mXmlWriter.WriteValue(((DateTime)value).ToString(mSettings.DateTimeFormat));
                } else if (value is IDictionary) {
                    mXmlWriter.WriteValue(ConvertUtils.ToSimpleString(value));
                } else {
                    mXmlWriter.WriteValue(value);
                }
                await mXmlWriter.WriteEndElementAsync();
                index++;
            }
            await mXmlWriter.WriteEndElementAsync();
        }
        private async Task WriteColumnsAsync() {
            mXmlWriter.WriteStartElement("columns");
            foreach (var dbColumn in mTable.Columns) {
                mXmlWriter.WriteStartElement("column");
                if (dbColumn.AutoIncrement) mXmlWriter.WriteAttributeString("auto-increment", "true");
                mXmlWriter.WriteAttributeString("name", dbColumn.Name);
                mXmlWriter.WriteAttributeString("dbtype", dbColumn.DBType.FullName);
                if (dbColumn.DefaultValue is { }) mXmlWriter.WriteAttributeString("default-value", dbColumn.DefaultValue.ToString());
                if (!string.IsNullOrEmpty(dbColumn.Description)) mXmlWriter.WriteAttributeString("description", dbColumn.Description);
                if (dbColumn.Format != DBColumnFormat.None) mXmlWriter.WriteAttributeString("format", dbColumn.Format.ToString());
                if (dbColumn.MaxLength != 0) mXmlWriter.WriteAttributeString("max-length", dbColumn.MaxLength.ToString());
                if (dbColumn.MinLength != 0) mXmlWriter.WriteAttributeString("min-length", dbColumn.MinLength.ToString());
                if (dbColumn.ReadOnly) mXmlWriter.WriteAttributeString("readonly", "true");
                if (dbColumn.Required) mXmlWriter.WriteAttributeString("required", "true");
                if (!string.IsNullOrEmpty(dbColumn.Title)) mXmlWriter.WriteAttributeString("title", dbColumn.Title);
                if (dbColumn.Unique) mXmlWriter.WriteAttributeString("unique", "true");
                await mXmlWriter.WriteEndElementAsync();
            }
            await mXmlWriter.WriteEndElementAsync();
            mXmlWriter.WriteStartElement("rows");
            mColumnsWrited = true;
        }
        public async Task FlushAsync() {
            await mXmlWriter.FlushAsync();
        }


    }


}
