// Updated: 2026-07-14
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
        double altitudeDeg = geometricAltitudeDeg;
        double refractionArcmin = 1.0 / Math.Tan(TimeUtils.ToRadians(altitudeDeg + (7.31 / (altitudeDeg + 4.4))));
        return geometricAltitudeDeg + (refractionArcmin / 60.0);
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

        double hourAngle = TimeUtils.NormalizeDegrees(LST - RA);

        double hourAngleRad = TimeUtils.ToRadians(hourAngle);
        double declinationRad = TimeUtils.ToRadians(Dec);
        double latitudeRad = TimeUtils.ToRadians(latitude);

        double altitudeRad = Math.Asin((Math.Sin(declinationRad) * Math.Sin(latitudeRad)) + (Math.Cos(declinationRad) * Math.Cos(latitudeRad) * Math.Cos(hourAngleRad)));
        double azimuthRad = Math.Acos((Math.Sin(declinationRad) - (Math.Sin(altitudeRad) * Math.Sin(latitudeRad))) / (Math.Cos(altitudeRad) * Math.Cos(latitudeRad)));

        if (Math.Sin(hourAngleRad) > 0)
        {
            azimuthRad = (2 * Math.PI) - azimuthRad;
        }

        double altDeg = TimeUtils.ToDegrees(altitudeRad);
        if (applyRefraction)
            altDeg = ApplyRefraction(altDeg);

        return new HorizontalCoordinates(TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(azimuthRad)), altDeg);
    }
}
