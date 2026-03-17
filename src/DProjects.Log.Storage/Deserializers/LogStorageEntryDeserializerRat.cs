using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;


namespace DProjects.Log.Storage.Serializers {


    //class
    public class LogStorageEntryDeserializerRat : ILogStorageEntryDeserializer {


        //methods
        public LogEntry Deserialize(string line) {
            try { 
                var aDate = DateTime.Parse(line.Substring(0, 24)).ToUniversalTime();
                var i = line.IndexOf("] ");
                if (i == -1) throw new InvalidDataException("expected ' ['");
                var logLevel = LogLevel.Information;
                var tags = new List<string>(line.Substring(26, i - 26).Split('|'));
                if (tags.Contains("trace")) {
                    logLevel = LogLevel.Trace;
                    tags.Remove("trace");
                } else if (tags.Contains("debug")) {
                    logLevel = LogLevel.Debug;
                    tags.Remove("debug");
                } else if (tags.Contains("info")) {
                    logLevel = LogLevel.Information;
                    tags.Remove("info");
                } else if (tags.Contains("warn")) {
                    logLevel = LogLevel.Warning;
                    tags.Remove("warn");
                } else if (tags.Contains("error")) {
                    logLevel = LogLevel.Error;
                    tags.Remove("error");
                } else if (tags.Contains("fatal")) {
                    logLevel = LogLevel.Fatal;
                    tags.Remove("fatal");
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
                            fieldValue = fieldValue.Replace("\\u007C", "|").Replace("\\n", CharUtils.CHAR_LF.ToString()).Replace("\\r", CharUtils.CHAR_CR.ToString()).Replace("\\\\", "\\");
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
                message = message.Replace("\\u007C", "|").Replace("\\n", CharUtils.CHAR_LF.ToString()).Replace("\\r", CharUtils.CHAR_CR.ToString()).Replace("\\\\", "\\").TrimEnd();
                return new LogEntry(logLevel, message, fields, tags.ToArray(), source, user, null, aDate);
            } catch (Exception e) {
                throw new Exception("Unable to parse log line: " + e.Message + ": " + line, e);
            }
        }
    }

}

