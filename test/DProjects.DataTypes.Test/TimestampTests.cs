using System.Globalization;

using DProjects.DataTypes;

namespace DProjects.DataTypes.Tests {

    public class TimestampTests {

        // methods
        [Fact]
        public void UnixAndUtcFactories_RoundTripDeterministically() {
            var utc = new DateTime(2024, 2, 29, 12, 34, 56, 789, DateTimeKind.Utc);
            var timestamp = Timestamp.FromDateTimeUtc(utc);

            Assert.Equal(utc, timestamp.ToDateTimeUtc());
            Assert.Equal(DateTimeKind.Utc, timestamp.ToDateTimeUtc().Kind);
            Assert.Equal(timestamp.UnixMs, timestamp.TotalMilliseconds);
            Assert.Equal(timestamp.UnixMs / 1000, timestamp.TotalSeconds);
            Assert.Equal(timestamp, Timestamp.FromUnixMilliseconds(timestamp.UnixMs));
        }
        [Theory]
        [InlineData(DateTimeKind.Local)]
        [InlineData(DateTimeKind.Unspecified)]
        public void UtcFactory_RejectsAmbiguousNonUtcDateTimes(DateTimeKind kind) {
            var value = DateTime.SpecifyKind(new DateTime(2024, 1, 1), kind);

            Assert.Throws<ArgumentException>(() => Timestamp.FromDateTimeUtc(value));
        }
        [Fact]
        public void CalendarAndDurationOperations_PreserveUtcSemantics() {
            var timestamp = Timestamp.FromDateTimeUtc(new DateTime(2024, 1, 31, 23, 30, 0, DateTimeKind.Utc));

            Assert.Equal(new DateTime(2024, 2, 29, 23, 30, 0, DateTimeKind.Utc), timestamp.AddMonths(1).ToDateTimeUtc());
            Assert.Equal(new DateTime(2024, 2, 1, 23, 30, 0, DateTimeKind.Utc), timestamp.AddDays(1).ToDateTimeUtc());
            Assert.Equal(new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc), timestamp.ToMidnight().ToDateTimeUtc());
            Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), timestamp.ToFirstDayOfMonth().ToDateTimeUtc());
        }
        [Fact]
        public void EqualityComparisonOrderingAndConversions_UseUnixMilliseconds() {
            Timestamp earlier = 1000L;
            Timestamp same = 1000L;
            Timestamp later = 2000L;

            Assert.Equal(earlier, same);
            Assert.Equal(earlier.GetHashCode(), same.GetHashCode());
            Assert.True(earlier < later);
            Assert.True(later > earlier);
            Assert.True(earlier <= same);
            Assert.True(same >= earlier);
            Assert.Equal(-1, Math.Sign(earlier.CompareTo(later)));
            Assert.Equal(1000L, (long)earlier);
        }
        [Fact]
        public void Parse_HandlesUnixMillisecondsAndOffsetTextAsUtc() {
            var fromNumber = Timestamp.Parse("1704067200000");
            var fromOffset = Timestamp.Parse("2024-01-01T01:00:00+01:00");

            Assert.Equal(fromNumber, fromOffset);
            Assert.Equal("2024-01-01T00:00:00.0000000Z", fromOffset.ToString());
            Assert.Equal("2024-01-01", fromOffset.ToString("yyyy-MM-dd"));
        }
        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-timestamp")]
        public void TryParse_InvalidInputReturnsFalseWithoutThrowing(string? input) {
            Assert.False(Timestamp.TryParse(input, out var result));
            Assert.Equal(default, result);
        }
        [Fact]
        public void UnixConversion_RejectsOutOfRangeDates() {
            var invalid = Timestamp.FromUnixMilliseconds(long.MaxValue);

            Assert.Throws<ArgumentOutOfRangeException>(() => invalid.ToDateTimeUtc());
        }
    }
}
