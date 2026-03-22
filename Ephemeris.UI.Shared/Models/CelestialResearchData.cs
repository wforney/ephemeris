// Updated: 2026-03-22
using Ephemeris.Phenomenology;

namespace Ephemeris.UI.Models;

/// <summary>
/// Immutable snapshot of all celestial data computed for a single observer, position, and time.
/// Returned by <see cref="Services.CelestialResearchService.GetDataAsync"/>.
/// </summary>
/// <remarks>
/// Designed as a record for value-equality and immutability. Each property is set via the
/// <c>init</c> accessor so instances are constructed with object-initialiser syntax.
/// </remarks>
public record CelestialResearchData
{
    /// <summary>UTC time of the observation.</summary>
    public DateTime TimeUtc { get; init; }

    /// <summary>Observer longitude in degrees (east positive).</summary>
    public double Longitude { get; init; }

    /// <summary>Observer latitude in degrees (north positive).</summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Approximate biblical (Hebrew luni-solar) calendar information for this moment
    /// and observer location, or <see langword="null"/> if calculation was not requested.
    /// </summary>
    public BiblicalCalendarHelper.BiblicalDate? BiblicalDate { get; init; }
}
