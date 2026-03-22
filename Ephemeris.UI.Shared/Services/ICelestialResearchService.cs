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
    /// Rise/set event times and next new/full moon fields will be <see langword="null"/> for
    /// historical epochs; instantaneous positions and lunar illumination are still computed.
    /// </summary>
    Task<CelestialResearchData> GetDataForJulianDayAsync(
        double julianDay,
        double longitude,
        double latitude,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the next <paramref name="count"/> notable celestial events after
    /// <paramref name="fromUtc"/>, ordered by ascending UTC time.
    /// </summary>
    /// <param name="fromUtc">Search for events after this UTC time.</param>
    /// <param name="count">Maximum number of events to return (default 5).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of up to <paramref name="count"/> <see cref="CelestialEventDetector.CelestialEvent"/> instances.
    /// </returns>
    Task<IReadOnlyList<CelestialEventDetector.CelestialEvent>> GetUpcomingEventsAsync(
        DateTime fromUtc, int count = 5, CancellationToken ct = default);
}
