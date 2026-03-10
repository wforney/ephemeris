// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris;

/// <summary>
/// Converts between ecliptic and equatorial coordinate systems using the obliquity of the ecliptic.
/// Also converts between equatorial (RA/Dec) and galactic (l, b) coordinate systems.
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

    // IAU 1958 galactic coordinate system constants (J2000.0 equivalents, from ESA/Hipparcos)
    private const double RaNGPDeg  = 192.859481; // RA of North Galactic Pole (J2000), degrees
    private const double DecNGPDeg =  27.128251; // Dec of North Galactic Pole (J2000), degrees
    private const double LNcpDeg   = 122.932;    // Galactic longitude of North Celestial Pole, degrees

    /// <summary>
    /// Converts equatorial (RA/Dec, J2000) coordinates to IAU 1958 galactic coordinates (l, b).
    /// </summary>
    /// <param name="coordinates">Equatorial coordinates (RA in degrees [0,360), Dec in degrees [-90,90]).</param>
    /// <returns>A tuple of (L, B) where L is galactic longitude [0,360) and B is galactic latitude [-90,90], both in degrees.</returns>
    /// <remarks>
    /// IAU 1958 galactic coordinate system, using J2000.0 pole and node constants from ESA/Hipparcos:
    /// <code>
    ///   sin(b) = sin(Dec)·sin(Dec_NGP) + cos(Dec)·cos(Dec_NGP)·cos(RA − RA_NGP)
    ///   l      = l_NCP − atan2(cos(Dec)·sin(RA − RA_NGP),
    ///                          sin(Dec)·cos(Dec_NGP) − cos(Dec)·sin(Dec_NGP)·cos(RA − RA_NGP))
    /// </code>
    /// where RA_NGP = 192.859481°, Dec_NGP = 27.128251°, and l_NCP = 122.932°.
    /// The Galactic Center lies at l ≈ 0°, b ≈ 0°.
    /// </remarks>
    public static (double L, double B) EquatorialToGalactic(EquatorialCoordinates coordinates)
    {
        double raRad    = TimeUtils.ToRadians(coordinates.RightAscension);
        double decRad   = TimeUtils.ToRadians(coordinates.Declination);
        double raNGPRad = TimeUtils.ToRadians(RaNGPDeg);
        double sinNGP   = Math.Sin(TimeUtils.ToRadians(DecNGPDeg));
        double cosNGP   = Math.Cos(TimeUtils.ToRadians(DecNGPDeg));

        double sinB = (Math.Sin(decRad) * sinNGP) + (Math.Cos(decRad) * cosNGP * Math.Cos(raRad - raNGPRad));
        double b    = TimeUtils.ToDegrees(Math.Asin(Math.Clamp(sinB, -1.0, 1.0)));

        double xL = Math.Cos(decRad) * Math.Sin(raRad - raNGPRad);
        double yL = (Math.Sin(decRad) * cosNGP) - (Math.Cos(decRad) * sinNGP * Math.Cos(raRad - raNGPRad));
        double l  = TimeUtils.NormalizeDegrees(LNcpDeg - TimeUtils.ToDegrees(Math.Atan2(xL, yL)));

        return (l, b);
    }

    /// <summary>
    /// Converts IAU 1958 galactic coordinates (l, b) to equatorial (RA/Dec, J2000) coordinates.
    /// </summary>
    /// <param name="l">Galactic longitude in degrees [0, 360).</param>
    /// <param name="b">Galactic latitude in degrees [-90, 90].</param>
    /// <returns>Equatorial coordinates (RA in degrees [0,360), Dec in degrees [-90,90]).</returns>
    /// <remarks>
    /// Inverse of <see cref="EquatorialToGalactic"/> using the IAU 1958 constants (J2000.0, ESA/Hipparcos):
    /// <code>
    ///   sin(Dec) = sin(b)·sin(Dec_NGP) + cos(b)·cos(Dec_NGP)·cos(l_NCP − l)
    ///   RA       = RA_NGP + atan2(cos(b)·sin(l_NCP − l),
    ///                             sin(b)·cos(Dec_NGP) − cos(b)·sin(Dec_NGP)·cos(l_NCP − l))
    /// </code>
    /// where RA_NGP = 192.859481°, Dec_NGP = 27.128251°, and l_NCP = 122.932°.
    /// </remarks>
    public static EquatorialCoordinates GalacticToEquatorial(double l, double b)
    {
        double lRad     = TimeUtils.ToRadians(l);
        double bRad     = TimeUtils.ToRadians(b);
        double lNcpRad  = TimeUtils.ToRadians(LNcpDeg);
        double raNGPRad = TimeUtils.ToRadians(RaNGPDeg);
        double sinNGP   = Math.Sin(TimeUtils.ToRadians(DecNGPDeg));
        double cosNGP   = Math.Cos(TimeUtils.ToRadians(DecNGPDeg));

        double sinDec = (Math.Sin(bRad) * sinNGP) + (Math.Cos(bRad) * cosNGP * Math.Cos(lNcpRad - lRad));
        double dec    = TimeUtils.ToDegrees(Math.Asin(Math.Clamp(sinDec, -1.0, 1.0)));

        double xRA = Math.Cos(bRad) * Math.Sin(lNcpRad - lRad);
        double yRA = (Math.Sin(bRad) * cosNGP) - (Math.Cos(bRad) * sinNGP * Math.Cos(lNcpRad - lRad));
        double ra  = TimeUtils.NormalizeDegrees(RaNGPDeg + TimeUtils.ToDegrees(Math.Atan2(xRA, yRA)));

        return new EquatorialCoordinates(ra, dec);
    }
}
