using DProjects.Text.Readers;
using DProjects.Utils;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace DProjects.Db.Readers {


    public class DBReaderXmlDocuments : IDBReader {


        //inner classes
        public class Settings {
            public bool InferDataTypes { get; set; }
            public Settings() {
                InferDataTypes = true;
            }
        }


        //variables
        private XmlDocumentsReader mXmlDocumentsReader;
        private XmlDocument? mFirstXmlDocument;
        private DBTable mTable;


        //constructor
        public DBReaderXmlDocuments(TextReader reader, bool leaveOpen, Settings settings) {
            mXmlDocumentsReader = new XmlDocumentsReader(reader, leaveOpen);
            mTable = new DBTable();
            mFirstXmlDocument = mXmlDocumentsReader.Read();
            if (mFirstXmlDocument != null) {
                foreach (XmlElement? xmlColumn in mFirstXmlDocument.DocumentElement.SelectNodes("//*")) {
                    if (xmlColumn != null && mFirstXmlDocument.DocumentElement != xmlColumn) {
                        var dbColumn = new DBColumn();
                        dbColumn.Name = xmlColumn.Name;
                        mTable.Columns.Add(dbColumn);
                    }
                }
            }
        }
        public DBReaderXmlDocuments(TextReader reader, bool leaveOpen) : this(reader, leaveOpen, new Settings()) {
        }
        public void Dispose() {
            mXmlDocumentsReader.Dispose();
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
            var xmlDocument = mFirstXmlDocument;
            mFirstXmlDocument = null;
            if (xmlDocument == null) xmlDocument = mXmlDocumentsReader.Read();
            if (xmlDocument == null) return null;
            var dbRow = mTable.NewRow();
            foreach (XmlElement? xmlChildNode in xmlDocument.DocumentElement.ChildNodes) {
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
            throw new NotImplementedException();
        }
        public Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            throw new NotImplementedException();
        }
        public bool NextResult() {
            return false;
        }
        public Task<bool> NextResultAsync(CancellationToken cancellationToken = default) {
            return Task.FromResult(NextResult());
        }

    }


}
