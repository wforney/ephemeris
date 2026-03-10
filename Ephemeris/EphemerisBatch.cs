// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Heliology;
using Ephemeris.Planetology;
using Ephemeris.Selenography;
using Ephemeris.Stellarography;

namespace Ephemeris;

/// <summary>
/// Generates ephemeris time series for celestial bodies at regular intervals.
/// </summary>
public static class EphemerisBatch
{
    /// <summary>
    /// Generates a time series of solar positions at specified intervals.
    /// </summary>
    /// <param name="startUtc">The starting UTC time.</param>
    /// <param name="intervalMinutes">The interval in minutes between successive calculations.</param>
    /// <param name="count">The number of records to generate.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A lazily-evaluated sequence of EphemerisRecord containing solar positions.</returns>
    public static IEnumerable<EphemerisRecord> GenerateSunSeries(
        DateTime startUtc, int intervalMinutes, int count,
        double longitude, double latitude)
    {
        for (int i = 0; i < count; i++)
        {
            var dt = startUtc.AddMinutes(i * intervalMinutes);
            double jd = TimeZoneUtils.ToJulianDay(dt);
            double T = TimeUtils.JulianCentury(jd);
            var (ra, dec, sunDist) = SunEphemeris.ApparentEquatorialCoordinates(T);
            var (az, alt) = ObserverGeometry.EquatorialToHorizontal(ra, dec, jd, longitude, latitude);

            yield return new EphemerisRecord(
                TimeUtc: dt,
                Body: "Sun",
                RightAscension: ra,
                Declination: dec,
                Azimuth: az,
                Altitude: alt,
                Illumination: null,
                Distance: sunDist);
        }
    }

