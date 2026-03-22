// Updated: 2026-03-22
namespace Ephemeris.Chronology;

/// <summary>
/// Represents a date — including dates before year 1 CE — using the proleptic Julian/Gregorian
/// calendar as defined in <em>Astronomical Algorithms</em> (Meeus, 2nd ed., Ch. 7).
/// Stored internally as a Julian Day Number so that the full range of historical dates,
/// including those in the BC/BCE era, is supported without the <see cref="DateTime"/>
/// lower-bound restriction (year 1 CE).
/// </summary>
/// <remarks>
/// Astronomical year numbering is used: year 0 = 1 BCE, year −1 = 2 BCE, etc.
/// The conversion between BC year and astronomical year is:
/// <code>
///   astronomicalYear = -(bceYear - 1)   →   bceYear = 1 - astronomicalYear
/// </code>
/// Julian Day formula (Meeus Ch. 7, Eq. 7.1):
/// <code>
///   if month ≤ 2 then year -= 1, month += 12
///   A = ⌊year/100⌋
///   B = 2 − A + ⌊A/4⌋   (Gregorian calendar; for pure Julian: B = 0)
///   JD = ⌊365.25(year+4716)⌋ + ⌊30.6001(month+1)⌋ + day + hour/24 + B − 1524.5
/// </code>
/// All dates before 15 Oct 1582 (JD 2299160.5) are treated as proleptic Gregorian for
/// consistency with modern astronomical practice. This matches the Meeus Eq. 7.1 approach.
/// </remarks>
public readonly struct ProlepticDate : IEquatable<ProlepticDate>, IComparable<ProlepticDate>
{
    // ── Month name abbreviations (ISO / astronomical convention) ──────────
    private static readonly string[] MonthAbbrev =
        ["", "Jan", "Feb", "Mar", "Apr", "May", "Jun",
              "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

    // ── Properties ────────────────────────────────────────────────────────

    /// <summary>
    /// Astronomical year number. Positive = CE, zero = 1 BCE, negative = further BCE.
    /// For display use <see cref="ToHistoricalString"/> which converts to "yyyy BCE/CE".
    /// </summary>
    public int Year { get; }

    /// <summary>Month number, 1–12.</summary>
    public int Month { get; }

    /// <summary>Day of month, 1–31.</summary>
    public int Day { get; }

    /// <summary>
    /// Fractional hour (UT), 0.0–24.0 (exclusive).  Defaults to noon (12.0)
    /// to minimise date-boundary ambiguities in Julian Day rounding.
    /// </summary>
    public double Hour { get; }

    // ── Constructor ───────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a <see cref="ProlepticDate"/> from its calendar components.
    /// </summary>
    /// <param name="year">Astronomical year (0 = 1 BCE, −1 = 2 BCE, etc.).</param>
    /// <param name="month">Month 1–12.</param>
    /// <param name="day">Day of month 1–31.</param>
    /// <param name="hour">Fractional hour UT (default 12.0 = noon).</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="month"/> is outside 1–12 or <paramref name="day"/>
    /// is outside 1–31.
    /// </exception>
    public ProlepticDate(int year, int month, int day, double hour = 12.0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(month, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(month, 12);
        ArgumentOutOfRangeException.ThrowIfLessThan(day, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(day, 31);

        Year  = year;
        Month = month;
        Day   = day;
        Hour  = hour;
    }

    // ── Factory helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="ProlepticDate"/> from a Before Common Era (BCE) year.
    /// </summary>
    /// <param name="bceYear">
    /// Positive BCE year number (e.g. 701 for 701 BCE).
    /// Must be ≥ 1.
    /// </param>
    /// <param name="month">Month 1–12.</param>
    /// <param name="day">Day of month 1–31.</param>
    /// <param name="hour">Fractional hour UT (default 12.0 = noon).</param>
    /// <returns>A <see cref="ProlepticDate"/> whose <see cref="Year"/> is <c>-(bceYear−1)</c>.</returns>
    /// <remarks>
    /// Conversion: astronomical year = 1 − bceYear.
    /// Examples:
    /// <list type="bullet">
    ///   <item>1 BCE → year 0</item>
    ///   <item>701 BCE → year −700</item>
    ///   <item>1406 BCE → year −1405</item>
    /// </list>
    /// </remarks>
    public static ProlepticDate FromBce(int bceYear, int month, int day, double hour = 12.0)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(bceYear, 1);
        return new ProlepticDate(1 - bceYear, month, day, hour);
    }

    /// <summary>
    /// Creates a <see cref="ProlepticDate"/> from a Julian Day number.
    /// </summary>
    /// <param name="jd">Julian Day number (fractional).</param>
    /// <returns>The corresponding <see cref="ProlepticDate"/>.</returns>
    /// <remarks>
    /// Uses the inverse of Meeus Ch. 7, Eq. 7.1 (proleptic Gregorian calendar throughout).
    /// The fractional part of <paramref name="jd"/> determines the <see cref="Hour"/> value.
    /// </remarks>
    public static ProlepticDate FromJulianDay(double jd)
    {
        double J = jd + 0.5;
        int Z = (int)Math.Floor(J);
        double F = J - Z;

        // Gregorian calendar adjustment (Meeus Eq. 7.1 — proleptic for all dates)
        int alpha = (int)Math.Floor((Z - 1867216.25) / 36524.25);
        int A = Z + 1 + alpha - (alpha / 4);

        int B = A + 1524;
        int C = (int)Math.Floor((B - 122.1) / 365.25);
        int D = (int)Math.Floor(365.25 * C);
        int E = (int)Math.Floor((B - D) / 30.6001);

        int day   = B - D - (int)Math.Floor(30.6001 * E);
        int month = E < 14 ? E - 1 : E - 13;
        int year  = month > 2 ? C - 4716 : C - 4715;

        double hour = F * 24.0;

        return new ProlepticDate(year, month, day, hour);
    }

    // ── Julian Day conversion ─────────────────────────────────────────────

    /// <summary>
    /// Converts this date to a Julian Day number (fractional).
    /// </summary>
    /// <returns>Julian Day number corresponding to this date at <see cref="Hour"/> UT.</returns>
    /// <remarks>
    /// Implements Meeus Ch. 7, Eq. 7.1 (proleptic Gregorian calendar, B term applied):
    /// <code>
    ///   if month ≤ 2 then year -= 1, month += 12
    ///   A = ⌊year/100⌋
    ///   B = 2 − A + ⌊A/4⌋
    ///   JD = ⌊365.25(year+4716)⌋ + ⌊30.6001(month+1)⌋ + day + hour/24 + B − 1524.5
    /// </code>
    /// This formula handles negative years correctly and is valid for the full range
    /// of astronomical dates.
    /// </remarks>
    public double ToJulianDay()
    {
        int y = Year;
        int m = Month;

        if (m <= 2)
        {
            y--;
            m += 12;
        }

        int A = (int)Math.Floor(y / 100.0);
        int B = 2 - A + (A / 4);

        return Math.Floor(365.25 * (y + 4716))
             + Math.Floor(30.6001 * (m + 1))
             + Day + (Hour / 24.0) + B - 1524.5;
    }

    // ── Formatting ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns a human-readable date string using historical BCE/CE notation.
    /// </summary>
    /// <returns>
    /// For BCE dates (year ≤ 0): <c>"nnn BCE Mon dd"</c>, e.g. <c>"701 BCE Aug 01"</c>.
    /// For CE dates (year ≥ 1):  <c>"yyyy CE Mon dd"</c>, e.g. <c>"2024 CE Jun 21"</c>.
    /// </returns>
    public string ToHistoricalString()
    {
        string mon = Month is >= 1 and <= 12 ? MonthAbbrev[Month] : "???";
        if (Year <= 0)
        {
            int bce = 1 - Year;
            return $"{bce} BCE {mon} {Day:D2}";
        }

        return $"{Year} CE {mon} {Day:D2}";
    }

    /// <summary>
    /// Returns the date in ISO 8601-like astronomical notation (negative year for BCE).
    /// </summary>
    /// <returns>
    /// e.g. <c>"-0700-08-01"</c> (for 701 BCE, Aug 1) or <c>"2024-06-21"</c> (for 2024 CE, Jun 21).
    /// Year 0 (1 BCE) is formatted as <c>"+0000-01-01"</c>.
    /// </returns>
    public string ToAstronomicalString()
    {
        string yearStr = Year switch
        {
            > 0 => $"{Year:D4}",
            0   => "+0000",
            _   => $"-{Math.Abs(Year):D4}",
        };
        return $"{yearStr}-{Month:D2}-{Day:D2}";
    }

    // ── Equality / comparison ─────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Equals(ProlepticDate other) =>
        Year == other.Year && Month == other.Month && Day == other.Day &&
        Math.Abs(Hour - other.Hour) < 1e-9;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is ProlepticDate pd && Equals(pd);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Year, Month, Day, Hour);

    /// <inheritdoc/>
    public int CompareTo(ProlepticDate other) => ToJulianDay().CompareTo(other.ToJulianDay());

    /// <summary>Equality operator.</summary>
    public static bool operator ==(ProlepticDate left, ProlepticDate right) => left.Equals(right);

    /// <summary>Inequality operator.</summary>
    public static bool operator !=(ProlepticDate left, ProlepticDate right) => !left.Equals(right);

    /// <summary>Less-than operator.</summary>
    public static bool operator <(ProlepticDate left, ProlepticDate right) => left.CompareTo(right) < 0;

    /// <summary>Greater-than operator.</summary>
    public static bool operator >(ProlepticDate left, ProlepticDate right) => left.CompareTo(right) > 0;

    /// <summary>Less-than-or-equal operator.</summary>
    public static bool operator <=(ProlepticDate left, ProlepticDate right) => left.CompareTo(right) <= 0;

    /// <summary>Greater-than-or-equal operator.</summary>
    public static bool operator >=(ProlepticDate left, ProlepticDate right) => left.CompareTo(right) >= 0;

    /// <inheritdoc/>
    public override string ToString() => ToHistoricalString();
}
