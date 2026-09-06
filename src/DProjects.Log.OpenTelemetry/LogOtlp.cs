using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading.Channels;

using DProjects.Utils;
using System.Text.Json.Serialization;
using System.Threading;

namespace DProjects.Log.OpenTelemetry {

    public class LogOtlp : ILog {

        //class
        #region "DOM definition"
        public class ExportLogsServiceRequest {
            public List<ResourceLogs> ResourceLogs { get; set; } = new();
        }
        public class ResourceLogs {
            public Resource? Resource { get; set; }
            public List<ScopeLogs> ScopeLogs { get; set; } = new();
        }
        public class Resource {
            public List<KeyValue> Attributes { get; set; } = new();
        }
        public class ScopeLogs {
            public InstrumentationScope? Scope { get; set; }
            public List<LogRecord> LogRecords { get; set; } = new();
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
            public List<KeyValue> Attributes { get; set; } = new();
            public string? TraceId { get; set; }
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
        #endregion


        //variables
        private readonly LogLevel mLevel;
        private readonly string mHost;
        private readonly int mPort;
        private readonly HttpClient mHttpClient;
        private readonly Channel<LogRecord> mQueue;

        private readonly Task mExportTask;

        private readonly int mMaxWaitTime = 10000;
        private readonly int mMaxBatchSize = 100;


        //constructor
        public LogOtlp(string host, int port) {
            mLevel = LogLevel.Information;
            mHost = host;
            mPort = port;
            mQueue = Channel.CreateUnbounded<LogRecord>();
            mExportTask = Task.Run(() => ExportAsync(default));
            var httpClientHandler = new HttpClientHandler();
            mHttpClient = new HttpClient(httpClientHandler);
            mHttpClient.BaseAddress = new Uri("http://" + host + ":" + port);
        }
        public void Dispose() {
            mQueue.Writer.Complete();
            //wait until complete
        }


        // props
        public LogLevel Level => mLevel;


        //methods
        public void Trace(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
            Enqueue(CreateLogRecord(LogLevel.Trace, message, fields, tags, source, user, resource, spanId, traceId));
        }
        public void Debug(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
            Enqueue(CreateLogRecord(LogLevel.Debug, message, fields, tags, source, user, resource, spanId, traceId));
        }
        public void Info(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
            Enqueue(CreateLogRecord(LogLevel.Information, message, fields, tags, source, user, resource, spanId, traceId));
        }
        public void Warning(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
            Enqueue(CreateLogRecord(LogLevel.Warning, message, fields, tags, source, user, resource, spanId, traceId));
        }
        public void Error(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null, Exception? exception = null) {
            if (exception != null) {
                if (fields == null) fields = new Dictionary<string, object?>();
                fields["exception"] = ExceptionUtils.GetMessageDetailed(exception);
            }
            Enqueue(CreateLogRecord(LogLevel.Error, message, fields, tags, source, user, resource, spanId, traceId));
        }
        public void Fatal(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null, Exception? exception = null) {
            if (exception != null) {
                if (fields == null) fields = new Dictionary<string, object?>();
                fields["exception"] = ExceptionUtils.GetMessageDetailed(exception);
            }
            Enqueue(CreateLogRecord(LogLevel.Fatal, message, fields, tags, source, user, resource, spanId, traceId));
        }
        public void Write(LogEntry logEntry) {
            Enqueue(CreateLogRecord(logEntry.Level, logEntry.Message, logEntry.Fields, logEntry.Tags, logEntry.Source, logEntry.User, logEntry.Resource, logEntry.SpanId, logEntry.TraceId));
        }
          

