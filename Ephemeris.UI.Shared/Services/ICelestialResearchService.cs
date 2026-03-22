// Updated: 2026-03-22
namespace Ephemeris.UI.Services;

/// <summary>
/// Provides high-level celestial data queries for the research workspace.
/// Wraps <see cref="EphemerisCalculator"/> and <see cref="Ephemeris.Phenomenology.RiseSetCalculator"/>
/// and returns a combined <see cref="CelestialResearchData"/> snapshot for a given UTC instant
/// and observer location.
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
}
