using DProjects.Utils;
using System;
using System.Collections.Generic;

using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Provider {

    internal static class LoggerAdapter {

        public static bool IsEnabled(LogLevel minimumLevel, LogLevelNative logLevel) {
            return LogLevelMappings.TryFromMicrosoftLogLevel(logLevel, out var mappedLevel)
                && LogLevelMappings.IsEnabled(minimumLevel, mappedLevel);
        }

        public static void Log<TState>(
            Log log,
            LogLevel minimumLevel,
            string? source,
            LogLevelNative logLevel,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) {
            if (!IsEnabled(minimumLevel, logLevel)
                || !LogLevelMappings.TryFromMicrosoftLogLevel(logLevel, out var mappedLevel)) {
                return;
            }

            var fields = GetFields(state);
            var message = formatter(state, exception);
            if (exception != null && mappedLevel != LogLevel.Error && mappedLevel != LogLevel.Fatal) {
                if (fields == null) {
                    fields = new Dictionary<string, object?>();
                }
                fields["exception"] = ExceptionUtils.GetMessageDetailed(exception);
            }
            switch (mappedLevel) {
                case LogLevel.Trace:
                    log.Trace(message, fields, source: source);
                    break;
                case LogLevel.Debug:
                    log.Debug(message, fields, source: source);
                    break;
                case LogLevel.Information:
                    log.Info(message, fields, source: source);
                    break;
                case LogLevel.Warning:
                    log.Warning(message, fields, source: source);
                    break;
                case LogLevel.Error:
                    log.Error(message, fields, source: source, exception: exception);
                    break;
                case LogLevel.Fatal:
                    log.Fatal(message, fields, source: source, exception: exception);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mappedLevel), mappedLevel, "Unsupported log level.");
            }
        }

        private static IDictionary<string, object?>? GetFields<TState>(TState state) {
            if (!(state is IEnumerable<KeyValuePair<string, object?>> structuredState)) {
                return null;
            }

            Dictionary<string, object?>? fields = null;
            foreach (var item in structuredState) {
                if (item.Key == "{OriginalFormat}") {
                    continue;
                }

                if (fields == null) {
                    fields = new Dictionary<string, object?>();
                }
                fields[item.Key] = item.Value;
            }
            return fields;
        }
    }
}
