// Updated: 2026-03-09
using Ephemeris.Chronology;
using Ephemeris.Geodesy;

namespace Ephemeris.Selenography;

/// <summary>
/// Calculates the Moon's geocentric coordinates, phase, and illumination using the Meeus Ch. 47 algorithm.
/// Includes 60 longitude terms, 30 latitude terms, and 25 distance terms for sub-arcminute accuracy.
/// </summary>
public static class MoonEphemeris
{
    // Meeus Table 47.A: Longitude (Σl) and Distance (Σr) terms
    // Columns: D, M, Mp, F, El_coeff (0.000001°), Er_coeff (0.1 km)
    private static readonly (int D, int M, int Mp, int F, double El, double Er)[] s_lonDist =
    [
        ( 0,  0,  1,  0,  6288774, -20905355),
        ( 2,  0, -1,  0,  1274027,  -3699111),
        ( 2,  0,  0,  0,   658314,  -2955968),
        ( 0,  0,  2,  0,   213618,   -569925),
        ( 0,  1,  0,  0,  -185116,     48888),
        ( 0,  0,  0,  2,  -114332,     -3149),
        ( 2,  0, -2,  0,    58793,    246158),
        ( 2, -1, -1,  0,    57066,   -152138),
        ( 2,  0,  1,  0,    53322,   -170733),
        ( 2, -1,  0,  0,    45758,   -204586),
        ( 0,  1, -1,  0,   -40923,   -129620),
        ( 1,  0,  0,  0,   -34720,    108743),
        ( 0,  1,  1,  0,   -30383,    104755),
        ( 2,  0,  0, -2,    15327,     10321),
        ( 0,  0,  1,  2,   -12528,         0),
        ( 0,  0,  1, -2,    10980,     79661),
        ( 4,  0, -1,  0,    10675,    -34782),
        ( 0,  0,  3,  0,    10034,    -23210),
        ( 4,  0, -2,  0,     8548,    -21636),
        ( 2,  1, -1,  0,    -7888,     24208),
        ( 2,  1,  0,  0,    -6766,     30824),
        ( 1,  0, -1,  0,    -5163,     -8379),
        ( 1,  1,  0,  0,     4987,    -16675),
        ( 2, -1,  1,  0,     4036,    -12831),
        ( 2,  0,  2,  0,     3994,    -10445),
        ( 4,  0,  0,  0,     3861,    -11650),
        ( 2,  0, -3,  0,     3665,     14403),
        ( 0,  1, -2,  0,    -2689,     -7003),
        ( 2,  0, -1,  2,    -2602,         0),
        ( 2, -1, -2,  0,     2390,     10056),
        ( 1,  0,  1,  0,    -2348,      6322),
        ( 2, -2,  0,  0,     2236,     -9884),
        ( 0,  1,  2,  0,    -2120,      5751),
        ( 0,  2,  0,  0,    -2069,         0),
        ( 2, -2, -1,  0,     2048,     -4950),
        ( 2,  0,  1, -2,    -1773,      4130),
        ( 2,  0,  0,  2,    -1595,         0),
        ( 4, -1, -1,  0,     1215,     -3958),
        ( 0,  0,  2,  2,    -1110,         0),
        ( 3,  0, -1,  0,     -892,      3258),
        ( 2,  1,  1,  0,     -810,      2616),
        ( 4, -1, -2,  0,      759,     -1897),
        ( 0,  2, -1,  0,     -713,     -2117),
        ( 2,  2, -1,  0,     -700,      2354),
        ( 2,  1, -2,  0,      691,         0),
        ( 2, -1,  0, -2,      596,         0),
        ( 4,  0,  1,  0,      549,     -1423),
        ( 0,  0,  4,  0,      537,     -1117),
        ( 4, -1,  0,  0,      520,     -1571),
        ( 1,  0, -2,  0,     -487,     -1739),
        ( 2,  1,  0, -2,     -399,         0),
        ( 0,  0,  2, -2,     -381,     -4421),
        ( 1,  1,  1,  0,      351,         0),
        ( 3,  0, -2,  0,     -340,         0),
        ( 4,  0, -3,  0,      330,         0),
        ( 2, -1,  2,  0,      327,         0),
        ( 0,  2,  1,  0,     -323,      1165),
        ( 1,  1, -1,  0,      299,         0),
        ( 2,  0,  3,  0,      294,         0),
        ( 2,  0, -1, -2,        0,      8752),
    ];

