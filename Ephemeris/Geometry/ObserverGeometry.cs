using Ephemeris.Chronology;

namespace Ephemeris.Geometry;

/// <summary>
/// Converts equatorial coordinates to horizontal coordinates for a given observer location and time.
/// </summary>
public static class ObserverGeometry
{
    /// <summary>
    /// Converts equatorial coordinates (RA/Dec) to horizontal coordinates (Azimuth/Altitude) for an observer.
    /// </summary>
    /// <param name="RA">Right ascension in degrees [0, 360).</param>
    /// <param name="Dec">Declination in degrees [-90, 90].</param>
    /// <param name="jd">Julian Day number (fractional) for the observation.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A tuple of (Azimuth, Altitude) in degrees, where Azimuth is [0, 360) measured from North clockwise, and Altitude is [-90, 90].</returns>
    public static (double Azimuth, double Altitude) EquatorialToHorizontal(double RA, double Dec, double jd, double longitude, double latitude)
    {
        double LST = TimeUtils.GMST(jd) + longitude;
        LST = TimeUtils.NormalizeDegrees(LST);

        double H = TimeUtils.NormalizeDegrees(LST - RA);

        double Hrad = TimeUtils.ToRadians(H);
        double Decrad = TimeUtils.ToRadians(Dec);
        double Latrad = TimeUtils.ToRadians(latitude);

        double Alt = Math.Asin((Math.Sin(Decrad) * Math.Sin(Latrad)) + (Math.Cos(Decrad) * Math.Cos(Latrad) * Math.Cos(Hrad)));
        double Az = Math.Acos((Math.Sin(Decrad) - (Math.Sin(Alt) * Math.Sin(Latrad))) / (Math.Cos(Alt) * Math.Cos(Latrad)));

        if (Math.Sin(Hrad) > 0)
        {
            Az = (2 * Math.PI) - Az;
        }

        return (TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Az)), TimeUtils.ToDegrees(Alt));
    }
}
