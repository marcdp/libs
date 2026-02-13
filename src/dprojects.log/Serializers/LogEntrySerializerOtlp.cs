using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace DProjects.Log.Serializers {


    //class
    public class LogEntrySerializerOtlp : ILogEntrySerializer {


        // DOM definition
        public class ExportLogsServiceRequest {
            public ResourceLogs[] ResourceLogs { get; set; } = [];
        }
        public class ResourceLogs {
            public Resource? Resource { get; set; }
            public ScopeLogs[] ScopeLogs { get; set; } = [];
        }
        public class Resource {
            public KeyValue[] Attributes { get; set; } = [];
        }
        public class ScopeLogs {
            public InstrumentationScope? Scope { get; set; }
            public LogRecord[] LogRecords { get; set; } = [];
        }
        public class InstrumentationScope {
            public string? Name { get; set; }
            public string? Version { get; set; }
        }
        public class LogRecord {
            public string? TimeUnixNano { get; set; }
            public string? SeverityText { get; set; }
            public int SeverityNumber { get; set; }
            public AnyValue? Body { get; set; }
            public KeyValue[] Attributes { get; set; } = [];
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? TraceId { get; set; }
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string? SpanId { get; set; }
        }
        public class KeyValue {
            public string? Key { get; set; }
            public AnyValue? Value { get; set; }
        }
        public class AnyValue {
            public string? StringValue { get; set; }
            // You could expand this to include intValue, boolValue, etc. depending on usage.
        }


        //vars
        private readonly string mServiceName;
        private readonly string mScopeName;
        private readonly JsonSerializerOptions mJsonSerializerOptions;


        //ctor
        public LogEntrySerializerOtlp(string serviceName, string scopeName) {
            mServiceName = serviceName;
            mScopeName = scopeName;
            mJsonSerializerOptions = new JsonSerializerOptions {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }


        //public
        public LogRecord CreateLogRecord(LogEntry logEntry) {
            var logRecord = new LogRecord() {
                TimeUnixNano = DProjects.Utils.DateTimeUtils.ToUnixTimeNanoseconds(logEntry.Date).ToString(),
            };
            if (logEntry.Level == LogLevel.Trace) {
                logRecord.SeverityNumber = 1;
                logRecord.SeverityText = "TRACE";
            } else if (logEntry.Level == LogLevel.Debug) {
                logRecord.SeverityNumber = 5;
                logRecord.SeverityText = "DEBUG";
            } else if (logEntry.Level == LogLevel.Information) {
                logRecord.SeverityNumber = 9;
                logRecord.SeverityText = "INFO";
            } else if (logEntry.Level == LogLevel.Warning) {
                logRecord.SeverityNumber = 13;
                logRecord.SeverityText = "WARN";
            } else if (logEntry.Level == LogLevel.Error) {
                logRecord.SeverityNumber = 17;
                logRecord.SeverityText = "ERROR";
            } else if (logEntry.Level == LogLevel.Fatal) {
                logRecord.SeverityNumber = 21;
                logRecord.SeverityText = "FATAL";
            } else if (logEntry.Level == LogLevel.Custom) {
                logRecord.SeverityNumber = 13;
                logRecord.SeverityText = "WARN";
            }
            logRecord.Body = new AnyValue() {
                StringValue = logEntry.Message
            };
            var attributes = new List<KeyValue>();
            if (logEntry.Fields != null) {
                foreach (var keyPair in logEntry.Fields) {
                    attributes.Add(new KeyValue() {
                        Key = keyPair.Key,
                        Value = new AnyValue() { StringValue = (keyPair.Value ?? "").ToString() }
                    });
                }
            }
            if (logEntry.Tags != null && logEntry.Tags.Length > 0) {
                attributes.Add(new KeyValue() {
                    Key = "tags",
                    Value = new AnyValue() { StringValue = String.Join(",", logEntry.Tags) }
                });
            }
            if (logEntry.Source != null && logEntry.Source.Length > 0) {
                attributes.Add(new KeyValue() {
                    Key = "source",
                    Value = new AnyValue() { StringValue = logEntry.Source }
                });
            }
            if (logEntry.User != null && logEntry.User.Length > 0) {
                attributes.Add(new KeyValue() {
                    Key = "user",
                    Value = new AnyValue() { StringValue = logEntry.User }
                });
            }
            if (logEntry.Resource != null) {
                attributes.Add(new KeyValue() {
                    Key = "resource",
                    Value = new AnyValue() { StringValue = logEntry.Resource }
                });
            }
            logRecord.Attributes = attributes.ToArray();
            logRecord.SpanId = logEntry.SpanId;
            logRecord.TraceId = logEntry.TraceId;
            return logRecord;
        }
        public ExportLogsServiceRequest CreateExportLogsServiceRequest(LogRecord[] logRecords) {
            var result = new ExportLogsServiceRequest() {
                ResourceLogs = new ResourceLogs[] {
                    new ResourceLogs() {
                        Resource = new Resource() {
                            Attributes = new KeyValue[] {
                                new KeyValue() { Key = "service.name", Value = new AnyValue() { StringValue = mServiceName } }
                            }
                        },
                        ScopeLogs = new ScopeLogs[] {
                            new ScopeLogs() {
                                Scope = new InstrumentationScope() {
                                    Name = mScopeName,
                                    Version = "1.0.0"
                                },
                                LogRecords = logRecords
                            }
                        }
                    }
                }
            };
            return result;
        }
        public string Serialize(LogEntry entry) {
            var logRecord = CreateLogRecord(entry);
            var request = CreateExportLogsServiceRequest(new LogRecord[] { logRecord });
            return System.Text.Json.JsonSerializer.Serialize(request, mJsonSerializerOptions);
        }
    }
}

