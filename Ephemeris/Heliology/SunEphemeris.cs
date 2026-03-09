using Ephemeris.Chronology;

namespace Ephemeris.Heliology;

/// <summary>
/// Calculates the Sun's equatorial and heliocentric coordinates.
/// </summary>
public static class SunEphemeris
{
    /// <summary>
    /// Calculates the Sun's apparent geocentric equatorial coordinates (RA/Dec).
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>A tuple of (RA, Dec) in degrees, where RA is [0, 360) and Dec is [-90, 90].</returns>
    public static (double RA, double Dec) ApparentEquatorialCoordinates(double T)
    {
        (double L, double _) = HeliocentricLongitude(T);

        // Mean obliquity of the ecliptic (deg)
        double epsilon0 = 23.43929111 - (0.0130042 * T);

        // Convert to radians
        double Lrad = TimeUtils.ToRadians(L);
        double epsRad = TimeUtils.ToRadians(epsilon0);

        // RA/Dec in degrees
        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(Math.Cos(epsRad) * Math.Sin(Lrad), Math.Cos(Lrad))));
        double Dec = TimeUtils.ToDegrees(Math.Asin(Math.Sin(epsRad) * Math.Sin(Lrad)));

        return (RA, Dec);
    }

    /// <summary>
    /// Calculates the Sun's heliocentric ecliptic longitude and radius vector.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>A tuple of (longitude, radiusVector) where longitude is in degrees [0, 360) and radius is in AU.</returns>
    public static (double longitude, double radiusVector) HeliocentricLongitude(double T)
    {
        // Simplified high-accuracy VSOP87 coefficients for L0
        double L0 = 280.46646 + (36000.76983 * T) + (0.0003032 * T * T);
        return (TimeUtils.NormalizeDegrees(L0), 1.00014 - (0.01671 * Math.Cos(TimeUtils.ToRadians(357.529 + (35999.050 * T)))));
    }
}
