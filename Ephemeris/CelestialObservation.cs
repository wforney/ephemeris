// Updated: 2026-03-09
namespace Ephemeris;

/// <summary>
/// Instantaneous observed position of a celestial body as seen from an Earth-based observer,
/// combining equatorial and horizontal coordinates.
/// </summary>
/// <param name="RightAscension">Right ascension in decimal degrees, range [0, 360).</param>
/// <param name="Declination">Declination in decimal degrees, range [−90, 90].</param>
/// <param name="Azimuth">Topocentric azimuth in decimal degrees (North = 0°, East = 90°).</param>
/// <param name="Altitude">Topocentric altitude above the horizon in decimal degrees.</param>
/// <param name="Illumination">
/// Fraction of the body's disk that is illuminated, range [0, 1].
/// <see langword="null"/> for bodies where illumination is not computed (e.g., the Sun).
/// </param>
public readonly record struct CelestialObservation(
    double RightAscension,
    double Declination,
    double Azimuth,
    double Altitude,
    double? Illumination = null);
