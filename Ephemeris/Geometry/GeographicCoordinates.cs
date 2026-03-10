// Updated: 2026-03-10
namespace Ephemeris.Geometry;

/// <summary>
/// Geographic (observer) coordinates on the Earth's surface.
/// </summary>
/// <param name="Longitude">Geographic longitude in degrees, east positive [−180, 180].</param>
/// <param name="Latitude">Geographic latitude in degrees, north positive [−90, 90].</param>
/// <param name="AltitudeMeters">Height above the WGS-84 ellipsoid in metres (default 0).</param>
public readonly record struct GeographicCoordinates(double Longitude, double Latitude, double AltitudeMeters = 0);
