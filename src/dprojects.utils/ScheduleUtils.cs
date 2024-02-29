using System;
using System.Globalization;

namespace DProjects.Utils {


    public static class ScheduleUtils {


        //schedule
        public static DateTime GetNextSchedule(DateTime dateNow, string schedule, DateTime lastSchedule, bool useUniversalTime) {
            if (lastSchedule == default) {
                lastSchedule = DateTime.Today.AddSeconds(-1);
            }
            DateTime nextExecutionTime = lastSchedule;
            string[] scheduleArray = schedule.Split('|');
            if (StringUtils.Equals(scheduleArray[0], "NOW")) {
                if (dateNow == default) {
                    if (useUniversalTime) {
                        return DateTime.Now.ToUniversalTime();
                    } else {
                        return DateTime.Now;
                    }
                }
                return dateNow;
            } else if (StringUtils.Equals(scheduleArray[0], "EXACT")) {
                if (useUniversalTime) {
                    DateTime.TryParseExact(scheduleArray[1], "yyyy-MM-dd-HH-mm-ss", null, DateTimeStyles.AssumeUniversal, out nextExecutionTime);
                } else {
                    DateTime.TryParseExact(scheduleArray[1], "yyyy-MM-dd-HH-mm-ss", null, DateTimeStyles.AssumeLocal, out nextExecutionTime);
                }
                return nextExecutionTime;
            }
            string type = scheduleArray[0];
            int value1 = int.Parse(scheduleArray[1]);
            int value2 = int.Parse(scheduleArray[2]);
            int value3 = int.Parse(scheduleArray[3]);
            switch (type) {
                case "L":
                    int everyXMilliseconds = value1;
                    while (nextExecutionTime <= dateNow) {
                        nextExecutionTime = nextExecutionTime.AddMilliseconds(everyXMilliseconds);
                        nextExecutionTime = new DateTime(nextExecutionTime.Year, nextExecutionTime.Month, nextExecutionTime.Day, nextExecutionTime.Hour, nextExecutionTime.Minute, nextExecutionTime.Second, nextExecutionTime.Millisecond, useUniversalTime ? DateTimeKind.Utc : DateTimeKind.Local);
                    }
                    break;
                case "S":
                    int everyXSeconds = value1;
                    int atMillisecond_1 = value2;
                    int atMicrosecond = value3;
                    while (nextExecutionTime <= dateNow) {
                        nextExecutionTime = nextExecutionTime.AddSeconds(everyXSeconds);
                        nextExecutionTime = new DateTime(nextExecutionTime.Year, nextExecutionTime.Month, nextExecutionTime.Day, nextExecutionTime.Hour, nextExecutionTime.Minute, nextExecutionTime.Second, atMillisecond_1, useUniversalTime ? DateTimeKind.Utc : DateTimeKind.Local);
                    }
                    break;
                case "M":
                    int everyXMinutes = value1;
                    int atSecond_1 = value2;
                    int atMillisecond = value3;
                    while (nextExecutionTime < dateNow) {
                        nextExecutionTime = nextExecutionTime.AddMinutes(everyXMinutes);
                        nextExecutionTime = new DateTime(nextExecutionTime.Year, nextExecutionTime.Month, nextExecutionTime.Day, nextExecutionTime.Hour, nextExecutionTime.Minute, atSecond_1, atMillisecond, useUniversalTime ? DateTimeKind.Utc : DateTimeKind.Local);
                    }
                    break;
                case "H":
                    int everyXHours = value1;
                    int atMinute_1 = value2;
                    int atSecond = value3;
                    while (nextExecutionTime < dateNow) {
                        nextExecutionTime = nextExecutionTime.AddHours(everyXHours);
                        nextExecutionTime = new DateTime(nextExecutionTime.Year, nextExecutionTime.Month, nextExecutionTime.Day, nextExecutionTime.Hour, atMinute_1, atSecond, useUniversalTime ? DateTimeKind.Utc : DateTimeKind.Local);
                    }
                    break;
                case "D":
                    int everyXDays = value1;
                    int atHour_1 = value2;
                    int atMinute = value3;
                    while (nextExecutionTime < dateNow) {
                        nextExecutionTime = nextExecutionTime.AddDays(everyXDays);
                        nextExecutionTime = new DateTime(nextExecutionTime.Year, nextExecutionTime.Month, nextExecutionTime.Day, atHour_1, atMinute, 0, useUniversalTime ? DateTimeKind.Utc : DateTimeKind.Local);
                    }
                    break;
                case "MM":
                    int everyXMonths = value1;
                    int atDay_1 = value2;
                    int atHour = value3;
                    while (nextExecutionTime < dateNow) {
                        nextExecutionTime = nextExecutionTime.AddMonths(everyXMonths);
                        nextExecutionTime = new DateTime(nextExecutionTime.Year, nextExecutionTime.Month, atDay_1, atHour, 0, 0, useUniversalTime ? DateTimeKind.Utc : DateTimeKind.Local);
                    }
                    break;
                case "Y":
                    int everyXYears = value1;
                    int atMonth = value2;
                    int atDay = value3;
                    while (nextExecutionTime < dateNow) {
                        nextExecutionTime = nextExecutionTime.AddYears(everyXYears);
                        nextExecutionTime = new DateTime(nextExecutionTime.Year, atMonth, atDay, 0, 0, 0, useUniversalTime ? DateTimeKind.Utc : DateTimeKind.Local);
                    }
                    break;
                default:
                    throw new Exception("Invalid schedule:" + type + "," + value1 + "," + value2 + "," + value3);
            }
            if (nextExecutionTime < dateNow) {
                nextExecutionTime = dateNow;
                nextExecutionTime = nextExecutionTime.AddMinutes(1);
            }
            return nextExecutionTime;
        }
    }
}