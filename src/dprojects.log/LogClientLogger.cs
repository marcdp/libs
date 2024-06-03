using DProjects.Utils;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DProjects.Log {


    public class LogClientLogger<T> : LogClientLogger, ILogClient<T> where T : class {
        public LogClientLogger(ILogger<T> logger) :base(logger) {
            mLogger = logger;
            Source = typeof(T).FullName;
        }
    }


    public class LogClientLogger : ILogClient {


        //variables
        protected ILogger mLogger;

        //events
        public event EventHandler<LogEntry>? Writed;


        //constructor
        public LogClientLogger(ILogger logger) {
            mLogger = logger;
        }
        public void Dispose() {
        }


        //props
        public string? Prefix { get; set; }
        public string? User { get; set; }
        public string? Source { get; set; }
        public string? Resource { get; set; }
        public string[]? Tags { get; set; }
        public Dictionary<string, object?>? Fields { get; set; }


        //methods
        public void Trace(string message, params object?[] args) {
            mLogger.LogDebug(message, args);
            if (Writed != null) Writed?.Invoke(this, CreateLogEntry(LogLevel.Trace, message, args));
        }
        public void Debug(string message, params object?[] args) {
            mLogger.LogDebug(message, args);
            if (Writed != null) Writed?.Invoke(this, CreateLogEntry(LogLevel.Debug, message, args));
        }
        public void Info(string message, params object?[] args) {
            mLogger.LogInformation(message, args);
            if (Writed != null) Writed?.Invoke(this, CreateLogEntry(LogLevel.Information, message, args));
        }
        public void Warning(string message, params object?[] args) {
            mLogger.LogWarning(message, args);
            if (Writed != null) Writed?.Invoke(this, CreateLogEntry(LogLevel.Warning, message, args));
        }
        public void Error(string message, params object?[] args) {
            mLogger.LogError(message, args);
            if (Writed != null) Writed?.Invoke(this, CreateLogEntry(LogLevel.Error, message, args));
        }
        public void Fatal(string message, params object?[] args) {
            mLogger.LogCritical(message, args);
            if (Writed != null) Writed?.Invoke(this, CreateLogEntry(LogLevel.Fatal, message, args));
        }

        //private 
        private LogEntry CreateLogEntry(LogLevel logType, string message, params object?[] args) {
            var now = DateTime.Now;
            Dictionary<string, object?>? fields = null;
            if (Fields != null && Fields.Count > 0) {
                if (fields == null) fields = new Dictionary<string, object?>();
                foreach (var key in Fields.Keys) fields[key] = Fields[key];
            }
            if (args.Length > 0) {
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
            return new LogEntry(logType, Prefix + message, fields, Tags, Source, User, Resource, now);
        }

    }


}

