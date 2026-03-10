// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Heliology;
using Ephemeris.Planetology;

namespace Ephemeris.Phenomenology;

/// <summary>
/// Predicts planetary events for outer planets: oppositions, superior conjunctions, and quadratures.
/// Uses simplified Kepler orbital elements (Meeus Ch. 33) to scan for the event via day-stepping
/// with linear interpolation for sub-day precision.
/// </summary>
/// <remarks>
/// Accuracy is limited by the simplified planetary model (~1°). Results should be accurate
/// to within 1–2 days for Jupiter/Saturn and within a few hours for Mars.
/// </remarks>
public static class PlanetaryEventCalculator
{
    private const double StepDays = 0.5;
    private const int MaxScanDays = 750; // slightly more than Saturn's 378-day synodic period

    /// <summary>
    /// Finds the next opposition of an outer planet (planet opposite the Sun as seen from Earth).
    /// Applicable planets: Mars, Jupiter, Saturn, Uranus, Neptune.
    /// </summary>
    /// <param name="planet">Planet name (case-insensitive). Mars through Neptune.</param>
    /// <param name="after">Search start date (UTC).</param>
    /// <returns>Approximate UTC date-time of the next opposition, or <see langword="null"/> if none found within 2 years.</returns>
    public static DateTime? NextOpposition(string planet, DateTime after)
        => FindElongationCrossing(planet, after, crossingType: EventType.Opposition);

    /// <summary>
    /// Finds the next superior conjunction of an outer planet (planet behind the Sun as seen from Earth).
    /// Applicable planets: Mars, Jupiter, Saturn, Uranus, Neptune.
    /// </summary>
    /// <param name="planet">Planet name (case-insensitive). Mars through Neptune.</param>
    /// <param name="after">Search start date (UTC).</param>
    /// <returns>Approximate UTC date-time of the next superior conjunction, or <see langword="null"/> if none found within 2 years.</returns>
    public static DateTime? NextConjunction(string planet, DateTime after)
        => FindElongationCrossing(planet, after, crossingType: EventType.Conjunction);

    /// <summary>
    /// Finds the next eastern quadrature of an outer planet (planet 90° east of the Sun — highest in evening sky).
    /// Applicable planets: Mars, Jupiter, Saturn, Uranus, Neptune.
    /// </summary>
    /// <param name="planet">Planet name (case-insensitive).</param>
    /// <param name="after">Search start date (UTC).</param>
    /// <returns>Approximate UTC date-time of the next eastern quadrature, or <see langword="null"/> if none found within 2 years.</returns>
    public static DateTime? NextEastQuadrature(string planet, DateTime after)
        => FindElongationCrossing(planet, after, crossingType: EventType.EastQuadrature);

    /// <summary>
    /// Finds the next western quadrature of an outer planet (planet 90° west of the Sun — highest in morning sky).
    /// Applicable planets: Mars, Jupiter, Saturn, Uranus, Neptune.
    /// </summary>
    /// <param name="planet">Planet name (case-insensitive).</param>
    /// <param name="after">Search start date (UTC).</param>
    /// <returns>Approximate UTC date-time of the next western quadrature, or <see langword="null"/> if none found within 2 years.</returns>
    public static DateTime? NextWestQuadrature(string planet, DateTime after)
        => FindElongationCrossing(planet, after, crossingType: EventType.WestQuadrature);

    private enum EventType { Opposition, Conjunction, EastQuadrature, WestQuadrature }

