using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace DProjects.DataTypes {

    public readonly struct Timestamp : IComparable<Timestamp>, IEquatable<Timestamp> {

        // vars
        public readonly long UnixMs;

        // ctor
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp(long unixMs) {
            UnixMs = unixMs;
        }

        //props
        public static Timestamp UtcNow => new(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        public long TotalMilliseconds {
            get => UnixMs;
        }
        public long TotalSeconds {
            get => UnixMs / 1000;
        }
        public int Year {
            get => ToDateTimeUtc().Year;
        }
        public int Month {
            get => ToDateTimeUtc().Month;
        }

        public int Day {
            get => ToDateTimeUtc().Day;
        }

        // factories
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timestamp FromUnixMilliseconds(long ms) => new(ms);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timestamp FromDateTimeUtc(DateTime dt) => new(new DateTimeOffset(dt).ToUnixTimeMilliseconds());
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timestamp From(int year, int month, int day) => FromDateTimeUtc(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc));

        // methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset ToDateTimeOffset() {
            return DateTimeOffset.FromUnixTimeMilliseconds(UnixMs);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToDateTimeUtc() {
            return DateTimeOffset.FromUnixTimeMilliseconds(UnixMs).UtcDateTime;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp AddMilliseconds(long ms) {
            return new(UnixMs + ms);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp AddMonths(int months) {
            var dt = ToDateTimeUtc().AddMonths(months);
            return FromDateTimeUtc(dt);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp AddDays(int days) {
            var dt = ToDateTimeUtc().AddDays(days);
            return FromDateTimeUtc(dt);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp ToMidnight() {
            var dt = ToDateTimeUtc();
            var midnight = new DateTime(dt.Year, dt.Month, dt.Day, 0, 0, 0, DateTimeKind.Utc);
            return FromDateTimeUtc(midnight);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string ToString(string format) {
            return ToDateTimeUtc().ToString(format);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Timestamp ToFirstDayOfMonth() {
            var dt = ToDateTimeUtc(); // siempre UTC en tu sistema
            var first = new DateTime(dt.Year, dt.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            return FromDateTimeUtc(first);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Timestamp Parse(string text) {
            // Se usa DateTime.Parse, que soporta:
            // - ISO 8601
            // - "2025-03-10 14:20:10"
            // - Con o sin Z
            // - Con o sin offset
            // Siempre se convierte a UTC
            if (long.TryParse(text, out long result)) return FromUnixMilliseconds(result);
            var dt = DateTime.Parse(text, null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
            return FromDateTimeUtc(dt);
        }

        // operators
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <(Timestamp a, Timestamp b) => a.UnixMs < b.UnixMs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >(Timestamp a, Timestamp b) => a.UnixMs > b.UnixMs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator <=(Timestamp a, Timestamp b) => a.UnixMs <= b.UnixMs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator >=(Timestamp a, Timestamp b) => a.UnixMs >= b.UnixMs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Timestamp a, Timestamp b) => a.UnixMs == b.UnixMs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Timestamp a, Timestamp b) => a.UnixMs != b.UnixMs;

        // Implicit conversions (safety optional: puedes quitarlas si quieres evitar errores)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator long(Timestamp t) => t.UnixMs;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Timestamp(long unixMs) => new(unixMs);

        // interfaces impl
        public int CompareTo(Timestamp other) => UnixMs.CompareTo(other.UnixMs);
        public bool Equals(Timestamp other) => UnixMs == other.UnixMs;
        public override bool Equals(object obj) => obj is Timestamp ts && ts.UnixMs == UnixMs;
        public override int GetHashCode() => UnixMs.GetHashCode();
        public override string ToString() => UnixMs.ToString();

    }
}