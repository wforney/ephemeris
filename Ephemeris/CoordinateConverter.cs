// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris;

/// <summary>
/// Converts between ecliptic and equatorial coordinate systems using the obliquity of the ecliptic.
/// </summary>
public static class CoordinateConverter
{
    /// <summary>
    /// Converts ecliptic longitude and latitude to equatorial coordinates (RA/Dec).
    /// </summary>
    /// <param name="lon">Ecliptic longitude in degrees [0, 360).</param>
    /// <param name="lat">Ecliptic latitude in degrees [-90, 90].</param>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>An <see cref="EquatorialCoordinates"/> of (RA, Dec) in degrees.</returns>
    public static EquatorialCoordinates EclipticToEquatorial(double lon, double lat, double T)
    {
        double eclipticObliquity = 23.439291 - (0.0130042 * T);
        double lonRad = TimeUtils.ToRadians(lon);
        double latRad = TimeUtils.ToRadians(lat);
        double eclipticObliquityRad = TimeUtils.ToRadians(eclipticObliquity);

        double x = Math.Cos(lonRad) * Math.Cos(latRad); // ecliptic→equatorial direction cosines
        double y = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Cos(eclipticObliquityRad)) - (Math.Sin(latRad) * Math.Sin(eclipticObliquityRad));
        double z = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Sin(eclipticObliquityRad)) + (Math.Sin(latRad) * Math.Cos(eclipticObliquityRad));

        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(y, x)));
        double Dec = TimeUtils.ToDegrees(Math.Asin(Math.Clamp(z, -1.0, 1.0)));

        return new EquatorialCoordinates(RA, Dec);
    }

    /// <summary>
    /// Converts equatorial coordinates (RA/Dec) to ecliptic longitude and latitude.
    /// </summary>
    /// <param name="RA">Right ascension in degrees [0, 360).</param>
    /// <param name="Dec">Declination in degrees [-90, 90].</param>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>An <see cref="EclipticCoordinates"/> of (Longitude, Latitude) in degrees.</returns>
    public static EclipticCoordinates EquatorialToEcliptic(double RA, double Dec, double T)
    {
        double eclipticObliquity = 23.439291 - (0.0130042 * T);
        double raRad = TimeUtils.ToRadians(RA);
        double decRad = TimeUtils.ToRadians(Dec);
        double eclipticObliquityRad = TimeUtils.ToRadians(eclipticObliquity);

        double x = Math.Cos(raRad) * Math.Cos(decRad); // equatorial→ecliptic direction cosines
        double y = Math.Sin(raRad) * Math.Cos(decRad);
        double z = Math.Sin(decRad);

        double xe = x;
        double ye = (y * Math.Cos(eclipticObliquityRad)) + (z * Math.Sin(eclipticObliquityRad));
        double ze = (-y * Math.Sin(eclipticObliquityRad)) + (z * Math.Cos(eclipticObliquityRad));

        double lon = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(ye, xe)));
        double lat = TimeUtils.ToDegrees(Math.Asin(Math.Clamp(ze, -1.0, 1.0)));

        return new EclipticCoordinates(lon, lat);
    }

    /// <summary>
    /// Calculates the angular separation between two celestial objects using the haversine formula.
    /// </summary>
    /// <param name="ra1">Right ascension of the first object in degrees [0, 360).</param>
    /// <param name="dec1">Declination of the first object in degrees [-90, 90].</param>
    /// <param name="ra2">Right ascension of the second object in degrees [0, 360).</param>
    /// <param name="dec2">Declination of the second object in degrees [-90, 90].</param>
    /// <returns>Angular separation in degrees [0, 180].</returns>
    public static double AngularSeparation(double ra1, double dec1, double ra2, double dec2)
    {
        double dRa = TimeUtils.ToRadians(ra2 - ra1);
        double dDec = TimeUtils.ToRadians(dec2 - dec1);
        double dec1Rad = TimeUtils.ToRadians(dec1);
        double dec2Rad = TimeUtils.ToRadians(dec2);

        double haversineA = (Math.Sin(dDec / 2) * Math.Sin(dDec / 2))
                 + (Math.Cos(dec1Rad) * Math.Cos(dec2Rad) * Math.Sin(dRa / 2) * Math.Sin(dRa / 2));
        // Clamp to [-1,1]: haversineA can slightly exceed 1 for near-antipodal points due to floating-point rounding
        return TimeUtils.ToDegrees(2 * Math.Asin(Math.Clamp(Math.Sqrt(haversineA), 0.0, 1.0)));
    }
}
