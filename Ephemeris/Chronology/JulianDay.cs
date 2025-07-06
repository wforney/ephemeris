using Ephemeris.Heliology;

namespace Ephemeris.Chronology;

/// <summary>
/// Represents a Julian Day, with conversions to and from DateTimeOffset.
/// </summary>
public readonly record struct JulianDay(double Value)
{
    private const double UnixEpochJulianDay = 2440587.5;

    /// <summary>
    /// Creates a JulianDay from a specific date and time.
    /// </summary>
    /// <param name="year">The year of the date.</param>
    /// <param name="month">The month of the date (1-12).</param>
    /// <param name="day">The day of the month (1-31).</param>
    /// <param name="hour">The hour of the day (0-23).</param>
    /// <param name="minute">The minute of the hour (0-59).</param>
    /// <param name="second">The second of the minute (0-59).</param>
    /// <param name="millisecond">The milliseconds of the second (0-999).</param>
    public JulianDay(int year, int month, int day, int hour = 0, int minute = 0, int second = 0, int millisecond = 0)
        : this(FromDateTimeOffset(new DateTime(year, month, day, hour, minute, second, millisecond).ToUniversalTime()).Value)
    {
    }

    /// <summary>
    /// Implicitly converts a JulianDay to a DateTime in UTC.
    /// </summary>
    /// <param name="julianDay">The JulianDay to convert.</param>
    public static implicit operator DateTime(JulianDay julianDay) => julianDay.ToDateTimeOffset().UtcDateTime;

    /// <summary>
    /// Implicitly converts a DateTime to a JulianDay.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    public static implicit operator JulianDay(DateTime dateTime) => FromDateTime(dateTime);

    /// <summary>
    /// Implicitly converts a JulianDay to a DateTimeOffset in UTC.
    /// </summary>
    /// <param name="julianDay">The JulianDay to convert.</param>
    public static implicit operator DateTimeOffset(JulianDay julianDay) => julianDay.ToDateTimeOffset();

    /// <summary>
    /// Implicitly converts a DateTimeOffset to a JulianDay.
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset to convert.</param>
    public static implicit operator JulianDay(DateTimeOffset dateTimeOffset) => FromDateTimeOffset(dateTimeOffset);

    /// <summary>
    /// Implicitly converts a JulianDay to a double representing the Julian Day value.
    /// </summary>
    /// <param name="julianDay">The JulianDay to convert.</param>
    public static implicit operator double(JulianDay julianDay) => julianDay.Value;

    /// <summary>
    /// Implicitly converts a double to a JulianDay.
    /// </summary>
    /// <param name="julianDayValue">The double value representing the Julian Day.</param>
    public static implicit operator JulianDay(double julianDayValue) => new(julianDayValue);

    /// <summary>
    /// Calculates the Delta T (ΔT) value for a given year and month.
    /// </summary>
    /// <param name="year">The year for which to calculate Delta T.</param>
    /// <param name="month">The month for which to calculate Delta T.</param>
    /// <returns>The calculated Delta T value.</returns>
    public static double GetDeltaT(int year, int month)
    {
        double y = year + ((month - 0.5) / 12.0);
        double u = (y - 2000) / 100.0;

        // Simplified empirical formula (Meeus approximation)
        return 64.7 + (64.216 * u) + (0.293 * u * u);
    }

    /// <summary>
    /// Calculates the Delta T (ΔT) value for a given year and month.
    /// </summary>
    /// <param name="utc">The UTC time to calculate the Julian Day from.</param>
    /// <returns>The calculated Julian Day.</returns>
    public static double GetJulianDay(DateTime utc)
    {
        int year = utc.Year;
        int month = utc.Month;
        double day = utc.Day + (utc.Hour / 24.0) + (utc.Minute / 1440.0) + (utc.Second / 86400.0);

        if (month <= 2)
        {
            year -= 1;
            month += 12;
        }

        int A = year / 100;
        int B = 2 - A + (A / 4);

        return Math.Floor(365.25 * (year + 4716)) +
               Math.Floor(30.6001 * (month + 1)) +
               day + B - 1524.5;
    }

    /// <summary>
    /// Converts a DateTime to a JulianDay.
    /// </summary>
    /// <param name="dateTime">The DateTime to convert.</param>
    /// <returns>A JulianDay representation of the specified DateTime.</returns>
    public static JulianDay FromDateTime(DateTime dateTime)
    {
        // Convert to UTC to avoid timezone issues
        DateTime utc = dateTime.ToUniversalTime();
        double unixTime = (utc - DateTime.UnixEpoch).TotalDays;
        return new JulianDay(UnixEpochJulianDay + unixTime);
    }

    /// <summary>
    /// Converts a DateTimeOffset to a JulianDay.
    /// </summary>
    /// <param name="dateTime">The DateTimeOffset to convert.</param>
    public static JulianDay FromDateTimeOffset(DateTimeOffset dateTime)
    {
        // Convert to UTC to avoid timezone issues
        DateTime utc = dateTime.UtcDateTime;
        double unixTime = (utc - DateTime.UnixEpoch).TotalDays;
        return new JulianDay(UnixEpochJulianDay + unixTime);
    }

    /// <summary>
    /// Converts this JulianDay to a DateTimeOffset in UTC.
    /// </summary>
    public DateTimeOffset ToDateTimeOffset()
    {
        double daysSinceUnixEpoch = Value - UnixEpochJulianDay;
        DateTime utcDateTime = DateTime.UnixEpoch.AddDays(daysSinceUnixEpoch);
        return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
    }

    /// <inheritdoc/>
    public override string ToString() => Value.ToString("F5");
}
