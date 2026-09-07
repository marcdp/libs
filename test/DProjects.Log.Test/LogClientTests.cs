namespace DProjects.Log.Tests {

    public class LogClientTests {

        [Fact]
        public void Template_BasicPlaceholderPreservesRenderedAndStructuredValue() {
            // verifies a basic template is rendered and captured as a structured field
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info("Order {Id}", 42);

            var entry = Assert.Single(log.Entries);
            Assert.Equal("Order 42", entry.Message);
            Assert.Equal(42, entry.Fields!["Id"]);
        }

        [Fact]
        public void Template_DoesNotAddMessageOriginalField() {
            // verifies parser bridge metadata does not leak into application fields
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info("Order {Id}", 42);

            Assert.False(Assert.Single(log.Entries).Fields!.ContainsKey("messageOriginal"));
        }

        [Fact]
        public void Template_MultiplePlaceholdersPreserveRenderedAndStructuredValues() {
            // verifies multiple template values retain their names and values
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info("Order {Id} for {User}", 42, "Marc");

            var entry = Assert.Single(log.Entries);
            Assert.Equal("Order 42 for Marc", entry.Message);
            Assert.Equal(42, entry.Fields!["Id"]);
            Assert.Equal("Marc", entry.Fields["User"]);
        }

        [Fact]
        public void Template_RepeatedPlaceholderUsesLastValueInFields() {
            // verifies repeated names render positionally and retain the last structured value
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info("{Id} -> {Id}", 1, 2);

            var entry = Assert.Single(log.Entries);
            Assert.Equal("1 -> 2", entry.Message);
            Assert.Equal(2, entry.Fields!["Id"]);
        }

        [Fact]
        public void Template_MissingArgumentPreservesUnresolvedPlaceholder() {
            // verifies a missing argument remains literal without creating a field
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info("Order {Id} {State}", 42);

            var entry = Assert.Single(log.Entries);
            Assert.Equal("Order 42 {State}", entry.Message);
            Assert.Equal(42, entry.Fields!["Id"]);
            Assert.False(entry.Fields.ContainsKey("State"));
        }

        [Fact]
        public void Template_ExtraArgumentsAreIgnored() {
            // verifies arguments without matching placeholders do not affect the entry
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info("Order {Id}", 42, "extra");

            var entry = Assert.Single(log.Entries);
            Assert.Equal("Order 42", entry.Message);
            Assert.Equal(42, entry.Fields!["Id"]);
            Assert.Single(entry.Fields);
        }

        [Fact]
        public void Template_EscapedBracesAreTreatedAsLiteralText() {
            // verifies escaped braces render as literal braces without creating fields
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info("Value {{literal}}");

            var entry = Assert.Single(log.Entries);
            Assert.Equal("Value {literal}", entry.Message);
            Assert.Null(entry.Fields);
        }

        [Theory]
        [InlineData("Order {Id")]
        [InlineData("Order Id}")]
        public void Template_MalformedBracesAreTreatedLiterally(string message) {
            // verifies malformed braces are preserved safely and deterministically
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info(message);

            var entry = Assert.Single(log.Entries);
            Assert.Equal(message, entry.Message);
            Assert.Null(entry.Fields);
        }

        [Fact]
        public void Template_FormatAndAlignmentUseSemanticFieldNames() {
            // verifies format and alignment suffixes are excluded from structured field names
            var log = new RecordingLog();
            var client = new LogClient(log);

            client.Info("{Amount:N2} {Name,-20}", 12.5m, "Marc");

            var fields = Assert.Single(log.Entries).Fields!;
            Assert.Equal(12.5m, fields["Amount"]);
            Assert.Equal("Marc", fields["Name"]);
            Assert.False(fields.ContainsKey("Amount:N2"));
            Assert.False(fields.ContainsKey("Name,-20"));
        }

        [Fact]
        public void TemplateFieldsOverrideConfiguredFieldsAndPreserveNonOverlappingFields() {
            // verifies template fields use the same merge precedence as LogClientLogger
            var log = new RecordingLog();
            var client = new LogClient(log) {
                Fields = new Dictionary<string, object?> {
                    ["Id"] = 1,
                    ["Tenant"] = "north"
                }
            };

            client.Info("Order {Id}", 42);

            var fields = Assert.Single(log.Entries).Fields!;
            Assert.Equal(42, fields["Id"]);
            Assert.Equal("north", fields["Tenant"]);
        }

        [Fact]
        public void ConvenienceMethod_PreservesConfiguredMetadata() {
            // verifies parser use does not alter configured entry metadata
            var log = new RecordingLog();
            var tags = new[] { "important", "orders" };
            var client = new LogClient(log) {
                Prefix = "prefix: ",
                Tags = tags,
                Source = "checkout",
                User = "user-7",
                Resource = "order-42",
                SpanId = "span-1",
                TraceId = "trace-1"
            };

            client.Info("Order {Id}", 42);

            var entry = Assert.Single(log.Entries);
            Assert.Equal("prefix: Order 42", entry.Message);
            Assert.Same(tags, entry.Tags);
            Assert.Equal("checkout", entry.Source);
            Assert.Equal("user-7", entry.User);
            Assert.Equal("order-42", entry.Resource);
            Assert.Equal("span-1", entry.SpanId);
            Assert.Equal("trace-1", entry.TraceId);
        }

        [Fact]
        public void ConvenienceMethod_RaisesWritedExactlyOnceWithWrittenEntry() {
            // verifies the event receives exactly the entry sent to the underlying log
            var log = new RecordingLog();
            var client = new LogClient(log);
            var count = 0;
            LogEntry? receivedEntry = null;
            client.Writed += (_, entry) => {
                count++;
                receivedEntry = entry;
            };

            client.Info("Order {Id}", 42);

            var writtenEntry = Assert.Single(log.Entries);
            Assert.Equal(1, count);
            Assert.Same(writtenEntry, receivedEntry);
        }

        private sealed class RecordingLog : ILog {

            public LogLevel Level => LogLevel.Trace;
            public List<LogEntry> Entries { get; } = new List<LogEntry>();

            public void Dispose() {
            }

            public void Trace(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
                Write(new LogEntry(LogLevel.Trace, message, fields, tags, source, user, resource, spanId: spanId, traceId: traceId));
            }

            public void Debug(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
                Write(new LogEntry(LogLevel.Debug, message, fields, tags, source, user, resource, spanId: spanId, traceId: traceId));
            }

            public void Info(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
                Write(new LogEntry(LogLevel.Information, message, fields, tags, source, user, resource, spanId: spanId, traceId: traceId));
            }

            public void Warning(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null) {
                Write(new LogEntry(LogLevel.Warning, message, fields, tags, source, user, resource, spanId: spanId, traceId: traceId));
            }

            public void Error(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null, Exception? exception = null) {
                Write(new LogEntry(LogLevel.Error, message, fields, tags, source, user, resource, spanId: spanId, traceId: traceId));
            }

            public void Fatal(string message, IDictionary<string, object?>? fields = null, string[]? tags = null, string? source = null, string? user = null, string? resource = null, string? spanId = null, string? traceId = null, Exception? exception = null) {
                Write(new LogEntry(LogLevel.Fatal, message, fields, tags, source, user, resource, spanId: spanId, traceId: traceId));
            }

            public void Write(LogEntry logEntry) {
                Entries.Add(logEntry);
            }
        }
    }
}
