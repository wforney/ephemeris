using Ephemeris.Chronology;

namespace Ephemeris.Selenography;

/// <summary>
/// Calculates the Moon's geocentric coordinates, phase angle, and illumination.
/// </summary>
public static class MoonEphemeris
{
    /// <summary>
    /// Calculates the Moon's geocentric equatorial coordinates and distance from Earth.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>A tuple of (RA, Dec, distanceKm) where RA and Dec are in degrees, distance is in kilometres.</returns>
    public static (double RA, double Dec, double distanceKm) GeocentricEquatorialCoordinates(double T)
    {
        // Mean elongation of the Moon
        _ = TimeUtils.NormalizeDegrees(297.8501921 + (445267.1114034 * T));
        _ = TimeUtils.NormalizeDegrees(357.5291092 + (35999.0502909 * T)); // Sun
        double M1 = TimeUtils.NormalizeDegrees(134.9633964 + (477198.8675055 * T)); // Moon
        double F = TimeUtils.NormalizeDegrees(93.2720950 + (483202.0175233 * T));

        // Ecliptic longitude (simplified model)
        double lon = 218.316 + (13.176396 * T * 36525.0) + (6.289 * Math.Sin(TimeUtils.ToRadians(M1)));
        double lat = 5.128 * Math.Sin(TimeUtils.ToRadians(F));
        double dist = 385001.0 - (20905.0 * Math.Cos(TimeUtils.ToRadians(M1)));

        // Convert to RA/Dec
        double eps = 23.439291 - (0.0130042 * T);
        double lonRad = TimeUtils.ToRadians(lon);
        double latRad = TimeUtils.ToRadians(lat);
        double epsRad = TimeUtils.ToRadians(eps);

        double x = Math.Cos(lonRad) * Math.Cos(latRad);
        double y = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Cos(epsRad)) - (Math.Sin(latRad) * Math.Sin(epsRad));
        double z = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Sin(epsRad)) + (Math.Sin(latRad) * Math.Cos(epsRad));

        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(y, x)));
        double Dec = TimeUtils.ToDegrees(Math.Asin(z));

        return (RA, Dec, dist);
    }

    /// <summary>
    /// Calculates the Moon's illumination fraction given its phase angle.
    /// </summary>
    /// <param name="phaseAngle">The phase angle in degrees (angle between Sun and Moon as seen from Earth).</param>
    /// <returns>Illumination fraction in [0, 1], where 0 is new moon and 1 is full moon.</returns>
    public static double Illumination(double phaseAngle)
    {
        double i = TimeUtils.ToRadians(phaseAngle);
        return (1 + Math.Cos(i)) / 2;
    }

    /// <summary>
    /// Calculates the Moon's phase angle (the geocentric elongation angle between Sun and Moon).
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>The phase angle in degrees.</returns>
    public static double PhaseAngle(double T)
    {
        double D = TimeUtils.NormalizeDegrees(297.8501921 + (445267.1114034 * T));
        double M = TimeUtils.NormalizeDegrees(357.5291092 + (35999.0502909 * T));
        double M1 = TimeUtils.NormalizeDegrees(134.9633964 + (477198.8675055 * T));
        return 180 - D - (6.289 * Math.Sin(TimeUtils.ToRadians(M1))) + (2.100 * Math.Sin(TimeUtils.ToRadians(M))) - (1.274 * Math.Sin(TimeUtils.ToRadians((2 * D) - M1)));
    }
}
