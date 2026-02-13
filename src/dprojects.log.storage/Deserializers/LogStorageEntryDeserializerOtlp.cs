//using System;
//using System.Collections.Generic;
//using System.Text.Json;

//using DProjects.Utils;


//namespace DProjects.Log.Storage.Serializers {


//    //class
//    public class LogStorageEntryDeserializerOtlp : ILogStorageEntryDeserializer {


//        // DOM definition
//        public class ExportLogsServiceRequest {
//            public ResourceLogs[] ResourceLogs { get; set; } = [];
//        }
//        public class ResourceLogs {
//            public Resource? Resource { get; set; }
//            public ScopeLogs[] ScopeLogs { get; set; } = [];
//        }
//        public class Resource {
//            public KeyValue[] Attributes { get; set; } = [];
//        }
//        public class ScopeLogs {
//            public InstrumentationScope? Scope { get; set; }
//            public LogRecord[] LogRecords { get; set; } = [];
//        }
//        public class InstrumentationScope {
//            public string? Name { get; set; }
//            public string? Version { get; set; }
//        }
//        public class LogRecord {
//            public string? TimeUnixNano { get; set; }
//            public string? SeverityText { get; set; }
//            public int SeverityNumber { get; set; }
//            public AnyValue? Body { get; set; }
//            public KeyValue[] Attributes { get; set; } = [];
//            public string? TraceId { get; set; }
//            public string? SpanId { get; set; }
//        }
//        public class KeyValue {
//            public string? Key { get; set; }
//            public AnyValue? Value { get; set; }
//        }
//        public class AnyValue {
//            public string? StringValue { get; set; }
//            // You could expand this to include intValue, boolValue, etc. depending on usage.
//        }


//        // vars
//        private readonly JsonSerializerOptions mJsonSerializerOptions;


//        // ctor
//        public LogStorageEntryDeserializerOtlp() {
//            mJsonSerializerOptions = new JsonSerializerOptions {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//            };
//        }
//        // methods
//        public ExportLogsServiceRequest DeserializeExportLogsServiceRequest(string line) {
//            return System.Text.Json.JsonSerializer.Deserialize<ExportLogsServiceRequest>(line, mJsonSerializerOptions);
//        }
//        public LogEntry Deserialize(string line) {
//            var logEntry = new LogEntry();
//            var jsonDeserializer = new DProjects.Text.Json.JsonDeserializer(new() {
//                UseDateTimeLaxConverter = true,
//            });
//            var request = System.Text.Json.JsonSerializer.Deserialize<ExportLogsServiceRequest>(line, mJsonSerializerOptions);
//            throw new NotImplementedException();

//            //var logEntry = new LogEntry();
//            //if (request != null && request.ResourceLogs != null && request.ResourceLogs.Length > 0) {
//            //    var resourceLogs = request.ResourceLogs[0];
//            //    if (resourceLogs.ScopeLogs != null && resourceLogs.ScopeLogs.Length > 0) {
//            //        var scopeLogs = resourceLogs.ScopeLogs[0];
//            //        if (scopeLogs.LogRecords != null && scopeLogs.LogRecords.Length > 0) {

//            //        }
//            //    }
//            //}
//            //return logEntry;


//            ////var entry = jsonDeserializer.Deserialize<LogEntry>(line);
//            //var dict = jsonDeserializer.Deserialize<IDictionary<string, object?>>(line);
//            ////date
//            //foreach (var key in new string[] { "date", "timestamp", "StartUTC", "time" }) {
//            //    if (dict.TryGetValue(key, out object? value) && value != null && value.GetType() == typeof(DateTime)) {
//            //        logEntry.Date = (DateTime)value;
//            //        dict.Remove(key);
//            //        break;
//            //    }
//            //}
//            ////level
//            //foreach (var key in new string[] { "type", "level" }) {
//            //    if (dict.TryGetValue(key, out object? value)) {
//            //        if (value == null) {
//            //        } else if ("trace".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
//            //            logEntry.Level = LogLevel.Trace;
//            //        } else if ("debug".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
//            //            logEntry.Level = LogLevel.Debug;
//            //        } else if ("information".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "info".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
//            //            logEntry.Level = LogLevel.Information;
//            //        } else if ("warning".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "warn".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
//            //            logEntry.Level = LogLevel.Warning;
//            //        } else if ("error".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "err".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
//            //            logEntry.Level = LogLevel.Error;
//            //        } else if ("fatal".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "severe".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "sev".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
//            //            logEntry.Level = LogLevel.Fatal;
//            //        } else {
//            //            logEntry.Level = LogLevel.Custom;
//            //            dict["type_custom"] = value;
//            //        }
//            //        dict.Remove(key);
//            //        break;
//            //    }
//            //}
//            ////tags
//            //foreach (var key in new string[] { "tags" }) {
//            //    if (dict.TryGetValue(key, out object? value)) {
//            //        logEntry.Tags = ConvertUtils.To<string[]>(value);                   
//            //        dict.Remove(key);
//            //        break;
//            //    }
//            //}
//            ////source
//            //foreach (var key in new string[] { "source" }) {
//            //    if (dict.TryGetValue(key, out object? value)) {
//            //        if (value == null) {
//            //        } else {
//            //            logEntry.Source = value.ToString();
//            //        }
//            //        dict.Remove(key);
//            //        break;
//            //    }
//            //}
//            ////user
//            //foreach (var key in new string[] { "user" }) {
//            //    if (dict.TryGetValue(key, out object? value)) {
//            //        if (value == null) {
//            //        } else {
//            //            logEntry.User = value.ToString();
//            //        }
//            //        dict.Remove(key);
//            //        break;
//            //    }
//            //}
//            ////message
//            //foreach (var key in new string[] { "message", "msg" }) {
//            //    if (dict.TryGetValue(key, out object? value)) {
//            //        if (value == null) {
//            //        } else {
//            //            logEntry.Message = value.ToString();
//            //        }
//            //        dict.Remove(key);
//            //        break;
//            //    }
//            //}
//            ////fields
//            //if (dict.TryGetValue("fields", out object? fields)) {
//            //    if (fields == null) {
//            //    } else {
//            //        logEntry.Fields = (IDictionary<string, object?>)fields;
//            //    }
//            //    dict.Remove("fields");
//            //}
//            //return
//            return logEntry;
//        }
//        public LogEntry? TryDeserialize(string line) {
//            try {
//                return Deserialize(line);
//            } catch {
//                return null;
//            }
//        }
//        public ExportLogsServiceRequest? TryDeserializeExportLogsServiceRequest(string line) {
//            try {
//                return DeserializeExportLogsServiceRequest(line);
//            } catch {
//                return null;
//            }
//        }

//    }

//}

