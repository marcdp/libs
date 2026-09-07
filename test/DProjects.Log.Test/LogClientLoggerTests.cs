using Microsoft.Extensions.Logging;
using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Tests {

    public class LogClientLoggerTests {

        [Fact]
        public void InterfaceTrace_LogsAtNativeTraceLevelAndIncludesInterfaceResource() {
            var logger = new RecordingLogger();
            ILogClient client = new LogClientLogger(logger);
            LogEntry? writtenEntry = null;
            client.Resource = "orders";
            client.Writed += (_, entry) => writtenEntry = entry;

            client.Trace("message");

            Assert.Equal(LogLevelNative.Trace, Assert.Single(logger.Entries).Level);
            Assert.Equal("orders", Assert.IsType<LogEntry>(writtenEntry).Resource);
        }

        [Theory]
        [InlineData(DProjects.Log.LogLevel.Debug, LogLevelNative.Debug)]
        [InlineData(DProjects.Log.LogLevel.Information, LogLevelNative.Information)]
        [InlineData(DProjects.Log.LogLevel.Warning, LogLevelNative.Warning)]
        [InlineData(DProjects.Log.LogLevel.Error, LogLevelNative.Error)]
        [InlineData(DProjects.Log.LogLevel.Fatal, LogLevelNative.Critical)]
        public void ConvenienceMethods_MapToExpectedNativeLevel(
            DProjects.Log.LogLevel level,
            LogLevelNative expectedLevel
        ) {
            var logger = new RecordingLogger();
            var client = new LogClientLogger(logger);

            switch (level) {
                case DProjects.Log.LogLevel.Debug:
                    client.Debug("message");
                    break;
                case DProjects.Log.LogLevel.Information:
                    client.Info("message");
                    break;
                case DProjects.Log.LogLevel.Warning:
                    client.Warning("message");
                    break;
                case DProjects.Log.LogLevel.Error:
                    client.Error("message");
                    break;
                case DProjects.Log.LogLevel.Fatal:
                    client.Fatal("message");
                    break;
            }

            Assert.Equal(expectedLevel, Assert.Single(logger.Entries).Level);
        }

        [Theory]
        [InlineData(DProjects.Log.LogLevel.Information, LogLevelNative.Information)]
        [InlineData(DProjects.Log.LogLevel.Trace, LogLevelNative.Trace)]
        [InlineData(DProjects.Log.LogLevel.Debug, LogLevelNative.Debug)]
        [InlineData(DProjects.Log.LogLevel.Warning, LogLevelNative.Warning)]
        [InlineData(DProjects.Log.LogLevel.Error, LogLevelNative.Error)]
        [InlineData(DProjects.Log.LogLevel.Fatal, LogLevelNative.Critical)]
        [InlineData(DProjects.Log.LogLevel.Custom, LogLevelNative.Information)]
        public void Write_PreservesLevelAndMessage(
            DProjects.Log.LogLevel level,
            LogLevelNative expectedLevel
        ) {
            var logger = new RecordingLogger();
            var client = new LogClientLogger(logger);
            var entry = new LogEntry(level, "original message");

            client.Write(entry);

            var recorded = Assert.Single(logger.Entries);
            Assert.Equal(expectedLevel, recorded.Level);
            Assert.Equal("original message", recorded.Message);
        }

        [Fact]
        public void Write_InvalidLevelThrowsArgumentOutOfRangeException() {
            var logger = new RecordingLogger();
            ILogClient client = new LogClientLogger(logger);
            var entry = new LogEntry((DProjects.Log.LogLevel)999, "message");

            Assert.Throws<ArgumentOutOfRangeException>(() => client.Write(entry));
            Assert.Empty(logger.Entries);
        }

        [Fact]
        public void Write_PreservesStructuredFieldsAndMetadata() {
            var logger = new RecordingLogger();
            var client = new LogClientLogger(logger);
            var tags = new[] { "important", "orders" };
            var entry = new LogEntry(
                DProjects.Log.LogLevel.Information,
                "order updated",
                new Dictionary<string, object?> {
                    ["orderId"] = 42,
                    ["state"] = "ready"
                },
                tags,
                "checkout",
                "user-7",
                "order-42",
                spanId: "span-1",
                traceId: "trace-1"
            );

            client.Write(entry);

            var state = Assert.Single(logger.Entries).State;
            Assert.Equal(42, state["orderId"]);
            Assert.Equal("ready", state["state"]);
            Assert.Same(tags, state["tags"]);
            Assert.Equal("checkout", state["source"]);
            Assert.Equal("user-7", state["user"]);
            Assert.Equal("order-42", state["resource"]);
            Assert.Equal("span-1", state["spanId"]);
            Assert.Equal("trace-1", state["traceId"]);
        }

        [Fact]
        public void Write_RaisesWritedExactlyOnceWithSuppliedEntry() {
            var logger = new RecordingLogger();
            var client = new LogClientLogger(logger);
            var entry = new LogEntry(DProjects.Log.LogLevel.Information, "message");
            var count = 0;
            LogEntry? receivedEntry = null;
            client.Writed += (_, received) => {
                count++;
                receivedEntry = received;
            };

            client.Write(entry);

            Assert.Equal(1, count);
            Assert.Same(entry, receivedEntry);
        }

        [Fact]
        public void ConvenienceMethod_RaisesWritedExactlyOnce() {
            var logger = new RecordingLogger();
            var client = new LogClientLogger(logger);
            var count = 0;
            client.Writed += (_, _) => count++;

            client.Info("message");

            Assert.Equal(1, count);
        }

        [Fact]
        public void ConvenienceMethod_PropagatesConfiguredMetadataAndTemplateFields() {
            var logger = new RecordingLogger();
            var client = new LogClientLogger(logger) {
                Prefix = "prefix: ",
                Source = "checkout",
                User = "user-7",
                Resource = "order-42",
                SpanId = "span-1",
                TraceId = "trace-1",
                Fields = new Dictionary<string, object?> { ["tenant"] = "north" }
            };
            var writtenCount = 0;
            client.Writed += (_, _) => writtenCount++;

            client.Info("Order {Id}", 42);

            var recorded = Assert.Single(logger.Entries);
            Assert.Equal(42, recorded.State["Id"]);
            Assert.Equal("north", recorded.State["tenant"]);
            Assert.Equal("checkout", recorded.State["source"]);
            Assert.Equal("user-7", recorded.State["user"]);
            Assert.Equal("order-42", recorded.State["resource"]);
            Assert.Equal("span-1", recorded.State["spanId"]);
            Assert.Equal("trace-1", recorded.State["traceId"]);
            Assert.Equal(1, writtenCount);
        }

        private sealed class RecordingLogger : ILogger {

            public List<RecordedLog> Entries { get; } = new List<RecordedLog>();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevelNative logLevel) => true;

            public void Log<TState>(
                LogLevelNative logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            ) {
                var structuredState = state as IEnumerable<KeyValuePair<string, object?>>;
                Entries.Add(new RecordedLog(
                    logLevel,
                    formatter(state, exception),
                    structuredState?.ToDictionary(item => item.Key, item => item.Value)
                        ?? new Dictionary<string, object?>(),
                    exception
                ));
            }
        }

        private sealed class RecordedLog {

            public LogLevelNative Level { get; }
            public string Message { get; }
            public IReadOnlyDictionary<string, object?> State { get; }
            public Exception? Exception { get; }

            public RecordedLog(
                LogLevelNative level,
                string message,
                IReadOnlyDictionary<string, object?> state,
                Exception? exception
            ) {
                Level = level;
                Message = message;
                State = state;
                Exception = exception;
            }
        }
    }
}
