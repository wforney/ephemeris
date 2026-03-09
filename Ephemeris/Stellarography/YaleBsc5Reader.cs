// Updated: 2026-03-09
namespace Ephemeris.Stellarography;

/// <summary>
/// Reads the Yale Bright Star Catalog, 5th edition (BSC5) in its original fixed-width ASCII format.
/// Each data record is exactly 197 characters wide (plus line ending).
/// The catalog data must be obtained separately; it is not included in this repository.
/// </summary>
/// <remarks>
/// The Yale BSC5 is a public-domain catalog of 9,096 stars with visual magnitudes brighter than 7.1.
/// The catalog file (typically named <c>catalog</c> with no extension) is available from multiple
/// astronomical data archives including the VizieR CDS archive (catalogue V/50).
/// </remarks>
public static class YaleBsc5Reader
{
    /// <summary>
    /// Loads all star entries from a Yale BSC5 fixed-width catalog file.
    /// </summary>
    /// <param name="filePath">Absolute path to the BSC5 catalog file (typically named <c>catalog</c>).</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{FixedStar}"/> containing every successfully-parsed entry
    /// (records with blank HR numbers are skipped).
    /// </returns>
    /// <exception cref="FileNotFoundException">Thrown when <paramref name="filePath"/> does not exist.</exception>
    public static IReadOnlyList<FixedStar> Load(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Yale BSC5 catalog file not found: {filePath}", filePath);

        List<FixedStar> stars = [];

        foreach (string rawLine in File.ReadLines(filePath))
        {
            // Records shorter than 90 chars cannot contain RA/Dec fields.
            if (rawLine.Length < 90)
                continue;

            // Cols 0–3: HR number (right-justified integer); blank or zero = skip.
            string hrStr = rawLine[..4].Trim();
            if (string.IsNullOrEmpty(hrStr) || hrStr == "0")
                continue;
            if (!int.TryParse(hrStr, out int hrNumber) || hrNumber <= 0)
                continue;

            // Cols 4–13: star name (trimmed)
            string starName = rawLine.Length >= 14 ? rawLine[4..14].Trim() : string.Empty;

            // ── RA J2000 (cols 75–82) ────────────────────────────────────────
            if (rawLine.Length < 90)
                continue;

            double raHours   = TryParseDouble(rawLine[75..77]) ?? 0.0;
            double raMinutes = TryParseDouble(rawLine[77..79]) ?? 0.0;
            double raSeconds = TryParseDouble(rawLine[79..83]) ?? 0.0;
            double ra = (raHours + raMinutes / 60.0 + raSeconds / 3600.0) * 15.0;

            // ── Dec J2000 (cols 83–89) ──────────────────────────────────────
            char decSign    = rawLine[83];
            double decDeg   = TryParseDouble(rawLine[84..86]) ?? 0.0;
            double decMin   = TryParseDouble(rawLine[86..88]) ?? 0.0;
            double decSec   = TryParseDouble(rawLine[88..90]) ?? 0.0;
            double dec      = (decSign == '-' ? -1.0 : 1.0) * (decDeg + decMin / 60.0 + decSec / 3600.0);

            // ── Visual magnitude (cols 102–106) ─────────────────────────────
            double magnitude = 0.0;
            if (rawLine.Length >= 107)
                magnitude = TryParseDouble(rawLine[102..107]) ?? 0.0;

            // ── Spectral type (cols 127–146, 20 chars) ──────────────────────
            string spectralType = string.Empty;
            if (rawLine.Length >= 147)
                spectralType = rawLine[127..147].Trim();

            // ── Proper motion in RA (cols 148–153, 6 chars): seconds of time/yr ──
            // BSC5 stores the rate of change of RA coordinate (not × cos δ).
            // Convert: s/yr → arcsec/yr × cos(δ) → mas/yr
            double pmRaCosD = 0.0;
            if (rawLine.Length >= 154)
            {
                double? pmRaRaw = TryParseDouble(rawLine[148..154]);
                if (pmRaRaw.HasValue)
                {
                    double decRad = double.DegreesToRadians(dec);
                    pmRaCosD = pmRaRaw.Value * 15.0 * 1000.0 * Math.Cos(decRad);
                }
            }

            // ── Proper motion in Dec (cols 154–159, 6 chars): arcsec/yr → mas/yr ──
            double pmDec = 0.0;
            if (rawLine.Length >= 160)
            {
                double? pmDecRaw = TryParseDouble(rawLine[154..160]);
                if (pmDecRaw.HasValue)
                    pmDec = pmDecRaw.Value * 1000.0;
            }

            // ── Trigonometric parallax (cols 161–165, 5 chars): arcsec → mas ──
            double parallaxMas = 0.0;
            if (rawLine.Length >= 166)
            {
                double? parRaw = TryParseDouble(rawLine[161..166]);
                if (parRaw.HasValue)
                    parallaxMas = parRaw.Value * 1000.0;
            }

            string bayerDesig = $"HR{hrNumber}";
            string commonName = string.IsNullOrEmpty(starName) ? bayerDesig : starName;

            stars.Add(new FixedStar(
                commonName, bayerDesig, "J2000",
                ra, dec,
                pmRaCosD, pmDec,
                RadialVelocityKmS: 0.0,
                parallaxMas,
                magnitude,
                spectralType));
        }

        return stars.AsReadOnly();
    }

    private static double? TryParseDouble(string s)
    {
        s = s.Trim();
        if (string.IsNullOrEmpty(s))
            return null;
        if (double.TryParse(s, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
            return v;
        return null;
    }
}
