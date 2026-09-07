using DProjects.Factories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Tests {

    public class ProviderLoggerTests {

        [Theory]
        [InlineData(DProjects.Log.LogLevel.Trace, LogLevelNative.Trace, true)]
        [InlineData(DProjects.Log.LogLevel.Debug, LogLevelNative.Debug, true)]
        [InlineData(DProjects.Log.LogLevel.Information, LogLevelNative.Debug, false)]
        [InlineData(DProjects.Log.LogLevel.Custom, LogLevelNative.Debug, false)]
        [InlineData(DProjects.Log.LogLevel.Custom, LogLevelNative.Information, true)]
        public void Logger_FiltersUsingWrappedLogSeverity(
            DProjects.Log.LogLevel minimumLevel,
            LogLevelNative nativeLevel,
            bool expectedEmitted
        ) {
            var log = new RecordingLog(minimumLevel);
            ILogger logger = new Provider.Logger(log);

            logger.Log(nativeLevel, "message");

            Assert.Equal(expectedEmitted ? 1 : 0, log.Entries.Count);
        }

        [Theory]
        [InlineData(LogLevelNative.Trace, false)]
        [InlineData(LogLevelNative.Debug, false)]
        [InlineData(LogLevelNative.Information, true)]
        [InlineData(LogLevelNative.Warning, true)]
        [InlineData(LogLevelNative.Error, true)]
        [InlineData(LogLevelNative.Critical, true)]
        [InlineData(LogLevelNative.None, false)]
        public void IsEnabled_MapsEveryNativeLevelBySeverity(LogLevelNative nativeLevel, bool expected) {
            var log = new RecordingLog(DProjects.Log.LogLevel.Custom);
            ILogger logger = new Provider.Logger(log);

            Assert.Equal(expected, logger.IsEnabled(nativeLevel));
        }

        [Fact]
        public void LoggerClient_EmitsDebugWhenWrappedClientAllowsDebug() {
            var client = new RecordingLogClient(DProjects.Log.LogLevel.Debug);
            ILogger logger = new Provider.LoggerClient<ProviderLoggerTests>(client);

            logger.LogDebug("debug");

            var entry = Assert.Single(client.Entries);
            Assert.Equal(DProjects.Log.LogLevel.Debug, entry.Level);
        }

        [Fact]
        public void LoggerClient_PropagatesScopeFields() {
            var client = new RecordingLogClient(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.LoggerClient<ProviderLoggerTests>(client);

            using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = "client-42" })) {
                logger.LogInformation("inside");
            }

            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(
                Assert.Single(client.Entries).Fields
            );
            Assert.Equal("client-42", fields["RequestId"]);
        }

        [Fact]
        public void StructuredState_PreservesFieldsAndOmitsOriginalFormat() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            logger.LogInformation("Order {OrderId} completed", 42);

            var entry = Assert.Single(log.Entries);
            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(entry.Fields);
            Assert.Equal("Order 42 completed", entry.Message);
            Assert.Equal(42, fields["OrderId"]);
            Assert.DoesNotContain("{OriginalFormat}", fields.Keys);
        }

        [Fact]
        public void Error_PreservesStructuredFieldsAndException() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);
            var exception = new InvalidOperationException("broken");

            logger.LogError(exception, "Failure {Id}", 42);

            var entry = Assert.Single(log.Entries);
            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(entry.Fields);
            Assert.Equal(DProjects.Log.LogLevel.Error, entry.Level);
            Assert.Equal("Failure 42", entry.Message);
            Assert.Equal(42, fields["Id"]);
            Assert.Contains("broken", Assert.IsType<string>(fields["exception"]));
        }

        [Fact]
        public void NonErrorLevel_DoesNotDiscardNativeException() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);
            var exception = new InvalidOperationException("warning failure");

            logger.LogWarning(exception, "Warning");

            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(
                Assert.Single(log.Entries).Fields
            );
            Assert.Contains("warning failure", Assert.IsType<string>(fields["exception"]));
        }

        [Fact]
        public void StructuredScope_IsAddedToFieldsAndRemovedWhenDisposed() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = "42" })) {
                logger.LogInformation("inside");
            }
            logger.LogInformation("outside");

            Assert.Equal("42", Assert.IsAssignableFrom<IDictionary<string, object?>>(log.Entries[0].Fields)["RequestId"]);
            Assert.Null(log.Entries[1].Fields);
        }

        [Fact]
        public void NestedScopes_UseInnerValuesUntilInnerScopeIsDisposed() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            using (logger.BeginScope(new Dictionary<string, object?> {
                ["Tenant"] = "north",
                ["RequestId"] = "outer"
            })) {
                using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = "inner" })) {
                    logger.LogInformation("inner");
                }
                logger.LogInformation("outer");
            }

            var innerFields = Assert.IsAssignableFrom<IDictionary<string, object?>>(log.Entries[0].Fields);
            Assert.Equal("north", innerFields["Tenant"]);
            Assert.Equal("inner", innerFields["RequestId"]);
            var outerFields = Assert.IsAssignableFrom<IDictionary<string, object?>>(log.Entries[1].Fields);
            Assert.Equal("outer", outerFields["RequestId"]);
        }

        [Fact]
        public void ExplicitFieldsOverrideScopeFields() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = "scope" })) {
                logger.LogInformation("Request {RequestId}", "explicit");
            }

            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(log.Entries).Fields);
            Assert.Equal("explicit", fields["RequestId"]);
        }

        [Fact]
        public async Task Scope_FlowsAcrossAwait() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = "async-42" })) {
                await Task.Yield();
                logger.LogInformation("after await");
            }

            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(log.Entries).Fields);
            Assert.Equal("async-42", fields["RequestId"]);
        }

        [Fact]
        public async Task Scopes_AreIsolatedBetweenAsyncFlows() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            async Task WriteScoped(string requestId) {
                using (logger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = requestId })) {
                    await Task.Yield();
                    logger.LogInformation(requestId);
                }
            }

            await Task.WhenAll(WriteScoped("first"), WriteScoped("second"));

            Assert.Collection(
                log.Entries.OrderBy(entry => entry.Message),
                entry => Assert.Equal("first", entry.Fields!["RequestId"]),
                entry => Assert.Equal("second", entry.Fields!["RequestId"])
            );
        }

        [Fact]
        public void NonStructuredScopes_ArePreservedInOrder() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            using (logger.BeginScope("outer"))
            using (logger.BeginScope(42)) {
                logger.LogInformation("inside");
            }

            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(log.Entries).Fields);
            Assert.Equal(new object[] { "outer", 42 }, Assert.IsType<object[]>(fields["scopes"]));
        }

        [Fact]
        public void Scope_DoesNotLeakToAnotherLoggerInstance() {
            var firstLog = new RecordingLog(DProjects.Log.LogLevel.Trace);
            var secondLog = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger firstLogger = new Provider.Logger(firstLog);
            ILogger secondLogger = new Provider.Logger(secondLog);

            using (firstLogger.BeginScope(new Dictionary<string, object?> { ["RequestId"] = "first" })) {
                secondLogger.LogInformation("second");
            }

            Assert.Null(Assert.Single(secondLog.Entries).Fields);
        }

        [Fact]
        public void EventId_IsPreservedWithoutOverwritingExplicitFields() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            logger.LogInformation(new EventId(42, "OrderCompleted"), "Order {eventId}", "explicit");

            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(log.Entries).Fields);
            Assert.Equal("explicit", fields["eventId"]);
            Assert.Equal("OrderCompleted", fields["eventName"]);
        }

        [Fact]
        public void EventIdAndName_AreAddedWhenSupplied() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            logger.LogInformation(new EventId(42, "OrderCompleted"), "Order completed");

            var fields = Assert.IsAssignableFrom<IDictionary<string, object?>>(Assert.Single(log.Entries).Fields);
            Assert.Equal(42, fields["eventId"]);
            Assert.Equal("OrderCompleted", fields["eventName"]);
        }

        [Fact]
        public void DefaultEventId_DoesNotAddMetadata() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            ILogger logger = new Provider.Logger(log);

            logger.LogInformation("message");

            Assert.Null(Assert.Single(log.Entries).Fields);
        }

        [Fact]
        public void Provider_PreservesEachLoggerCategoryAsSource() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            using var provider = CreateProvider(log);
            var orders = provider.CreateLogger("MyApp.Orders");
            var payments = provider.CreateLogger("MyApp.Payments");

            orders.LogInformation("order");
            payments.LogInformation("payment");

            Assert.Collection(
                log.Entries,
                entry => Assert.Equal("MyApp.Orders", entry.Source),
                entry => Assert.Equal("MyApp.Payments", entry.Source)
            );
        }

        [Fact]
        public void Provider_DisposeDisposesFactoryCreatedLogExactlyOnce() {
            var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            var provider = CreateProvider(log);
            provider.CreateLogger("category");

            provider.Dispose();
            provider.Dispose();

            Assert.Equal(1, log.DisposeCount);
        }

        private static Provider.LoggerProvider CreateProvider(RecordingLog log) {
            var configuration = new Provider.LoggerProviderConfiguration(new ServiceCollection());
            return new Provider.LoggerProvider(configuration, new RecordingLogFactory(log));
        }

        private sealed class RecordingLogFactory : IFactoryByUrl<ILog> {
            private readonly ILog mLog;

            public RecordingLogFactory(ILog log) {
                mLog = log;
            }

            public ILog Create(string url) {
                return mLog;
            }
        }

        private sealed class RecordingLog : ILog {
            public RecordingLog(DProjects.Log.LogLevel level) {
                Level = level;
            }

            public List<LogEntry> Entries { get; } = new List<LogEntry>();
            public int DisposeCount { get; private set; }
            public DProjects.Log.LogLevel Level { get; }

            public void Dispose() {
                DisposeCount++;
            }

            public void Trace(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
                Write(new LogEntry(DProjects.Log.LogLevel.Trace, message, fields, tags, source, user, resource, default, spanId, traceId));
            }

            public void Debug(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
                Write(new LogEntry(DProjects.Log.LogLevel.Debug, message, fields, tags, source, user, resource, default, spanId, traceId));
            }

            public void Info(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
                Write(new LogEntry(DProjects.Log.LogLevel.Information, message, fields, tags, source, user, resource, default, spanId, traceId));
            }

            public void Warning(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
                Write(new LogEntry(DProjects.Log.LogLevel.Warning, message, fields, tags, source, user, resource, default, spanId, traceId));
            }

            public void Error(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null, Exception? exception = null) {
                Write(new LogEntry(DProjects.Log.LogLevel.Error, message, fields, tags, source, user, resource, default, spanId, traceId));
            }

            public void Fatal(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null, Exception? exception = null) {
                Write(new LogEntry(DProjects.Log.LogLevel.Fatal, message, fields, tags, source, user, resource, default, spanId, traceId));
            }

            public void Write(LogEntry logEntry) {
                lock (Entries) {
                    Entries.Add(logEntry);
                }
            }
        }

        private sealed class RecordingLogClient : ILogClient {
            public RecordingLogClient(DProjects.Log.LogLevel level) {
                Level = level;
            }

            public event EventHandler<LogEntry>? Writed;
            public List<LogEntry> Entries { get; } = new List<LogEntry>();
            public DProjects.Log.LogLevel Level { get; }
            public string? Prefix { get; set; }
            public string? User { get; set; }
            public string? Source { get; set; }
            public string? Resource { get; set; }
            public string[]? Tags { get; set; }
            public Dictionary<string, object?>? Fields { get; set; }
            public string? SpanId { get; set; }
            public string? TraceId { get; set; }

            public void Trace(string message, params object?[] args) {
            }
            public void Debug(string message, params object?[] args) {
            }
            public void Info(string message, params object?[] args) {
            }
            public void Warning(string message, params object?[] args) {
            }
            public void Error(string message, params object?[] args) {
            }
            public void Fatal(string message, params object?[] args) {
            }
            public void Write(LogEntry logEntry) {
                Entries.Add(logEntry);
                Writed?.Invoke(this, logEntry);
            }
        }
    }
}
