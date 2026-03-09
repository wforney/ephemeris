namespace Ephemeris.Geodesy;

/// <summary>
/// Calculates the nutation in longitude (Δψ) and obliquity (Δε) using the IAU 1980 simplified series.
/// Nutation is the periodic wobble of Earth's rotation axis caused primarily by the Moon and Sun.
/// </summary>
public static class NutationCalculator
{
    // IAU 1980 leading terms: (l, l', F, D, Ω, ΔψCoeff(0.0001"), ΔεCoeff(0.0001"))
    // l  = mean anomaly of the Moon
    // l' = mean anomaly of the Sun
    // F  = Moon's argument of latitude
    // D  = elongation of the Moon from Sun
    // Ω  = longitude of ascending node of Moon's orbit
    private static readonly (int l, int lp, int F, int D, int Om, double psiS, double psiT, double epsS, double epsT)[] s_terms =
    [
        ( 0,  0,  0,  0,  1, -171996, -174.2,  92025,   8.9),
        (-2,  0,  0,  2,  2,  -13187,   -1.6,   5736,  -3.1),
        ( 0,  0,  0,  2,  2,   -2274,   -0.2,    977,  -0.5),
        ( 0,  0,  0,  0,  2,    2062,    0.2,   -895,   0.5),
        ( 0,  1,  0,  0,  0,    1426,   -3.4,     54,  -0.1),
        ( 0,  0,  1,  0,  0,     712,    0.1,     -7,   0.0),
        (-2,  1,  0,  2,  2,    -517,    1.2,    224,  -0.6),
        ( 0,  0,  0,  2,  1,    -386,   -0.4,    200,   0.0),
        ( 0,  0,  1,  2,  2,    -301,    0.0,    129,  -0.1),
        (-2, -1,  0,  2,  2,     217,   -0.5,    -95,   0.3),
        (-2,  0,  1,  0,  0,    -158,    0.0,      0,   0.0),
        (-2,  0,  0,  2,  1,     129,    0.1,    -70,   0.0),
        ( 0,  0, -1,  2,  2,     123,    0.0,    -53,   0.0),
        ( 2,  0,  0,  0,  0,      63,    0.0,      0,   0.0),
        ( 0,  0,  1,  0,  1,      63,    0.1,    -33,   0.0),
        ( 2,  0, -1,  2,  2,     -59,    0.0,     26,   0.0),
        ( 0,  0, -1,  0,  1,     -58,   -0.1,     32,   0.0),
        ( 0,  0,  1,  2,  1,     -51,    0.0,     27,   0.0),
        (-2,  0,  2,  0,  0,      48,    0.0,      0,   0.0),
        ( 0,  0, -2,  2,  1,      46,    0.0,    -24,   0.0),
        ( 2,  0,  0,  2,  2,     -38,    0.0,     16,   0.0),
        ( 0,  0,  2,  2,  2,     -31,    0.0,     13,   0.0),
        ( 0,  0,  2,  0,  0,      29,    0.0,      0,   0.0),
        (-2,  0,  1,  2,  2,      29,    0.0,    -12,   0.0),
        ( 0,  0,  0,  2,  0,      26,    0.0,      0,   0.0),
        (-2,  0,  0,  2,  0,     -22,    0.0,      0,   0.0),
        ( 0,  0, -1,  2,  1,      21,    0.0,    -10,   0.0),
        ( 0,  2,  0,  0,  0,      17,   -0.1,      0,   0.0),
        ( 2,  0, -1,  0,  1,      16,    0.0,     -8,   0.0),
        (-2,  2,  0,  2,  2,     -16,    0.1,      7,   0.0),
        ( 0,  1,  0,  0,  1,     -15,    0.0,      9,   0.0),
        (-2,  0,  1,  0,  1,     -13,    0.0,      7,   0.0),
        ( 0, -1,  0,  0,  1,     -12,    0.0,      6,   0.0),
        ( 0,  0,  2, -2,  0,      11,    0.0,      0,   0.0),
        ( 2,  0, -1,  2,  1,     -10,    0.0,      5,   0.0),
        ( 2,  0,  1,  2,  2,      -8,    0.0,      3,   0.0),
        ( 0,  1,  0,  2,  2,       7,    0.0,     -3,   0.0),
        (-2,  1,  1,  0,  0,      -7,    0.0,      0,   0.0),
        ( 0, -1,  0,  2,  2,      -7,    0.0,      3,   0.0),
        ( 2,  0,  0,  2,  1,      -7,    0.0,      3,   0.0),
        ( 0,  0,  1,  2,  0,       6,    0.0,      0,   0.0),
        ( 2,  0,  1,  0,  0,      -6,    0.0,      0,   0.0),
        ( 0,  0, -2,  2,  2,       6,    0.0,     -3,   0.0),
        ( 2,  0,  0,  0,  1,      -5,    0.0,      3,   0.0),
        ( 0, -1,  1,  0,  0,       5,    0.0,      0,   0.0),
        (-2, -1,  0,  2,  1,      -5,    0.0,      3,   0.0),
        (-2,  0,  0,  0,  1,      -5,    0.0,      3,   0.0),
        ( 0,  0,  2,  2,  1,      -5,    0.0,      3,   0.0),
        (-2,  0,  2,  2,  2,       4,    0.0,     -2,   0.0),
        (-2,  1,  0,  2,  1,       4,    0.0,     -2,   0.0),
    ];

