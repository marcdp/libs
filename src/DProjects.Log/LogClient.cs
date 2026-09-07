using System;
using System.Collections.Generic;

namespace DProjects.Log {


    public class LogClient : ILogClient {


        //variables
        protected ILog mLog;        

        //events
        public event EventHandler<LogEntry>? Writed;


        //constructor
        public LogClient(ILog log) {
            mLog = log;
        }
        public void Dispose() {
        }


        //props
        public LogLevel Level => mLog.Level;
        public string? Prefix { get; set; }
        public string? User { get; set; }
        public string? Source { get; set; }
        public string? Resource { get; set; }
        public string[]? Tags { get; set; }
        public Dictionary<string, object?>? Fields { get; set; }
        public string? SpanId { get; set; }
        public string? TraceId { get; set; }


        //methods
        public void Trace(string message, params object?[] args) {
            var logEntry = CreateLogEntry(LogLevel.Trace, message, args);
            mLog.Write(logEntry);
            Writed?.Invoke(this, logEntry);
        }
        public void Debug(string message, params object?[] args) {
            var logEntry = CreateLogEntry(LogLevel.Debug, message, args);
            mLog.Write(logEntry);
            Writed?.Invoke(this, logEntry);
        }
        public void Info(string message, params object?[] args) {
            var logEntry = CreateLogEntry(LogLevel.Information, message, args);
            mLog.Write(logEntry);
            Writed?.Invoke(this, logEntry);
        }
        public void Warning(string message, params object?[] args) {
            var logEntry = CreateLogEntry(LogLevel.Warning, message, args);
            mLog.Write(logEntry);
            Writed?.Invoke(this, logEntry);
        }
        public void Error(string message, params object?[] args) {
            var logEntry = CreateLogEntry(LogLevel.Error, message, args);
            mLog.Write(logEntry);
            Writed?.Invoke(this, logEntry);
        }
        public void Fatal(string message, params object?[] args) {
            var logEntry = CreateLogEntry(LogLevel.Fatal, message, args);
            mLog.Write(logEntry);
            Writed?.Invoke(this, logEntry);
        }
        public void Write(LogEntry logEntry) {
            mLog.Write(logEntry);
            Writed?.Invoke(this, logEntry);
        }

        //private 
        private LogEntry CreateLogEntry(LogLevel logLevel, string message, params object?[] args) {
            var now = DateTime.Now;
            Dictionary<string, object?>? fields = null;
            if (Fields != null && Fields.Count > 0) {
                if (fields == null) fields = new Dictionary<string, object?>();
                foreach (var key in Fields.Keys) fields[key] = Fields[key];
            }
            var parsedMessage = LogMessageTemplate.Parse(message, args);
            if (parsedMessage.Fields.Count > 0) {
                if (fields == null) fields = new Dictionary<string, object?>();
                foreach (var field in parsedMessage.Fields) fields[field.Key] = field.Value;
            }
            return new LogEntry(logLevel, Prefix + parsedMessage.RenderedMessage, fields, Tags, Source, User, Resource, now, SpanId, TraceId);
        }

    }


}

