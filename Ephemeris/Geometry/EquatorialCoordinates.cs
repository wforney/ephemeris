// Updated: 2026-03-09
namespace Ephemeris.Geometry;

/// <summary>
/// Equatorial coordinates (right ascension and declination) at a given epoch.
/// </summary>
/// <param name="RightAscension">Right ascension in decimal degrees, range [0, 360).</param>
/// <param name="Declination">Declination in decimal degrees, range [−90, 90].</param>
/// <remarks>
/// All values are at the epoch used by the producing calculation (typically J2000.0 ICRS).
/// </remarks>
public readonly record struct EquatorialCoordinates(double RightAscension, double Declination);
