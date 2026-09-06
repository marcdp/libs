using System.Threading.Tasks;
using System;
using System.Threading;

namespace DProjects.Db.Readers {


    public class DBReaderDBTable : IDBReader {



        //variables
        private DBTable mTable;
        private int mIndex;

        //constructor
        public DBReaderDBTable(DBTable dbTable) {
            mTable = dbTable;
            mIndex = 0;
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
            if (mIndex < mTable.Rows.Count) {
                return mTable.Rows[mIndex++];
            }
            return null;
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