    // Meeus Table 47.B: Latitude (Σb) terms
    // Columns: D, M, Mp, F, Eb_coeff (0.000001°)
    private static readonly (int D, int M, int Mp, int F, double Eb)[] s_lat =
    [
        ( 0,  0,  0,  1,  5128122),
        ( 0,  0,  1,  1,   280602),
        ( 0,  0,  1, -1,   277693),
        ( 2,  0,  0, -1,   173237),
        ( 2,  0, -1,  1,    55413),
        ( 2,  0, -1, -1,    46271),
        ( 2,  0,  0,  1,    32573),
        ( 0,  0,  2,  1,    17198),
        ( 2,  0,  1, -1,     9266),
        ( 0,  0,  2, -1,     8822),
        ( 2, -1,  0, -1,     8216),
        ( 2,  0, -2, -1,     4324),
        ( 2,  0,  1,  1,     4200),
        ( 2,  1,  0, -1,    -3359),
        ( 2, -1, -1,  1,     2463),
        ( 2, -1,  0,  1,     2211),
        ( 2, -1, -1, -1,     2065),
        ( 0,  1, -1, -1,    -1870),
        ( 4,  0, -1, -1,     1828),
        ( 0,  1,  0,  1,    -1794),
        ( 0,  0,  0,  3,    -1749),
        ( 0,  1, -1,  1,    -1565),
        ( 1,  0,  0,  1,    -1491),
        ( 0,  1,  1,  1,    -1475),
        ( 0,  1,  1, -1,    -1410),
        ( 0,  1,  0, -1,    -1344),
        ( 1,  0,  0, -1,    -1335),
        ( 0,  0,  3,  1,     1107),
        ( 4,  0,  0, -1,     1021),
        ( 4,  0, -1,  1,      833),
    ];

