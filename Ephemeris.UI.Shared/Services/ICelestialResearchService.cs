// Updated: 2026-03-22
using Ephemeris.Phenomenology;

namespace Ephemeris.UI.Services;

/// <summary>
/// Provides high-level celestial data queries and event detection for the research workspace.
/// </summary>
public interface ICelestialResearchService
{
    /// <summary>
    /// Returns a full celestial data snapshot for the specified UTC time and observer location.
    /// </summary>
    Task<CelestialResearchData> GetDataAsync(
        DateTime utcTime,
        double longitude,
        double latitude,
        CancellationToken ct = default);

    /// <summary>
    /// Returns a celestial data snapshot for any Julian Day (including BCE/historical dates).
    /// Rise/set and lunar phase data will be null for historical epochs.
    /// </summary>
    Task<CelestialResearchData> GetDataForJulianDayAsync(
        double julianDay,
        double longitude,
        double latitude,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the next count notable celestial events after fromUtc, ordered by ascending UTC time.
    /// </summary>
    Task<IReadOnlyList<CelestialEventDetector.CelestialEvent>> GetUpcomingEventsAsync(
        DateTime fromUtc, int count = 5, CancellationToken ct = default);
}
