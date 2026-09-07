using Microsoft.Extensions.Logging;
using System;

using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Provider {

    public class Logger : Log, Microsoft.Extensions.Logging.ILogger {

        private readonly ILog mLog;
        private readonly string? mSource;
        private readonly LoggerScopeContext mScopeContext = new LoggerScopeContext();

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
            return mScopeContext.Push(state!);
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
            LoggerAdapter.Log(
                this,
                mLog.Level,
                mSource,
                mScopeContext,
                logLevel,
                eventId,
                state,
                exception,
                formatter
            );
        }
    }

    public class Logger<TCategory> : Log, Microsoft.Extensions.Logging.ILogger<TCategory> {

        private readonly ILog mLog;
        private readonly LoggerScopeContext mScopeContext = new LoggerScopeContext();

        public Logger(ILog log) : base(false, false, LogLevel.Trace) {
            mLog = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected override void ProcessEntry(LogEntry logEntry) {
            mLog.Write(logEntry);
        }

        IDisposable ILogger.BeginScope<TState>(TState state) {
            return mScopeContext.Push(state!);
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
            LoggerAdapter.Log(
                this,
                mLog.Level,
                typeof(TCategory).FullName,
                mScopeContext,
                logLevel,
                eventId,
                state,
                exception,
                formatter
            );
        }
    }
}
