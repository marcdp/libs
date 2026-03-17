
using DProjects.Factories;
using DProjects.Factories.Attributes;
using DProjects.Utils;
using DProjects.Text.Expressions;

using System;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.IO;

namespace DProjects.Db.Readers {

    public class DBReaderView : IDBReader {


        //inner class
        private class ColumnDef {
            public string SourceColumnName;
            public string DestinationColumnName;
            public string? Expression;
            public Expression? ExpressionEval;
            public ColumnDef(string definition) {
                SourceColumnName = definition;
                DestinationColumnName = definition;
                if (definition.IndexOf("=") != -1) {
                    DestinationColumnName = definition.Substring(0, definition.IndexOf("="));
                    SourceColumnName = definition.Substring(definition.IndexOf("=") + 1);
                }
                if (SourceColumnName.StartsWith("expression:")) {
                    Expression = SourceColumnName.Substring(SourceColumnName.IndexOf(":") + 1).Replace("@", "$");
                    ExpressionEval = new Expression(Expression);
                    SourceColumnName = "";
                }
            }
        }


        //variables
        private IDBReader mDBReader;
        private ColumnDef[] mColumnDefs;
        private DBTable mTable;
        private Expression mExpression;
        private bool mLeaveOpen;
        private long mOffset;
        private long mLimit;
        private long mCount;


        //constructor
        public DBReaderView(IDBReader dbReader, string[] columns, string where, object[] values, string orderBy, bool leaveOpen, long offset = 0, long limit = 0) {
            mDBReader = dbReader;
            if (orderBy.Length != 0) {
                var table = DBTable.FromDBReader(dbReader);
                if (!leaveOpen) dbReader.Dispose();
                table.Rows.Sort(orderBy);
                mDBReader = new DBReaderDBTable(table);
            }
            mTable = new DBTable();
            var columnDefs = new List<ColumnDef>();
            if (columns.Length == 0) {
                var aux = new List<string>();
                foreach (var dbColumn in dbReader.GetColumns()) {
                    aux.Add(dbColumn.Name);
                }
                columns = aux.ToArray();
            } else if (columns.Length == 1 && columns[0] == "*") {
                var aux = new List<string>();
                foreach (var dbColumn in dbReader.GetColumns()) {
                    aux.Add(dbColumn.Name);
                }
                columns = aux.ToArray();
            } else {
                for (var i = 0; i < columns.Length; i++) {
                    if (StringUtils.IsNumeric(columns[i]) && !dbReader.GetColumns().Contains(columns[i])) {
                        columns[i] = dbReader.GetColumns()[i].Name;
                    }
                }
            }
            foreach (var column in columns) {
                var columnDef = new ColumnDef(column.Trim());
                if (columnDef.Expression != null) {
                    var dbColumnNew = new DBColumn(columnDef.DestinationColumnName);
                    mTable.Columns.Add(dbColumnNew);
                    columnDefs.Add(columnDef);
                } else if (!dbReader.GetColumns().Contains(columnDef.SourceColumnName)) {
                    throw new Exception("column not found: " + columnDef.SourceColumnName);
                } else {
                    var dbColumnCloned = dbReader.GetColumns()[columnDef.SourceColumnName].Clone();
                    dbColumnCloned.Name = columnDef.DestinationColumnName;
                    mTable.Columns.Add(dbColumnCloned);
                    columnDefs.Add(columnDef);
                }
            }
            mColumnDefs = columnDefs.ToArray();
            mExpression = new Expression(where, values);
            mLeaveOpen = leaveOpen;
            mOffset = offset;
            mLimit = limit;
        }
        public DBReaderView(DBTable dbTable, string[] columns, string where, object[] values, string orderBy, bool leaveOpen) : this(new DBReaderDBTable(dbTable), columns, where, values, orderBy, leaveOpen) {
        }
        public void Dispose() {
            if (!mLeaveOpen && mDBReader != null) {
                mDBReader.Dispose();
            }
        }


        //properties
        public long Count => mCount;


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
            if (mLimit > 0 && mCount >= mOffset + mLimit) {
                while (mDBReader.Read() != null) {
                    mCount++;
                }
                return null;
            }
            DBRow? dbRow = null;
            while ((dbRow = mDBReader.Read()) != null) {
                if (mExpression.IsEmpty || mExpression.Eval<bool>(dbRow)) {
                    var values = new List<object?>();
                    foreach (var columnDef in mColumnDefs) {
                        if (columnDef.ExpressionEval != null) {
                            values.Add(columnDef.ExpressionEval.Eval(dbRow));
                        } else {
                            values.Add(dbRow[columnDef.SourceColumnName]);
                        }
                    }
                    mCount++;
                    if (mCount <= mOffset) continue;
                    return new DBRow(mTable, values.ToArray());
                }
            };
            return null;
        }
        public bool Read(object?[] values) {
            throw new NotImplementedException();
        }
        public async Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            if (mLimit > 0 && mCount >= mOffset + mLimit) {
                while (await mDBReader.ReadAsync() != null) {
                    mCount++;
                }
                return null;
            }
            DBRow? dbRow = null;
            while ((dbRow = await mDBReader.ReadAsync()) != null) {
                if (mExpression.IsEmpty || mExpression.Eval<bool>(dbRow)) {
                    var values = new List<object?>();
                    foreach (var columnDef in mColumnDefs) {
                        if (columnDef.ExpressionEval != null) {
                            values.Add(columnDef.ExpressionEval.Eval(dbRow));
                        } else {
                            values.Add(dbRow[columnDef.SourceColumnName]);
                        }
                    }
                    mCount++;
                    if (mCount <= mOffset) continue;
                    return new DBRow(mTable, values.ToArray());
                }
            };
            return null;
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
