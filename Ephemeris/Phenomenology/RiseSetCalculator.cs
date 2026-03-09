// Updated: 2026-03-09
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Planetology;

namespace Ephemeris.Phenomenology;

/// <summary>
/// Calculates rise, set, and transit times for any celestial body using the Meeus Ch. 15 algorithm.
/// Rise/set is defined as the moment the geometric centre crosses the horizon at standard altitude,
/// corrected for atmospheric refraction (≈ −0.5667° for stars/planets, −0.8333° for the Sun).
/// </summary>
public static class RiseSetCalculator
{
    /// <summary>Standard altitude for stars and planets (geometric horizon + refraction).</summary>
    public const double StarStandardAltitude = -0.5667;

    /// <summary>Standard altitude for the Sun (accounts for solar disk radius + refraction).</summary>
    public const double SunStandardAltitude = -0.8333;

    /// <summary>Standard altitude for the Moon (approximate; accurate formula uses horizontal parallax).</summary>
    public const double MoonStandardAltitude = 0.125;

    /// <summary>
    /// Encapsulates rise, transit, and set UTC times for a body on a given date.
    /// </summary>
    /// <param name="Rise">UTC time of rising, or <c>null</c> if the body is circumpolar or never rises.</param>
    /// <param name="Transit">UTC time of upper transit.</param>
    /// <param name="Set">UTC time of setting, or <c>null</c> if the body is circumpolar or never sets.</param>
    public record struct RiseTransitSet(DateTime? Rise, DateTime Transit, DateTime? Set);

    /// <summary>
    /// Calculates rise, transit, and set times for a celestial body on a given date.
    /// The RA/Dec provider delegate is called with a Julian Day and returns (RA, Dec) in degrees.
    /// </summary>
    /// <param name="date">The calendar date (UTC) for which to calculate events.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <param name="raDecProvider">
    ///   Function returning <see cref="EquatorialCoordinates"/> (RA and Dec in degrees) for any Julian Day.
    /// </param>
    /// <param name="standardAltitude">
    ///   The body's standard altitude at rise/set in degrees. Use <see cref="SunStandardAltitude"/>,
    ///   <see cref="MoonStandardAltitude"/>, or <see cref="StarStandardAltitude"/>.
    /// </param>
    /// <returns>A <see cref="RiseTransitSet"/> for the requested date.</returns>
    public static RiseTransitSet Calculate(
        DateTime date,
        double longitude,
        double latitude,
        Func<double, EquatorialCoordinates> raDecProvider,
        double standardAltitude = StarStandardAltitude)
    {
        double jd0 = TimeZoneUtils.ToJulianDay(DateTime.SpecifyKind(date.Date, DateTimeKind.Utc));

        // Coordinates at JD0 − 1, JD0, JD0 + 1
        var (ra1, dec1) = raDecProvider(jd0 - 1);
        var (ra2, dec2) = raDecProvider(jd0);
        var (ra3, dec3) = raDecProvider(jd0 + 1);

        // GMST at 0h UT (degrees)
        double theta0 = TimeUtils.NormalizeDegrees(TimeUtils.GMST(jd0));

        double latRad = TimeUtils.ToRadians(latitude);
        double dec2Rad = TimeUtils.ToRadians(dec2);
        double h0Rad = TimeUtils.ToRadians(standardAltitude);

        // Cosine of the hour angle at rise/set
        double cosH0 = (Math.Sin(h0Rad) - (Math.Sin(latRad) * Math.Sin(dec2Rad)))
                     / (Math.Cos(latRad) * Math.Cos(dec2Rad));

        bool circumpolar = cosH0 < -1.0;
        bool neverRises  = cosH0 >  1.0;

        double H0 = Math.Acos(Math.Clamp(cosH0, -1.0, 1.0));
        double H0deg = TimeUtils.ToDegrees(H0);

        double ra2h = ra2 / 15.0;   // RA in hours
        double lonH = longitude / 15.0; // longitude in hours

        // Approximate sidereal fraction m = transit fraction [0, 1)
        double mTransit = TimeUtils.NormalizeDegrees(ra2 - longitude - theta0) / 360.0;
        double mRise    = mTransit - (H0deg / 360.0);
        double mSet     = mTransit + (H0deg / 360.0);

        // Iteratively correct each event (3 iterations)
        double correctedTransit = CorrectTransit(mTransit, theta0, longitude, jd0, ra1, ra2, ra3, latitude, dec1, dec2, dec3, standardAltitude, isTransit: true);
        DateTime transitUtc = jd0.JdToDateTime().AddHours(correctedTransit * 24.0);

        DateTime? riseUtc = null;
        DateTime? setUtc  = null;

        if (!circumpolar && !neverRises)
        {
            double correctedRise = CorrectEvent(mRise, theta0, longitude, jd0, ra1, ra2, ra3, latitude, dec1, dec2, dec3, standardAltitude);
            double correctedSet  = CorrectEvent(mSet,  theta0, longitude, jd0, ra1, ra2, ra3, latitude, dec1, dec2, dec3, standardAltitude);
            riseUtc = jd0.JdToDateTime().AddHours(correctedRise * 24.0);
            setUtc  = jd0.JdToDateTime().AddHours(correctedSet  * 24.0);
        }

        return new RiseTransitSet(riseUtc, transitUtc, setUtc);
    }

