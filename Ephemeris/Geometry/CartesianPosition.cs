// Updated: 2026-03-09
namespace Ephemeris.Geometry;

/// <summary>
/// Three-dimensional Cartesian position vector.
/// </summary>
/// <param name="X">X component in AU (astronomical units).</param>
/// <param name="Y">Y component in AU.</param>
/// <param name="Z">Z component in AU.</param>
/// <remarks>
/// Coordinates are in the J2000.0 equatorial (ICRS) reference frame unless otherwise stated.
/// </remarks>
public readonly record struct CartesianPosition(double X, double Y, double Z);