    /// <summary>
    /// Scans forward from <paramref name="after"/> in half-day steps, looking for a sign
    /// change in the signed elongation that corresponds to the requested <paramref name="crossingType"/>.
    /// </summary>
    /// <param name="planet">Planet name (case-insensitive).</param>
    /// <param name="after">Search start (UTC).</param>
    /// <param name="crossingType">The event geometry to detect.</param>
    /// <returns>Interpolated UTC of the event, or <see langword="null"/> if not found within the scan window.</returns>
    /// <remarks>
    /// Signed elongation ε ∈ (−180, +180] is the angular distance from the Sun, positive east:
    /// <list type="bullet">
    ///   <item><b>Opposition</b>: ε wraps from ≈ −180 to ≈ +180 (continuous crossing through ±180).</item>
    ///   <item><b>Conjunction</b>: ε crosses through 0 going from positive to negative (or vice versa near 0).</item>
    ///   <item><b>East quadrature</b>: ε decreases through +90°.</item>
    ///   <item><b>West quadrature</b>: ε decreases through −90°.</item>
    /// </list>
    /// Sub-step precision is obtained by linear interpolation between the bracketing half-day samples:
    /// <c>fraction = (target − prev) / (curr − prev)</c>.
    /// </remarks>
    private static DateTime? FindElongationCrossing(string planet, DateTime after, EventType crossingType)
    {
        double jd0 = TimeUtils.JulianDay(after.Year, after.Month, after.Day, 12.0);
        double prev = SignedElongation(planet, jd0);

        for (double d = StepDays; d <= MaxScanDays; d += StepDays)
        {
            double jd = jd0 + d;
            double curr = SignedElongation(planet, jd);

            double fraction = crossingType switch
            {
                EventType.Opposition when prev < 0 && curr > 0 && Math.Abs(prev) > 90 && Math.Abs(curr) > 90
                    // Signed elongation wrapped from ~-180 to ~+180; interpolate through -180
                    => (-180.0 - prev) / ((curr - 360.0) - prev),

                EventType.Conjunction when prev > 0 && curr < 0 && Math.Abs(prev) < 90 && Math.Abs(curr) < 90
                    // Signed elongation crossed 0 (going negative)
                    => prev / (prev - curr),

                EventType.EastQuadrature when prev > 90 && curr < 90
                    // Signed elongation decreased through +90°
                    => (prev - 90.0) / (prev - curr),

                EventType.WestQuadrature when prev > -90 && curr < -90
                    // Signed elongation decreased through −90° (planet moving from -80° to -100°)
                    => (prev - (-90.0)) / (prev - curr),

                _ => double.NaN,
            };

            if (!double.IsNaN(fraction))
            {
                fraction = Math.Clamp(fraction, 0.0, 1.0);
                double jdEvent = jd - StepDays + (fraction * StepDays);
                return TimeZoneUtils.FromJulianDay(jdEvent);
            }

            prev = curr;
        }

        return null;
    }