    /// <summary>
    /// Calculates rise, transit, and set times for the Sun on a given date.
    /// </summary>
    /// <param name="date">The calendar date (UTC).</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>Rise, transit, and set times (UTC) for the Sun.</returns>
    public static RiseTransitSet Sun(DateTime date, double longitude, double latitude)
    {
        return Calculate(date, longitude, latitude, jd =>
        {
            double T = (jd - 2451545.0) / 36525.0;
            (double ra, double dec, double _) = Heliology.SunEphemeris.ApparentEquatorialCoordinates(T);
            return new EquatorialCoordinates(ra, dec);
        }, SunStandardAltitude);
    }

    /// <summary>
    /// Calculates rise, transit, and set times for the Moon on a given date.
    /// </summary>
    /// <param name="date">The calendar date (UTC).</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>Rise, transit, and set times (UTC) for the Moon.</returns>
    public static RiseTransitSet Moon(DateTime date, double longitude, double latitude)
    {
        return Calculate(date, longitude, latitude, jd =>
        {
            double T = (jd - 2451545.0) / 36525.0;
            (double ra, double dec, double _) = Selenography.MoonEphemeris.GeocentricEquatorialCoordinates(T);
            return new EquatorialCoordinates(ra, dec);
        }, MoonStandardAltitude);
    }

    /// <summary>
    /// Calculates rise, transit, and set times for a named planet on a given date.
    /// </summary>
    /// <param name="planet">Planet name (e.g., "Mars", "Jupiter").</param>
    /// <param name="date">The calendar date (UTC).</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>Rise, transit, and set times (UTC) for the named planet.</returns>
    public static RiseTransitSet Planet(string planet, DateTime date, double longitude, double latitude)
    {
        return Calculate(date, longitude, latitude, jd =>
        {
            double T = (jd - 2451545.0) / 36525.0;
            return PlanetRaDec(planet, T);
        }, StarStandardAltitude);
    }

    private static EquatorialCoordinates PlanetRaDec(string planet, double T)
    {
        OrbitalElements elements = planet.ToLowerInvariant() switch
        {
            "mercury" => new OrbitalElements(48.3313 + (3.24587E-5 * T), 7.0047 + (5.00E-8 * T), 29.1241 + (1.01444E-5 * T),
                          0.387098, 0.205635 + (5.59E-10 * T), 168.6562 + (4.0923344368 * T * 36525)),
            "venus"   => new OrbitalElements(76.6799 + (2.46590E-5 * T), 3.3946 + (2.75E-8 * T), 54.8910 + (1.38374E-5 * T),
                          0.723330, 0.006773 - (1.302E-9 * T), 48.0052 + (1.6021302244 * T * 36525)),
            "mars"    => new OrbitalElements(49.5574 + (2.11081E-5 * T), 1.8497 - (1.78E-8 * T), 286.5016 + (2.92961E-5 * T),
                          1.523688, 0.093405 + (2.516E-9 * T), 18.6021 + (0.5240207766 * T * 36525)),
            "jupiter" => new OrbitalElements(100.4542 + (2.76854E-5 * T), 1.3030 - (1.557E-7 * T), 273.8777 + (1.64505E-5 * T),
                          5.20256, 0.048498 + (4.469E-9 * T), 19.8950 + (0.0830853001 * T * 36525)),
            "saturn"  => new OrbitalElements(113.6634 + (2.38980E-5 * T), 2.4886 - (1.081E-7 * T), 339.3939 + (2.97661E-5 * T),
                          9.55475, 0.055546 - (9.499E-9 * T), 316.9670 + (0.0334442282 * T * 36525)),
            "uranus"  => new OrbitalElements(74.0005 + (1.3978E-5 * T), 0.7733 + (1.9E-8 * T), 96.6612 + (3.0565E-5 * T),
                          19.18171, 0.047318 + (7.45E-9 * T), 142.5905 + (0.011725806 * T * 36525)),
            "neptune" => new OrbitalElements(131.7806 + (3.0173E-5 * T), 1.7700 - (2.55E-7 * T), 272.8461 - (6.027E-6 * T),
                          30.05826, 0.008606 + (2.15E-9 * T), 260.2471 + (0.005995147 * T * 36525)),
            "pluto"   => new OrbitalElements(110.30347, 17.14175, 113.76329, 39.482, 0.2488, 14.53 + (0.00396 * T * 36525)),
            _ => throw new ArgumentException($"Unknown planet: {planet}", nameof(planet))
        };
        return Planetology.PlanetEphemeris.SimplifiedPlanetPosition(T, elements);
    }

