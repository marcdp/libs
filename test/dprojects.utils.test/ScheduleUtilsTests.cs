using Xunit;
using DProjects.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace DProjects.Utils.Tests {


    public class ScheduleUtilsTests {

        [Theory()]
        [InlineData("2020-01-01T00:00:01", "M|1|0|0", "2020-01-01T00:00:00", false, "2020-01-01T00:01:00")]
        [InlineData("2020-01-01T00:00:01", "M|10|0|0", "2020-01-01T00:00:00", false, "2020-01-01T00:10:00")]
        [InlineData("2020-01-01T00:00:01", "M|10|10|0", "2020-01-01T00:00:00", false, "2020-01-01T00:10:10")]
        [InlineData("2020-01-01T00:00:01", "H|1|0|0", "2020-01-01T00:00:00", false, "2020-01-01T01:00:00")]
        [InlineData("2020-01-01T00:00:01", "H|1|10|0", "2020-01-01T00:00:00", false, "2020-01-01T01:10:00")]
        [InlineData("2020-01-01T00:00:01", "H|6|10|0", "2020-01-01T00:00:00", false, "2020-01-01T06:10:00")]
        public void GetNextScheduleTest(string dateNowStr, string schedule, string lastScheduleStr, bool useUniversalTime, string resultString) {
            var dateNow = DateTime.Parse(dateNowStr);
            var lastSchedule = lastScheduleStr.Equals("") ? default(DateTime) : DateTime.Parse(lastScheduleStr);
            var result = DateTime.Parse(resultString);
            Assert.Equal(result, ScheduleUtils.GetNextSchedule(dateNow, schedule, lastSchedule, useUniversalTime));
        }
    }
}