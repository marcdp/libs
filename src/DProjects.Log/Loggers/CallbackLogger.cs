
using DProjects.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace DProjects.Log.Loggers {

    public sealed class CallbackLogger : Microsoft.Extensions.Logging.ILogger {

        // inner classes
        private sealed class NullScope : IDisposable {
            public static NullScope Instance { get; } = new();
            public void Dispose() {
            }
        }

        // vars
        private readonly Action<Microsoft.Extensions.Logging.LogLevel, Microsoft.Extensions.Logging.EventId, string, Exception?> mCallback;

        // methods
        public CallbackLogger(Action<Microsoft.Extensions.Logging.LogLevel, Microsoft.Extensions.Logging.EventId, string, Exception?> callback) {
            mCallback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        // methods
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) {
            return logLevel != Microsoft.Extensions.Logging.LogLevel.None;
        }
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (!IsEnabled(logLevel)) {
                return;
            }
            string message = formatter(state, exception);
            mCallback(logLevel, eventId, message, exception);
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull {
            return NullScope.Instance;
        }


    }
}