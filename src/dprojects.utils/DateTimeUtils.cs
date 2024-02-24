using System;
using System.Globalization;

namespace DProjects.Utils {

    public static class DateTimeUtils {

        //constants
        public const string DATETIME_ISO8601 = "yyyy-MM-ddTHH:mm:ssK";
        public const string DATETIME_ISO8601_MS = "yyyy-MM-ddTHH:mm:ss.fffK";
        public const string DATETIME_ISO8601_MS2 = "yyyy-MM-ddTHH:mm:ss.ffK";
        public const string DATETIME_ISO8601_MS4 = "yyyy-MM-ddTHH:mm:ss.ffffK";
        public const string DATETIME_ISO8601_MS5 = "yyyy-MM-ddTHH:mm:ss.fffffK";
        public const string DATETIME_ISO8601_MS6 = "yyyy-MM-ddTHH:mm:ss.ffffffK";
        public const string DATETIME_ISO8601_MS7 = "yyyy-MM-ddTHH:mm:ss.fffffffK";
        public const string DATETIME_ISO8601_DATE = "yyyy-MM-dd";
        public const string DATETIME_ISO8601_TIME = "HH:mm:ssK";
        public const string DATETIME_ISO8601_TIME_MS = "HH:mm:ss.fffK";


        //format
        public static string Format(TimeSpan timeSpan) {
            var ms = timeSpan.TotalMilliseconds;
            var seconds = Math.Floor(ms / 1000);
            if (seconds < 60) return seconds + " sec";
            var minutes = Math.Floor(seconds / 60);
            if (minutes < 5) return minutes + "," + Math.Floor((seconds - minutes * 60) * 100 / 60) + " min";
            if (minutes < 60) return minutes + " min";
            var hours = Math.Floor(minutes / 60);
            if (hours < 5) return hours + "," + Math.Floor((minutes - hours * 60) * 100 / 60) + " hours";
            return hours + " hours";
        }
        public static string FormatMilliseconds(double milliseconds) {
            if (milliseconds < 1000) {
                return (int)milliseconds + " ms";
            } else if (milliseconds < 60 * 1000) {
                return (int)(milliseconds / 1000) + " sec";
            } else {
                return FormatSeconds((int)(milliseconds / 1000));
            }
        }
        public static string FormatSeconds(int seconds) {
            if (seconds < 60) {
                return seconds + " sec";
            } else {
                int hours = (int)(Math.Floor((double)seconds / 3600));
                int minutes = (int)(Math.Floor((double)seconds / 60) - (hours * 60));
                int sec = seconds - hours * 60 * 60 - minutes * 60;
                return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + sec.ToString("00") + " sec";
            }
        }
        public static string FormatHHMMSS(int seconds) {
            int hours = (int)(Math.Floor((double)seconds / 3600));
            int minutes = (int)(Math.Floor((double)seconds / 60) - (hours * 60));
            int sec = seconds - hours * 60 * 60 - minutes * 60;
            return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + sec.ToString("00");
        }
        public static string FormatSecondsInHours(int seconds) {
            int hours = (int)(Math.Floor((double)seconds / 3600));
            int minutes = (int)(Math.Floor((double)seconds / 60) - (hours * 60));
            return (hours < 10 ? "0" : "") + hours + ":" + (minutes < 10 ? "0" : "") + minutes;
        }
        public static string FormatSecondsInDays(long seconds) {
            int hours = (int)(Math.Floor(System.Convert.ToDecimal((double)seconds / 3600)));
            int days = (int)(Math.Floor((double)hours / 24));
            int minutes = (int)(Math.Floor(System.Convert.ToDecimal((double)seconds / 60)) - (hours * 60));
            hours = hours - days * 24;
            return (days > 0 ? days + "d " : "") + (hours < 10 ? "0" : "") + (hours) + ":" + (minutes < 10 ? "0" : "") + minutes;
        }
        public static bool EqualsWithoutMilliseconds(DateTime dt1, DateTime dt2) {
            dt1 = dt1.ToUniversalTime();
            dt2 = dt2.ToUniversalTime();
            return
                dt1.Second == dt2.Second && // 1 of 60 match chance
                dt1.Minute == dt2.Minute && // 1 of 60 chance
                dt1.Day == dt2.Day &&       // 1 of 28-31 chance
                dt1.Hour == dt2.Hour &&     // 1 of 24 chance
                dt1.Month == dt2.Month &&   // 1 of 12 chance
                dt1.Year == dt2.Year;       // depends on dataset
        }


