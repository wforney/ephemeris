// Updated: 2026-03-10
namespace Ephemeris.Stellarography;

/// <summary>
/// Provides access to the embedded subset of the Yale Bright Star Catalog (BSC5).
/// The built-in catalog contains 100 of the brightest named stars (V ≤ 3.1).
/// For the full BSC5 catalog, use <see cref="YaleBsc5Reader"/> with an external data file.
/// </summary>
/// <remarks>
/// The catalog is loaded once on first access and cached for subsequent calls.
/// All entries are referenced at epoch J2000.0 (ICRS).
/// </remarks>
public static class BrightStarCatalog
{
    private static readonly Lazy<IReadOnlyList<FixedStar>> _catalog =
        new(StarCatalog.LoadBuiltIn, isThreadSafe: true);

    /// <summary>
    /// Gets the full built-in bright-star catalog (all embedded entries).
    /// </summary>
    public static IReadOnlyList<FixedStar> All => _catalog.Value;

    /// <summary>
    /// Looks up a star by common name or Bayer designation (case-insensitive).
    /// </summary>
    /// <param name="name">
    /// Common name (e.g., "Sirius") or Bayer designation (e.g., "alCMa").
    /// </param>
    /// <returns>
    /// The first matching <see cref="FixedStar"/>, or <see langword="null"/> if not found.
    /// </returns>
    public static FixedStar? GetStar(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _catalog.Value.FirstOrDefault(s =>
            s.CommonName.Equals(name, StringComparison.OrdinalIgnoreCase) ||
            s.BayerDesignation.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns all catalog entries whose common name or Bayer designation contains
    /// <paramref name="name"/> (case-insensitive, partial match).
    /// </summary>
    /// <param name="name">Partial or full star name to search for.</param>
    /// <returns>Sequence of matching <see cref="FixedStar"/> entries.</returns>
    public static IEnumerable<FixedStar> Search(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        return _catalog.Value.Where(s =>
            s.CommonName.Contains(name, StringComparison.OrdinalIgnoreCase) ||
            s.BayerDesignation.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Returns all catalog entries with visual magnitude ≤ <paramref name="magnitudeLimit"/>.
    /// Brighter stars have lower (or negative) magnitudes.
    /// </summary>
    /// <param name="magnitudeLimit">Upper magnitude bound (inclusive); e.g., 2.0 returns stars brighter than 2nd magnitude.</param>
    /// <returns>Sequence of <see cref="FixedStar"/> entries satisfying the magnitude constraint.</returns>
    public static IEnumerable<FixedStar> GetBrighter(double magnitudeLimit) =>
        _catalog.Value.Where(s => s.Magnitude <= magnitudeLimit);

    /// <summary>
    /// Returns all catalog entries within <paramref name="radiusDeg"/> degrees of the given sky position.
    /// </summary>
    /// <param name="raDeg">Centre right ascension in degrees [0, 360).</param>
    /// <param name="decDeg">Centre declination in degrees [−90, 90].</param>
    /// <param name="radiusDeg">Search radius in degrees.</param>
    /// <returns>Sequence of <see cref="FixedStar"/> entries within the cone.</returns>
    public static IEnumerable<FixedStar> GetInRegion(double raDeg, double decDeg, double radiusDeg) =>
        StarCatalog.GetInRegion(_catalog.Value, raDeg, decDeg, radiusDeg);
}
