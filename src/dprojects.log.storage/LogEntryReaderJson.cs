using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DProjects.Log.Readers {


    //class
    public class LogEntryReaderJson : ILogEntryReader {


        //variables
        private TextReader mTextReader;
        private bool mLeaveOpen;

        //constructor
        public LogEntryReaderJson(TextReader textReader, bool leaveOpen = false) {
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
                var logEntry = new LogEntry();
                var settings = new DProjects.Serialization.JsonDeserializer.Settings();
                settings.UseDateTimeLaxConverter = true;
                var dict = DProjects.Serialization.JsonDeserializer.Deserialize<IDictionary<string, object?>>(line, settings);
                //date
                foreach (var key in new string[] { "date", "timestamp", "StartUTC", "time" }) {
                    if (dict.TryGetValue(key, out object? value) && value != null && value.GetType() == typeof(DateTime)) {
                        logEntry.Date = (DateTime)value;
                        dict.Remove(key);
                        break;
                    }
                }
                //level
                foreach (var key in new string[] { "type", "level" }) {
                    if (dict.TryGetValue(key, out object? value)) {
                        if (value == null) {
                        } else if ("trace".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                            logEntry.LogType = LogTypes.Trace;
                        } else if ("debug".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                            logEntry.LogType = LogTypes.Debug;
                        } else if ("information".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "info".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                            logEntry.LogType = LogTypes.Information;
                        } else if ("warning".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "warn".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                            logEntry.LogType = LogTypes.Warning;
                        } else if ("error".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "err".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                            logEntry.LogType = LogTypes.Error;
                        } else if ("critical".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "severe".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "sev".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                            logEntry.LogType = LogTypes.Critical;
                        } else {
                            logEntry.LogType = LogTypes.Custom;
                            dict["type_custom"] = value;
                        }
                        dict.Remove(key);
                        break;
                    }
                }
                //tags
                foreach (var key in new string[] { "tags" }) {
                    if (dict.TryGetValue(key, out object? value)) {
                        if (value == null) {
                        } else if (value.GetType() == typeof(string[])) {
                            logEntry.Tags = (string[])value;
                        } else {
                            logEntry.Tags = new string[] { value.ToString() };
                        }
                        dict.Remove(key);
                        break;
                    }
                }
                //source
                foreach (var key in new string[] { "source" }) {
                    if (dict.TryGetValue(key, out object? value)) {
                        if (value == null) {
                        } else {
                            logEntry.Source = value.ToString();
                        }
                        dict.Remove(key);
                        break;
                    }
                }
                //user
                foreach (var key in new string[] { "user" }) {
                    if (dict.TryGetValue(key, out object? value)) {
                        if (value == null) {
                        } else {
                            logEntry.User = value.ToString();
                        }
                        dict.Remove(key);
                        break;
                    }
                }
                //message
                foreach (var key in new string[] { "message", "msg" }) {
                    if (dict.TryGetValue(key, out object? value)) {
                        if (value == null) {
                        } else {
                            logEntry.Message = value.ToString();
                        }
                        dict.Remove(key);
                        break;
                    }
                }
                //fields
                logEntry.Fields = dict;
                //return
                return logEntry;
            } catch (Exception e) {
                throw new Exception("Unable to pase log line: " + e.Message + ": " + line, e);
            }
        }

    }
}

