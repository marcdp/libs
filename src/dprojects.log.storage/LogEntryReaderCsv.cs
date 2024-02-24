using DProjects.Db;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //class
    public class LogEntryReaderCsv : ILogEntryReader {


        //variables
        private IDBReader mDBReader;

        //constructor
        public LogEntryReaderCsv(TextReader textReader, bool leaveOpen = false, Db.Readers.DBReaderCsv.Settings? settings = null) {
            if (settings == null) {
                settings = new Db.Readers.DBReaderCsv.Settings();
                settings.EscapeChar = "\\";
            }
            mDBReader = new Db.Readers.DBReaderCsv(textReader, leaveOpen, settings);
        }
        public void Dispose() {
            mDBReader.Dispose();
        }

        //methods
        public LogEntry? Read() {
            return ParseDBRow(mDBReader.Read());
        }
        public async Task<LogEntry?> ReadAsync(CancellationToken cancellationToken) {
            return ParseDBRow(await mDBReader.ReadAsync(cancellationToken));
        }

        //private
        private LogEntry? ParseDBRow(DBRow? dbRow) {
            if (dbRow == null) return null;
            var logEntry = new LogEntry();
            //"timestamp","type","source","user","message","tags","fields"
            logEntry.Date = dbRow.GetAs<DateTime>("timestamp");
            logEntry.LogType = ConvertUtils.To<LogTypes>(dbRow.GetAs<string>("type"));
            logEntry.Source = dbRow.GetAs<string>("source");
            logEntry.User = dbRow.GetAs<string>("user");
            logEntry.Message = dbRow.GetAs<string>("message");
            logEntry.Tags = dbRow.GetAs<string[]>("tags");
            var fields = dbRow.GetAs<string>("fields");
            logEntry.Fields = DProjects.Serialization.JsonDeserializer.Deserialize<IDictionary<string, object?>>(fields);
            return logEntry;
        }

    }
}

