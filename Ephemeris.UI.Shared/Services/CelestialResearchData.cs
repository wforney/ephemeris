// Updated: 2026-03-22
namespace Ephemeris.UI.Services;

/// <summary>
/// Snapshot of computed celestial data for a single body at a given instant.
/// </summary>
/// <param name="Azimuth">Azimuth in degrees, measured from North clockwise (0–360).</param>
/// <param name="Altitude">Altitude in degrees above the horizon (−90 to +90).</param>
/// <param name="RightAscension">Right ascension in degrees (0–360).</param>
/// <param name="Declination">Declination in degrees (−90 to +90).</param>
/// <param name="Illumination">Fractional illumination [0, 1] if applicable (Moon only).</param>
public sealed record CelestialObservation(
    double  Azimuth,
    double  Altitude,
    double  RightAscension,
    double  Declination,
    double? Illumination = null);

/// <summary>
/// Aggregate of computed celestial positions and rise/set times for a single
/// observer instant.  Returned by <c>CelestialResearchService</c>.
/// </summary>
public sealed record CelestialResearchData
{
    /// <summary>Computed Sun position and coordinates.</summary>
    public required CelestialObservation Sun { get; init; }

    /// <summary>Computed Moon position, coordinates, and illumination.</summary>
    public required CelestialObservation Moon { get; init; }

    /// <summary>UTC time of sunrise, or <see langword="null"/> if the Sun does not rise.</summary>
    public DateTime? Sunrise { get; init; }

    /// <summary>UTC time of sunset, or <see langword="null"/> if the Sun does not set.</summary>
    public DateTime? Sunset { get; init; }

    /// <summary>UTC time of moonrise, or <see langword="null"/> if the Moon does not rise.</summary>
    public DateTime? Moonrise { get; init; }

    /// <summary>UTC time of moonset, or <see langword="null"/> if the Moon does not set.</summary>
    public DateTime? Moonset { get; init; }
}