    // --- Private helpers ---

    private static double CorrectEvent(
        double m, double theta0, double longitude,
        double jd0,
        double ra1, double ra2, double ra3,
        double latitude, double dec1, double dec2, double dec3,
        double h0)
    {
        m = NormalizeFraction(m);
        for (int i = 0; i < 3; i++)
        {
            double theta = TimeUtils.NormalizeDegrees(theta0 + (360.985647 * m));
            double n = m + (jd0 - jd0);   // n = 0 offset; Meeus uses (JD0−2451545)/... here
            double ra  = InterpolateDelta(ra1, ra2, ra3, m);
            double dec = InterpolateDelta(dec1, dec2, dec3, m);
            double H   = TimeUtils.NormalizeDegrees(theta - longitude - ra); // local hour angle (deg)
            if (H > 180) H -= 360;
            double latRad = TimeUtils.ToRadians(latitude);
            double decRad = TimeUtils.ToRadians(dec);
            double hRad   = TimeUtils.ToRadians(H);
            double altitude = TimeUtils.ToDegrees(Math.Asin((Math.Sin(latRad) * Math.Sin(decRad)) + (Math.Cos(latRad) * Math.Cos(decRad) * Math.Cos(hRad))));
            double dm = (altitude - h0) / (360.0 * Math.Cos(decRad) * Math.Cos(latRad) * Math.Sin(hRad)); // altitude correction (fraction of day)
            m = NormalizeFraction(m + dm);
        }
        return m;
    }

    private static double CorrectTransit(
        double m, double theta0, double longitude,
        double jd0,
        double ra1, double ra2, double ra3,
        double latitude, double dec1, double dec2, double dec3,
        double h0, bool isTransit)
    {
        m = NormalizeFraction(m);
        for (int i = 0; i < 3; i++)
        {
            double theta = TimeUtils.NormalizeDegrees(theta0 + (360.985647 * m));
            double ra  = InterpolateDelta(ra1, ra2, ra3, m);
            double H   = TimeUtils.NormalizeDegrees(theta - longitude - ra); // local hour angle (deg)
            if (H > 180) H -= 360;
            m = NormalizeFraction(m - (H / 360.0));
        }
        return m;
    }

    private static double InterpolateDelta(double y1, double y2, double y3, double n)
    {
        double a = y2 - y1; // first finite difference
        double b = y3 - y2; // second finite difference
        // Handle RA wrap-around
        if (Math.Abs(a) > 180) a = a > 0 ? a - 360 : a + 360;
        if (Math.Abs(b) > 180) b = b > 0 ? b - 360 : b + 360;
        double c = b - a; // second-order difference
        return y2 + ((n * (a + b + (n * c))) / 2.0);
    }

    private static double NormalizeFraction(double m)
    {
        while (m < 0) m += 1;
        while (m >= 1) m -= 1;
        return m;
    }
}

/// <summary>Extension helpers for Julian Day ↔ DateTime conversions used internally.</summary>
file static class JdExtensions
{
    internal static DateTime JdToDateTime(this double jd)
    {
        double z = Math.Floor(jd + 0.5);
        double f = jd + 0.5 - z;
        double a = z >= 2299161
            ? Math.Floor((z - 1867216.25) / 36524.25)
            : z;
        if (z >= 2299161)
            a = z + 1 + a - Math.Floor(a / 4);
        double b = a + 1524;
        double c = Math.Floor((b - 122.1) / 365.25);
        double d = Math.Floor(365.25 * c);
        double e = Math.Floor((b - d) / 30.6001);
        int day   = (int)(b - d - Math.Floor(30.6001 * e));
        int month = e < 14 ? (int)e - 1 : (int)e - 13;
        int year  = month > 2 ? (int)c - 4716 : (int)c - 4715;
        double fracDay = f;
        int hour = (int)(fracDay * 24);
        int min  = (int)((fracDay * 24 - hour) * 60);
        int sec  = (int)(((fracDay * 24 - hour) * 60 - min) * 60);
        return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc);
    }
}
