// Updated: 2026-03-10
using Ephemeris.Chronology;

namespace Ephemeris.Phenomenology;

/// <summary>
/// Calculates the times of equinoxes and solstices for any year using Meeus Ch. 27 algorithm.
/// </summary>
public static class SeasonCalculator
{
    /// <summary>Identifies the four astronomical seasons / cardinal solar events.</summary>
    public enum Season
    {
        /// <summary>March (vernal/spring) equinox — Sun crosses celestial equator northward.</summary>
        SpringEquinox,
        /// <summary>June solstice — Sun reaches maximum northerly declination.</summary>
        SummerSolstice,
        /// <summary>September (autumnal) equinox — Sun crosses celestial equator southward.</summary>
        AutumnEquinox,
        /// <summary>December solstice — Sun reaches maximum southerly declination.</summary>
        WinterSolstice,
    }

    // Mean JDE polynomial coefficients (Meeus Table 27.a — years 1000–3000)
    // JDE0 = A + B*Y + C*Y^2 + D*Y^3 + E*Y^4, where Y = (year - 2000) / 1000
    private static readonly (double A, double B, double C, double D, double E)[] s_meanJde =
    [
        // SpringEquinox
        (2451623.80984, 365242.37404, 0.05169, -0.00411, -0.00057),
        // SummerSolstice
        (2451716.56767, 365241.62603, 0.00325,  0.00888, -0.00030),
        // AutumnEquinox
        (2451810.21715, 365242.01767, -0.11575,  0.00337,  0.00078),
        // WinterSolstice
        (2451900.05952, 365242.74049, -0.06223, -0.00823,  0.00032),
    ];

    // Periodic correction terms (Meeus Table 27.b): A, B (JDE), W (amplitude days)
    private static readonly (double A, double B, double W)[] s_corrections =
    [
        (485, 324.96,   1934.136),
        (203, 337.23,  32964.467),
        (199, 342.08,     20.186),
        (182,  27.85, 445267.112),
        (156,  73.14,  45036.886),
        (136, 171.52,  22518.443),
        ( 77, 222.54,  65928.934),
        ( 74, 296.72,   3034.906),
        ( 70, 243.58,   9037.513),
        ( 58, 119.81,  33718.147),
        ( 52, 297.17,    150.678),
        ( 50,  21.02,   2281.226),
        ( 45, 247.54,  29929.562),
        ( 44, 325.15,  31555.956),
        ( 29,  60.93,   4443.417),
        ( 18, 155.12,  67555.328),
        ( 17, 288.79,   4562.452),
        ( 16, 198.04,  62894.029),
        ( 14, 199.76,  31557.381),
        ( 12,  95.39,  14577.848),
        ( 10, 287.11,  31555.956),
        (  8, 320.81,  29929.562),
        (  6, 227.73,  31436.921),
        (  5,  15.45,   2237.227),
    ];

    /// <summary>
    /// Calculates the UTC DateTime of a given equinox or solstice for a specified year.
    /// </summary>
    /// <param name="year">The year (positive for AD, may be fractional).</param>
    /// <param name="season">Which equinox or solstice to calculate.</param>
    /// <returns>The UTC DateTime of the event, accurate to approximately 1 minute.</returns>
    public static DateTime Calculate(int year, Season season)
    {
        double Y = (year - 2000.0) / 1000.0; // millennia since J2000.0
        var (A, B, C, D, E) = s_meanJde[(int)season];
        double jde0 = A + (B * Y) + (C * Y * Y) + (D * Y * Y * Y) + (E * Y * Y * Y * Y);

        double T = (jde0 - 2451545.0) / 36525.0;
        double W = 35999.373 * T - 2.47; // Sun's mean anomaly term (deg)
        double deltaLambda = 1.0 + (0.0334 * Math.Cos(TimeUtils.ToRadians(W)))
                                  + (0.0007 * Math.Cos(TimeUtils.ToRadians(2 * W))); // correction factor for mean JDE

        double S = s_corrections.Sum(c =>
            c.A * Math.Cos(TimeUtils.ToRadians(c.B + (c.W * T)))); // sum of periodic correction terms

        double jde = jde0 + (0.00001 * S / deltaLambda);

        // Subtract ΔT to convert JDE (Terrestrial Time) → UTC
        double approxYear = year + ((int)season + 0.5) / 4.0;
        double deltaTdays = TimeUtils.DeltaT(approxYear) / 86400.0;
        return TimeZoneUtils.FromJulianDay(jde - deltaTdays);
    }

    /// <summary>
    /// Returns the UTC DateTime of the next equinox or solstice of the given type occurring after <paramref name="after"/>.
    /// </summary>
    /// <param name="season">Which equinox or solstice to find.</param>
    /// <param name="after">Find the first occurrence strictly after this UTC DateTime.</param>
    /// <returns>The UTC DateTime of the next occurrence.</returns>
    public static DateTime Next(Season season, DateTime after)
    {
        int year = after.Year;
        DateTime result = Calculate(year, season);
        if (result <= after)
            result = Calculate(year + 1, season);
        return result;
    }

    /// <summary>
    /// Returns the UTC DateTime of the next spring equinox after <paramref name="after"/>.
    /// </summary>
    public static DateTime NextSpringEquinox(DateTime after) => Next(Season.SpringEquinox, after);

    /// <summary>
    /// Returns the UTC DateTime of the next summer solstice after <paramref name="after"/>.
    /// </summary>
    public static DateTime NextSummerSolstice(DateTime after) => Next(Season.SummerSolstice, after);

    /// <summary>
    /// Returns the UTC DateTime of the next autumnal equinox after <paramref name="after"/>.
    /// </summary>
    public static DateTime NextAutumnEquinox(DateTime after) => Next(Season.AutumnEquinox, after);

    /// <summary>
    /// Returns the UTC DateTime of the next winter solstice after <paramref name="after"/>.
    /// </summary>
    public static DateTime NextWinterSolstice(DateTime after) => Next(Season.WinterSolstice, after);

}