    /// <summary>
    /// Calculates the Moon's geocentric equatorial coordinates and distance from Earth.
    /// Uses the Meeus Ch. 47 truncated series for sub-arcminute accuracy.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>A tuple of (RA, Dec, distanceKm) where RA and Dec are in degrees, distance is in kilometres.</returns>
    public static (double RA, double Dec, double distanceKm) GeocentricEquatorialCoordinates(double T)
    {
        // Fundamental arguments (Meeus Ch. 47)
        double Lp = TimeUtils.NormalizeDegrees(218.3164477 + (481267.88123421 * T) - (0.0015786 * T * T) + (T * T * T / 538841.0) - (T * T * T * T / 65194000.0)); // L': Moon's mean longitude (deg)
        double D  = TimeUtils.NormalizeDegrees(297.8501921 + (445267.1114034 * T) - (0.0018819 * T * T) + (T * T * T / 545868.0) - (T * T * T * T / 113065000.0)); // D: Moon's mean elongation (deg)
        double M  = TimeUtils.NormalizeDegrees(357.5291092 + ( 35999.0502909 * T) - (0.0001536 * T * T) + (T * T * T / 24490000.0)); // M: Sun's mean anomaly (deg)
        double Mp = TimeUtils.NormalizeDegrees(134.9633964 + (477198.8675055 * T) + (0.0087414 * T * T) + (T * T * T / 69699.0)   - (T * T * T * T / 14712000.0)); // M': Moon's mean anomaly (deg)
        double F  = TimeUtils.NormalizeDegrees( 93.2720950 + (483202.0175233 * T) - (0.0036539 * T * T) - (T * T * T / 3526000.0) + (T * T * T * T / 863310000.0)); // F: Moon's argument of latitude (deg)

        double A1 = TimeUtils.NormalizeDegrees(119.75 + (131.849 * T)); // Venus correction term
        double A2 = TimeUtils.NormalizeDegrees( 53.09 + (479264.290 * T)); // Jupiter correction term
        double A3 = TimeUtils.NormalizeDegrees(313.45 + (481266.484 * T)); // flattening correction term

        double e  = 1.0 - (0.002516 * T) - (0.0000074 * T * T); // Earth orbital eccentricity correction
        double e2 = e * e; // e² for second-order M terms

        double DRad  = TimeUtils.ToRadians(D);
        double MRad  = TimeUtils.ToRadians(M);
        double MpRad = TimeUtils.ToRadians(Mp);
        double FRad  = TimeUtils.ToRadians(F);

        double sumL = 0.0, sumR = 0.0, sumB = 0.0;

        foreach (var (termD, termM, termMp, termF, el, er) in s_lonDist)
        {
            double arg = (termD * DRad) + (termM * MRad) + (termMp * MpRad) + (termF * FRad);
            double eFactor = Math.Abs(termM) == 1 ? e : (Math.Abs(termM) == 2 ? e2 : 1.0);
            sumL += eFactor * el * Math.Sin(arg);
            sumR += eFactor * er * Math.Cos(arg);
        }

        foreach (var (termD, termM, termMp, termF, eb) in s_lat)
        {
            double arg = (termD * DRad) + (termM * MRad) + (termMp * MpRad) + (termF * FRad);
            double eFactor = Math.Abs(termM) == 1 ? e : (Math.Abs(termM) == 2 ? e2 : 1.0);
            sumB += eFactor * eb * Math.Sin(arg);
        }

        // Additive corrections (Meeus eq. 47.6)
        sumL += 3958 * Math.Sin(TimeUtils.ToRadians(A1))
              + 1962 * Math.Sin(TimeUtils.ToRadians(Lp - F))
              +  318 * Math.Sin(TimeUtils.ToRadians(A2));
        sumB += -2235 * Math.Sin(TimeUtils.ToRadians(Lp))
              +   382 * Math.Sin(TimeUtils.ToRadians(A3))
              +   175 * Math.Sin(TimeUtils.ToRadians(A1 - F))
              +   175 * Math.Sin(TimeUtils.ToRadians(A1 + F))
              +   127 * Math.Sin(TimeUtils.ToRadians(Lp - Mp))
              -   115 * Math.Sin(TimeUtils.ToRadians(Lp + Mp));

        double moonLon = TimeUtils.NormalizeDegrees(Lp + (sumL / 1_000_000.0));
        double moonLat = sumB / 1_000_000.0;
        double dist    = 385000.56 + (sumR / 1000.0);

        double epsilon = NutationCalculator.TrueObliquity(T);
        double lonRad = TimeUtils.ToRadians(moonLon);
        double latRad = TimeUtils.ToRadians(moonLat);
        double epsRad = TimeUtils.ToRadians(epsilon);

        double x = Math.Cos(lonRad) * Math.Cos(latRad); // direction cosines
        double y = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Cos(epsRad)) - (Math.Sin(latRad) * Math.Sin(epsRad));
        double z = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Sin(epsRad)) + (Math.Sin(latRad) * Math.Cos(epsRad));

        double RA  = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(y, x)));
        double Dec = TimeUtils.ToDegrees(Math.Asin(Math.Clamp(z, -1.0, 1.0)));

        return (RA, Dec, dist);
    }

    /// <summary>
    /// Calculates the Moon's illumination fraction given its phase angle.
    /// </summary>
    /// <param name="phaseAngle">The phase angle in degrees (angle between Sun and Moon as seen from Earth).</param>
    /// <returns>Illumination fraction in [0, 1], where 0 is new moon and 1 is full moon.</returns>
    public static double Illumination(double phaseAngle)
    {
        double phaseAngleRad = TimeUtils.ToRadians(phaseAngle);
        return (1 + Math.Cos(phaseAngleRad)) / 2;
    }

    /// <summary>
    /// Calculates the Moon's phase angle (the geocentric elongation angle between Sun and Moon).
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>The phase angle in degrees.</returns>
    public static double PhaseAngle(double T)
    {
        double D  = TimeUtils.NormalizeDegrees(297.8501921 + (445267.1114034 * T)); // Moon's mean elongation (deg)
        double M  = TimeUtils.NormalizeDegrees(357.5291092 + ( 35999.0502909 * T)); // Sun's mean anomaly (deg)
        double Mp = TimeUtils.NormalizeDegrees(134.9633964 + (477198.8675055 * T)); // Moon's mean anomaly (deg)
        return 180 - D - (6.289 * Math.Sin(TimeUtils.ToRadians(Mp)))
                       + (2.100 * Math.Sin(TimeUtils.ToRadians(M)))
                       - (1.274 * Math.Sin(TimeUtils.ToRadians((2 * D) - Mp)));
    }

    /// <summary>
    /// Returns the Moon's phase name based on its phase angle.
    /// </summary>
    /// <param name="phaseAngle">Phase angle in degrees [0, 360).</param>
    /// <returns>A descriptive phase name such as "Full Moon" or "Waxing Crescent".</returns>
    public static string PhaseName(double phaseAngle)
    {
        double norm = ((phaseAngle % 360) + 360) % 360;
        return norm switch
        {
            < 22.5  => "New Moon",
            < 67.5  => "Waxing Crescent",
            < 112.5 => "First Quarter",
            < 157.5 => "Waxing Gibbous",
            < 202.5 => "Full Moon",
            < 247.5 => "Waning Gibbous",
            < 292.5 => "Last Quarter",
            < 337.5 => "Waning Crescent",
            _       => "New Moon",
        };
    }

    // Inclination of Moon's equator to the ecliptic (Meeus Ch. 53)
    private const double MoonEquatorInclinationDeg = 1.5424;

    /// <summary>
    /// Calculates the optical libration of the Moon in longitude and latitude (Meeus Ch. 53).
    /// Libration is the apparent rocking of the Moon that reveals up to 59% of its surface over time.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>
    /// A tuple of (LongitudeDeg, LatitudeDeg) optical libration angles, both in degrees.
    /// Longitude is in [-8, +8]°; latitude is in [-7, +7]°.
    /// </returns>
    public static (double LongitudeDeg, double LatitudeDeg) Libration(double T)
    {
        // Recompute Ch. 47 fundamental arguments
        double Lp = TimeUtils.NormalizeDegrees(218.3164477 + (481267.88123421 * T) - (0.0015786 * T * T) + (T * T * T / 538841.0) - (T * T * T * T / 65194000.0)); // L': mean longitude
        double D  = TimeUtils.NormalizeDegrees(297.8501921 + (445267.1114034  * T) - (0.0018819 * T * T) + (T * T * T / 545868.0) - (T * T * T * T / 113065000.0)); // D: mean elongation
        double M  = TimeUtils.NormalizeDegrees(357.5291092 + ( 35999.0502909  * T) - (0.0001536 * T * T) + (T * T * T / 24490000.0)); // M: Sun's mean anomaly
        double Mp = TimeUtils.NormalizeDegrees(134.9633964 + (477198.8675055  * T) + (0.0087414 * T * T) + (T * T * T / 69699.0)   - (T * T * T * T / 14712000.0)); // M': Moon's mean anomaly
        double F  = TimeUtils.NormalizeDegrees( 93.2720950 + (483202.0175233  * T) - (0.0036539 * T * T) - (T * T * T / 3526000.0) + (T * T * T * T / 863310000.0)); // F: argument of latitude
        double Om = TimeUtils.NormalizeDegrees(125.0445479 - (1934.1362608    * T) + (0.0020691 * T * T) + (T * T * T / 450160.0)); // Ω: longitude of ascending node

        double A1 = TimeUtils.NormalizeDegrees(119.75 + (131.849 * T));
        double A3 = TimeUtils.NormalizeDegrees(313.45 + (481266.484 * T));
        double e  = 1.0 - (0.002516 * T) - (0.0000074 * T * T);
        double e2 = e * e;

        double DRad  = TimeUtils.ToRadians(D);
        double MRad  = TimeUtils.ToRadians(M);
        double MpRad = TimeUtils.ToRadians(Mp);
        double FRad  = TimeUtils.ToRadians(F);

        // Compute longitude (ΣL) and latitude (ΣB) sums using the Ch.47 tables
        double sumL = 0.0, sumB = 0.0;
        foreach (var (termD, termM, termMp, termF, el, _) in s_lonDist)
        {
            double arg = (termD * DRad) + (termM * MRad) + (termMp * MpRad) + (termF * FRad);
            double eFactor = Math.Abs(termM) == 1 ? e : (Math.Abs(termM) == 2 ? e2 : 1.0);
            sumL += eFactor * el * Math.Sin(arg);
        }

        foreach (var (termD, termM, termMp, termF, eb) in s_lat)
        {
            double arg = (termD * DRad) + (termM * MRad) + (termMp * MpRad) + (termF * FRad);
            double eFactor = Math.Abs(termM) == 1 ? e : (Math.Abs(termM) == 2 ? e2 : 1.0);
            sumB += eFactor * eb * Math.Sin(arg);
        }

        sumL += (3958 * Math.Sin(TimeUtils.ToRadians(A1)))
              + (1962 * Math.Sin(TimeUtils.ToRadians(Lp - F)))
              +  (318 * Math.Sin(TimeUtils.ToRadians(TimeUtils.NormalizeDegrees(53.09 + (479264.290 * T)))));
        sumB += (-2235 * Math.Sin(TimeUtils.ToRadians(Lp)))
              +   (382 * Math.Sin(TimeUtils.ToRadians(A3)))
              +   (175 * Math.Sin(TimeUtils.ToRadians(A1 - F)))
              +   (175 * Math.Sin(TimeUtils.ToRadians(A1 + F)))
              +   (127 * Math.Sin(TimeUtils.ToRadians(Lp - Mp)))
              -   (115 * Math.Sin(TimeUtils.ToRadians(Lp + Mp)));

        double moonLon = TimeUtils.NormalizeDegrees(Lp + (sumL / 1_000_000.0));
        double moonLat = sumB / 1_000_000.0;

        // Apply nutation to get apparent ecliptic longitude (Meeus Ch. 53)
        double deltaPsiDeg = NutationCalculator.Calculate(T).DeltaPsi;
        double lambdaApparent = moonLon + deltaPsiDeg;

        // Optical libration in longitude (l') and latitude (b') — Meeus Eq. 53.1–53.2
        double I   = MoonEquatorInclinationDeg;
        double W   = lambdaApparent - Om;
        double WRad   = TimeUtils.ToRadians(W);
        double betaRad = TimeUtils.ToRadians(moonLat);
        double IRad   = TimeUtils.ToRadians(I);

        double A = Math.Atan2(
            (Math.Sin(WRad) * Math.Cos(betaRad) * Math.Cos(IRad)) - (Math.Sin(betaRad) * Math.Sin(IRad)),
            Math.Cos(WRad) * Math.Cos(betaRad));

        double librationLon = TimeUtils.ToDegrees(A) - F; // l' in degrees

        double sinB2 = -(Math.Sin(WRad) * Math.Cos(betaRad) * Math.Sin(IRad)) - (Math.Sin(betaRad) * Math.Cos(IRad));
        double librationLat = TimeUtils.ToDegrees(Math.Asin(Math.Clamp(sinB2, -1.0, 1.0))); // b' in degrees

        // Normalize longitude libration to (-180, 180] so callers see the expected ±8° range
        librationLon = ((librationLon + 180.0) % 360.0 + 360.0) % 360.0 - 180.0;

        return (librationLon, librationLat);
    }
}
