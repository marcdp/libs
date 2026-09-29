using System.Globalization;

namespace DProjects.XShell.Services.XTemplate {

    internal static class XTemplateValues {

        // methods
        public static bool IsNumeric(object? value) => value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
        public static bool IsTruthy(object? value) => value switch {
            null => false,
            bool boolean => boolean,
            string text => text.Length != 0,
            _ when IsNumeric(value) => NormalizeNumber(value) != 0,
            _ => true
        };
        public static string ToScalarString(object? value) => value switch {
            null => string.Empty,
            bool boolean => boolean ? "true" : "false",
            string text => text,
            _ when IsNumeric(value) => NormalizeNumber(value).ToString("R", CultureInfo.InvariantCulture),
            DateTime dateTime => ToCanonicalUtcIsoString(ToUtcDateTimeOffset(dateTime)),
            DateTimeOffset dateTimeOffset => ToCanonicalUtcIsoString(dateTimeOffset),
            _ => throw new XTemplateValueException("Objects and collections cannot be converted to text")
        };
        public static double ToNumber(object? value) => value is not null && IsNumeric(value) ? NormalizeNumber(value) : throw new XTemplateValueException("Numeric operands are required");
        public static double NormalizeNumber(object value) {
            var number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            if (!double.IsFinite(number)) throw new XTemplateValueException("Numbers must be finite");
            return number == 0 ? 0 : number;
        }

        // normalize date/time values to a UTC instant without using the machine time zone for unspecified values
        public static DateTimeOffset ToUtcDateTimeOffset(DateTime value) {
            var utc = value.Kind switch {
                DateTimeKind.Local => value.ToUniversalTime(),
                DateTimeKind.Unspecified => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                _ => value
            };
            return new DateTimeOffset(utc);
        }
        public static string ToCanonicalUtcIsoString(DateTimeOffset value) => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
    }

    internal sealed class XTemplateValueException : Exception {

        // ctor
        public XTemplateValueException(string message) : base(message) { }
    }
}
