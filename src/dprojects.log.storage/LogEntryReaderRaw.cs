using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //class
    public class LogEntryReaderRaw : ILogEntryReader {


        //variables
        private TextReader mTextReader;
        private bool mLeaveOpen;


        //constructor
        public LogEntryReaderRaw(TextReader textReader, bool leaveOpen = false) {
            mTextReader = textReader;
            mLeaveOpen = leaveOpen;

        }
        public void Dispose() {
            if (!mLeaveOpen) {
                mTextReader.Close();
            }
        }


        //methods
        public LogEntry? Read() {
            return ParseLine(mTextReader.ReadLine());
        }
        public async Task<LogEntry?> ReadAsync(CancellationToken cancellationToken) {
            return ParseLine(await mTextReader.ReadLineAsync());
        }


        //private
        private LogEntry? ParseLine(string? line) {
            if (line == null) return null;
            return new LogEntry(LogTypes.Information, line, (IDictionary<string, object?>?)null, null, null, null, DateTime.Now);
        }
    }
}

