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
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="day">The day of month.</param>
    /// <param name="hour">The hour in decimal (0-24).</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A tuple of (RA, Dec, Azimuth, Altitude, Illumination) in degrees, with illumination as [0, 1].</returns>
    public static (double RA, double Dec, double Az, double Alt, double Illumination) GetMoonPosition(int year, int month, int day, double hour, double longitude, double latitude)
    {
        double jd = TimeUtils.JulianDay(year, month, day, hour);
        double T = TimeUtils.JulianCentury(jd);
        (double RA, double Dec, double _) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        (double Az, double Alt) = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        double phaseAngle = MoonEphemeris.PhaseAngle(T);
        double illumination = MoonEphemeris.Illumination(phaseAngle);
        return (RA, Dec, Az, Alt, illumination);
    }

    /// <summary>
    /// Calculates the Sun's position in equatorial and horizontal coordinates for a given date and time.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="day">The day of month.</param>
    /// <param name="hour">The hour in decimal (0-24).</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A tuple of (RA, Dec, Azimuth, Altitude) in degrees.</returns>
    public static (double RA, double Dec, double Az, double Alt) GetSunPosition(int year, int month, int day, double hour, double longitude, double latitude)
    {
        double jd = TimeUtils.JulianDay(year, month, day, hour);
        double T = TimeUtils.JulianCentury(jd);
        (double RA, double Dec, double _) = SunEphemeris.ApparentEquatorialCoordinates(T);
        (double Az, double Alt) = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        return (RA, Dec, Az, Alt);
    }

    /// <summary>
    /// Calculates the Sun's position in equatorial and horizontal coordinates for a local date and time.
    /// </summary>
    /// <param name="localDateTime">The local DateTime.</param>
    /// <param name="timeZoneId">The IANA or Windows timezone identifier (e.g., "Pacific Standard Time").</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A tuple of (RA, Dec, Azimuth, Altitude) in degrees.</returns>
    public static (double RA, double Dec, double Az, double Alt) GetSunPosition(
        DateTime localDateTime, string timeZoneId, double longitude, double latitude)
    {
        DateTime utcTime = TimeZoneUtils.ToUtc(localDateTime, timeZoneId);
        double jd = TimeZoneUtils.ToJulianDay(utcTime);
        double T = TimeUtils.JulianCentury(jd);

        var (RA, Dec, _) = SunEphemeris.ApparentEquatorialCoordinates(T);
        var (Az, Alt) = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);

        return (RA, Dec, Az, Alt);
    }

    /// <summary>
    /// Calculates the Moon's position in equatorial and horizontal coordinates for a local date and time.
    /// </summary>
    /// <param name="localDateTime">The local DateTime.</param>
    /// <param name="timeZoneId">The IANA or Windows timezone identifier (e.g., "Pacific Standard Time").</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A tuple of (RA, Dec, Azimuth, Altitude, Illumination) in degrees, with illumination as [0, 1].</returns>
    public static (double RA, double Dec, double Az, double Alt, double Illumination) GetMoonPosition(
        DateTime localDateTime, string timeZoneId, double longitude, double latitude)
    {
        DateTime utcTime = TimeZoneUtils.ToUtc(localDateTime, timeZoneId);
        double jd = TimeZoneUtils.ToJulianDay(utcTime);
        double T = TimeUtils.JulianCentury(jd);

        var (RA, Dec, _) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        var (Az, Alt) = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        double phaseAngle = MoonEphemeris.PhaseAngle(T);
        double illumination = MoonEphemeris.Illumination(phaseAngle);

        return (RA, Dec, Az, Alt, illumination);
    }

    /// <summary>
    /// Calculates a planet's position in equatorial and horizontal coordinates for a local date and time.
    /// </summary>
    /// <param name="planet">The planet name (mercury, venus, mars, jupiter, saturn, uranus, neptune, pluto).</param>
    /// <param name="localDateTime">The local DateTime.</param>
    /// <param name="timeZoneId">The IANA or Windows timezone identifier (e.g., "Pacific Standard Time").</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A tuple of (RA, Dec, Azimuth, Altitude) in degrees.</returns>
    public static (double RA, double Dec, double Az, double Alt) GetPlanetPosition(
        string planet, DateTime localDateTime, string timeZoneId, double longitude, double latitude)
    {
        DateTime utcTime = TimeZoneUtils.ToUtc(localDateTime, timeZoneId);
        double jd = TimeZoneUtils.ToJulianDay(utcTime);
        double T = TimeUtils.JulianCentury(jd);

        (double N, double i, double w, double a, double e, double M) = planet.ToLower() switch
        {
            "mercury" => (48.3313 + 3.24587E-5 * T, 7.0047 + 5.00E-8 * T, 29.1241 + 1.01444E-5 * T,
                          0.387098, 0.205635 + 5.59E-10 * T, 168.6562 + 4.0923344368 * T * 36525),
            "venus" => (76.6799 + 2.46590E-5 * T, 3.3946 + 2.75E-8 * T, 54.8910 + 1.38374E-5 * T,
                        0.723330, 0.006773 - 1.302E-9 * T, 48.0052 + 1.6021302244 * T * 36525),
            "mars" => (49.5574 + 2.11081E-5 * T, 1.8497 - 1.78E-8 * T, 286.5016 + 2.92961E-5 * T,
                       1.523688, 0.093405 + 2.516E-9 * T, 18.6021 + 0.5240207766 * T * 36525),
            "jupiter" => (100.4542 + 2.76854E-5 * T, 1.3030 - 1.557E-7 * T, 273.8777 + 1.64505E-5 * T,
                         5.20256, 0.048498 + 4.469E-9 * T, 19.8950 + 0.0830853001 * T * 36525),
            "saturn" => (113.6634 + 2.38980E-5 * T, 2.4886 - 1.081E-7 * T, 339.3939 + 2.97661E-5 * T,
                         9.55475, 0.055546 - 9.499E-9 * T, 316.9670 + 0.0334442282 * T * 36525),
            "uranus" => (74.0005 + 1.3978E-5 * T, 0.7733 + 1.9E-8 * T, 96.6612 + 3.0565E-5 * T,
                         19.18171, 0.047318 + 7.45E-9 * T, 142.5905 + 0.011725806 * T * 36525),
            "neptune" => (131.7806 + 3.0173E-5 * T, 1.7700 - 2.55E-7 * T, 272.8461 - 6.027E-6 * T,
                          30.05826, 0.008606 + 2.15E-9 * T, 260.2471 + 0.005995147 * T * 36525),
            "pluto" => (110.30347, 17.14175, 113.76329, 39.482, 0.2488, 14.53 + 0.00396 * T * 36525),
            _ => throw new ArgumentException("Unknown planet name", nameof(planet))
        };

        var (RA, Dec) = PlanetEphemeris.SimplifiedPlanetPosition(T, N, i, w, a, e, M);
        var (Az, Alt) = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        return (RA, Dec, Az, Alt);
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
    public static DateTime? NextSunrise(DateTime after, double longitude, double latitude)
    {
        var dt = after.Date;
        for (int i = 0; i < 400; i++)
        {
            var rts = Phenomenology.RiseSetCalculator.Sun(dt, longitude, latitude);
            if (rts.Rise.HasValue && rts.Rise.Value > after)
                return rts.Rise.Value;
            dt = dt.AddDays(1);
        }
        return null;
    }

    /// <summary>
    /// Returns the next sunset for the specified location after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>UTC DateTime of the next sunset, or <c>null</c> if circumpolar.</returns>
    public static DateTime? NextSunset(DateTime after, double longitude, double latitude)
    {
        var dt = after.Date;
        for (int i = 0; i < 400; i++)
        {
            var rts = Phenomenology.RiseSetCalculator.Sun(dt, longitude, latitude);
            if (rts.Set.HasValue && rts.Set.Value > after)
                return rts.Set.Value;
            dt = dt.AddDays(1);
        }
        return null;
    }

    /// <summary>
    /// Returns the next moonrise for the specified location after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>UTC DateTime of the next moonrise, or <c>null</c> if the Moon is circumpolar.</returns>
    public static DateTime? NextMoonrise(DateTime after, double longitude, double latitude)
    {
        var dt = after.Date;
        for (int i = 0; i < 400; i++)
        {
            var rts = Phenomenology.RiseSetCalculator.Moon(dt, longitude, latitude);
            if (rts.Rise.HasValue && rts.Rise.Value > after)
                return rts.Rise.Value;
            dt = dt.AddDays(1);
        }
        return null;
    }

    /// <summary>
    /// Returns the next moonset for the specified location after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>UTC DateTime of the next moonset, or <c>null</c> if the Moon is circumpolar.</returns>
    public static DateTime? NextMoonset(DateTime after, double longitude, double latitude)
    {
        var dt = after.Date;
        for (int i = 0; i < 400; i++)
        {
            var rts = Phenomenology.RiseSetCalculator.Moon(dt, longitude, latitude);
            if (rts.Set.HasValue && rts.Set.Value > after)
                return rts.Set.Value;
            dt = dt.AddDays(1);
        }
        return null;
    }

    // Returns the Moon's phase angle (0°=new, 180°=full) for a given UTC DateTime.
    private static double GetPhase(DateTime utc)
    {
        double jd = TimeZoneUtils.ToJulianDay(utc);
        double T  = TimeUtils.JulianCentury(jd);
        double phase = MoonEphemeris.PhaseAngle(T);
        return ((phase % 360) + 360) % 360;
    }
}
