// Updated: 2026-03-09
namespace Ephemeris.Chronology;

/// <summary>
/// Provides utilities for converting between Julian Day numbers and DateTime, and for timezone conversions.
/// </summary>
public static class TimeZoneUtils
{
    /// <summary>
    /// Converts a Julian Day number to a UTC DateTime.
    /// </summary>
    /// <param name="jd">The Julian Day number (fractional).</param>
    /// <returns>The corresponding UTC DateTime.</returns>
    public static DateTime FromJulianDay(double jd)
    {
        double J = jd + 0.5; // shift so integer part = calendar day
        int Z = (int)Math.Floor(J); // integer day
        double F = J - Z; // fractional day (time of day)

        int A = Z; // Gregorian calendar adjustment
        if (Z >= 2299161)
        {
            int alpha = (int)((Z - 1867216.25) / 36524.25);
            A = Z + 1 + alpha - (alpha / 4);
        }

        int B = A + 1524; // Meeus algorithm step
        int C = (int)((B - 122.1) / 365.25); // approximate year
        int D = (int)(365.25 * C); // days in years
        int E = (int)((B - D) / 30.6001); // approximate month

        int day = B - D - (int)(30.6001 * E);
        int month = (E < 14) ? E - 1 : E - 13;
        int year = (month > 2) ? C - 4716 : C - 4715;

        double dayFraction = F * 24.0;
        int hour = (int)Math.Floor(dayFraction);
        double minuteFraction = (dayFraction - hour) * 60.0;
        int minute = (int)Math.Floor(minuteFraction);
        double secondFraction = (minuteFraction - minute) * 60.0;
        int second = (int)Math.Floor(secondFraction);
        int millisecond = (int)((secondFraction - second) * 1000.0);

        return new DateTime(year, month, day, hour, minute, second, millisecond, DateTimeKind.Utc);
    }

    /// <summary>
    /// Converts a UTC DateTime to a Julian Day number.
    /// </summary>
    /// <param name="dt">The UTC DateTime to convert.</param>
    /// <returns>The corresponding Julian Day number (fractional).</returns>
    public static double ToJulianDay(DateTime dt)
    {
        return TimeUtils.JulianDay(dt.Year, dt.Month, dt.Day,
            dt.Hour + (dt.Minute / 60.0) + (dt.Second / 3600.0) + (dt.Millisecond / 3600000.0));
    }

    /// <summary>
    /// Converts a UTC DateTime to a local DateTime in the specified timezone.
    /// </summary>
    /// <param name="utcTime">The UTC DateTime.</param>
    /// <param name="timeZoneId">The IANA or Windows timezone identifier (e.g., "Pacific Standard Time").</param>
    /// <returns>The local DateTime in the specified timezone.</returns>
    public static DateTime ToLocal(DateTime utcTime, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
    }

    /// <summary>
    /// Converts a local DateTime to UTC DateTime given a timezone identifier.
    /// </summary>
    /// <param name="localTime">The local DateTime.</param>
    /// <param name="timeZoneId">The IANA or Windows timezone identifier (e.g., "Pacific Standard Time").</param>
    /// <returns>The equivalent UTC DateTime.</returns>
    public static DateTime ToUtc(DateTime localTime, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
    }
}
