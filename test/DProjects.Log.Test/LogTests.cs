using Microsoft.Extensions.Logging;
using LogLevelNative = Microsoft.Extensions.Logging.LogLevel;

namespace DProjects.Log.Tests {

    public class LogTests {

        [Theory]
        [InlineData(DProjects.Log.LogLevel.Information, DProjects.Log.LogLevel.Information, true)]
        [InlineData(DProjects.Log.LogLevel.Information, DProjects.Log.LogLevel.Custom, true)]
        [InlineData(DProjects.Log.LogLevel.Information, DProjects.Log.LogLevel.Warning, true)]
        [InlineData(DProjects.Log.LogLevel.Information, DProjects.Log.LogLevel.Trace, false)]
        [InlineData(DProjects.Log.LogLevel.Information, DProjects.Log.LogLevel.Debug, false)]
        [InlineData(DProjects.Log.LogLevel.Warning, DProjects.Log.LogLevel.Information, false)]
        [InlineData(DProjects.Log.LogLevel.Warning, DProjects.Log.LogLevel.Custom, false)]
        [InlineData(DProjects.Log.LogLevel.Warning, DProjects.Log.LogLevel.Warning, true)]
        [InlineData(DProjects.Log.LogLevel.Warning, DProjects.Log.LogLevel.Error, true)]
        [InlineData(DProjects.Log.LogLevel.Warning, DProjects.Log.LogLevel.Fatal, true)]
        [InlineData(DProjects.Log.LogLevel.Fatal, DProjects.Log.LogLevel.Custom, false)]
        public void Write_FiltersUsingEffectiveSeverity(
            DProjects.Log.LogLevel minimumLevel,
            DProjects.Log.LogLevel entryLevel,
            bool expectedEmitted
        ) {
            using var log = new RecordingLog(minimumLevel);

            log.Write(new LogEntry(entryLevel, "message"));

            Assert.Equal(expectedEmitted ? 1 : 0, log.Entries.Count);
        }

        [Theory]
        [InlineData(DProjects.Log.LogLevel.Trace, LogLevelNative.Trace)]
        [InlineData(DProjects.Log.LogLevel.Debug, LogLevelNative.Debug)]
        [InlineData(DProjects.Log.LogLevel.Information, LogLevelNative.Information)]
        [InlineData(DProjects.Log.LogLevel.Custom, LogLevelNative.Information)]
        [InlineData(DProjects.Log.LogLevel.Warning, LogLevelNative.Warning)]
        [InlineData(DProjects.Log.LogLevel.Error, LogLevelNative.Error)]
        [InlineData(DProjects.Log.LogLevel.Fatal, LogLevelNative.Critical)]
        public void LogLogger_WriteMapsToExpectedNativeLevel(
            DProjects.Log.LogLevel level,
            LogLevelNative expectedLevel
        ) {
            var logger = new RecordingLogger();
            using var log = new LogLogger(logger, DProjects.Log.LogLevel.Trace);

            log.Write(new LogEntry(level, "message"));

            Assert.Equal(expectedLevel, Assert.Single(logger.Entries));
        }

        [Fact]
        public void Write_InvalidLevelThrowsArgumentOutOfRangeException() {
            using var log = new RecordingLog(DProjects.Log.LogLevel.Trace);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                log.Write(new LogEntry((DProjects.Log.LogLevel)999, "message"))
            );
            Assert.Empty(log.Entries);
        }

        [Fact]
        public void Write_InvalidMinimumLevelThrowsArgumentOutOfRangeException() {
            using var log = new RecordingLog((DProjects.Log.LogLevel)999);

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                log.Write(new LogEntry(DProjects.Log.LogLevel.Information, "message"))
            );
            Assert.Empty(log.Entries);
        }

        [Fact]
        public void Fatal_PreservesTraceMetadataAndException() {
            using var log = new RecordingLog(DProjects.Log.LogLevel.Trace);
            var exception = new InvalidOperationException("fatal failure");

            log.Fatal("fatal", spanId: "span-1", traceId: "trace-1", exception: exception);

            var entry = Assert.Single(log.Entries);
            Assert.Equal("span-1", entry.SpanId);
            Assert.Equal("trace-1", entry.TraceId);
            Assert.Contains(
                "fatal failure",
                Assert.IsType<string>(Assert.IsAssignableFrom<IDictionary<string, object?>>(entry.Fields)["exception"])
            );
        }

        private sealed class RecordingLog : Log {

            public List<LogEntry> Entries { get; } = new List<LogEntry>();

            public RecordingLog(DProjects.Log.LogLevel level) : base(false, false, level) {
            }

            protected override void ProcessEntry(LogEntry logEntry) {
                Entries.Add(logEntry);
            }
        }

        private sealed class RecordingLogger : ILogger {

            public List<LogLevelNative> Entries { get; } = new List<LogLevelNative>();

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevelNative logLevel) => true;

            public void Log<TState>(
                LogLevelNative logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            ) {
                Entries.Add(logLevel);
            }
        }
    }
}
