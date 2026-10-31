using DProjects.Factories;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using System.Collections.Concurrent;

namespace DProjects.Log.Tests {

    public class LogOtlpTests {

        [Fact]
        public void LogAssembliesContainOnlyOneOtlpLogFactory() {
            var factoryTypes = new[] {
                typeof(DProjects.Log.Assembly).Assembly,
                typeof(DProjects.Log.OpenTelemetry.Assembly).Assembly
            }
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => typeof(IFactoryByUrl<ILog>).IsAssignableFrom(type))
                .Where(type => type.GetCustomAttributes(false).Any(attribute =>
                    attribute.GetType().Name == "ProtocolAttribute" &&
                    String.Equals(
                        attribute.GetType().GetProperty("Name")?.GetValue(attribute) as string,
                        "otlp",
                        StringComparison.OrdinalIgnoreCase
                    )
                ))
                .ToArray();

            Assert.Single(factoryTypes);
            Assert.Equal(typeof(DProjects.Log.OpenTelemetry.LogOtlpFactory), factoryTypes[0]);
        }

        [Fact]
        public void FactoryParsesEndpointAndMetadata() {
            using var log = Assert.IsType<DProjects.Log.OpenTelemetry.LogOtlp>(
                new DProjects.Log.OpenTelemetry.LogOtlpFactory().Create(
                    "otlp://collector.example:4318?service=orders&scope=worker"
                )
            );

            Assert.Equal(new Uri("http://collector.example:4318/v1/logs"), log.Endpoint);
            Assert.Equal("orders", log.ServiceName);
            Assert.Equal("worker", log.ScopeName);
        }

        [Fact]
        public void FactoryUsesStandardHttpPortWhenPortIsOmitted() {
            using var log = Assert.IsType<DProjects.Log.OpenTelemetry.LogOtlp>(
                new DProjects.Log.OpenTelemetry.LogOtlpFactory().Create("otlp://collector.example")
            );

            Assert.Equal(new Uri("http://collector.example:4318/v1/logs"), log.Endpoint);
        }

        [Fact]
        public void DProjectsEntryReachesOpenTelemetryPipeline() {
            var exporter = new CollectingExporter();
            using var loggerFactory = LoggerFactory.Create(builder => {
                builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                builder.AddOpenTelemetry(options => {
                    options.IncludeFormattedMessage = true;
                    options.ParseStateValues = true;
                    options.AddProcessor(new SimpleLogRecordExportProcessor(exporter));
                });
            });
            using var log = new DProjects.Log.OpenTelemetry.LogOtlp(loggerFactory, "test.scope");

            log.Info(
                "Order accepted",
                new Dictionary<string, object?> { ["orderId"] = 42 },
                ["api"],
                "orders",
                "user-1",
                "order/42",
                "span-1",
                "trace-1"
            );

            var record = Assert.Single(exporter.Records);
            Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, record.LogLevel);
            Assert.StartsWith("Order accepted", record.Body);
            Assert.Equal("test.scope", record.CategoryName);
            var attributes = record.Attributes;
            Assert.Contains(attributes, item => item.Key == "orderId" && Equals(item.Value, 42));
            Assert.Contains(attributes, item => item.Key == "source" && Equals(item.Value, "orders"));
            Assert.Contains(attributes, item => item.Key == "user" && Equals(item.Value, "user-1"));
            Assert.Contains(attributes, item => item.Key == "resource" && Equals(item.Value, "order/42"));
            Assert.Contains(attributes, item => item.Key == "spanId" && Equals(item.Value, "span-1"));
            Assert.Contains(attributes, item => item.Key == "traceId" && Equals(item.Value, "trace-1"));
        }

        [Fact]
        public void DisposingAdapterDoesNotDisposeExternalLoggerFactory() {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(new RecordingLoggerProvider()));
            var log = new DProjects.Log.OpenTelemetry.LogOtlp(loggerFactory);

            log.Dispose();
            log.Dispose();

            loggerFactory.CreateLogger("still-usable").LogInformation("after adapter disposal");
        }


        private sealed class CollectingExporter : BaseExporter<global::OpenTelemetry.Logs.LogRecord> {

            public ConcurrentQueue<LogRecordSnapshot> Records { get; } = new();

            public override ExportResult Export(in Batch<global::OpenTelemetry.Logs.LogRecord> batch) {
                foreach (var record in batch) {
                    var attributes = new List<KeyValuePair<string, object?>>();
                    if (record.Attributes != null) {
                        foreach (var attribute in record.Attributes) {
                            attributes.Add(attribute);
                        }
                    }
                    Records.Enqueue(new LogRecordSnapshot(record.LogLevel, record.Body?.ToString(), record.CategoryName, attributes));
                }
                return ExportResult.Success;
            }
        }

        private sealed class LogRecordSnapshot {

            public LogRecordSnapshot(
                Microsoft.Extensions.Logging.LogLevel logLevel,
                string? body,
                string? categoryName,
                IReadOnlyList<KeyValuePair<string, object?>> attributes
            ) {
                LogLevel = logLevel;
                Body = body;
                CategoryName = categoryName;
                Attributes = attributes;
            }

            public Microsoft.Extensions.Logging.LogLevel LogLevel { get; }
            public string? Body { get; }
            public string? CategoryName { get; }
            public IReadOnlyList<KeyValuePair<string, object?>> Attributes { get; }
        }

        private sealed class RecordingLoggerProvider : ILoggerProvider {

            public ILogger CreateLogger(string categoryName) => new RecordingLogger();
            public void Dispose() {
            }

            private sealed class RecordingLogger : ILogger {

                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
                public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;
                public void Log<TState>(
                    Microsoft.Extensions.Logging.LogLevel logLevel,
                    EventId eventId,
                    TState state,
                    Exception? exception,
                    Func<TState, Exception?, string> formatter
                ) {
                }
            }
        }
    }

}
