// Updated: 2026-03-09
using Ephemeris.Chronology;
using Ephemeris.Geodesy;

namespace Ephemeris.Heliology;

/// <summary>
/// Calculates the Sun's equatorial and heliocentric coordinates using the Meeus Ch. 25 algorithm.
/// Accuracy is approximately 0.01° (≈ 1 arcminute) for dates within a few centuries of J2000.
/// </summary>
public static class SunEphemeris
{
    /// <summary>
    /// Calculates the Sun's apparent geocentric equatorial coordinates (RA/Dec) and Earth–Sun distance.
    /// Uses the full Meeus Ch. 25 algorithm including equation of center, aberration, and nutation.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>
    /// A tuple of (RA, Dec, R) where RA is [0, 360) degrees, Dec is [−90, 90] degrees,
    /// and R is the Sun–Earth distance in AU.
    /// </returns>
    public static (double RA, double Dec, double R) ApparentEquatorialCoordinates(double T)
    {
        // Geometric mean longitude (deg)
        double L0 = TimeUtils.NormalizeDegrees(280.46646 + (36000.76983 * T) + (0.0003032 * T * T));
        // Mean anomaly of the Sun (deg)
        double M = TimeUtils.NormalizeDegrees(357.52911 + (35999.05029 * T) - (0.0001537 * T * T));
        double Mrad = TimeUtils.ToRadians(M);

        // Equation of center (deg)
        double C = ((1.914602 - (0.004817 * T) - (0.000014 * T * T)) * Math.Sin(Mrad))
                 + ((0.019993 - (0.000101 * T)) * Math.Sin(2 * Mrad))
                 + (0.000289 * Math.Sin(3 * Mrad));

        // Sun's true longitude and true anomaly
        double sunLon = TimeUtils.NormalizeDegrees(L0 + C);

        // Earth–Sun distance (radius vector, AU)
        double e = 0.016708634 - (0.000042037 * T) - (0.0000001267 * T * T); // orbital eccentricity
        double v = sunLon - (282.93768 + (1.7195 * T));   // approx true anomaly
        double vRad = TimeUtils.ToRadians(v);
        double R = 1.000001018 * (1.0 - (e * e)) / (1.0 + (e * Math.Cos(vRad)));

        // Apparent longitude: correct for nutation and aberration
        double Om = TimeUtils.NormalizeDegrees(125.04 - (1934.136 * T)); // Ω: ascending node of Moon's orbit (deg)
        double omRad = TimeUtils.ToRadians(Om);
        (double deltaPsi, _) = NutationCalculator.Calculate(T);
        double appLon = sunLon + deltaPsi - 0.00569 - (0.00478 * Math.Sin(omRad));

        // True obliquity with nutation correction
        double epsilon = NutationCalculator.TrueObliquity(T);
        // Additional aberration correction for obliquity
        epsilon += 0.00256 * Math.Cos(omRad);

        double appLonRad = TimeUtils.ToRadians(appLon);
        double epsRad = TimeUtils.ToRadians(epsilon);

        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(Math.Cos(epsRad) * Math.Sin(appLonRad), Math.Cos(appLonRad))));
        double Dec = TimeUtils.ToDegrees(Math.Asin(Math.Sin(epsRad) * Math.Sin(appLonRad)));

        return (RA, Dec, R);
    }

    /// <summary>
    /// Calculates the Sun's heliocentric ecliptic longitude and radius vector.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>A tuple of (longitude, radiusVector) where longitude is in degrees [0, 360) and radius is in AU.</returns>
    public static (double longitude, double radiusVector) HeliocentricLongitude(double T)
    {
        double L0 = 280.46646 + (36000.76983 * T) + (0.0003032 * T * T);
        return (TimeUtils.NormalizeDegrees(L0), 1.00014 - (0.01671 * Math.Cos(TimeUtils.ToRadians(357.529 + (35999.050 * T)))));
    }
}
