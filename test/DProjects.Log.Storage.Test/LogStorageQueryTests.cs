namespace DProjects.Log.Storage.Tests {

    public class LogStorageQueryTests {

        // methods
        [Fact]
        public void Check_AppliesInclusiveDatesMinimumLevelAndExactFields() {
            var date = new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
            var entry = new LogEntry(LogLevel.Warning, "prefix message suffix", tags: ["tag"], source: "source", user: "user", aDate: date);
            var query = new LogStorageQuery { From = date, To = date, Level = LogLevel.Warning, Message = "message", Tag = "tag", Source = "source", User = "user" };

            Assert.True(query.Check(entry));
            query.Level = LogLevel.Error;
            Assert.False(query.Check(entry));
        }
        [Fact]
        public void Check_RejectsInvalidDateRange() {
            var query = new LogStorageQuery { From = new DateTime(2026, 1, 2), To = new DateTime(2026, 1, 1) };

            Assert.Throws<ArgumentException>(() => query.Check(new LogEntry()));
        }
        [Fact]
        public void Check_TreatsEmptyExactFiltersAsValues() {
            var entry = new LogEntry(LogLevel.Information, "message", tags: [""], source: "", user: "", aDate: DateTime.MinValue);
            var query = new LogStorageQuery { Tag = "", Source = "", User = "" };

            Assert.True(query.Check(entry));
        }
        [Fact]
        public void RawDeserializer_UsesStableUnknownTimestamp() {
            var deserializer = new DProjects.Log.Storage.Serializers.LogStorageEntryDeserializerRaw();

            var first = deserializer.Deserialize("record");
            var second = deserializer.Deserialize("record");

            Assert.Equal(DateTime.MinValue, first.Date);
            Assert.Equal(first.Date, second.Date);
        }
    }
}
