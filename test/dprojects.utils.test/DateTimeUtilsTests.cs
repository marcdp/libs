using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using System.Net;
using static System.Net.Mime.MediaTypeNames;

namespace DProjects.Utils.Tests {
    public class DateTimeUtilsTests {


        //private utils        
        private object ProcessEncodedValue(object? value) {
            if (value != null && value.ToString()!.StartsWith("date:")) value = DateTime.Parse(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            if (value != null && value.ToString()!.StartsWith("timespan:")) value = TimeSpan.Parse(value.ToString()!.Substring(value.ToString()!.IndexOf(":") + 1));
            return value!;

        }
        [Theory()]
        [InlineData("timespan:6", "144 hours")]
        [InlineData("timespan:6:12:14", "6 hours")]
        [InlineData("timespan:6:12:14:45", "156 hours")]
        public void FormatTest(object value, string result) {
            value = ProcessEncodedValue(value);
            Assert.Equal(result, DateTimeUtils.Format((TimeSpan)value));
        }

        [Theory()]
        [InlineData(0, "0 ms")]
        [InlineData(123, "123 ms")]
        [InlineData(1000, "1 sec")]
        [InlineData(2000, "2 secs")]
        [InlineData(59000, "59 secs")]
        [InlineData(60000, "00:01:00 sec")]
        [InlineData(2*61000, "00:02:02 sec")]
        public void FormatMillisecondsTest(double value, string result) {
            Assert.Equal(result, DateTimeUtils.FormatMilliseconds(value));
        }
        [Theory()]
        [InlineData(0, "0 secs")]
        [InlineData(1, "1 sec")]
        [InlineData(2, "2 secs")]
        [InlineData(59, "59 secs")]
        [InlineData(62, "00:01:02 sec")]
        [InlineData(189, "00:03:09 sec")]
        public void FormatSeconds(int value, string result) {
            Assert.Equal(result, DateTimeUtils.FormatSeconds(value));
        }

        [Theory()]
        [InlineData(0, "00:00:00")]
        [InlineData(1, "00:00:01")]
        [InlineData(60, "00:01:00")]
        [InlineData(120, "00:02:00")]
        [InlineData(121, "00:02:01")]
        public void FormatSecondsHHMMSSTest(int value, string result) {
            Assert.Equal(result, DateTimeUtils.FormatSecondsHHMMSS(value));
        }

        [Theory()]
        [InlineData(0, "00:00")]
        [InlineData(1, "00:00")]
        [InlineData(60, "00:01")]
        [InlineData(120, "00:02")]
        [InlineData(121, "00:02")]
        public void FormatSecondsHHMMTest(int value, string result) {
            Assert.Equal(result, DateTimeUtils.FormatSecondsHHMM(value));
        }

        [Theory()]
        [InlineData(0, "00:00")]
        [InlineData(1, "00:00")]
        [InlineData(60, "00:01")]
        [InlineData(120, "00:02")]
        [InlineData(121, "00:02")]
        [InlineData(986121, "11d 09:55")]
        public void FormatSecondsDDHHMMTest(int value, string result) {
            Assert.Equal(result, DateTimeUtils.FormatSecondsDDHHMM(value));
        }

        [Theory()]
        [InlineData("2020-01-01T16:16:16.123", "2020-01-01T16:16:16.898", true)]
        public void EqualsWithoutMillisecondsTest(string value1, string value2, bool equal) {
            var v1 = DateTime.Parse(value1);
            var v2 = DateTime.Parse(value2);
            if (equal) {
                Assert.NotEqual(v1, v2);
            } else {
                Assert.Equal(v1, v2);
            }
        }


        [Theory()]
        [InlineData("2020-01-01T16:16:16.1Z", 637134957761000000, true)]
        [InlineData("2020-01-01T16:16:16.12Z", 637134957761200000, true)]
        [InlineData("2020-01-01T16:16:16.123Z",  637134957761230000, true)]
        [InlineData("2020-01-01T16:16:16.1235Z", 637134957761235000, true)]
        [InlineData("2020-01-01T16:16:16.1235123Z", 637134957761235123, true)]
        [InlineData("2020-01-01Z", 0, false)]
        public void TryParseTest(string value1, long ticks, bool equal) {
            if (DateTimeUtils.TryParse(value1, out DateTime result)) {
                Assert.True(equal);
                Assert.Equal(ticks, result.Ticks);
            } else {
                Assert.False(equal);
            }
        }

        [Theory()]
        [InlineData(0, "now")]
        [InlineData(1, "1 second ago")]
        [InlineData(2, "2 seconds ago")]
        [InlineData(61, "1 minute ago")]
        [InlineData(121, "2 minutes ago")]
        [InlineData(60*60, "1 hour ago")]
        [InlineData(4*60*60, "4 hours ago")]
        [InlineData(14 * 60 * 60, "yesterday")]
        [InlineData(24 * 60 * 60, "yesterday")]
        [InlineData(4*24 * 60 * 60, "3 days ago")]
        public void DateTimeToTextRelativeTest(int seconds, string result) {
            var d1 = DateTime.Parse("2020-01-01T16:00:00");
            var d2 = d1.AddSeconds(seconds);
            Assert.Equal(result, DateTimeUtils.DateTimeToTextRelative(d2, d1));
        }

        [Theory()]
        [InlineData("2020-01-01", "2020-01-02", "2020-01-01T01:00:00", "2020-01-01T01:00:00", true)]
        [InlineData("2020-01-01", "2020-01-02", "2020-01-03", "2020-01-04", false)]
        [InlineData("2020-01-01", "2020-01-02", "2020-01-02", "2020-01-03", true)]
        public void AreSpansOverlappingTest(string sd1, string ed1, string sd2, string ed2, bool result) {
            var d1 = DateTimeUtils.Parse(sd1);
            var e1 = DateTimeUtils.Parse(ed1);
            var d2 = DateTimeUtils.Parse(sd2);
            var e2 = DateTimeUtils.Parse(ed2);
            Assert.Equal(result, DateTimeUtils.AreSpansOverlapping(d1, e1, d2, e2));
        }
    }
}