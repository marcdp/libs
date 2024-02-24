using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //class
    public class LogEntryReaderRat : ILogEntryReader {


        //variables
        private TextReader mTextReader;
        private bool mLeaveOpen;

        //constructor
        public LogEntryReaderRat(TextReader textReader, bool leaveOpen = false) {
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
                var aDate = DateTime.Parse(line.Substring(0, 24)).ToUniversalTime();
                var i = line.IndexOf("] ");
                if (i == -1) throw new InvalidDataException("expected ' ['");
                var logType = LogTypes.Information;
                var tags = new List<string>(line.Substring(26, i - 26).Split('|'));
                if (tags.Contains("trace")) {
                    logType = LogTypes.Trace;
                    tags.Remove("trace");
                } else if (tags.Contains("debug")) {
                    logType = LogTypes.Debug;
                    tags.Remove("debug");
                } else if (tags.Contains("info")) {
                    logType = LogTypes.Information;
                    tags.Remove("info");
                } else if (tags.Contains("warn")) {
                    logType = LogTypes.Warning;
                    tags.Remove("warn");
                } else if (tags.Contains("error")) {
                    logType = LogTypes.Error;
                    tags.Remove("error");
                } else if (tags.Contains("critical")) {
                    logType = LogTypes.Critical;
                    tags.Remove("critical");
                } else if (tags.Contains("severe")) {
                    logType = LogTypes.Critical;
                    tags.Remove("severe");
                }
                var message = line.Substring(i + 2);
                var source = "";
                var user = "";
                var fields = new Dictionary<string, object?>();
                var j = message.IndexOf("|");
                if (j != -1) {
                    foreach (var field in message.Substring(j + 1).Split('|')) {
                        if (field.IndexOf(":") != -1) {
                            var fieldName = field.Substring(0, field.IndexOf(":")).Trim();
                            var fieldValue = field.Substring(field.IndexOf(":") + 1).Trim();
                            fieldValue = fieldValue.Replace("\\u007C", "|").Replace("\\n", ConstantsUtils.CHAR_LF.ToString()).Replace("\\r", ConstantsUtils.CHAR_CR.ToString()).Replace("\\\\", "\\");
                            if (fieldName.Equals("source")) {
                                source = fieldValue;
                            } else if (fieldName.Equals("user")) {
                                user = fieldValue;
                            } else {
                                fields[fieldName] = fieldValue;
                            }
                        }
                    }
                    message = message.Substring(0, j);
                }
                message = message.Replace("\\u007C", "|").Replace("\\n", ConstantsUtils.CHAR_LF.ToString()).Replace("\\r", ConstantsUtils.CHAR_CR.ToString()).Replace("\\\\", "\\").TrimEnd();
                return new LogEntry(logType, message, fields, tags.ToArray(), source, user, aDate);
            } catch (Exception e) {
                throw new Exception("Unable to pase log line: " + e.Message + ": " + line, e);
            }
        } 

    }
}

