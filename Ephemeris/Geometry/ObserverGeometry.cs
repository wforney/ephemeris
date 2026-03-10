// Updated: 2026-03-10
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
        // Clamp to [-1,1] to prevent NaN from Math.Acos when altitude ≈ 90° (zenith) or latitude ≈ ±90° (pole)
        double azimuthArg = (Math.Sin(declinationRad) - (Math.Sin(altitudeRad) * Math.Sin(latitudeRad))) / (Math.Cos(altitudeRad) * Math.Cos(latitudeRad));
        double azimuthRad = Math.Acos(Math.Clamp(azimuthArg, -1.0, 1.0));

        if (Math.Sin(hourAngleRad) > 0)
        {
            azimuthRad = (2 * Math.PI) - azimuthRad;
        }

        double altDeg = TimeUtils.ToDegrees(altitudeRad);
        if (applyRefraction)
            altDeg = ApplyRefraction(altDeg);

        return new HorizontalCoordinates(TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(azimuthRad)), altDeg);
    }

    /// <summary>
    /// Calculates the parallactic angle — the angle between the great circle through the object and the zenith,
    /// and the great circle through the object and the north celestial pole.
    /// Used for instrument position angle corrections and camera orientation.
    /// </summary>
    /// <param name="hourAngleDeg">Hour angle of the object in degrees (positive = west of meridian).</param>
    /// <param name="decDeg">Declination of the object in degrees [-90, 90].</param>
    /// <param name="latDeg">Observer latitude in degrees (north positive).</param>
    /// <returns>
    /// Parallactic angle in degrees. Zero at transit; positive when object is east of meridian.
    /// Returns 0° when sin(hourAngle) = 0 (at transit or anti-transit).
    /// </returns>
    public static double ParallacticAngle(double hourAngleDeg, double decDeg, double latDeg)
    {
        double H   = TimeUtils.ToRadians(hourAngleDeg);
        double dec = TimeUtils.ToRadians(decDeg);
        double lat = TimeUtils.ToRadians(latDeg);
        double sinH = Math.Sin(H);
        double denominator = (Math.Tan(lat) * Math.Cos(dec)) - (Math.Sin(dec) * Math.Cos(H));
        if (Math.Abs(sinH) < 1e-10 && Math.Abs(denominator) < 1e-10)
            return 0.0;
        return TimeUtils.ToDegrees(Math.Atan2(sinH, denominator));
    }
}
