// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Heliology;
using Ephemeris.Planetology;
using Ephemeris.Selenography;

namespace Ephemeris;

/// <summary>
/// Provides simple high-level queries for celestial body positions at specific times.
/// </summary>
public static class EphemerisCalculator
{
    /// <summary>
    /// Calculates the Moon's position in equatorial and horizontal coordinates for a given date and time.
    /// Applies topocentric parallax correction (Meeus Ch. 40) to shift geocentric RA/Dec to the
    /// apparent position as seen from the observer's location on Earth's surface.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="day">The day of month.</param>
    /// <param name="hour">The hour in decimal (0-24).</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="altitudeMeters">Observer altitude above sea level in metres (default 0).</param>
    /// <returns>A <see cref="CelestialObservation"/> with (RA, Dec, Azimuth, Altitude, Illumination) in degrees, with illumination as [0, 1].</returns>
    public static CelestialObservation GetMoonPosition(int year, int month, int day, double hour, double longitude, double latitude, double altitudeMeters = 0)
    {
        double jd = TimeUtils.JulianDay(year, month, day, hour);
        double T = TimeUtils.JulianCentury(jd);
        (double RA, double Dec, double distanceKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);

        // Apply topocentric parallax to correct for observer's position on Earth's surface.
        // This shifts the Moon's apparent RA/Dec by up to ~1° near the horizon.
        var topocentricMoon = TopocentricParallax.ApplyLunarParallax(
            new EquatorialCoordinates(RA, Dec), distanceKm, jd, longitude, latitude, altitudeMeters);
        (RA, Dec) = (topocentricMoon.RightAscension, topocentricMoon.Declination);

        var horizontalPos = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        double phaseAngle = MoonEphemeris.PhaseAngle(T);
        double illumination = MoonEphemeris.Illumination(phaseAngle);
        return new CelestialObservation(RA, Dec, horizontalPos.Azimuth, horizontalPos.Altitude, illumination);
    }

    /// <summary>
    /// Calculates the Sun's position in equatorial and horizontal coordinates for a given date and time.
    /// Optionally applies topocentric parallax correction (Meeus Ch. 40) for the observer's location.
    /// The Sun's equatorial horizontal parallax is ~8.794″ (max shift ~0.002°).
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="day">The day of month.</param>
    /// <param name="hour">The hour in decimal (0-24).</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="altitudeMeters">Observer altitude above sea level in metres (default 0). Used for topocentric parallax.</param>
    /// <returns>A <see cref="CelestialObservation"/> with (RA, Dec, Azimuth, Altitude) in degrees.</returns>
    public static CelestialObservation GetSunPosition(int year, int month, int day, double hour, double longitude, double latitude, double altitudeMeters = 0)
    {
        double jd = TimeUtils.JulianDay(year, month, day, hour);
        double T = TimeUtils.JulianCentury(jd);
        var (RA, Dec, R) = SunEphemeris.ApparentEquatorialCoordinates(T);

        // Apply topocentric parallax correction using Sun's geocentric distance.
        // At ~1 AU the shift is ≤ 8.8 arcseconds — small but included for completeness.
        double distanceKm = R * 149_597_870.7; // 1 AU in km
        var topocentricSun = TopocentricParallax.ApplyParallax(
            new EquatorialCoordinates(RA, Dec), distanceKm, jd, longitude, latitude, altitudeMeters);
        (RA, Dec) = (topocentricSun.RightAscension, topocentricSun.Declination);

        var horizontalPos = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        return new CelestialObservation(RA, Dec, horizontalPos.Azimuth, horizontalPos.Altitude);
    }

    /// <summary>
    /// Calculates the Sun's position in equatorial and horizontal coordinates for a local date and time.
    /// Optionally applies topocentric parallax correction (Meeus Ch. 40) for the observer's location.
    /// </summary>
    /// <param name="localDateTime">The local DateTime.</param>
    /// <param name="timeZoneId">The IANA or Windows timezone identifier (e.g., "Pacific Standard Time").</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="altitudeMeters">Observer altitude above sea level in metres (default 0). Used for topocentric parallax.</param>
    /// <returns>A <see cref="CelestialObservation"/> with (RA, Dec, Azimuth, Altitude) in degrees.</returns>
    public static CelestialObservation GetSunPosition(
        DateTime localDateTime, string timeZoneId, double longitude, double latitude, double altitudeMeters = 0)
    {
        DateTime utcTime = TimeZoneUtils.ToUtc(localDateTime, timeZoneId);
        double jd = TimeZoneUtils.ToJulianDay(utcTime);
        double T = TimeUtils.JulianCentury(jd);

        var (RA, Dec, R) = SunEphemeris.ApparentEquatorialCoordinates(T);

        // Apply topocentric parallax correction using Sun's geocentric distance (~1 AU).
        double distanceKm = R * 149_597_870.7;
        var topocentricSun = TopocentricParallax.ApplyParallax(
            new EquatorialCoordinates(RA, Dec), distanceKm, jd, longitude, latitude, altitudeMeters);
        (RA, Dec) = (topocentricSun.RightAscension, topocentricSun.Declination);

        var horizontalPos = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);

