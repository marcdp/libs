using Microsoft.Extensions.Logging;
using System;
using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Provider {


    public class Logger : Log, Microsoft.Extensions.Logging.ILogger {


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
        private ILog mLog;


        //constructor
        public Logger(ILog log) : base(false, false) {
            mLog = log;
        }


        //methods
        protected override void ProcessEntry(LogEntry logEntry) {
            mLog.Write(logEntry);
        }


        //methods
        IDisposable ILogger.BeginScope<TState>(TState state) {
            return new Scope<TState>(state);
        }
        bool ILogger.IsEnabled(LogLevelNative logLevel) {
            if (logLevel == LogLevelNative.None) {
                return false;
            } else if (logLevel == LogLevelNative.Trace) {
                return (mLog.Level <= LogLevel.Trace);
            } else if (logLevel == LogLevelNative.Debug) {
                return (mLog.Level <= LogLevel.Debug);
            } else if (logLevel == LogLevelNative.Information) {
                return (mLog.Level <= LogLevel.Information);
            } else if (logLevel == LogLevelNative.Warning) {
                return (mLog.Level <= LogLevel.Warning);
            } else if (logLevel == LogLevelNative.Error) {
                return (mLog.Level <= LogLevel.Error);
            } else if (logLevel == LogLevelNative.Critical) {
                return (mLog.Level <= LogLevel.Fatal);
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

