using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //class
    public class LogEntryReaderClassic : ILogEntryReader {


        //variables
        private TextReader mTextReader;
        private bool mLeaveOpen;

        //constructor
        public LogEntryReaderClassic(TextReader textReader, bool leaveOpen = false) {
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
            //Ex: Information|2018-04-26 00:00:00 34|/campus/campusrpc.ashx?60233135-a6c8-4e5c-a042-a96a2c596e5e|campusrpc:{"id":32,"method":"/quiHiHa/admin.aspx/GetContactesOnline","params":[true],"jsonrpc":"2.0"}|0||350416|
            if (line == null) return null;
            try {
                string[] parts = line.Split('|');
                var logType = LogTypes.Information;
                if (parts[0].Equals("Trace")) logType = LogTypes.Trace;
                if (parts[0].Equals("Debug")) logType = LogTypes.Debug;
                if (parts[0].Equals("Information")) logType = LogTypes.Information;
                if (parts[0].Equals("Warning")) logType = LogTypes.Warning;
                if (parts[0].Equals("Error")) logType = LogTypes.Error;
                if (parts[0].Equals("Critical") || parts[0].Equals("Severe")) logType = LogTypes.Critical;
                var aDate = DateTimeUtils.Parse(parts[1], true).ToUniversalTime();
                string message = parts[3].Replace("\\r", ConstantsUtils.CHAR_CR.ToString()).Replace("\\n", ConstantsUtils.CHAR_LF.ToString()).Replace("\\u007C", "|").Replace("\\\\", "\\");
                var fields = new Dictionary<string, object?>();
                if (parts.Length > 4 && !parts[4].Equals("0")) fields["extra"] = parts[4];
                string source = parts[2];
                string hostname = (parts.Length > 5 ? (parts[5]) : "");
                if (!String.IsNullOrEmpty(hostname)) source += "," + hostname;
                string user = (parts.Length > 6 ? (parts[6]) : "");
                return new LogEntry(logType, message, fields, null, source, user, aDate);
            } catch (Exception e) {
                throw new Exception("Unable to pase log line: " + e.Message + ": " + line, e);
            }
        } 

    }
}

