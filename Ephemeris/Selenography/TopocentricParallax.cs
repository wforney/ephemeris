// Updated: 2026-03-09
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris.Selenography;

/// <summary>
/// Applies diurnal (topocentric) parallax corrections to the Moon's geocentric equatorial coordinates.
/// </summary>
public static class TopocentricParallax
{
    /// <summary>
    /// Applies the diurnal (topocentric) parallax correction to the Moon's geocentric
    /// equatorial coordinates, yielding the position as seen from a specific point on Earth's surface.
    /// Uses the Meeus Ch. 40 algorithm.
    /// Correction can reach ~1° when the Moon is near the horizon; it is zero at the zenith.
    /// </summary>
    /// <param name="geocentric">Geocentric RA/Dec in degrees.</param>
    /// <param name="distanceKm">Geocentric Moon–Earth distance in kilometres.</param>
    /// <param name="jd">Julian Day number (UT/UTC).</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="altitudeMeters">Observer altitude above sea level in metres (default 0).</param>
    /// <returns>Topocentric <see cref="EquatorialCoordinates"/> in degrees.</returns>
    public static EquatorialCoordinates ApplyLunarParallax(
        EquatorialCoordinates geocentric,
        double distanceKm,
        double jd,
        double longitude,
        double latitude,
        double altitudeMeters = 0)
    {
        // Step 1 — Equatorial horizontal parallax π (angular radius of Earth as seen from Moon)
        double sinPi = 6378.14 / distanceKm;
        double piRad = Math.Asin(sinPi);

        // Step 2 — Observer's geocentric latitude and radius (Meeus eq. 40.3–40.4)
        double latRad = double.DegreesToRadians(latitude);
        double u = Math.Atan(0.99664719 * Math.Tan(latRad));
        double rhoSinPhiPrime = (0.99664719 * Math.Sin(u)) + ((altitudeMeters / 6_378_140.0) * Math.Sin(latRad));
        double rhoCosPhiPrime = Math.Cos(u) + ((altitudeMeters / 6_378_140.0) * Math.Cos(latRad));

        // Step 3 — Local hour angle H
        double lst = TimeUtils.GMST(jd) + longitude;
        double H = TimeUtils.NormalizeDegrees(lst - geocentric.RightAscension);
        double hourAngleRad = double.DegreesToRadians(H);

        // Step 4 — Parallax correction in RA (Meeus eq. 40.6)
        double raRad = double.DegreesToRadians(geocentric.RightAscension);
        double decRad = double.DegreesToRadians(geocentric.Declination);

        double deltaAlphaRad = Math.Atan2(
            -rhoCosPhiPrime * sinPi * Math.Sin(hourAngleRad),
            Math.Cos(decRad) - (rhoCosPhiPrime * sinPi * Math.Cos(hourAngleRad)));

        // Step 5 — Corrected RA
        double raPrimeRad = raRad + deltaAlphaRad;

        // Step 6 — Corrected Dec (Meeus eq. 40.7)
        double decPrimeRad = Math.Atan2(
            (Math.Sin(decRad) - (rhoSinPhiPrime * sinPi)) * Math.Cos(deltaAlphaRad),
            Math.Cos(decRad) - (rhoCosPhiPrime * sinPi * Math.Cos(hourAngleRad)));

        return new EquatorialCoordinates(
            TimeUtils.NormalizeDegrees(double.RadiansToDegrees(raPrimeRad)),
            double.RadiansToDegrees(decPrimeRad));
    }
}
