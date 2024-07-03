using DProjects.Text.Readers;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Db.Readers {


    public class DBReaderAuto : IDBReader {


        //variables
        private readonly LineReader mLineReader;
        private IDBReader? mInnerReader;


        //constructor
        public DBReaderAuto(TextReader reader, bool leaveOpen = false) {
            mLineReader = new LineReader(reader, leaveOpen);
            
        }
        public void Dispose() {
            if (mLineReader != null) {
                mLineReader.Dispose();
            }
        }


        //methods sync
        public DBColumns GetColumns() {
            return GetInnerReader().GetColumns();
        }
        public int GetColumnsCount() {
            return GetInnerReader().GetColumnsCount();
        }
        public DBRow? Read() {
            return GetInnerReader().Read();
        }
        public bool Read(object?[] values) {
            return GetInnerReader().Read(values);
        }
        public bool NextResult() {
            return GetInnerReader().NextResult();
        }


        //methods async
        public async Task<DBColumns> GetColumnsAsync(CancellationToken cancellationToken = default) {
            return await (await GetInnerReaderAsync(cancellationToken)).GetColumnsAsync(cancellationToken);
        }        
        public async Task<DBRow?> ReadAsync(CancellationToken cancellationToken = default) {
            return await (await GetInnerReaderAsync(cancellationToken)).ReadAsync(cancellationToken);
        }
        public async Task<bool> ReadAsync(object?[] values, CancellationToken cancellationToken = default) {
            return await (await GetInnerReaderAsync(cancellationToken)).ReadAsync(values, cancellationToken);
        }
        public async Task<bool> NextResultAsync(CancellationToken cancellationToken = default) {
            return await (await GetInnerReaderAsync(cancellationToken)).NextResultAsync(cancellationToken);
        }


        //private methods sync
        private IDBReader GetInnerReader() {
            if (mInnerReader == null) {
                var line = mLineReader.ReadLine();
                if (line == null) { 
                    mInnerReader = new DBReaderDBTable(new DBTable());
                } else {
                    mLineReader.PushBackLine(line);
                    mInnerReader = CreateInnerReader(line);
                }
            }
            return mInnerReader;
        }
        private async Task<IDBReader> GetInnerReaderAsync(CancellationToken cancellationToken) {
            if (mInnerReader == null) {
                var line = await mLineReader.ReadLineAsync();
                if (line == null) {
                    mInnerReader = new DBReaderDBTable(new DBTable());
                } else {
                    mLineReader.PushBackLine(line);
                    mInnerReader = CreateInnerReader(line);
                }
            }
            return mInnerReader;
        }
        private IDBReader CreateInnerReader(string line) {
            if (line.StartsWith("{")) { 
                return new DBReaderJsonLines(mLineReader, false, new());
            } else if(line.StartsWith("<")) {
                return new DBReaderXml(mLineReader, false, new());
            } else if (line.StartsWith("\"")) {
                return new DBReaderCsv(mLineReader, false, new());
            } else if (line.StartsWith("[")) {
                return new DBReaderJson(mLineReader, false, new());
            } else {
                return new DBReaderPlain(mLineReader, false, new());
            }
        }


    }


}
