using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Db.Readers {


    public class DBReaderXml : IDBReader {


        //inner classes
        public class Settings {
            public Settings() {
            }
        }


        //variables
        private bool mLeaveOpen;
        private DBTable mTable;
        private Settings mSettings;
        private XmlDocument mXmlDocument;
        private Queue<XmlElement> mXmlRows;

        //constructor
        public DBReaderXml(TextReader reader, bool leaveOpen, Settings settings) {
            mLeaveOpen = leaveOpen;
            mTable = new DBTable();
            mSettings = settings;
            var xml = reader.ReadToEnd();
            mXmlDocument = new XmlDocument();
            mXmlDocument.LoadXml(xml);
            if (!leaveOpen) reader.Dispose();
            foreach (XmlElement? xmlColumn in mXmlDocument.SelectNodes("/table/columns/column")) {
                if (xmlColumn != null) {
                    var dbColumn = new DBColumn();
                    dbColumn.AutoIncrement = ConvertUtils.ToBoolean(xmlColumn.GetAttribute("auto-increment"));
                    dbColumn.Name = xmlColumn.GetAttribute("name");
                    Type? dbType = System.Type.GetType(xmlColumn.GetAttribute("dbtype"));
                    dbColumn.DBType = (dbType is null ? typeof(object) : dbType);
                    dbColumn.DefaultValue = xmlColumn.GetAttribute("default-value");
                    dbColumn.Description = xmlColumn.GetAttribute("description");
                    if (!string.IsNullOrEmpty(xmlColumn.GetAttribute("format"))) {
                        dbColumn.Format = (DBColumnFormat)System.Enum.Parse(typeof(DBColumnFormat), xmlColumn.GetAttribute("format"));
                    }
                    dbColumn.MaxLength = ConvertUtils.ToInteger(xmlColumn.GetAttribute("max-length"));
                    dbColumn.MinLength = ConvertUtils.ToInteger(xmlColumn.GetAttribute("min-length"));
                    dbColumn.ReadOnly = ConvertUtils.ToBoolean(xmlColumn.GetAttribute("readonly"));
                    dbColumn.Required = ConvertUtils.ToBoolean(xmlColumn.GetAttribute("required"));
                    dbColumn.Title = xmlColumn.GetAttribute("title");
                    dbColumn.Unique = ConvertUtils.ToBoolean(xmlColumn.GetAttribute("unique"));
                    mTable.Columns.Add(dbColumn);
                }
            }
            mXmlRows = new Queue<XmlElement>();
            foreach (XmlElement? xmlRow in mXmlDocument.SelectNodes("/table/rows/row")) {
                if (xmlRow != null) mXmlRows.Enqueue(xmlRow);
            }
        }
        public DBReaderXml(TextReader reader, bool leaveOpen) : this(reader, leaveOpen, new Settings()) {
        }
        public void Dispose() {
        }


        //methods
        public DBColumns GetColumns() {
            return mTable.Columns;
        }
        public int GetColumnsCount() {
            return mTable.Columns.Count;
        }
        public Task<DBColumns> GetColumnsAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(mTable.Columns);
        }
        public DBRow? Read() {
            if (mXmlRows.Count == 0) return null;
            XmlElement xmlRow = mXmlRows.Dequeue();
            var dbRow = mTable.NewRow();
            foreach (XmlElement? xmlChildNode in xmlRow.ChildNodes) {
                if (xmlChildNode != null) {
                    var dbColumn = mTable.Columns[xmlChildNode.Name];
                    if (dbColumn != null) {
                        var value = ConvertUtils.To(xmlChildNode.InnerText, dbColumn.DBType, true);
                        dbRow[dbColumn.Name] = value;
                    }
                }
            }
            return dbRow;
        }
        public bool Read(object?[] values) {
            var dbRow = Read();
            if (dbRow == null) return false;
            for (var i = 0; i < dbRow.Values.Length; i++) {
                values[i] = dbRow.Values[i];
            }
            return true;
        }
        public Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read());
        }
        public Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Read(values));
        }
        public bool NextResult() {
            return false;
        }
        public Task<bool> NextResultAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(NextResult());
        }

    }

}
