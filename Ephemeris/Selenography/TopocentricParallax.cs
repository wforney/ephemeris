// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris.Selenography;

/// <summary>
/// Applies diurnal (topocentric) parallax corrections to geocentric equatorial coordinates.
/// </summary>
public static class TopocentricParallax
{
    /// <summary>
    /// The equatorial radius of the Earth in kilometres, used to compute the equatorial horizontal parallax.
    /// </summary>
    private const double EarthEquatorialRadiusKm = 6378.14;

    /// <summary>
    /// The equatorial radius of the Earth in metres, used for the observer height term.
    /// </summary>
    private const double EarthEquatorialRadiusM = 6_378_140.0;

    /// <summary>
    /// Applies the diurnal (topocentric) parallax correction to geocentric equatorial coordinates,
    /// yielding the apparent position as seen from a specific point on Earth's surface.
    /// Uses the Meeus Ch. 40 algorithm.
    ///
    /// The correction is largest for nearby bodies:
    /// <list type="bullet">
    ///   <item><description>Moon: up to ~1° near the horizon</description></item>
    ///   <item><description>Sun: up to ~8.8″ (≈ 0.002°)</description></item>
    ///   <item><description>Mars at opposition: up to ~23″ (≈ 0.006°)</description></item>
    ///   <item><description>Outer planets: sub-arcsecond</description></item>
    /// </list>
    /// </summary>
    /// <param name="geocentric">Geocentric RA/Dec in degrees.</param>
    /// <param name="distanceKm">Geocentric body–Earth distance in kilometres.</param>
    /// <param name="jd">Julian Day number (UT/UTC).</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="altitudeMeters">Observer altitude above sea level in metres (default 0).</param>
    /// <returns>Topocentric <see cref="EquatorialCoordinates"/> in degrees.</returns>
    public static EquatorialCoordinates ApplyParallax(
        EquatorialCoordinates geocentric,
        double distanceKm,
        double jd,
        double longitude,
        double latitude,
        double altitudeMeters = 0)
    {
        // Step 1 — Equatorial horizontal parallax π (angular radius of Earth as seen from the body).
        // sin π = Earth_equatorial_radius / distance
        double sinPi = EarthEquatorialRadiusKm / distanceKm;

        // Step 2 — Observer's geocentric latitude φ′ and geocentric radius ρ (Meeus eq. 40.3–40.4).
        // u = atan(0.99664719 · tan φ)  corrects geodetic → geocentric latitude via Earth's flattening.
        double latRad = double.DegreesToRadians(latitude);
        double u = Math.Atan(0.99664719 * Math.Tan(latRad));
        double heightTerm = altitudeMeters / EarthEquatorialRadiusM;
        double rhoSinPhiPrime = (0.99664719 * Math.Sin(u)) + (heightTerm * Math.Sin(latRad));
        double rhoCosPhiPrime = Math.Cos(u) + (heightTerm * Math.Cos(latRad));

        // Step 3 — Local hour angle H (deg → rad).
        double lst = TimeUtils.GMST(jd) + longitude;
        double H = TimeUtils.NormalizeDegrees(lst - geocentric.RightAscension);
        double hourAngleRad = double.DegreesToRadians(H);

        // Step 4 — Parallax correction in RA Δα (Meeus eq. 40.6).
        double raRad  = double.DegreesToRadians(geocentric.RightAscension);
        double decRad = double.DegreesToRadians(geocentric.Declination);

        double deltaAlphaRad = Math.Atan2(
            -rhoCosPhiPrime * sinPi * Math.Sin(hourAngleRad),
            Math.Cos(decRad) - (rhoCosPhiPrime * sinPi * Math.Cos(hourAngleRad)));

        // Step 5 — Topocentric RA.
        double raPrimeRad = raRad + deltaAlphaRad;

        // Step 6 — Topocentric Dec (Meeus eq. 40.7).
        double decPrimeRad = Math.Atan2(
            (Math.Sin(decRad) - (rhoSinPhiPrime * sinPi)) * Math.Cos(deltaAlphaRad),
            Math.Cos(decRad) - (rhoCosPhiPrime * sinPi * Math.Cos(hourAngleRad)));

        return new EquatorialCoordinates(
            TimeUtils.NormalizeDegrees(double.RadiansToDegrees(raPrimeRad)),
            double.RadiansToDegrees(decPrimeRad));
    }

    /// <summary>
    /// Convenience overload for the Moon. Equivalent to calling
    /// <see cref="ApplyParallax(EquatorialCoordinates, double, double, double, double, double)"/>
    /// with the Moon's geocentric distance.
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
        => ApplyParallax(geocentric, distanceKm, jd, longitude, latitude, altitudeMeters);
}