        return new CelestialObservation(RA, Dec, horizontalPos.Azimuth, horizontalPos.Altitude);
    }

    /// <summary>
    /// Calculates the Moon's position in equatorial and horizontal coordinates for a local date and time.
    /// Applies topocentric parallax correction (Meeus Ch. 40) to shift geocentric RA/Dec to the
    /// apparent position as seen from the observer's location on Earth's surface.
    /// </summary>
    /// <param name="localDateTime">The local DateTime.</param>
    /// <param name="timeZoneId">The IANA or Windows timezone identifier (e.g., "Pacific Standard Time").</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="altitudeMeters">Observer altitude above sea level in metres (default 0).</param>
    /// <returns>A <see cref="CelestialObservation"/> with (RA, Dec, Azimuth, Altitude, Illumination) in degrees, with illumination as [0, 1].</returns>
    public static CelestialObservation GetMoonPosition(
        DateTime localDateTime, string timeZoneId, double longitude, double latitude, double altitudeMeters = 0)
    {
        DateTime utcTime = TimeZoneUtils.ToUtc(localDateTime, timeZoneId);
        double jd = TimeZoneUtils.ToJulianDay(utcTime);
        double T = TimeUtils.JulianCentury(jd);

        var (RA, Dec, distanceKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);

        // Apply topocentric parallax to correct for observer's position on Earth's surface.
        // This shifts the Moon's apparent RA/Dec by up to ~1° near the horizon.
        var topocentricMoon = TopocentricParallax.ApplyLunarParallax(
            new EquatorialCoordinates(RA, Dec), distanceKm, jd, longitude, latitude, altitudeMeters);
        (RA, Dec) = (topocentricMoon.RightAscension, topocentricMoon.Declination);

        var horizontalPos = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        double phaseAngle = MoonEphemeris.PhaseAngle(T);
        double illumination = MoonEphemeris.Illumination(phaseAngle);

        return new CelestialObservation(RA, Dec, horizontalPos.Azimuth, horizontalPos.Altitude, illumination);
    }

    /// <summary>
    /// Calculates a planet's position in equatorial and horizontal coordinates for a local date and time.
    /// Optionally applies topocentric parallax correction (Meeus Ch. 40) for the observer's location.
    /// </summary>
    /// <param name="planet">The planet name (mercury, venus, mars, jupiter, saturn, uranus, neptune, pluto).</param>
    /// <param name="localDateTime">The local DateTime.</param>
    /// <param name="timeZoneId">The IANA or Windows timezone identifier (e.g., "Pacific Standard Time").</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="altitudeMeters">Observer altitude above sea level in metres (default 0). Used for topocentric parallax.</param>
    /// <returns>A <see cref="CelestialObservation"/> with (RA, Dec, Azimuth, Altitude) in degrees.</returns>
    public static CelestialObservation GetPlanetPosition(
        string planet, DateTime localDateTime, string timeZoneId, double longitude, double latitude, double altitudeMeters = 0)
    {
        DateTime utcTime = TimeZoneUtils.ToUtc(localDateTime, timeZoneId);
        double jd = TimeZoneUtils.ToJulianDay(utcTime);
        double T = TimeUtils.JulianCentury(jd);

        OrbitalElements elements = planet.ToLower() switch
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
            _ => throw new ArgumentException("Unknown planet name", nameof(planet))
        };

        var (equatorialPos, distanceAu) = PlanetEphemeris.SimplifiedPlanetPosition(T, elements);

        // Apply topocentric parallax using the planet's geocentric distance.
        // Effect is largest at opposition for inner planets; negligible for outer planets.
        double distanceKm = distanceAu * 149_597_870.7;
        var topocentricPos = TopocentricParallax.ApplyParallax(
            equatorialPos, distanceKm, jd, longitude, latitude, altitudeMeters);

        var horizontalPos = ObserverGeometry.EquatorialToHorizontal(
            topocentricPos.RightAscension, topocentricPos.Declination, jd, longitude, latitude);
        return new CelestialObservation(
            topocentricPos.RightAscension, topocentricPos.Declination,
            horizontalPos.Azimuth, horizontalPos.Altitude);
    }

    /// <summary>
    /// Calculates the angular separation between two celestial objects using the haversine formula.
    /// </summary>
    /// <param name="ra1">Right ascension of the first object in degrees.</param>
    /// <param name="dec1">Declination of the first object in degrees.</param>
    /// <param name="ra2">Right ascension of the second object in degrees.</param>
    /// <param name="dec2">Declination of the second object in degrees.</param>
    /// <returns>Angular separation in degrees [0, 180].</returns>
    public static double AngularSeparation(double ra1, double dec1, double ra2, double dec2)
        => CoordinateConverter.AngularSeparation(ra1, dec1, ra2, dec2);

    /// <summary>
    /// Returns the next full moon after the given UTC time.
    /// Searches forward in 1-hour steps until the phase angle crosses 180°.
    /// </summary>
    /// <param name="after">Starting UTC time.</param>
    /// <returns>Approximate UTC DateTime of the next full moon (within ~30 minutes).</returns>
    public static DateTime NextFullMoon(DateTime after)
    {
        var dt = after;
        double prevPhase = GetPhase(dt);
        while (true)
        {
            dt = dt.AddHours(1);
            double phase = GetPhase(dt);
            // Crossed through 180° (full moon)
            if (prevPhase < 180 && phase >= 180)
                return dt.AddMinutes(-30);  // approximate mid-crossing
            // Handle wrap-around: 360→0 without passing through 180
            prevPhase = phase;
        }
    }

    /// <summary>
    /// Returns the next new moon after the given UTC time.
    /// </summary>
    /// <param name="after">Starting UTC time.</param>
    /// <returns>Approximate UTC DateTime of the next new moon (within ~30 minutes).</returns>
    public static DateTime NextNewMoon(DateTime after)
    {
        var dt = after.AddHours(1); // skip current instant
        double prevPhase = GetPhase(dt);
        while (true)
        {
            dt = dt.AddHours(1);
            double phase = GetPhase(dt);
            // Crossed through 0° (new moon): detect wrap 350→10 or crossing 0
            if (prevPhase > 300 && phase < 60)
                return dt.AddMinutes(-30);
            prevPhase = phase;
        }
    }

    /// <summary>
    /// Returns the next occurrence of the specified season/equinox/solstice after the given date.
    /// </summary>
    /// <param name="season">The astronomical season to find.</param>
    /// <param name="after">Starting UTC date.</param>
    /// <returns>UTC DateTime of the next occurrence.</returns>
    public static DateTime NextSeason(Phenomenology.SeasonCalculator.Season season, DateTime after)
        => Phenomenology.SeasonCalculator.Next(season, after);

    /// <summary>
    /// Returns the next vernal equinox after the given date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <returns>UTC DateTime of the next vernal equinox.</returns>
    public static DateTime NextVernalEquinox(DateTime after)
        => Phenomenology.SeasonCalculator.NextSpringEquinox(after);

    /// <summary>
    /// Returns the next summer solstice after the given date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <returns>UTC DateTime of the next summer solstice.</returns>
    public static DateTime NextSummerSolstice(DateTime after)
        => Phenomenology.SeasonCalculator.NextSummerSolstice(after);

    /// <summary>
    /// Returns the next sunrise for the specified location after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>UTC DateTime of the next sunrise, or <c>null</c> if circumpolar.</returns>
    public static DateTime? NextSunrise(DateTime after, double longitude, double latitude) =>
        Enumerable.Range(0, 400)
            .Select(i => Phenomenology.RiseSetCalculator.Sun(after.Date.AddDays(i), longitude, latitude).Rise)
            .FirstOrDefault(r => r.HasValue && r.Value > after);

    /// <summary>
    /// Returns the next sunset for the specified location after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>UTC DateTime of the next sunset, or <c>null</c> if circumpolar.</returns>
    public static DateTime? NextSunset(DateTime after, double longitude, double latitude) =>
        Enumerable.Range(0, 400)
            .Select(i => Phenomenology.RiseSetCalculator.Sun(after.Date.AddDays(i), longitude, latitude).Set)
            .FirstOrDefault(r => r.HasValue && r.Value > after);

    /// <summary>
    /// Returns the next moonrise for the specified location after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>UTC DateTime of the next moonrise, or <c>null</c> if the Moon is circumpolar.</returns>
    public static DateTime? NextMoonrise(DateTime after, double longitude, double latitude) =>
        Enumerable.Range(0, 400)
            .Select(i => Phenomenology.RiseSetCalculator.Moon(after.Date.AddDays(i), longitude, latitude).Rise)
            .FirstOrDefault(r => r.HasValue && r.Value > after);

    /// <summary>
    /// Returns the next moonset for the specified location after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>UTC DateTime of the next moonset, or <c>null</c> if the Moon is circumpolar.</returns>
    public static DateTime? NextMoonset(DateTime after, double longitude, double latitude) =>
        Enumerable.Range(0, 400)
            .Select(i => Phenomenology.RiseSetCalculator.Moon(after.Date.AddDays(i), longitude, latitude).Set)
            .FirstOrDefault(r => r.HasValue && r.Value > after);

    // Returns the Moon's phase angle (0°=new, 180°=full) for a given UTC DateTime.
    private static double GetPhase(DateTime utc)
    {
        double jd = TimeZoneUtils.ToJulianDay(utc);
        double T  = TimeUtils.JulianCentury(jd);
        double phase = MoonEphemeris.PhaseAngle(T);
        return ((phase % 360) + 360) % 360;
    }
}
