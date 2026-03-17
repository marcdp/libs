using System;
using System.Collections.Generic;
using DProjects.Utils;


namespace DProjects.Log.Storage.Serializers {


    //class
    public class LogStorageEntryDeserializerJson : ILogStorageEntryDeserializer {


        //methods
        public LogEntry Deserialize(string line) {
            var logEntry = new LogEntry();
            var jsonDeserializer = new DProjects.Text.Json.JsonDeserializer(new() {
                UseDateTimeLaxConverter = true,
            });
            //var entry = jsonDeserializer.Deserialize<LogEntry>(line);
            var dict = jsonDeserializer.Deserialize<IDictionary<string, object?>>(line);
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
                        logEntry.Level = LogLevel.Trace;
                    } else if ("debug".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                        logEntry.Level = LogLevel.Debug;
                    } else if ("information".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "info".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                        logEntry.Level = LogLevel.Information;
                    } else if ("warning".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "warn".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                        logEntry.Level = LogLevel.Warning;
                    } else if ("error".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "err".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                        logEntry.Level = LogLevel.Error;
                    } else if ("fatal".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "severe".Equals((string)value, StringComparison.OrdinalIgnoreCase) || "sev".Equals((string)value, StringComparison.OrdinalIgnoreCase)) {
                        logEntry.Level = LogLevel.Fatal;
                    } else {
                        logEntry.Level = LogLevel.Custom;
                        dict["type_custom"] = value;
                    }
                    dict.Remove(key);
                    break;
                }
            }
            //tags
            foreach (var key in new string[] { "tags" }) {
                if (dict.TryGetValue(key, out object? value)) {
                    logEntry.Tags = ConvertUtils.To<string[]>(value);                   
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
            if (dict.TryGetValue("fields", out object? fields)) {
                if (fields == null) {
                } else {
                    logEntry.Fields = (IDictionary<string, object?>)fields;
                }
                dict.Remove("fields");
            }
            //return
            return logEntry;
        }

    }

}

