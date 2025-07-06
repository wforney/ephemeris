using Ephemeris.Trigonometry;

namespace Ephemeris.Chronology;

/// <summary>
/// Represents a class for calculating sidereal time.
/// </summary>
public class SiderealTime
{
    /// <summary>
    /// Calculates the Local Mean Sidereal Time (LMST) based on a given time in hours and longitude.
    /// </summary>
    /// <param name="mjd">The input double value representing time in hours.</param>
    /// <param name="longitude">The longitude in degrees. (west negative)</param>
    /// <returns>
    /// The calculated Local Mean Sidereal Time in degrees. Meeus formula 11.4 instead of UTo.
    /// </returns>
    public static double CalculateLocalMeanSiderealTime(double mjd, double longitude)
    {
        double d = mjd - 51544.5;
        double t = d / 36525d;
        double lst = Calculator.NormalizeAngle(280.46061837 + (360.98564736629 * d) + (0.000387933 * t * t) - (t * t * t / 38710000));
        double lmst = (lst / 15d) + (longitude / 15);
        return lmst;
    }

    /// <summary>
    /// Calculates the Local Sidereal Time (LST) for a given Julian Day and longitude.
    /// </summary>
    /// <param name="julianDay">The Julian Day for which to calculate the Local Sidereal Time.</param>
    /// <param name="longitude">The longitude in degrees.</param>
    /// <returns>The calculated Local Sidereal Time in degrees.</returns>
    public static double CalculateSiderealTime(JulianDay julianDay, double longitude)
    {
        double t = (julianDay - 2451545.0) / 36525.0;
        double lst = 280.46061837 + (360.98564736629 * (julianDay - 2451545.0)) + (t * t * (0.000387933 - (t / 38710000.0))) + longitude;
        return Calculator.NormalizeAngle(lst);
    }
}
