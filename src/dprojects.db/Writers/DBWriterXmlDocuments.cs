using DProjects.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Db.Writers {


    public class DBWriterXmlDocuments : IDBWriter {


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
        private TextWriter mWriter;
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;


        //constructor
        public DBWriterXmlDocuments(TextWriter writer, bool leaveOpen, Settings settings) {
            mWriter = writer;
            mTable = new DBTable();
            mSettings = settings;
            mLeaveOpen = leaveOpen;
        }
        public DBWriterXmlDocuments(TextWriter writer, bool leaveOpen) : this(writer, leaveOpen, new Settings()) {
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
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = mSettings.Indent;
            xmlWriterSettings.IndentChars = mSettings.IndentChars;
            xmlWriterSettings.OmitXmlDeclaration = mSettings.OmitXmlDeclaration;
            xmlWriterSettings.CloseOutput = false;
            using (var xmlWriter = XmlWriter.Create(mWriter, xmlWriterSettings)) {
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("document");
                var index = 0;
                foreach (DBColumn dbColumn in mTable.Columns) {
                    xmlWriter.WriteStartElement(dbColumn.Name);
                    object? value = values[index];
                    if (value == null) {
                    } else if (value is DateTime) {
                        xmlWriter.WriteValue(((DateTime)value).ToString(mSettings.DateTimeFormat));
                    } else if (value is IDictionary) {
                        xmlWriter.WriteValue(ConvertUtils.ToSimpleString(value));
                    } else {
                        xmlWriter.WriteValue(value);
                    }
                    xmlWriter.WriteEndElement();
                    index++;
                }
                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndDocument();
            }
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
            var xmlWriterSettings = new XmlWriterSettings();
            xmlWriterSettings.Indent = mSettings.Indent;
            xmlWriterSettings.IndentChars = mSettings.IndentChars;
            xmlWriterSettings.OmitXmlDeclaration = mSettings.OmitXmlDeclaration;
            xmlWriterSettings.CloseOutput = false;
            xmlWriterSettings.Async = true;
            using (var xmlWriter = XmlWriter.Create(mWriter, xmlWriterSettings)) {
                await xmlWriter.WriteStartDocumentAsync();
                xmlWriter.WriteStartElement("document");
                var index = 0;
                foreach (DBColumn dbColumn in mTable.Columns) {
                    xmlWriter.WriteStartElement(dbColumn.Name);
                    object? value = values[index];
                    if (value == null) {
                    } else if (value is DateTime) {
                        xmlWriter.WriteValue(((DateTime)value).ToString(mSettings.DateTimeFormat));
                    } else if (value is IDictionary) {
                        xmlWriter.WriteValue(ConvertUtils.ToSimpleString(value));
                    } else {
                        xmlWriter.WriteValue(value);
                    }
                    await xmlWriter.WriteEndElementAsync();
                    index++;
                }
                await xmlWriter.WriteEndElementAsync();
                await xmlWriter.WriteEndDocumentAsync();
            }
        }
        public async Task FlushAsync() {
            await mWriter.FlushAsync();
        }

    }


}
