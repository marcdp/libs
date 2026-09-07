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
        public LogLevel Level { get; } = LogLevel.Information;
        public string? SpanId { get; set; }
        public string? TraceId { get; set; }


        //methods
        public void Trace(string message, params object?[] args) {
            Write(CreateLogEntry(LogLevel.Trace, message, args));
        }
        public void Debug(string message, params object?[] args) {
            Write(CreateLogEntry(LogLevel.Debug, message, args));
        }
        public void Info(string message, params object?[] args) {
            Write(CreateLogEntry(LogLevel.Information, message, args));
        }
        public void Warning(string message, params object?[] args) {
            Write(CreateLogEntry(LogLevel.Warning, message, args));
        }
        public void Error(string message, params object?[] args) {
            Write(CreateLogEntry(LogLevel.Error, message, args));
        }
        public void Fatal(string message, params object?[] args) {
            Write(CreateLogEntry(LogLevel.Fatal, message, args));
        }
        public void Write(LogEntry logEntry) {
            if (logEntry == null) throw new ArgumentNullException(nameof(logEntry));

            var message = new StringBuilder();
            var args = new List<object?>();
            var messageFieldNames = new HashSet<string>(StringComparer.Ordinal);
            AppendMessage(logEntry, message, args, messageFieldNames);
            if (logEntry.Tags != null) {
                message.Append(" {tags}");
                args.Add(logEntry.Tags);
            }
            if (!string.IsNullOrEmpty(logEntry.Source)) {
                message.Append(" {source}");
                args.Add(logEntry.Source);
            }
            if (logEntry.Fields != null) {
                foreach (var field in logEntry.Fields) {
                    if (field.Key == "messageOriginal" || messageFieldNames.Contains(field.Key)) {
                        continue;
                    }
                    message.Append(" {" + field.Key + "}");
                    args.Add(field.Value);
                }
            }
            if (!string.IsNullOrEmpty(logEntry.User)) {
                message.Append(" {user}");
                args.Add(logEntry.User);
            }
            if (!string.IsNullOrEmpty(logEntry.Resource)) {
                message.Append(" {resource}");
                args.Add(logEntry.Resource);
            }
            if (!string.IsNullOrEmpty(logEntry.SpanId)) {
                message.Append(" {spanId}");
                args.Add(logEntry.SpanId);
            }
            if (!string.IsNullOrEmpty(logEntry.TraceId)) {
                message.Append(" {traceId}");
                args.Add(logEntry.TraceId);
            }

            mLogger.Log(LogLevelMappings.ToMicrosoftLogLevel(logEntry.Level), message.ToString(), args.ToArray());
            Writed?.Invoke(this, logEntry);
        }


        private static void AppendMessage(
            LogEntry logEntry,
            StringBuilder message,
            List<object?> args,
            HashSet<string> messageFieldNames
        ) {
            if (logEntry.Fields == null
                || !logEntry.Fields.TryGetValue("messageOriginal", out var originalValue)
                || !(originalValue is string originalMessage)) {
                message.Append(logEntry.Message);
                return;
            }

            var renderedMessage = new StringBuilder();
            var index = 0;
            while (index < originalMessage.Length) {
                var openIndex = originalMessage.IndexOf("{", index, StringComparison.Ordinal);
                if (openIndex == -1) {
                    renderedMessage.Append(originalMessage.Substring(index));
                    break;
                }

                var closeIndex = originalMessage.IndexOf("}", openIndex, StringComparison.Ordinal);
                if (closeIndex == -1) {
                    renderedMessage.Append(originalMessage.Substring(index));
                    break;
                }

                renderedMessage.Append(originalMessage.Substring(index, openIndex - index));
                var fieldName = originalMessage.Substring(openIndex + 1, closeIndex - openIndex - 1);
                if (logEntry.Fields.TryGetValue(fieldName, out var fieldValue)) {
                    renderedMessage.Append(fieldValue);
                    args.Add(fieldValue);
                    messageFieldNames.Add(fieldName);
                }
                index = closeIndex + 1;
            }

            var rendered = renderedMessage.ToString();
            if (logEntry.Message.EndsWith(rendered, StringComparison.Ordinal)) {
                message.Append(logEntry.Message.Substring(0, logEntry.Message.Length - rendered.Length));
            }
            message.Append(originalMessage);
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
            return new LogEntry(logType, Prefix + message, fields, Tags, Source, User, Resource, now, SpanId, TraceId);
        }

    }


}

