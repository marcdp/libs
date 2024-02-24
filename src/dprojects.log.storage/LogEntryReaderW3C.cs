using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //class
    public class LogEntryReaderW3C : ILogEntryReader {


        //variables
        private TextReader mTextReader;
        private bool mLeaveOpen;
        private string[]? mW3ExtendedLogFields;

        //constructor
        public LogEntryReaderW3C(TextReader textReader, bool leaveOpen = false) {
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
            try {
                while (line.StartsWith("#")) {
                    if (line.StartsWith("#Fields:")) {
                        mW3ExtendedLogFields = line.Substring(line.IndexOf(":") + 1).Trim().Split(' ');
                    }
                    line = mTextReader.ReadLine();
                    if (line == null) return null;
                }
                var columns = new StringDictionary();
                var values = line.Split(' ');
                if (mW3ExtendedLogFields != null) {
                    for (var i = 0; i < mW3ExtendedLogFields.Length; i++) {
                        if (i < values.Length) {
                            if (values[i].Equals("-")) values[i] = "";
                            columns[mW3ExtendedLogFields[i]] = values[i];
                        }
                    }
                }
                var message = columns["cs-method"] + " " + columns["cs-uri-stem"] + (columns["cs-uri-query"].Equals("") ? "" : "?") + columns["cs-uri-query"];
                var logType = LogTypes.Information;
                if (int.TryParse(columns["sc-status"], out int status)) {
                    if (status >= 500) {
                        logType = LogTypes.Error;
                    } else if (status >= 400) {
                        logType = LogTypes.Warning;
                    }
                }
                var user = columns["cs-username"];
                if (user.Equals("-")) user = "";
                var location = columns["cs-uri-stem"];
                var hostname = "" + columns["s-ip"];
                var aDate = DateTime.Parse(columns["date"] + " " + columns["time"]);
                columns.Remove("date");
                columns.Remove("time");
                var source = columns["s-computername"];
                if (source.Equals("-")) source = "";
                var fields = new Dictionary<string, object?>();
                foreach (var key in columns.Keys) {
                    fields[key.ToString()] = columns[key.ToString()];
                }
                return new LogEntry(logType, message, fields, null, source, user, aDate);
            } catch (Exception e) {
                throw new Exception("Unable to pase log line: " + e.Message + ": " + line, e);
            }
        }

    }
}

