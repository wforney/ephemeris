// Updated: 2026-03-09
namespace Ephemeris.Geometry;

/// <summary>
/// Ecliptic (celestial longitude and latitude) coordinates.
/// </summary>
/// <param name="Longitude">Ecliptic longitude in decimal degrees, range [0, 360).</param>
/// <param name="Latitude">Ecliptic latitude in decimal degrees, range [−90, 90].</param>
public readonly record struct EclipticCoordinates(double Longitude, double Latitude);
