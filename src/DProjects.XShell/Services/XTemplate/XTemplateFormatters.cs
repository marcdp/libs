using System.Globalization;
using System.Text.RegularExpressions;

namespace DProjects.XShell.Services.XTemplate {

    internal static partial class XTemplateFormatters {

        // consts
        private const int MaximumDigits = 15;

        // vars
        private static readonly IReadOnlyDictionary<string, (int Digits, string Symbol)> Currencies = new Dictionary<string, (int, string)> {
            ["EUR"] = (2, "€"), ["USD"] = (2, "$"), ["JPY"] = (0, "¥"), ["GBP"] = (2, "£"), ["CAD"] = (2, "CA$"), ["AUD"] = (2, "A$"), ["CHF"] = (2, "CHF"), ["CNY"] = (2, "CN¥"), ["KRW"] = (0, "₩")
        };
        private static readonly string[] PatternTokens = ["yyyy", "MMMM", "MMM", "MM", "dd", "HH", "mm", "ss", "M", "d", "H"];

        // methods
        public static object Apply(string name, object input, IReadOnlyList<object?> arguments, string? locale, int offset) {
            var culture = GetCulture(locale, offset);
            return name switch {
                "number" => Number(input, arguments, culture, offset),
                "percent" => Percent(input, arguments, culture, offset),
                "currency" => Currency(input, arguments, culture, offset),
                "upper" => String(input, arguments, culture, offset, true),
                "lower" => String(input, arguments, culture, offset, false),
                "trim" => Trim(input, arguments, offset),
                "date" => Date(input, arguments, culture, offset),
                "datetime" => DateTime(input, arguments, culture, offset),
                "time" => Time(input, arguments, culture, offset),
                _ => throw Error($"Unknown formatter '{name}'", offset)
            };
        }

