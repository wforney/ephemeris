// Updated: 2026-05-01
namespace Ephemeris.Stellarography;

/// <summary>
/// Reads the Swiss Ephemeris fixed-star catalog file (<c>sefstars.txt</c>) and
/// exposes its entries as <see cref="FixedStar"/> records.
/// </summary>
/// <remarks>
/// <para>
/// <c>sefstars.txt</c> is a comma-delimited text file with ~1,400 star entries.
/// Lines beginning with <c>#</c> are comments and are ignored.
/// Each data line has 14–16 fields; see <c>docs/sefstars-format.md</c> for the
/// full field specification.
/// </para>
/// <para>
/// The catalog is NOT included in this repository; it must be obtained separately
/// from the Swiss Ephemeris project (https://www.astro.com/swisseph/) and its
/// path passed to <see cref="Load"/>.
/// </para>
/// </remarks>
public static class StarCatalog
{
    /// <summary>
    /// Loads all star entries from a <c>sefstars.txt</c> file.
    /// </summary>
    /// <param name="filePath">Absolute path to the <c>sefstars.txt</c> file.</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{FixedStar}"/> containing every successfully-parsed entry.
    /// </returns>
    /// <exception cref="FileNotFoundException">Thrown when <paramref name="filePath"/> does not exist.</exception>
    /// <exception cref="FormatException">Thrown when a data line cannot be parsed.</exception>
    public static IReadOnlyList<FixedStar> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Star catalog file not found: {filePath}", filePath);

        var stars = new List<FixedStar>();

        foreach (string rawLine in File.ReadLines(filePath))
        {
            // Skip blank lines and comment lines (# prefix).
            string line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
                continue;

            FixedStar star = ParseLine(line);
            stars.Add(star);
        }

        return stars.AsReadOnly();
    }

    /// <summary>
    /// Parses a single comma-delimited data line from <c>sefstars.txt</c>.
    /// </summary>
    /// <param name="line">Trimmed, non-comment data line.</param>
    /// <returns>A <see cref="FixedStar"/> populated from the parsed fields.</returns>
    /// <exception cref="FormatException">Thrown when the line has too few fields or a field cannot be parsed.</exception>
    private static FixedStar ParseLine(string line)
    {
        // Split on commas; there are 14–16 fields per line.
        string[] f = line.Split(',');
        if (f.Length < 14)
            throw new FormatException(
                $"Expected at least 14 fields in star catalog line, got {f.Length}: \"{line}\"");

        // ── Field 1: common name ─────────────────────────────────────────────
        string commonName = f[0].Trim();

        // ── Field 2: Bayer designation ────────────────────────────────────────
        string bayerDesig = f[1].Trim();

        // ── Field 3: coordinate frame ("ICRS", "FK5", "2000") ────────────────
        string frame = f[2].Trim();

        // ── Fields 4–6: right ascension (hours, minutes, seconds) ─────────────
        // Convert sexagesimal HMS → decimal degrees: RA° = (h + m/60 + s/3600) × 15
        int    raH = ParseInt(f[3], "RA hours");
        int    raM = ParseInt(f[4], "RA minutes");
        double raS = ParseDouble(f[5], "RA seconds");
        double ra  = (raH + raM / 60.0 + raS / 3600.0) * 15.0;

        // ── Fields 7–9: declination (degrees, arcminutes, arcseconds) ──────────
        // The degrees field carries the sign; arcminutes and arcseconds are positive.
        // Step 1: detect sign from the raw degrees string.
        string decDStr = f[6].Trim();
        int    sign    = decDStr.StartsWith('-') ? -1 : 1;
        int    decD    = ParseInt(decDStr, "Dec degrees");
        int    decM    = ParseInt(f[7], "Dec arcminutes");
        double decSec  = ParseDouble(f[8], "Dec arcseconds");
        // Step 2: combine; use Math.Abs(decD) so the sign is applied once.
        double dec = sign * (Math.Abs(decD) + decM / 60.0 + decSec / 3600.0);

        // ── Field 10: proper motion in RA × cos(Dec), mas/yr ─────────────────
        double pmRaCosD = ParseDouble(f[9], "PM RA cos(Dec)");

        // ── Field 11: proper motion in Dec, mas/yr ────────────────────────────
        double pmDec = ParseDouble(f[10], "PM Dec");

        // ── Field 12: radial velocity, km/s ──────────────────────────────────
        double radVel = ParseDouble(f[11], "radial velocity");

        // ── Field 13: parallax, mas ───────────────────────────────────────────
        double parallax = ParseDouble(f[12], "parallax");

        // ── Field 14: apparent magnitude ─────────────────────────────────────
        double mag = ParseDouble(f[13], "magnitude");

        return new FixedStar(
            commonName, bayerDesig, frame,
            ra, dec,
            pmRaCosD, pmDec, radVel, parallax, mag);
    }

    // ── Private parsing helpers ─────────────────────────────────────────────

    private static int ParseInt(string s, string fieldName)
    {
        s = s.Trim();
        if (int.TryParse(s, out int v))
            return v;

        // Some lines use "+16" style; try trimming the leading '+'.
        if (s.Length > 1 && s[0] == '+' && int.TryParse(s[1..], out int vp))
            return vp;

        throw new FormatException($"Cannot parse integer field '{fieldName}' from value \"{s}\".");
    }

    private static double ParseDouble(string s, string fieldName)
    {
        s = s.Trim();

        // Handle double-negative "−−269.77" artefacts that appear in some catalog entries.
        // Collapse repeated leading minus signs to a single one.
        while (s.StartsWith("--", StringComparison.Ordinal))
            s = s[1..];

        if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
            return v;

        throw new FormatException($"Cannot parse double field '{fieldName}' from value \"{s}\".");
    }
}
