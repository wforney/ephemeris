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
    /// <param name="utcTime">The UTC date and time of the observation.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <param name="ct">Optional cancellation token.</param>
    /// <returns>
    /// A <see cref="CelestialResearchData"/> containing Sun/Moon positions, rise/set times,
    /// and the next full and new moon times.
    /// </returns>
    Task<CelestialResearchData> GetDataAsync(
        DateTime utcTime,
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
