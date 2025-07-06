namespace Ephemeris.Chronology;

public static class TimeZoneUtils
{
    // Converts Julian Day to DateTime (UTC)
    public static DateTime FromJulianDay(double jd)
    {
        double J = jd + 0.5;
        int Z = (int)Math.Floor(J);
        double F = J - Z;

        int A = Z;
        if (Z >= 2299161)
        {
            int alpha = (int)((Z - 1867216.25) / 36524.25);
            A = Z + 1 + alpha - (alpha / 4);
        }

        int B = A + 1524;
        int C = (int)((B - 122.1) / 365.25);
        int D = (int)(365.25 * C);
        int E = (int)((B - D) / 30.6001);

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

    // Converts DateTime to Julian Day number with fractional day
    public static double ToJulianDay(DateTime dt)
    {
        return TimeUtils.JulianDay(dt.Year, dt.Month, dt.Day,
            dt.Hour + (dt.Minute / 60.0) + (dt.Second / 3600.0) + (dt.Millisecond / 3600000.0));
    }

    // Converts UTC DateTime to local DateTime given a time zone ID
    public static DateTime ToLocal(DateTime utcTime, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
    }

    // Converts local DateTime to UTC DateTime given a time zone ID
    public static DateTime ToUtc(DateTime localTime, string timeZoneId)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
    }
}
