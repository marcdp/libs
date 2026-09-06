using System;
using System.Globalization;

namespace DProjects.Utils {

    public static class DateTimeUtils {

        private const DateTimeStyles UniversalDateTimeStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;

        //constants
        public const string DATETIME_ISO8601 = "yyyy-MM-ddTHH:mm:ssK";
        public const string DATETIME_ISO8601_MS = "yyyy-MM-ddTHH:mm:ss.fffK";
        public const string DATETIME_ISO8601_MSZ = "yyyy-MM-ddTHH:mm:ss.fffZ";
        public const string DATETIME_ISO8601_MS1 = "yyyy-MM-ddTHH:mm:ss.fK";
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
            } else if (milliseconds < 2 * 1000) {
                return (int)(milliseconds / 1000) + " sec";
            } else if (milliseconds < 60 * 1000) {
                return (int)(milliseconds / 1000) + " secs";
            } else {
                return FormatSeconds((int)(milliseconds / 1000));
            }
        }
        public static string FormatSeconds(int seconds) {
            if (seconds == 1) {
                return seconds + " sec";
            } else if (seconds < 60) {
                return seconds + " secs";
            } else {
                int hours = (int)(Math.Floor((double)seconds / 3600));
                int minutes = (int)(Math.Floor((double)seconds / 60) - (hours * 60));
                int sec = seconds - hours * 60 * 60 - minutes * 60;
                return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + sec.ToString("00") + " sec";
            }
        }
        public static string FormatSecondsHHMMSS(int seconds) {
            int hours = (int)(Math.Floor((double)seconds / 3600));
            int minutes = (int)(Math.Floor((double)seconds / 60) - (hours * 60));
            int sec = seconds - hours * 60 * 60 - minutes * 60;
            return hours.ToString("00") + ":" + minutes.ToString("00") + ":" + sec.ToString("00");
        }
        public static string FormatSecondsHHMM(int seconds) {
            int hours = (int)(Math.Floor((double)seconds / 3600));
            int minutes = (int)(Math.Floor((double)seconds / 60) - (hours * 60));
            return (hours < 10 ? "0" : "") + hours + ":" + (minutes < 10 ? "0" : "") + minutes;
        }
        public static string FormatSecondsDDHHMM(long seconds) {
            int hours = (int)(Math.Floor(System.Convert.ToDecimal((double)seconds / 3600)));
            int days = (int)(Math.Floor((double)hours / 24));
            int minutes = (int)(Math.Floor(System.Convert.ToDecimal((double)seconds / 60)) - (hours * 60));
            hours -= days * 24;
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
            if (TryParse(text, out DateTime result)) {
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
            if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS1, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS2, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS4, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS5, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS6, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_MS7, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_DATE, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_TIME, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, DateTimeUtils.DATETIME_ISO8601_TIME_MS, null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss", null, System.Globalization.DateTimeStyles.AssumeLocal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.AssumeLocal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss K", null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss.ff K", null, UniversalDateTimeStyles, out result)) {
                //old formats: used by old DProjects log files
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss.fff K", null, UniversalDateTimeStyles, out result)) {
                //old formats: used by old DProjects log files
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss ff", null, System.Globalization.DateTimeStyles.AssumeLocal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss fff", null, System.Globalization.DateTimeStyles.AssumeLocal, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss ff K", null, UniversalDateTimeStyles, out result)) {
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss fff K", null, UniversalDateTimeStyles, out result)) {
                //old format: used by GetText
                return true;
            } else if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mmK", null, UniversalDateTimeStyles, out result)) {
                return true;
            }
            return false;
        }
        public static long ToUnixTimeNanoseconds(DateTime dateTime) {
            if (dateTime.Kind == DateTimeKind.Local) {
                dateTime = dateTime.ToUniversalTime();
            } else if (dateTime.Kind == DateTimeKind.Unspecified) {
                dateTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
            }
            var dto = new DateTimeOffset(dateTime);
            return dto.ToUnixTimeMilliseconds() * 1_000_000 + (dto.Ticks % TimeSpan.TicksPerMillisecond) * 100;
        }


        //text to date format
        public static string DateTimeToTextRelative(DateTime aNow, DateTime aDate) {
            string result;
            int s = System.Convert.ToInt32(aNow.Subtract(aDate).TotalSeconds);
            int m = System.Convert.ToInt32(aNow.Subtract(aDate).TotalMinutes);
            int h = System.Convert.ToInt32(aNow.Subtract(aDate).TotalHours);
            int d = System.Convert.ToInt32(aNow.Date.Subtract(aDate).TotalDays);
            if (s == 0) {
                result = "now";
            } else if (s < 60 && m == 0) {
                if (s == 1) {
                    result = s + " second ago";
                } else {
                    result = s + " seconds ago";
                }
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
        

        //overlap
        public static bool AreSpansOverlapping(DateTime s1, DateTime e1, DateTime s2, DateTime e2) {
            var a1 = new DateRange(s1, e1);
            var a2 = new DateRange(s2, e2);
            return a1.Intersects(a2);

        }
        public class DateRange(DateTime start, DateTime end) {
            public DateTime Start { get; set; } = start;
            public DateTime End { get; set; } = end;
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


