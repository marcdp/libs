using Microsoft.Extensions.Logging;
using System;

using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Provider {

    public class Logger : Log, Microsoft.Extensions.Logging.ILogger {

        private sealed class Scope<T> : IDisposable {
            private readonly string mKey = Guid.NewGuid().ToString();

            public Scope(T state) {
                State = state;
            }

            public T State { get; }

            public void Dispose() {
            }

            public override string ToString() {
                return mKey;
            }
        }

        private readonly ILog mLog;
        private readonly string? mSource;

        public Logger(ILog log) : this(log, null) {
        }

        internal Logger(ILog log, string? source) : base(false, false, LogLevel.Trace) {
            mLog = log ?? throw new ArgumentNullException(nameof(log));
            mSource = source;
        }

        protected override void ProcessEntry(LogEntry logEntry) {
            mLog.Write(logEntry);
        }

        IDisposable ILogger.BeginScope<TState>(TState state) {
            return new Scope<TState>(state);
        }

        bool ILogger.IsEnabled(LogLevelNative logLevel) {
            return LoggerAdapter.IsEnabled(mLog.Level, logLevel);
        }

        public void Log<TState>(
            LogLevelNative logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) {
            LoggerAdapter.Log(this, mLog.Level, mSource, logLevel, state, exception, formatter);
        }
    }

    public class Logger<TCategory> : Log, Microsoft.Extensions.Logging.ILogger<TCategory> {

        private sealed class Scope<T> : IDisposable {
            private readonly string mKey = Guid.NewGuid().ToString();

            public Scope(T state) {
                State = state;
            }

            public T State { get; }

            public void Dispose() {
            }

            public override string ToString() {
                return mKey;
            }
        }

        private readonly ILog mLog;

        public Logger(ILog log) : base(false, false, LogLevel.Trace) {
            mLog = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected override void ProcessEntry(LogEntry logEntry) {
            mLog.Write(logEntry);
        }

        IDisposable ILogger.BeginScope<TState>(TState state) {
            return new Scope<TState>(state);
        }

        bool ILogger.IsEnabled(LogLevelNative logLevel) {
            return LoggerAdapter.IsEnabled(mLog.Level, logLevel);
        }

        public void Log<TState>(
            LogLevelNative logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) {
            LoggerAdapter.Log(this, mLog.Level, typeof(TCategory).FullName, logLevel, state, exception, formatter);
        }
    }
}
