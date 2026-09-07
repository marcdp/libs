using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DProjects.Log {


    public class LogClientLogger<T> : LogClientLogger, ILogClient<T> where T : class {
        public LogClientLogger(ILogger<T> logger) : this(logger, LogLevel.Information) {
        }
        public LogClientLogger(ILogger<T> logger, LogLevel level) :base(logger, level) {
            mLogger = logger;
            Source = typeof(T).FullName;
        }
    }


    public class LogClientLogger : ILogClient {


        //variables
        protected ILogger mLogger;
        private readonly ConditionalWeakTable<LogEntry, NativeMessage> mNativeMessages =
            new ConditionalWeakTable<LogEntry, NativeMessage>();


        //events
        public event EventHandler<LogEntry>? Writed;


        //constructor
        public LogClientLogger(ILogger logger) : this(logger, LogLevel.Information) {
        }
        public LogClientLogger(ILogger logger, LogLevel level) {
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
            LogLevelMappings.GetSeverityRank(level);
            Level = level;
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
        public LogLevel Level { get; }
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
                    if (messageFieldNames.Contains(field.Key)) {
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


        private void AppendMessage(
            LogEntry logEntry,
            StringBuilder message,
            List<object?> args,
            HashSet<string> messageFieldNames
        ) {
            if (!mNativeMessages.TryGetValue(logEntry, out var nativeMessage)) {
                message.Append(logEntry.Message);
                return;
            }
            message.Append(nativeMessage.Template);
            args.AddRange(nativeMessage.Arguments);
            messageFieldNames.UnionWith(nativeMessage.FieldNames);
        }


        //private 
        private LogEntry CreateLogEntry(LogLevel logType, string message, params object?[] args) {
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
            var logEntry = new LogEntry(
                logType,
                Prefix + parsedMessage.RenderedMessage,
                fields,
                Tags,
                Source,
                User,
                Resource,
                now,
                SpanId,
                TraceId
            );
            mNativeMessages.Add(logEntry, new NativeMessage(
                LogMessageTemplate.EscapeLiteral(Prefix) + parsedMessage.NativeTemplate,
                parsedMessage.NativeArguments,
                parsedMessage.FieldNames
            ));
            return logEntry;
        }

        private sealed class NativeMessage {
            public NativeMessage(string template, object?[] arguments, HashSet<string> fieldNames) {
                Template = template;
                Arguments = arguments;
                FieldNames = fieldNames;
            }

            public string Template { get; }
            public object?[] Arguments { get; }
            public HashSet<string> FieldNames { get; }
        }

    }


}