        //parse
        public static DateTime Parse(string text, bool avoidThrowException = false) {
            var result = default(DateTime);
            if (TryParse(text, out result)) {
                return result;
            } else {
                if (!avoidThrowException) {
                    result = DateTime.Parse(text).ToLocalTime();
                } else {
                    DateTime.TryParse(text, out result);
                }
            }
            return result;
        }
        public static bool TryParse(string text, out DateTime result) {
            if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS2, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS4, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS5, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS6, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS7, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_DATE, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_TIME, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_TIME_MS, null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeLocal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.AssumeLocal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss K", null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss.ff K", null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                //old formats: used by old DProjects log files
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss.fff K", null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                //old formats: used by old DProjects log files
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss ff", null, System.Globalization.DateTimeStyles.AssumeLocal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss fff", null, System.Globalization.DateTimeStyles.AssumeLocal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss ff K", null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss fff K", null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                //old format: used by GetText
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mmK", null, System.Globalization.DateTimeStyles.AssumeUniversal, out result)) {
                return true;
            }
            return false;
        }


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


        //text to date format
        public static string DateTimeToTextRelative(DateTime aNow, DateTime aDate) {
            var result = "";
            int m = System.Convert.ToInt32(aNow.Subtract(aDate).TotalMinutes);
            int h = System.Convert.ToInt32(aNow.Subtract(aDate).TotalHours);
            int d = System.Convert.ToInt32(aNow.Date.Subtract(aDate).TotalDays);
            if (m < 0) {
                result = "now";
            } else if (m == 1) {
                result = "1 minute ago";
            } else if (m < 50) {
                result = m + " minutes ago";
            } else if (h < 0 || h==1) {
                result = "1 hour ago";
            } else if (h < 12) {
                result = h + " hours ago";
            } else if (d <= 1) {
                if (aNow.Date.Day == aDate.Day) {
                    result = "today";
                } else {
                    result = "yesterday";
                }                
            } else if (d <= 10) {
                result = d + " days ago";
            } else {
                result = aDate.ToString("dd/MM/yyyy");
            }
            return result;
        }
        public static DateTime TextToRelativeAtDateTime(DateTime aNow, string text, DateTime defaultDate = default) {
            //http://www.softpanorama.org/Utilities/at.shtml
            //ex: at 16:00
            //ex: at 16:00 + 3 days
            //ex: at 1:00 tomorrow
            //ex: now
            //ex: today
            //ex: tomorrow
            //ex: midnight
            //ex: noon
            //ex: 1 year ago
            //ex: 15 days ago
            DateTime targetDate = defaultDate;
            text = text.Replace("+", "+ ");
            text = text.Replace("-", "- ");
            while (text.IndexOf("  ") != -1) {
                text = text.Replace("  ", " ");
            }
            if (text.StartsWith("at ")) {
                text = text.Substring(3);
            } else if (text.EndsWith(" ago")) {
                text = "- " + text.Substring(0, text.Length - 4);
            }
            if (text.StartsWith("next ")) {
                text = "now " + text;
            } else if (text.StartsWith("-")) {
                text = "now " + text;
            }
            string[] parts = text.Replace("next", "+ 1").Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i <= parts.Length - 1; i++) {
                string timeArg = parts[i];
                if (StringUtils.Equals(timeArg, "now")) { //ara
                    targetDate = aNow;
                } else if (StringUtils.Equals(timeArg, "noon")) { // migdia
                    if (targetDate == System.Convert.ToDateTime(null)) {
                        targetDate = aNow.Date.AddHours(12);
                    } else {
                        targetDate = targetDate.Date.AddHours(12);
                    }
                    if (targetDate < aNow) {
                        targetDate = targetDate.AddDays(1);
                    }
                } else if (StringUtils.Equals(timeArg, "teatime")) { // teatime
                    if (targetDate == System.Convert.ToDateTime(null)) {
                        targetDate = aNow.Date.AddHours(12 + 5);
                    } else {
                        targetDate = targetDate.Date.AddHours(12 + 5);
                    }
                    if (targetDate < aNow) {
                        targetDate = targetDate.AddDays(1);
                    }
                } else if (StringUtils.Equals(timeArg, "tomorrow")) { //dema
                    if (targetDate == System.Convert.ToDateTime(null)) {
                        targetDate = aNow.AddDays(1);
                    } else if (targetDate <= aNow.Date.AddDays(1)) {
                        targetDate = targetDate.AddDays(1);
                    }
                } else if (StringUtils.Equals(timeArg, "midnight")) { //mitjanit
                    if (targetDate == System.Convert.ToDateTime(null)) {
                        targetDate = aNow.Date.AddDays(1);
                    } else {
                        targetDate = targetDate.Date;
                    }
                    if (targetDate < aNow) {
                        targetDate = targetDate.AddDays(1);
                    }
                } else if (StringUtils.Like(timeArg, "????-??-??")) {
                    int year = System.Convert.ToInt32(timeArg.Split('-')[0]);
                    int month = System.Convert.ToInt32(timeArg.Split('-')[1]);
                    int day = System.Convert.ToInt32(timeArg.Split('-')[2]);
                    targetDate = new DateTime(year, month, day);
                } else if (StringUtils.Like(timeArg, "??:??")) {
                    int hours = System.Convert.ToInt32(timeArg.Split(':')[0]);
                    int minutes = System.Convert.ToInt32(timeArg.Split(':')[1]);
                    if (targetDate == System.Convert.ToDateTime(null)) {
                        targetDate = System.Convert.ToDateTime(aNow.Date.AddHours(hours).AddMinutes(minutes));
                    } else {
                        targetDate = System.Convert.ToDateTime(targetDate.Date.AddHours(hours).AddMinutes(minutes));
                    }
                    if (targetDate < aNow) {
                        targetDate = targetDate.AddDays(1);
                    }
                } else if (StringUtils.Like(timeArg, "??:??:??")) {
                    int hours = System.Convert.ToInt32(timeArg.Split(':')[0]);
                    int minutes = System.Convert.ToInt32(timeArg.Split(':')[1]);
                    int seconds = System.Convert.ToInt32(timeArg.Split(':')[2]);
                    if (targetDate == System.Convert.ToDateTime(null)) {
                        targetDate = System.Convert.ToDateTime(aNow.Date.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds));
                    } else {
                        targetDate = System.Convert.ToDateTime(targetDate.AddHours(hours).AddMinutes(minutes).AddSeconds(seconds));
                    }
                    if (targetDate < aNow) {
                        targetDate = targetDate.AddDays(1);
                    }
                } else if (StringUtils.Equals(timeArg, "+") || StringUtils.Equals(timeArg, "-")) {
                    int multiplyBy = (StringUtils.Equals(timeArg, "+") ? 1 : -1);
                    string timeArgNext = (i < parts.Length - 1 ? (parts[i + 1]) : "");
                    string timeArgNextNext = (i < parts.Length - 2 ? (parts[i + 2]) : "");
                    int timeArgNextInteger = 0;
                    if (int.TryParse(timeArgNext, out timeArgNextInteger)) {
                        if (timeArgNextNext == "second" || timeArgNextNext == "seconds") {
                            targetDate = targetDate.AddSeconds(timeArgNextInteger * multiplyBy);
                        } else if (timeArgNextNext == "minute" || timeArgNextNext == "minutes") {
                            targetDate = targetDate.AddMinutes(timeArgNextInteger * multiplyBy);
                        } else if (timeArgNextNext == "hour" || timeArgNextNext == "hours") {
                            targetDate = targetDate.AddHours(timeArgNextInteger * multiplyBy);
                        } else if (timeArgNextNext == "day" || timeArgNextNext == "days") {
                            targetDate = targetDate.AddDays(timeArgNextInteger * multiplyBy);
                        } else if (timeArgNextNext == "week" || timeArgNextNext == "weeks") {
                            targetDate = targetDate.AddDays(timeArgNextInteger * 7 * multiplyBy);
                        } else if (timeArgNextNext == "month" || timeArgNextNext == "months") {
                            targetDate = targetDate.AddMonths(timeArgNextInteger * multiplyBy);
                        } else if (timeArgNextNext == "year" || timeArgNextNext == "years") {
                            targetDate = targetDate.AddYears(timeArgNextInteger * multiplyBy);
                        } else {
                            throw new FormatException("Unable to convert text to date: invalid arguments: " + text);
                        }
                    } else {
                        throw new FormatException("Unable to convert text to date: invalid arguments: " + text);
                    }
                    i += 2;
                } else {
                    throw new FormatException("Unable to convert text to date: invalid arguments: " + text);
                }
            }
            return targetDate;
        }


        //overlap
        public static bool AreSpansOverlapping(DateTime s1, DateTime e1, DateTime s2, DateTime e2) {
            var a1 = new DateRange() { Start = s1, End = e1 };
            var a2 = new DateRange() { Start = s2, End = e2 };
            return a1.Intersects(a2);

        }
        public class DateRange {
            public DateTime Start { get; set; }
            public DateTime End { get; set; }
            public bool Intersects(DateRange target, bool excludeBorders = false) {
                if (excludeBorders) {
                    return Start < target.Start && End > target.Start || Start < target.End && End > target.End || Start > target.Start && End < target.End;
                } else {
                    return Start <= target.Start && End >= target.Start || Start <= target.End && End >= target.End || Start >= target.Start && End <= target.End;
                }
            }
            public bool Intersects(DateTime target, bool excludeBorders = false) {
                if (excludeBorders) {
                    return Start < target && target < End;
                } else {
                    return Start <= target && target <= End;
                }
            }
        }

    }


}


