// Updated: 2026-03-09
namespace Ephemeris.Import;

/// <summary>
/// Provides NAIF leap-second lookup and UTC → SPICE Ephemeris Time (ET/TDB) conversion.
/// </summary>
/// <remarks>
/// The leap-second table is embedded from the official NAIF kernel
/// <c>naif0012.tls</c> and covers 1972-Jan-01 through 2017-Jan-01 (37 s).
/// Before 1972, no leap seconds existed; after the last entry the most
/// recent value is returned until the table is updated.
/// </remarks>
public static class SpkLeapSeconds
{
    /// <summary>J2000.0 epoch expressed as UTC noon: 2000-Jan-01 12:00:00.000 UTC.</summary>
    private static readonly DateTime J2000Epoch = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>
    /// Fixed offset TT − TAI = 32.184 seconds (IAU standard).
    /// </summary>
    private const double TtMinusTai = 32.184;

    /// <summary>
    /// NAIF leap-second table: each entry is (UTC date, cumulative TAI − UTC in seconds).
    /// Sorted chronologically; binary search finds the applicable entry.
    /// </summary>
    private static readonly (DateTime Date, int LeapSeconds)[] LeapTable =
    [
        (new DateTime(1972,  1, 1, 0, 0, 0, DateTimeKind.Utc), 10),
        (new DateTime(1972,  7, 1, 0, 0, 0, DateTimeKind.Utc), 11),
        (new DateTime(1973,  1, 1, 0, 0, 0, DateTimeKind.Utc), 12),
        (new DateTime(1974,  1, 1, 0, 0, 0, DateTimeKind.Utc), 13),
        (new DateTime(1975,  1, 1, 0, 0, 0, DateTimeKind.Utc), 14),
        (new DateTime(1976,  1, 1, 0, 0, 0, DateTimeKind.Utc), 15),
        (new DateTime(1977,  1, 1, 0, 0, 0, DateTimeKind.Utc), 16),
        (new DateTime(1978,  1, 1, 0, 0, 0, DateTimeKind.Utc), 17),
        (new DateTime(1979,  1, 1, 0, 0, 0, DateTimeKind.Utc), 18),
        (new DateTime(1980,  1, 1, 0, 0, 0, DateTimeKind.Utc), 19),
        (new DateTime(1981,  7, 1, 0, 0, 0, DateTimeKind.Utc), 20),
        (new DateTime(1982,  7, 1, 0, 0, 0, DateTimeKind.Utc), 21),
        (new DateTime(1983,  7, 1, 0, 0, 0, DateTimeKind.Utc), 22),
        (new DateTime(1985,  7, 1, 0, 0, 0, DateTimeKind.Utc), 23),
        (new DateTime(1988,  1, 1, 0, 0, 0, DateTimeKind.Utc), 24),
        (new DateTime(1990,  1, 1, 0, 0, 0, DateTimeKind.Utc), 25),
        (new DateTime(1991,  1, 1, 0, 0, 0, DateTimeKind.Utc), 26),
        (new DateTime(1992,  7, 1, 0, 0, 0, DateTimeKind.Utc), 27),
        (new DateTime(1993,  7, 1, 0, 0, 0, DateTimeKind.Utc), 28),
        (new DateTime(1994,  7, 1, 0, 0, 0, DateTimeKind.Utc), 29),
        (new DateTime(1996,  1, 1, 0, 0, 0, DateTimeKind.Utc), 30),
        (new DateTime(1997,  7, 1, 0, 0, 0, DateTimeKind.Utc), 31),
        (new DateTime(1999,  1, 1, 0, 0, 0, DateTimeKind.Utc), 32),
        (new DateTime(2006,  1, 1, 0, 0, 0, DateTimeKind.Utc), 33),
        (new DateTime(2009,  1, 1, 0, 0, 0, DateTimeKind.Utc), 34),
        (new DateTime(2012,  7, 1, 0, 0, 0, DateTimeKind.Utc), 35),
        (new DateTime(2015,  7, 1, 0, 0, 0, DateTimeKind.Utc), 36),
        (new DateTime(2017,  1, 1, 0, 0, 0, DateTimeKind.Utc), 37),
    ];

    /// <summary>
    /// Returns the cumulative number of leap seconds (TAI − UTC) for the given UTC date.
    /// </summary>
    /// <param name="utc">A UTC <see cref="DateTime"/>.</param>
    /// <returns>
    /// The integer number of leap seconds in effect at <paramref name="utc"/>.
    /// Returns 0 for dates before 1972-Jan-01 (pre-leap-second era).
    /// Returns 37 for dates on or after 2017-Jan-01 (latest entry).
    /// </returns>
    public static int GetLeapSeconds(DateTime utc)
    {
        var utcNorm = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        int result = 0;
        for (int i = LeapTable.Length - 1; i >= 0; i--)
        {
            if (utcNorm >= LeapTable[i].Date)
            {
                result = LeapTable[i].LeapSeconds;
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// Converts a UTC <see cref="DateTime"/> to SPICE Ephemeris Time (ET ≈ TDB),
    /// expressed as seconds past J2000.0.
    /// </summary>
    /// <param name="utc">A UTC <see cref="DateTime"/>.</param>
    /// <returns>Ephemeris time in seconds past J2000.0 (2000-Jan-01 12:00:00.000 TT).</returns>
    /// <remarks>
    /// <para>
    /// The conversion is: <c>ET = (UTC − J2000_UTC_noon).TotalSeconds + leapSeconds + 32.184 + TDB_correction</c>.
    /// </para>
    /// <para>
    /// The TDB correction accounts for the periodic difference between TT and TDB
    /// (max ≈ 1.66 ms): <c>0.001657 × sin(628.3076 × T + 6.2401)</c>,
    /// where T is Julian centuries of TDB past J2000.0.
    /// </para>
    /// </remarks>
    public static double UtcToEt(DateTime utc)
    {
        var utcNorm = utc.Kind == DateTimeKind.Utc ? utc : utc.ToUniversalTime();
        int leapSec = GetLeapSeconds(utcNorm);

        // Seconds from UTC noon J2000.0 (2000-Jan-01 12:00:00 UTC)
        double secFromJ2000Utc = (utcNorm - J2000Epoch).TotalSeconds;

        // TT = UTC + leap_seconds + 32.184 → ET ≈ TT (ignoring TDB correction for now)
        double et = secFromJ2000Utc + leapSec + TtMinusTai;

        // TDB correction: small periodic term (max ~1.66 ms)
        // T = Julian centuries of TDB past J2000.0
        double T = et / (36525.0 * 86400.0);
        double tdbCorrection = 0.001657 * Math.Sin(628.3076 * T + 6.2401);
        et += tdbCorrection;

        return et;
    }
}
