using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //class
    public class LogEntryReaderFilter : ILogEntryReader {


        //variables
        private ILogEntryReader mLogEntryReader;
        private LogFilter mLogFilter;


        //constructor
        public LogEntryReaderFilter(ILogEntryReader logEntryReader, LogFilter logFilter) {
            mLogEntryReader = logEntryReader;
            mLogFilter = logFilter;
        }
        public void Dispose() {
            mLogEntryReader.Dispose(); 
        }


        //methods
        public LogEntry? Read() {
            do {
                var logEntry = mLogEntryReader.Read();
                if (logEntry == null) return null;
                if (mLogFilter.Check(logEntry)) return logEntry;
            } while (true);
        }
        public async Task<LogEntry?> ReadAsync(CancellationToken cancellationToken) {
            do {
                var logEntry = await mLogEntryReader.ReadAsync(cancellationToken);
                if (logEntry == null) return null;
                if (mLogFilter.Check(logEntry)) return logEntry;
            } while (true);
        }
    }

}

