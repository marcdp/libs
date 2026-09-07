using Microsoft.Extensions.Logging;
using System;

using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Provider {

    public class LoggerClient<TCategory> : Log, Microsoft.Extensions.Logging.ILogger<TCategory> {

        private readonly ILogClient mLogClient;
        private readonly LoggerScopeContext mScopeContext = new LoggerScopeContext();

        public LoggerClient(ILogClient logClient) : base(false, false, LogLevel.Trace) {
            mLogClient = logClient ?? throw new ArgumentNullException(nameof(logClient));
        }

        protected override void ProcessEntry(LogEntry logEntry) {
            mLogClient.Write(logEntry);
        }

        IDisposable ILogger.BeginScope<TState>(TState state) {
            return mScopeContext.Push(state!);
        }

        bool ILogger.IsEnabled(LogLevelNative logLevel) {
            return LoggerAdapter.IsEnabled(mLogClient.Level, logLevel);
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
                mLogClient.Level,
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
