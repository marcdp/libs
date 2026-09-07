using System;
using System.Collections;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Db {

    internal sealed class OwnedDbDataReader : DbDataReader {

        // vars
        private readonly DbDataReader mReader;
        private readonly DbCommand mCommand;
        private bool mIsDisposed;

        // props
        public override object this[int ordinal] => mReader[ordinal];
        public override object this[string name] => mReader[name];
        public override int Depth => mReader.Depth;
        public override int FieldCount => mReader.FieldCount;
        public override bool HasRows => mReader.HasRows;
        public override bool IsClosed => mReader.IsClosed;
        public override int RecordsAffected => mReader.RecordsAffected;
        public override int VisibleFieldCount => mReader.VisibleFieldCount;

        // ctor
        public OwnedDbDataReader(DbDataReader reader, DbCommand command) {
            mReader = reader;
            mCommand = command;
        }

        // methods
        public override void Close() {
            Dispose();
        }
        public override bool GetBoolean(int ordinal) {
            return mReader.GetBoolean(ordinal);
        }
        public override byte GetByte(int ordinal) {
            return mReader.GetByte(ordinal);
        }
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) {
            return mReader.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        }
        public override char GetChar(int ordinal) {
            return mReader.GetChar(ordinal);
        }
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) {
            return mReader.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        }
        public override string GetDataTypeName(int ordinal) {
            return mReader.GetDataTypeName(ordinal);
        }
        public override DateTime GetDateTime(int ordinal) {
            return mReader.GetDateTime(ordinal);
        }
        public override decimal GetDecimal(int ordinal) {
            return mReader.GetDecimal(ordinal);
        }
        public override double GetDouble(int ordinal) {
            return mReader.GetDouble(ordinal);
        }
        public override IEnumerator GetEnumerator() {
            return ((IEnumerable)mReader).GetEnumerator();
        }
        public override Type GetFieldType(int ordinal) {
            return mReader.GetFieldType(ordinal);
        }
        public override T GetFieldValue<T>(int ordinal) {
            return mReader.GetFieldValue<T>(ordinal);
        }
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) {
            return mReader.GetFieldValueAsync<T>(ordinal, cancellationToken);
        }
        public override float GetFloat(int ordinal) {
            return mReader.GetFloat(ordinal);
        }
        public override Guid GetGuid(int ordinal) {
            return mReader.GetGuid(ordinal);
        }
        public override short GetInt16(int ordinal) {
            return mReader.GetInt16(ordinal);
        }
        public override int GetInt32(int ordinal) {
            return mReader.GetInt32(ordinal);
        }
        public override long GetInt64(int ordinal) {
            return mReader.GetInt64(ordinal);
        }
        public override string GetName(int ordinal) {
            return mReader.GetName(ordinal);
        }
        public override int GetOrdinal(string name) {
            return mReader.GetOrdinal(name);
        }
        public override DataTable? GetSchemaTable() {
            return mReader.GetSchemaTable();
        }
        public override Stream GetStream(int ordinal) {
            return mReader.GetStream(ordinal);
        }
        public override string GetString(int ordinal) {
            return mReader.GetString(ordinal);
        }
        public override TextReader GetTextReader(int ordinal) {
            return mReader.GetTextReader(ordinal);
        }
        public override object GetValue(int ordinal) {
            return mReader.GetValue(ordinal);
        }
        public override int GetValues(object[] values) {
            return mReader.GetValues(values);
        }
        public override bool IsDBNull(int ordinal) {
            return mReader.IsDBNull(ordinal);
        }
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) {
            return mReader.IsDBNullAsync(ordinal, cancellationToken);
        }
        public override bool NextResult() {
            return mReader.NextResult();
        }
        public override Task<bool> NextResultAsync(CancellationToken cancellationToken) {
            return mReader.NextResultAsync(cancellationToken);
        }
        public override bool Read() {
            return mReader.Read();
        }
        public override Task<bool> ReadAsync(CancellationToken cancellationToken) {
            return mReader.ReadAsync(cancellationToken);
        }

        // methods (private)
        protected override void Dispose(bool disposing) {
            if (!disposing || mIsDisposed) return;
            mIsDisposed = true;
            try {
                mReader.Dispose();
            } finally {
                mCommand.Dispose();
            }
        }
    }
}
