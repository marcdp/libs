using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Text;

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
        public string? Prefix { get; set; }
        public string? User { get; set; }
        public string? Source { get; set; }
        public string[]? Tags { get; set; }
        public Dictionary<string, object?>? Fields { get; set; }


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
        public void Critical(string message, params object?[] args) {
            var logEntry = CreateLogEntry(LogLevel.Critical, message, args);
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
            if (args.Length> 0) {
                if (fields == null) fields = new Dictionary<string, object?>();
                var sb = new StringBuilder();
                var argIndex = 0;
                var iAnt = 0;
                do {
                    var i = message.IndexOf("{", iAnt);
                    if (i == -1) {
                        sb.Append(message.Substring(iAnt));
                        break;
                    }
                    var j = message.IndexOf("}", i);
                    if (j == -1) break;
                    var varName = message.Substring(i + 1, j - i - 1);
                    var varValue = (argIndex < args.Length ? args[argIndex++] : "{" + varName + "}");
                    sb.Append(message.Substring(iAnt, i - iAnt));
                    sb.Append(varValue);
                    fields[varName] = varValue;
                    iAnt = j + 1;
                } while (true);
                fields["messageOriginal"] = message;
                message = sb.ToString();
            }
            return new LogEntry(logLevel, Prefix + message, fields, Tags, Source, User, now);
        }

    }


}