        // methods (private)
        private static string Number(object input, IReadOnlyList<object?> arguments, CultureInfo culture, int offset) {
            RequireCount("number", arguments, 0, 1, offset);
            int? digits = arguments.Count == 0 ? null : Digits(arguments[0], offset);
            return FormatNumber(InputNumber(input, "number", offset), digits, culture, offset);
        }
        private static string Percent(object input, IReadOnlyList<object?> arguments, CultureInfo culture, int offset) {
            RequireCount("percent", arguments, 0, 1, offset);
            int? digits = arguments.Count == 0 ? null : Digits(arguments[0], offset);
            var text = FormatNumber(InputNumber(input, "percent", offset) * 100, digits, culture, offset);
            return culture.Name.Equals("tr-TR", StringComparison.OrdinalIgnoreCase) ? "%" + text : culture.Name.Equals("es-ES", StringComparison.OrdinalIgnoreCase) ? text + "\u00A0%" : text + "%";
        }
        private static string Currency(object input, IReadOnlyList<object?> arguments, CultureInfo culture, int offset) {
            RequireCount("currency", arguments, 1, 2, offset);
            if (arguments[0] is not string code || !CurrencyCode().IsMatch(code) || !Currencies.TryGetValue(code, out var currency)) throw Error("Formatter 'currency' requires a supported uppercase ISO 4217 currency code", offset);
            var digits = arguments.Count == 2 ? Digits(arguments[1], offset) : currency.Digits;
            var text = FormatNumber(InputNumber(input, "currency", offset), digits, culture, offset);
            var symbol = localeIsInvariant(culture) && currency.Symbol == code ? code : currency.Symbol;
            return culture.Name.Equals("es-ES", StringComparison.OrdinalIgnoreCase) || culture.Name.Equals("tr-TR", StringComparison.OrdinalIgnoreCase) ? text + "\u00A0" + symbol : symbol + text;
        }
        private static string String(object input, IReadOnlyList<object?> arguments, CultureInfo culture, int offset, bool upper) {
            RequireCount(upper ? "upper" : "lower", arguments, 0, 0, offset);
            if (input is not string text) throw Error($"Formatter '{(upper ? "upper" : "lower")}' requires a string input", offset);
            return upper ? text.ToUpper(culture) : text.ToLower(culture);
        }
        private static string Trim(object input, IReadOnlyList<object?> arguments, int offset) {
            RequireCount("trim", arguments, 0, 0, offset);
            if (input is not string text) throw Error("Formatter 'trim' requires a string input", offset);
            return text.Trim();
        }
        private static string Date(object input, IReadOnlyList<object?> arguments, CultureInfo culture, int offset) {
            var value = ParseDate(input, offset, true);
            return FormatPattern(value, Pattern(arguments, "date", offset), culture, true, false, offset);
        }
        private static string DateTime(object input, IReadOnlyList<object?> arguments, CultureInfo culture, int offset) {
            var value = ParseDate(input, offset, false);
            return FormatPattern(value, Pattern(arguments, "datetime", offset), culture, true, true, offset);
        }
        private static string Time(object input, IReadOnlyList<object?> arguments, CultureInfo culture, int offset) {
            var value = ParseDate(input, offset, false);
            return FormatPattern(value, Pattern(arguments, "time", offset), culture, false, true, offset);
        }
        private static string FormatNumber(double number, int? digits, CultureInfo culture, int offset) {
            var precision = digits ?? 3;
            var rounded = Math.Round(number, precision, MidpointRounding.AwayFromZero);
            var format = digits.HasValue ? "#,##0." + new string('0', digits.Value) : "#,##0." + new string('#', precision);
            if (precision == 0) format = "#,##0";
            try { return rounded.ToString(format, culture); }
            catch (FormatException) { throw Error("Unsupported formatter precision", offset); }
        }
        private static DateTimeOffset ParseDate(object input, int offset, bool dateAllowed) {
            if (input is not string text) throw Error("Date/time formatters require a string input", offset);
            if (dateAllowed && Regex.IsMatch(text, "^\\d{4}-\\d{2}-\\d{2}$")) {
                if (DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
            } else if (Regex.IsMatch(text, "^\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2}(Z|[+-]\\d{2}:\\d{2})$") && DateTimeOffset.TryParseExact(text, new[] { "yyyy-MM-dd'T'HH:mm:ss'Z'", "yyyy-MM-dd'T'HH:mm:sszzz" }, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var value)) return value;
            throw Error("Formatter received an invalid ISO-8601 value", offset);
        }
        private static string FormatPattern(DateTimeOffset value, string pattern, CultureInfo culture, bool dateAllowed, bool timeAllowed, int offset) {
            var result = new System.Text.StringBuilder();
            var hasToken = false;
            for (var position = 0; position < pattern.Length;) {
                var token = PatternTokens.FirstOrDefault(candidate => pattern.AsSpan(position).StartsWith(candidate, StringComparison.Ordinal));
                if (token == null) {
                    if (char.IsLetter(pattern[position])) throw Error("Formatter pattern contains an unsupported token", offset);
                    result.Append(pattern[position++]);
                    continue;
                }
                if (!dateAllowed && token is "yyyy" or "MMMM" or "MMM" or "MM" or "M" or "dd" or "d") throw Error("Formatter pattern token is not allowed", offset);
                if (!timeAllowed && token is "HH" or "H" or "mm" or "ss") throw Error("Formatter pattern token is not allowed", offset);
                result.Append(token switch {
                    "yyyy" => value.Year.ToString("D4", CultureInfo.InvariantCulture), "MMMM" => culture.DateTimeFormat.GetMonthName(value.Month), "MMM" => culture.DateTimeFormat.GetAbbreviatedMonthName(value.Month),
                    "MM" => value.Month.ToString("D2", CultureInfo.InvariantCulture), "M" => value.Month.ToString(CultureInfo.InvariantCulture), "dd" => value.Day.ToString("D2", CultureInfo.InvariantCulture), "d" => value.Day.ToString(CultureInfo.InvariantCulture),
                    "HH" => value.Hour.ToString("D2", CultureInfo.InvariantCulture), "H" => value.Hour.ToString(CultureInfo.InvariantCulture), "mm" => value.Minute.ToString("D2", CultureInfo.InvariantCulture), "ss" => value.Second.ToString("D2", CultureInfo.InvariantCulture), _ => throw Error("Formatter pattern contains an unsupported token", offset)
                });
                hasToken = true;
                position += token.Length;
            }
            if (!hasToken) throw Error("Formatter pattern must contain a token", offset);
            return result.ToString();
        }
        private static string Pattern(IReadOnlyList<object?> arguments, string name, int offset) {
            RequireCount(name, arguments, 1, 1, offset);
            return arguments[0] as string ?? throw Error($"Formatter '{name}' requires a string pattern", offset);
        }
        private static int Digits(object? value, int offset) {
            if (value is not double number || number < 0 || number != Math.Truncate(number) || number > MaximumDigits) throw Error("Formatter digits must be a supported non-negative integer", offset);
            return (int)number;
        }
        private static double InputNumber(object input, string name, int offset) => input is double number && double.IsFinite(number) ? number : throw Error($"Formatter '{name}' requires a numeric input", offset);
        private static void RequireCount(string name, IReadOnlyList<object?> values, int minimum, int maximum, int offset) { if (values.Count < minimum || values.Count > maximum) throw Error($"Formatter '{name}' received an invalid argument count", offset); }
        private static CultureInfo GetCulture(string? locale, int offset) { if (string.IsNullOrWhiteSpace(locale)) return CultureInfo.InvariantCulture; try { return CultureInfo.GetCultureInfo(locale); } catch (CultureNotFoundException) { throw Error($"Unsupported formatter locale '{locale}'", offset); } }
        private static bool localeIsInvariant(CultureInfo culture) => culture.Equals(CultureInfo.InvariantCulture);
        private static XTemplateExpressionEvaluationException Error(string message, int offset) => new(message, offset);

        [GeneratedRegex("^[A-Z]{3}$", RegexOptions.CultureInvariant)]
        private static partial Regex CurrencyCode();
    }
}
