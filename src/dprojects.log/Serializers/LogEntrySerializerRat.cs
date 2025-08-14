using DProjects.Utils;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace DProjects.Log.Serializers {


    //class
    public class LogEntrySerializerRat : ILogEntrySerializer {

        public string Serialize(LogEntry logEntry) {
            var result = new StringBuilder();
            result.Append(logEntry.Date.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS));
            result.Append(" [");
            if (logEntry.Level == LogLevel.Trace) {
                result.Append("trace");
            } else if (logEntry.Level == LogLevel.Debug) {
                result.Append("debug");
            } else if (logEntry.Level == LogLevel.Information) {
                result.Append("info");
            } else if (logEntry.Level == LogLevel.Warning) {
                result.Append("warn");
            } else if (logEntry.Level == LogLevel.Error) {
                result.Append("error");
            } else if (logEntry.Level == LogLevel.Fatal) {
                result.Append("fatal");
            }
            if (logEntry.Tags != null) {
                foreach (var tag in logEntry.Tags) {
                    result.Append("|").Append(tag);
                }
            }
            result.Append("] ");
            result.Append(logEntry.Message.Replace("\\", "\\\\").Replace(CharUtils.CHAR_CR.ToString(), "\\r").Replace(CharUtils.CHAR_LF.ToString(), "\\n").Replace("|", "\\u007C"));
            if (logEntry.Source != null && logEntry.Source.Length > 0) {
                result.Append(" | source: ");
                result.Append(logEntry.Source.Replace("|", "\\u007C"));
            }
            if (logEntry.User != null && logEntry.User.Length > 0) {
                result.Append(" | user: ");
                result.Append(logEntry.User.Replace("|", "\\u007C"));
            }
            if (logEntry.SpanId != null && logEntry.SpanId.Length > 0) {
                result.Append(" | spanId: ");
                result.Append(logEntry.SpanId.Replace("|", "\\u007C"));
            }
            if (logEntry.TraceId != null && logEntry.TraceId.Length > 0) {
                result.Append(" | traceId: ");
                result.Append(logEntry.TraceId.Replace("|", "\\u007C"));
            }
            if (logEntry.Fields != null) {
                var keys = new List<string>();
                foreach (string? key in logEntry.Fields.Keys) {
                    if (key != null) keys.Add(key);
                }
                keys.Sort();
                foreach (var key in keys) {
                    result.Append(" | ");
                    result.Append(key);
                    result.Append(": ");
                    var value = logEntry.Fields[key];
                    if (value == null) {
                        value = "";
                    } else if (value.GetType() == typeof(DateTime)) {
                        value = ((DateTime)value).ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS);
                    } else if (value.GetType() == typeof(string)) {
                    } else if (value.GetType().IsValueType) {
                        value = value.ToString();
                    } else {
                        value = System.Text.Json.JsonSerializer.Serialize(value);
                    }
                    result.Append(value.ToString().Replace("\\", "\\\\").Replace(CharUtils.CHAR_CR.ToString(), "\\r").Replace(CharUtils.CHAR_LF.ToString(), "\\n").Replace("|", "\\u007C"));
                }
            }
            return result.ToString();
        }
    }
}

