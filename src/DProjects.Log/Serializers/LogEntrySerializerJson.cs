using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;


namespace DProjects.Log.Serializers {


    //class
    public class LogEntrySerializerJson : ILogEntrySerializer {


        //private 
        public string Serialize(LogEntry entry) {
            var dict = new Dictionary<string, object?>();
            dict["timestamp"] = entry.Date.ToUniversalTime().ToString(DateTimeUtils.DATETIME_ISO8601_MS7);
            dict["level"] = entry.Level.ToString();
            dict["message"] = entry.Message;
            if (!string.IsNullOrEmpty(entry.SpanId)) dict["spanId"] = entry.SpanId;
            if (!string.IsNullOrEmpty(entry.TraceId)) dict["traceId"] = entry.TraceId;
            if (!string.IsNullOrEmpty(entry.Resource)) dict["resource"] = entry.Resource;
            if (!string.IsNullOrEmpty(entry.Source)) dict["source"] = entry.Source;
            if (!string.IsNullOrEmpty(entry.User)) dict["user"] = entry.User;
            if (entry.Tags != null && entry.Tags.Length > 0) dict["tags"] = entry.Tags ?? [];
            if (entry.Fields != null && entry.Fields.Count > 0) {
                var dictFields = new Dictionary<string, object?>();
                foreach (var pair in entry.Fields) {
                    if (pair.Value is Exception) {
                        var ex = (Exception)pair.Value;
                        var exDict = new Dictionary<string, object?>();
                        exDict["message"] = ex.Message;
                        exDict["stackTrace"] = ex.StackTrace;
                        if (ex.InnerException != null) {
                            var innerExDict = new Dictionary<string, object?>();
                            innerExDict["message"] = ex.InnerException.Message;
                            innerExDict["stackTrace"] = ex.InnerException.StackTrace;
                            exDict["innerException"] = innerExDict;
                        }
                        dictFields[pair.Key] = exDict;
                    } else {
                        dictFields[pair.Key] = pair.Value;
                    }
                }
                dict["fields"] = dictFields;
            }
            return JsonSerializer.Serialize(dict);
        }

    }
}

