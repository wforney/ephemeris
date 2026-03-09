// Updated: 2026-05-29
using Ephemeris.Chronology;

namespace Ephemeris.Geometry;

/// <summary>
/// Converts equatorial coordinates to horizontal coordinates for a given observer location and time.
/// </summary>
public static class ObserverGeometry
{
    /// <summary>
    /// Applies atmospheric refraction correction to a geometric altitude using Bennett's formula.
    /// Refraction lifts objects above the geometric horizon; this returns the apparent (observed) altitude.
    /// </summary>
    /// <param name="geometricAltitudeDeg">Geometric (true) altitude in degrees [-90, 90].</param>
    /// <returns>Apparent altitude in degrees after refraction. Below −1° refraction is not applied.</returns>
    public static double ApplyRefraction(double geometricAltitudeDeg)
    {
        if (geometricAltitudeDeg < -1.0)
            return geometricAltitudeDeg;

        // Bennett's formula: R in arcminutes, h in degrees
        double h = geometricAltitudeDeg;
        double R = 1.0 / Math.Tan(TimeUtils.ToRadians(h + (7.31 / (h + 4.4))));
        return geometricAltitudeDeg + (R / 60.0);
    }

    /// <summary>
    /// Converts equatorial coordinates (RA/Dec) to horizontal coordinates (Azimuth/Altitude) for an observer.
    /// </summary>
    /// <param name="RA">Right ascension in degrees [0, 360).</param>
    /// <param name="Dec">Declination in degrees [-90, 90].</param>
    /// <param name="jd">Julian Day number (fractional) for the observation.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="applyRefraction">If <see langword="true"/> (default), applies Bennett's atmospheric refraction correction.</param>
    /// <returns>A <see cref="HorizontalCoordinates"/> where Azimuth is [0, 360) measured from North clockwise, and Altitude is [-90, 90], in degrees.</returns>
    public static HorizontalCoordinates EquatorialToHorizontal(double RA, double Dec, double jd, double longitude, double latitude, bool applyRefraction = true)
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

        double altDeg = TimeUtils.ToDegrees(Alt);
        if (applyRefraction)
            altDeg = ApplyRefraction(altDeg);

        return new HorizontalCoordinates(TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Az)), altDeg);
    }
}
