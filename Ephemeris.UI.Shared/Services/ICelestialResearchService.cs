// Updated: 2026-03-22
using Ephemeris.Phenomenology;

namespace Ephemeris.UI.Services;

/// <summary>
/// Provides high-level celestial research operations for the UI layer.
/// </summary>
public interface ICelestialResearchService
{
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
