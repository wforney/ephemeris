namespace Ephemeris.Chronology;

/// <summary>
/// Utility class for time-related calculations, including Julian Day, GMST, Delta T, and angle normalization.
/// </summary>
public static class TimeUtils
{
    /// <summary>
    /// The Julian Day for the epoch J2000.0, which is the reference point for many astronomical calculations.
    /// </summary>
    private const double J2000 = 2451545.0;

    /// <summary>
    /// Calculates the Delta T (difference between Terrestrial Time and Universal Time) for a given year.
    /// </summary>
    /// <param name="year">
    /// The year for which to calculate Delta T. It can be a fractional year (e.g., 2000.5 for mid-year).
    /// </param>
    /// <returns>Delta T in seconds for the specified year.</returns>
    public static double DeltaT(double year)
    {
        double y = year;
        // Polynomial fit: approximate delta-T (in seconds)
        if (y < 948)
        {
            double u = (y - 2000) / 100;
            return 2177 + (497 * u) + (44.1 * u * u);
        }
        else if (y < 1600)
        {
            double u = (y - 1000) / 100;
            return 102 + (102 * u) + (25.3 * u * u);
        }
        else if (y < 2000)
        {
            double t = y - 2000;
            return 62.92 + (0.32217 * t) + (0.005589 * t * t);
        }
        else
        {
            double t = y - 2000;
            return 64.7 + (0.293 * t);
        }
    }

    /// <summary>
    /// Calculates the Greenwich Mean Sidereal Time (GMST) for a given Julian Day.
    /// </summary>
    /// <param name="jd">
    /// The Julian Day for which to calculate GMST. This is a continuous count of days since the epoch J2000.0.
    /// </param>
    /// <returns>GMST in degrees for the specified Julian Day.</returns>
    public static double GMST(double jd)
    {
        double T = JulianCentury(jd);
        double gmst = 280.46061837 + (360.98564736629 * (jd - J2000)) +
                      (0.000387933 * T * T) - (T * T * T / 38710000.0);
        return NormalizeDegrees(gmst);
    }

    /// <summary>
    /// Calculates the Julian Century from a given Julian Day.
    /// </summary>
    /// <param name="jd">The Julian Day for which the Julian Century is calculated.</param>
    /// <returns>The Julian Century.</returns>
    public static double JulianCentury(double jd) => (jd - J2000) / 36525.0;


    /// <summary>
    /// Calculates the Julian Day for a given date specified by year, month, day, and hour.
    /// </summary>
    /// <param name="year">The year of the date.</param>
    /// <param name="month">The month of the date.</param>
    /// <param name="day">The day of the date.</param>
    /// <param name="hour">The hour of the date, default is 0.0.</param>
    /// <returns>The Julian Day for the specified date.</returns>
    public static double JulianDay(int year, int month, int day, double hour = 0.0)
    {
        if (month <= 2)
        {
            year--;
            month += 12;
        }

        int A = year / 100;
        int B = 2 - A + (A / 4);

        return Math.Floor(365.25 * (year + 4716)) +
               Math.Floor(30.6001 * (month + 1)) +
               day + (hour / 24.0) + B - 1524.5;
    }

    /// <summary>
    /// Normalizes an angle in degrees to the range [0, 360).
    /// </summary>
    /// <param name="angle">The angle in degrees to normalize.</param>
    /// <returns>The normalized angle in degrees.</returns>
    public static double NormalizeDegrees(double angle)
    {
        angle %= 360.0;
        if (angle < 0)
        {
            angle += 360.0;
        }

        return angle;
    }

    /// <summary>
    /// Converts an angle in radians to degrees.
    /// </summary>
    /// <param name="rad">The angle in radians to convert.</param>
    /// <returns>The angle in degrees.</returns>
    public static double ToDegrees(double rad) => double.RadiansToDegrees(rad); // rad * 180.0 / Math.PI;

    /// <summary>
    /// Converts an angle in degrees to radians.
    /// </summary>
    /// <param name="deg">The angle in degrees to convert.</param>
    /// <returns>The angle in radians.</returns>
    public static double ToRadians(double deg) => double.DegreesToRadians(deg); // deg * Math.PI / 180.0;
}