    /// <summary>
    /// Computes the signed elongation of a planet from the Sun in degrees.
    /// Positive = east of Sun (evening sky), negative = west of Sun (morning sky).
    /// Range is (-180, +180]. Uses proper geocentric ecliptic longitude for inner planets.
    /// </summary>
    internal static double SignedElongation(string planet, double jd)
    {
        double T = TimeUtils.JulianCentury(jd);
        var (xg, yg, _, _, _) = GeocentricXyz(planet, T);

        // Sun's geocentric ecliptic longitude (direction from Earth toward Sun)
        var (sunLon, _) = SunEphemeris.HeliocentricLongitude(T);

        // Planet geocentric ecliptic longitude
        double planetLon = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(yg, xg)));
        double diff = planetLon - sunLon;
        return ((diff + 180.0) % 360.0 + 360.0) % 360.0 - 180.0;
    }

    /// <summary>
    /// Computes the geocentric ecliptic XYZ position of a planet relative to Earth,
    /// along with the planet's heliocentric distance and Earth–Sun distance.
    /// </summary>
    internal static (double xg, double yg, double zg, double rPlanet, double rEarth) GeocentricXyz(string planet, double T)
    {
        OrbitalElements el = GetOrbitalElements(planet, T);

        double N  = el.LongitudeAscendingNode;
        double inc = el.Inclination;
        double w  = el.ArgumentOfPerihelion;
        double a  = el.SemiMajorAxisAu;
        double e  = el.Eccentricity;
        double M  = TimeUtils.NormalizeDegrees(el.MeanAnomaly);
        double Erad = PlanetEphemeris.SolveKepler(TimeUtils.ToRadians(M), e);

        double xv = Math.Cos(Erad) - e;
        double yv = Math.Sqrt(1.0 - (e * e)) * Math.Sin(Erad);
        double v  = TimeUtils.ToDegrees(Math.Atan2(yv, xv));
        double r  = a * Math.Sqrt((xv * xv) + (yv * yv)); // distance in AU (a scales from normalized orbit)

        double vwRad  = TimeUtils.ToRadians(v + w);
        double NRad   = TimeUtils.ToRadians(N);
        double incRad = TimeUtils.ToRadians(inc);

        double xh = r * ((Math.Cos(NRad) * Math.Cos(vwRad)) - (Math.Sin(NRad) * Math.Sin(vwRad) * Math.Cos(incRad)));
        double yh = r * ((Math.Sin(NRad) * Math.Cos(vwRad)) + (Math.Cos(NRad) * Math.Sin(vwRad) * Math.Cos(incRad)));
        double zh = r * (Math.Sin(vwRad) * Math.Sin(incRad));

        // Earth's heliocentric ecliptic position (opposite to Sun's geocentric direction)
        var (sunLon, sunR) = SunEphemeris.HeliocentricLongitude(T);
        double earthLonRad = TimeUtils.ToRadians(sunLon + 180.0);
        double xe = sunR * Math.Cos(earthLonRad);
        double ye = sunR * Math.Sin(earthLonRad);

        return (xh - xe, yh - ye, zh, r, sunR);
    }

    internal static OrbitalElements GetOrbitalElements(string planet, double T) =>
        planet.ToLowerInvariant() switch
        {
            "mercury" => new OrbitalElements(48.3313 + (3.24587E-5 * T), 7.0047 + (5.00E-8 * T), 29.1241 + (1.01444E-5 * T),
                          0.387098, 0.205635 + (5.59E-10 * T), 168.6562 + (4.0923344368 * T * 36525)),
            "venus" => new OrbitalElements(76.6799 + (2.46590E-5 * T), 3.3946 + (2.75E-8 * T), 54.8910 + (1.38374E-5 * T),
                        0.723330, 0.006773 - (1.302E-9 * T), 48.0052 + (1.6021302244 * T * 36525)),
            "mars" => new OrbitalElements(49.5574 + (2.11081E-5 * T), 1.8497 - (1.78E-8 * T), 286.5016 + (2.92961E-5 * T),
                       1.523688, 0.093405 + (2.516E-9 * T), 18.6021 + (0.5240207766 * T * 36525)),
            "jupiter" => new OrbitalElements(100.4542 + (2.76854E-5 * T), 1.3030 - (1.557E-7 * T), 273.8777 + (1.64505E-5 * T),
                         5.20256, 0.048498 + (4.469E-9 * T), 19.8950 + (0.0830853001 * T * 36525)),
            "saturn" => new OrbitalElements(113.6634 + (2.38980E-5 * T), 2.4886 - (1.081E-7 * T), 339.3939 + (2.97661E-5 * T),
                         9.55475, 0.055546 - (9.499E-9 * T), 316.9670 + (0.0334442282 * T * 36525)),
            "uranus" => new OrbitalElements(74.0005 + (1.3978E-5 * T), 0.7733 + (1.9E-8 * T), 96.6612 + (3.0565E-5 * T),
                         19.18171, 0.047318 + (7.45E-9 * T), 142.5905 + (0.011725806 * T * 36525)),
            "neptune" => new OrbitalElements(131.7806 + (3.0173E-5 * T), 1.7700 - (2.55E-7 * T), 272.8461 - (6.027E-6 * T),
                          30.05826, 0.008606 + (2.15E-9 * T), 260.2471 + (0.005995147 * T * 36525)),
            "pluto" => new OrbitalElements(110.30347, 17.14175, 113.76329, 39.482, 0.2488, 14.53 + (0.00396 * T * 36525)),
            _ => throw new ArgumentException($"Unknown planet: {planet}", nameof(planet))
        };
}
