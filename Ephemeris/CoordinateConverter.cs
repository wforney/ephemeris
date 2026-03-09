// Updated: 2026-05-29
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
        double eps = 23.439291 - (0.0130042 * T);
        double lon_rad = TimeUtils.ToRadians(lon);
        double lat_rad = TimeUtils.ToRadians(lat);
        double eps_rad = TimeUtils.ToRadians(eps);

        double x = Math.Cos(lon_rad) * Math.Cos(lat_rad);
        double y = (Math.Sin(lon_rad) * Math.Cos(lat_rad) * Math.Cos(eps_rad)) - (Math.Sin(lat_rad) * Math.Sin(eps_rad));
        double z = (Math.Sin(lon_rad) * Math.Cos(lat_rad) * Math.Sin(eps_rad)) + (Math.Sin(lat_rad) * Math.Cos(eps_rad));

        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(y, x)));
        double Dec = TimeUtils.ToDegrees(Math.Asin(z));

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
        double eps = 23.439291 - (0.0130042 * T);
        double RA_rad = TimeUtils.ToRadians(RA);
        double Dec_rad = TimeUtils.ToRadians(Dec);
        double eps_rad = TimeUtils.ToRadians(eps);

        double x = Math.Cos(RA_rad) * Math.Cos(Dec_rad);
        double y = Math.Sin(RA_rad) * Math.Cos(Dec_rad);
        double z = Math.Sin(Dec_rad);

        double xe = x;
        double ye = (y * Math.Cos(eps_rad)) + (z * Math.Sin(eps_rad));
        double ze = (-y * Math.Sin(eps_rad)) + (z * Math.Cos(eps_rad));

        double lon = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(ye, xe)));
        double lat = TimeUtils.ToDegrees(Math.Asin(ze));

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
        double d1 = TimeUtils.ToRadians(dec1);
        double d2 = TimeUtils.ToRadians(dec2);

        double a = (Math.Sin(dDec / 2) * Math.Sin(dDec / 2))
                 + (Math.Cos(d1) * Math.Cos(d2) * Math.Sin(dRa / 2) * Math.Sin(dRa / 2));
        return TimeUtils.ToDegrees(2 * Math.Asin(Math.Sqrt(a)));
    }
}
