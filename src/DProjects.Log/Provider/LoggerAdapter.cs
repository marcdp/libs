using DProjects.Utils;
using Microsoft.Extensions.Logging;
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
            LoggerScopeContext scopeContext,
            LogLevelNative logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter
        ) {
            if (!IsEnabled(minimumLevel, logLevel)
                || !LogLevelMappings.TryFromMicrosoftLogLevel(logLevel, out var mappedLevel)) {
                return;
            }

            var fields = GetFields(eventId, scopeContext, state);
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

        private static IDictionary<string, object?>? GetFields<TState>(
            EventId eventId,
            LoggerScopeContext scopeContext,
            TState state
        ) {
            // Merge from lowest to highest precedence: adapter metadata, outer scopes,
            // inner scopes, then explicit log state.
            Dictionary<string, object?>? fields = null;
            if (eventId.Id != 0) {
                fields = new Dictionary<string, object?> { ["eventId"] = eventId.Id };
            }
            if (!string.IsNullOrEmpty(eventId.Name)) {
                if (fields == null) {
                    fields = new Dictionary<string, object?>();
                }
                fields["eventName"] = eventId.Name;
            }

            List<object>? unstructuredScopes = null;
            foreach (var scopeState in scopeContext.GetStates()) {
                if (scopeState is IEnumerable<KeyValuePair<string, object?>> structuredScope) {
                    fields = AddStructuredState(fields, structuredScope);
                } else {
                    if (unstructuredScopes == null) {
                        unstructuredScopes = new List<object>();
                    }
                    unstructuredScopes.Add(scopeState);
                }
            }
            if (unstructuredScopes != null) {
                if (fields == null) {
                    fields = new Dictionary<string, object?>();
                }
                fields["scopes"] = unstructuredScopes.ToArray();
            }

            if (state is IEnumerable<KeyValuePair<string, object?>> structuredState) {
                fields = AddStructuredState(fields, structuredState);
            }
            return fields;
        }

        private static Dictionary<string, object?>? AddStructuredState(
            Dictionary<string, object?>? fields,
            IEnumerable<KeyValuePair<string, object?>> structuredState
        ) {
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
