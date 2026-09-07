using System;
using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log {

    internal static class LogLevelMappings {

        public static LogLevelNative ToMicrosoftLogLevel(LogLevel logLevel) {
            switch (logLevel) {
                case LogLevel.Trace:
                    return LogLevelNative.Trace;
                case LogLevel.Debug:
                    return LogLevelNative.Debug;
                case LogLevel.Information:
                case LogLevel.Custom:
                    return LogLevelNative.Information;
                case LogLevel.Warning:
                    return LogLevelNative.Warning;
                case LogLevel.Error:
                    return LogLevelNative.Error;
                case LogLevel.Fatal:
                    return LogLevelNative.Critical;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "Unsupported log level.");
            }
        }

        public static bool TryFromMicrosoftLogLevel(LogLevelNative logLevel, out LogLevel result) {
            switch (logLevel) {
                case LogLevelNative.Trace:
                    result = LogLevel.Trace;
                    return true;
                case LogLevelNative.Debug:
                    result = LogLevel.Debug;
                    return true;
                case LogLevelNative.Information:
                    result = LogLevel.Information;
                    return true;
                case LogLevelNative.Warning:
                    result = LogLevel.Warning;
                    return true;
                case LogLevelNative.Error:
                    result = LogLevel.Error;
                    return true;
                case LogLevelNative.Critical:
                    result = LogLevel.Fatal;
                    return true;
                case LogLevelNative.None:
                default:
                    result = default;
                    return false;
            }
        }

        public static bool IsEnabled(LogLevel minimumLevel, LogLevel logLevel) {
            return GetSeverityRank(logLevel) >= GetSeverityRank(minimumLevel);
        }

        public static int GetSeverityRank(LogLevel logLevel) {
            switch (logLevel) {
                case LogLevel.Trace:
                    return 1;
                case LogLevel.Debug:
                    return 2;
                case LogLevel.Information:
                case LogLevel.Custom:
                    return 3;
                case LogLevel.Warning:
                    return 4;
                case LogLevel.Error:
                    return 5;
                case LogLevel.Fatal:
                    return 6;
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, "Unsupported log level.");
            }
        }
    }
}