        //private 
        private LogRecord CreateLogRecord(LogLevel level, string message, IDictionary<string, object?>? fields, string[]? tags, string? source, string? user, string? resource, string? spanId, string? traceId) {
            var logRecord = new LogRecord() {
                TimeUnixNano = DateTime.UtcNow.Ticks.ToString(),
            };
            if (level == LogLevel.Trace) {
                logRecord.SeverityNumber = 1;
                logRecord.SeverityText = "TRACE";
            } else if (level == LogLevel.Debug) {
                logRecord.SeverityNumber = 5;
                logRecord.SeverityText = "DEBUG";
            } else if (level == LogLevel.Information) {
                logRecord.SeverityNumber = 9;
                logRecord.SeverityText = "INFO";
            } else if (level == LogLevel.Warning) {
                logRecord.SeverityNumber = 13;
                logRecord.SeverityText = "WARN";
            } else if (level == LogLevel.Error) {
                logRecord.SeverityNumber = 17;
                logRecord.SeverityText = "ERROR";
            } else if (level == LogLevel.Fatal) {
                logRecord.SeverityNumber = 21;
                logRecord.SeverityText = "FATAL";
            } else if (level == LogLevel.Custom) {
                logRecord.SeverityNumber = 13;
                logRecord.SeverityText = "WARN";
            }
            logRecord.Body = new AnyValue() {
                StringValue = message
            };
            if (fields != null) {
                foreach (var keyPair in fields) {
                    logRecord.Attributes.Add(new KeyValue() {
                        Key = keyPair.Key,
                        Value = new AnyValue() { StringValue = (keyPair.Value ?? "").ToString() }
                    });
                }
            }
            if (tags != null && tags.Length > 0) {
                logRecord.Attributes.Add(new KeyValue() {
                    Key = "tags",
                    Value = new AnyValue() { StringValue = String.Join(",", tags) }
                });
            }
            if (source != null && source.Length > 0) {
                logRecord.Attributes.Add(new KeyValue() {
                    Key = "source",
                    Value = new AnyValue() { StringValue = source }
                }); 
            }
            if (user != null && user.Length > 0) {
                logRecord.Attributes.Add(new KeyValue() {
                    Key = "user",
                    Value = new AnyValue() { StringValue = user }
                });
            }
            if (resource != null) {
                logRecord.Attributes.Add(new KeyValue() {
                    Key = "resource",
                    Value = new AnyValue() { StringValue = resource }
                });
            }
            logRecord.SpanId = spanId;
            logRecord.TraceId = traceId;
            return logRecord;
        }
        private void Enqueue(LogRecord logRecord) {
            mQueue.Writer.TryWrite(logRecord);
        } 
        private async Task ExportAsync(CancellationToken cancellationToken) {
            var buffer = new List<LogRecord>(mMaxBatchSize);
            while (!mQueue.Reader.Completion.IsCompleted) {
                var timeoutTask = Task.Delay(mMaxWaitTime, cancellationToken);
                //clear buffer
                buffer.Clear();
                //read batch
                while (buffer.Count < mMaxBatchSize) {
                    var readTask = mQueue.Reader.WaitToReadAsync(cancellationToken).AsTask();
                    var completedTask = await Task.WhenAny(readTask, timeoutTask);
                    if (completedTask == timeoutTask) {
                        break; // timeout hit
                    }
                    while (mQueue.Reader.TryRead(out var item)) {
                        buffer.Add(item);
                        if (buffer.Count >= mMaxBatchSize)
                            break;
                    }
                }
                //send
                if (buffer.Count > 0) {
                    await SendBatchAsync(buffer.ToArray(), cancellationToken);
                }
            }
            //remaining
            await foreach(var logEntry in mQueue.Reader.ReadAllAsync(cancellationToken)) {
                buffer.Add(logEntry);
            }
            if (buffer.Count > 0) {
                await SendBatchAsync(buffer.ToArray(), cancellationToken);
            }

            //var requestUri = new Uri("/", UriKind.Relative);
            //var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUri);
            //using (var httpResponse = await mHttpClient.SendAsync(httpRequest)) {
            //    var json = await httpResponse.Content.ReadAsStringAsync();
            //    if (httpResponse.StatusCode != System.Net.HttpStatusCode.OK) {
            //        throw new Exception("Unable to restore directory item: " + httpResponse.StatusCode + " (" + json + ")");
            //    }
            //}

        }
        private async Task SendBatchAsync(LogRecord[] records, CancellationToken cancellationToken) {
            // TODO: Implement the logic to send the batch of log records to the OTLP endpoint using mHttpClient.
        }


    }

}