    /// <summary>
    /// Generates a time series of lunar positions at specified intervals.
    /// </summary>
    /// <param name="startUtc">The starting UTC time.</param>
    /// <param name="intervalMinutes">The interval in minutes between successive calculations.</param>
    /// <param name="count">The number of records to generate.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A lazily-evaluated sequence of EphemerisRecord containing lunar positions and illumination.</returns>
    public static IEnumerable<EphemerisRecord> GenerateMoonSeries(
        DateTime startUtc, int intervalMinutes, int count,
        double longitude, double latitude)
    {
        for (int i = 0; i < count; i++)
        {
            var dt = startUtc.AddMinutes(i * intervalMinutes);
            double jd = TimeZoneUtils.ToJulianDay(dt);
            double T = TimeUtils.JulianCentury(jd);
            var (ra, dec, distKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
            var (az, alt) = ObserverGeometry.EquatorialToHorizontal(ra, dec, jd, longitude, latitude);
            double illum = MoonEphemeris.Illumination(MoonEphemeris.PhaseAngle(T));

            yield return new EphemerisRecord(
                TimeUtc: dt,
                Body: "Moon",
                RightAscension: ra,
                Declination: dec,
                Azimuth: az,
                Altitude: alt,
                Illumination: illum,
                Distance: distKm);
        }
    }

    /// <summary>
    /// Generates a time series of planetary positions at specified intervals.
    /// </summary>
    /// <param name="planetName">The name of the planet (mercury, venus, mars, jupiter, saturn, uranus, neptune, pluto).</param>
    /// <param name="startUtc">The starting UTC time.</param>
    /// <param name="intervalMinutes">The interval in minutes between successive calculations.</param>
    /// <param name="count">The number of records to generate.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A lazily-evaluated sequence of EphemerisRecord containing planetary positions.</returns>
    public static IEnumerable<EphemerisRecord> GeneratePlanetSeries(
        string planetName,
        DateTime startUtc, int intervalMinutes, int count,
        double longitude, double latitude)
    {
        for (int i = 0; i < count; i++)
        {
            var dt = startUtc.AddMinutes(i * intervalMinutes);
            double jd = TimeZoneUtils.ToJulianDay(dt);
            double T = TimeUtils.JulianCentury(jd);

            OrbitalElements elements = planetName.ToLower() switch
            {
                "mercury" => new OrbitalElements(48.3313 + 3.24587E-5 * T, 7.0047 + 5.00E-8 * T, 29.1241 + 1.01444E-5 * T,
                              0.387098, 0.205635 + 5.59E-10 * T, 168.6562 + 4.0923344368 * T * 36525),
                "venus" => new OrbitalElements(76.6799 + 2.46590E-5 * T, 3.3946 + 2.75E-8 * T, 54.8910 + 1.38374E-5 * T,
                            0.723330, 0.006773 - 1.302E-9 * T, 48.0052 + 1.6021302244 * T * 36525),
                "mars" => new OrbitalElements(49.5574 + 2.11081E-5 * T, 1.8497 - 1.78E-8 * T, 286.5016 + 2.92961E-5 * T,
                           1.523688, 0.093405 + 2.516E-9 * T, 18.6021 + 0.5240207766 * T * 36525),
                "jupiter" => new OrbitalElements(100.4542 + 2.76854E-5 * T, 1.3030 - 1.557E-7 * T, 273.8777 + 1.64505E-5 * T,
                             5.20256, 0.048498 + 4.469E-9 * T, 19.8950 + 0.0830853001 * T * 36525),
                "saturn" => new OrbitalElements(113.6634 + 2.38980E-5 * T, 2.4886 - 1.081E-7 * T, 339.3939 + 2.97661E-5 * T,
                             9.55475, 0.055546 - 9.499E-9 * T, 316.9670 + 0.0334442282 * T * 36525),
                "uranus" => new OrbitalElements(74.0005 + 1.3978E-5 * T, 0.7733 + 1.9E-8 * T, 96.6612 + 3.0565E-5 * T,
                             19.18171, 0.047318 + 7.45E-9 * T, 142.5905 + 0.011725806 * T * 36525),
                "neptune" => new OrbitalElements(131.7806 + 3.0173E-5 * T, 1.7700 - 2.55E-7 * T, 272.8461 - 6.027E-6 * T,
                              30.05826, 0.008606 + 2.15E-9 * T, 260.2471 + 0.005995147 * T * 36525),
                "pluto" => new OrbitalElements(110.30347, 17.14175, 113.76329, 39.482, 0.2488, 14.53 + 0.00396 * T * 36525),
                _ => throw new ArgumentException("Unknown planet name", nameof(planetName))
            };

            var (equatorialPos, _) = PlanetEphemeris.SimplifiedPlanetPosition(T, elements);
            var horizontalPos = ObserverGeometry.EquatorialToHorizontal(equatorialPos.RightAscension, equatorialPos.Declination, jd, longitude, latitude);

            yield return new EphemerisRecord(
                TimeUtc: dt,
                Body: planetName,
                RightAscension: equatorialPos.RightAscension,
                Declination: equatorialPos.Declination,
                Azimuth: horizontalPos.Azimuth,
                Altitude: horizontalPos.Altitude,
                Illumination: null);
        }
    }

    /// <summary>
    /// Generates a time series of apparent positions for a fixed star at specified intervals.
    /// Proper-motion and precession corrections are applied at each time step.
    /// </summary>
    /// <param name="star">The star to track.</param>
    /// <param name="startUtc">The starting UTC time.</param>
    /// <param name="intervalMinutes">The interval in minutes between successive calculations.</param>
    /// <param name="count">The number of records to generate.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>
    /// A lazily-evaluated sequence of <see cref="EphemerisRecord"/> containing stellar positions.
    /// The <see cref="EphemerisRecord.Magnitude"/> field is populated from the star's catalog magnitude.
    /// </returns>
    public static IEnumerable<EphemerisRecord> GenerateStarSeries(
        FixedStar star,
        DateTime startUtc, int intervalMinutes, int count,
        double longitude, double latitude)
    {
        ArgumentNullException.ThrowIfNull(star);

        for (int i = 0; i < count; i++)
        {
            DateTime dt = startUtc.AddMinutes(i * intervalMinutes);
            double jd = TimeZoneUtils.ToJulianDay(dt);

            EquatorialCoordinates equatorial = StarEphemeris.ApparentPositionJd(star, jd);
            HorizontalCoordinates horizontal = ObserverGeometry.EquatorialToHorizontal(
                equatorial.RightAscension, equatorial.Declination, jd, longitude, latitude);

            yield return new EphemerisRecord(
                TimeUtc: dt,
                Body: string.IsNullOrEmpty(star.CommonName) ? star.BayerDesignation : star.CommonName,
                RightAscension: equatorial.RightAscension,
                Declination: equatorial.Declination,
                Azimuth: horizontal.Azimuth,
                Altitude: horizontal.Altitude,
                Illumination: null,
                Magnitude: star.Magnitude);
        }
    }

    /// <summary>
    /// Generates a time series of apparent positions for a named star from the built-in catalog.
    /// </summary>
    /// <param name="starName">
    /// Common name (e.g., "Sirius") or Bayer designation (e.g., "alCMa") to look up in
    /// <see cref="BrightStarCatalog"/>.
    /// </param>
    /// <param name="startUtc">The starting UTC time.</param>
    /// <param name="intervalMinutes">The interval in minutes between successive calculations.</param>
    /// <param name="count">The number of records to generate.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A lazily-evaluated sequence of <see cref="EphemerisRecord"/> for the named star.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="starName"/> is not found in the built-in catalog.
    /// </exception>
    public static IEnumerable<EphemerisRecord> GenerateStarSeries(
        string starName,
        DateTime startUtc, int intervalMinutes, int count,
        double longitude, double latitude)
    {
        FixedStar star = BrightStarCatalog.GetStar(starName)
            ?? throw new ArgumentException(
                $"Star '{starName}' not found in the built-in bright-star catalog.", nameof(starName));

        return GenerateStarSeries(star, startUtc, intervalMinutes, count, longitude, latitude);
    }

    /// <summary>
    /// Returns time windows during which a celestial body is above a given altitude threshold.
    /// </summary>
    /// <param name="body">Body name: "Sun", "Moon", or a planet name.</param>
    /// <param name="startUtc">Start of the search window (UTC).</param>
    /// <param name="windowDuration">Total duration of the search window.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="altitudeThresholdDeg">Minimum altitude in degrees to count as visible. Defaults to 0°.</param>
    /// <param name="stepMinutes">Sampling resolution in minutes. Defaults to 1.</param>
    /// <returns>
    /// A list of (Start, End) UTC DateTimes, each pair representing one continuous visibility window.
    /// </returns>
    public static List<(DateTime Start, DateTime End)> VisibilityWindows(
        string body,
        DateTime startUtc,
        TimeSpan windowDuration,
        double longitude,
        double latitude,
        double altitudeThresholdDeg = 0.0,
        int stepMinutes = 1)
    {
        int count = (int)Math.Ceiling(windowDuration.TotalMinutes / stepMinutes) + 1;
        IEnumerable<EphemerisRecord> series = body.ToLowerInvariant() switch
        {
            "sun"  => GenerateSunSeries(startUtc, stepMinutes, count, longitude, latitude),
            "moon" => GenerateMoonSeries(startUtc, stepMinutes, count, longitude, latitude),
            _      => GeneratePlanetSeries(body, startUtc, stepMinutes, count, longitude, latitude),
        };

        List<(DateTime Start, DateTime End)> windows = [];
        DateTime? windowStart = null;
        DateTime? lastAbove = null;

        foreach (EphemerisRecord record in series)
        {
            if (record.Altitude >= altitudeThresholdDeg)
            {
                windowStart ??= record.TimeUtc;
                lastAbove = record.TimeUtc;
            }
            else if (windowStart.HasValue)
            {
                windows.Add((windowStart.Value, lastAbove!.Value));
                windowStart = null;
                lastAbove = null;
            }
        }

        if (windowStart.HasValue)
            windows.Add((windowStart.Value, lastAbove!.Value));

        return windows;
    }
}
