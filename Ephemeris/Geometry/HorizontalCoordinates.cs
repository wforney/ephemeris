// Updated: 2026-03-09
namespace Ephemeris.Geometry;

/// <summary>
/// Topocentric horizontal coordinates (azimuth and altitude) for a specific observer and instant.
/// </summary>
/// <param name="Azimuth">Azimuth in decimal degrees, measured clockwise from North; East = 90°.</param>
/// <param name="Altitude">Altitude above the horizon in decimal degrees, range [−90, 90].</param>
public readonly record struct HorizontalCoordinates(double Azimuth, double Altitude);