    /// <summary>
    /// Calculates nutation in longitude (Δψ) and nutation in obliquity (Δε) for a given Julian century.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>
    /// A tuple of (DeltaPsi, DeltaEpsilon) in degrees.
    /// Δψ is nutation in ecliptic longitude; Δε is nutation in obliquity of the ecliptic.
    /// </returns>
    public static (double DeltaPsi, double DeltaEpsilon) Calculate(double T)
    {
        // Fundamental arguments (degrees)
        double l  = 134.96298  + (477198.867398 * T) + (0.0086972 * T * T);
        double lp = 357.52772  + ( 35999.050340 * T) - (0.0001603 * T * T);
        double F  =  93.27191  + (483202.017538 * T) - (0.0036825 * T * T);
        double D  = 297.85036  + (445267.111480 * T) - (0.0019142 * T * T);
        double Om = 125.04452  - (   1934.136261 * T) + (0.0020708 * T * T);

        double lRad  = Ephemeris.Chronology.TimeUtils.ToRadians(l);
        double lpRad = Ephemeris.Chronology.TimeUtils.ToRadians(lp);
        double FRad  = Ephemeris.Chronology.TimeUtils.ToRadians(F);
        double DRad  = Ephemeris.Chronology.TimeUtils.ToRadians(D);
        double OmRad = Ephemeris.Chronology.TimeUtils.ToRadians(Om);

        double deltaPsi = 0.0, deltaEps = 0.0;
        foreach (var term in s_terms)
        {
            double arg = (term.l * lRad) + (term.lp * lpRad) + (term.F * FRad)
                       + (term.D * DRad) + (term.Om * OmRad);
            double sinArg = Math.Sin(arg);
            double cosArg = Math.Cos(arg);

            deltaPsi += (term.psiS + (term.psiT * T)) * sinArg;
            deltaEps += (term.epsS + (term.epsT * T)) * cosArg;
        }

        // Coefficients are in units of 0.0001 arcseconds → convert to degrees
        deltaPsi /= (10000.0 * 3600.0);
        deltaEps /= (10000.0 * 3600.0);

        return (deltaPsi, deltaEps);
    }

    /// <summary>
    /// Calculates the true obliquity of the ecliptic, including nutation in obliquity.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>True obliquity of the ecliptic in degrees.</returns>
    public static double TrueObliquity(double T)
    {
        double epsilon0 = 23.439291111 - (0.013004167 * T) - (0.0000001639 * T * T) + (0.0000005036 * T * T * T);
        (_, double deltaEps) = Calculate(T);
        return epsilon0 + deltaEps;
    }
}
