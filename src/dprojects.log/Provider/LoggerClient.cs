using Microsoft.Extensions.Logging;

using System;

using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Provider {


    public class LoggerClient<TCategory> : Log, Microsoft.Extensions.Logging.ILogger<TCategory> {


        //inner class
        private class Scope<T> : IDisposable {
            public T State;
            public string Key;
            public Scope(T state) {
                State = state;
                Key = System.Guid.NewGuid().ToString();
            }
            public void Dispose() {
            }
            public override string ToString() {
                return Key;
            }
        }


        //variable  
        private ILogClient mLogClient;


        //constructor
        public LoggerClient(ILogClient logClient) : base(false, false) {
            mLogClient = logClient;
        }


        //methods
        protected override void ProcessEntry(LogEntry logEntry) {
            mLogClient.Write(logEntry);
        }


        //methods
        IDisposable ILogger.BeginScope<TState>(TState state) {
            return new Scope<TState>(state);
        }
        bool ILogger.IsEnabled(LogLevelNative logLevel) {
            if (logLevel == LogLevelNative.None) {
                return false;
            } else if (logLevel == LogLevelNative.Trace) {
                return (mLogClient.Level <= LogLevel.Trace);
            } else if (logLevel == LogLevelNative.Debug) {
                return (mLogClient.Level <= LogLevel.Debug);
            } else if (logLevel == LogLevelNative.Information) {
                return (mLogClient.Level <= LogLevel.Information);
            } else if (logLevel == LogLevelNative.Warning) {
                return (mLogClient.Level <= LogLevel.Warning);
            } else if (logLevel == LogLevelNative.Error) {
                return (mLogClient.Level <= LogLevel.Error);
            } else if (logLevel == LogLevelNative.Critical) {
                return (mLogClient.Level <= LogLevel.Fatal);
            }
            return false;
        }
        public void Log<TState>(LogLevelNative logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (logLevel == LogLevelNative.None) {
            } else if (logLevel == LogLevelNative.Trace) {
                var message = formatter(state, exception);
                base.Trace(message);
            } else if (logLevel == LogLevelNative.Debug) {
                var message = formatter(state, exception);
                base.Debug(message);
            } else if (logLevel == LogLevelNative.Information) {
                var message = formatter(state, exception);
                base.Info(message);
            } else if (logLevel == LogLevelNative.Warning) {
                var message = formatter(state, exception);
                base.Warning(message);
            } else if (logLevel == LogLevelNative.Error) {
                var message = formatter(state, exception);
                base.Error(message);
            } else if (logLevel == LogLevelNative.Critical) {
                var message = formatter(state, exception);
                base.Fatal(message);
            }
        }


    }

}

