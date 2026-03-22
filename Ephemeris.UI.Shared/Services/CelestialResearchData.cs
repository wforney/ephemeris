// Updated: 2026-03-22
namespace Ephemeris.UI.Services;

/// <summary>
/// Snapshot of celestial data for a single observer location and UTC instant,
/// as returned by <see cref="ICelestialResearchService.GetDataAsync"/>.
/// </summary>
/// <param name="Sun">Sun's equatorial and horizontal coordinates.</param>
/// <param name="Moon">Moon's equatorial and horizontal coordinates with illumination fraction.</param>
/// <param name="Sunrise">UTC time of today's sunrise, or <c>null</c> if circumpolar.</param>
/// <param name="Sunset">UTC time of today's sunset, or <c>null</c> if circumpolar.</param>
/// <param name="Moonrise">UTC time of today's moonrise, or <c>null</c> if circumpolar or not visible.</param>
/// <param name="Moonset">UTC time of today's moonset, or <c>null</c> if circumpolar or not visible.</param>
/// <param name="NextFullMoon">UTC time of the next full moon after the query instant.</param>
/// <param name="NextNewMoon">UTC time of the next new moon after the query instant.</param>
public record CelestialResearchData(
    CelestialObservation Sun,
    CelestialObservation Moon,
    DateTime? Sunrise,
    DateTime? Sunset,
    DateTime? Moonrise,
    DateTime? Moonset,
    DateTime? NextFullMoon,
    DateTime? NextNewMoon);
