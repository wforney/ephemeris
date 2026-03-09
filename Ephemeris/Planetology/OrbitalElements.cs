// Updated: 2026-03-09
namespace Ephemeris.Planetology;

/// <summary>
/// Keplerian orbital elements for a solar-system body at a given epoch.
/// </summary>
/// <param name="LongitudeAscendingNode">
/// Longitude of the ascending node (N) in decimal degrees.
/// The angle from the vernal equinox to the point where the orbit crosses the ecliptic northward.
/// </param>
/// <param name="Inclination">
/// Orbital inclination (i) in decimal degrees.
/// The tilt of the orbit plane relative to the ecliptic.
/// </param>
/// <param name="ArgumentOfPerihelion">
/// Argument of perihelion (w) in decimal degrees.
/// The angle from the ascending node to the point of closest approach, measured in the orbital plane.
/// </param>
/// <param name="SemiMajorAxisAu">
/// Semi-major axis (a) in astronomical units (AU).
/// Half the longest diameter of the elliptical orbit.
/// </param>
/// <param name="Eccentricity">
/// Orbital eccentricity (e), dimensionless.
/// 0 = circular, 0–1 = elliptical, 1 = parabolic, &gt;1 = hyperbolic.
/// </param>
/// <param name="MeanAnomaly">
/// Mean anomaly (M) in decimal degrees.
/// The fraction of the orbital period that has elapsed since perihelion, scaled to [0, 360).
/// </param>
public readonly record struct OrbitalElements(
    double LongitudeAscendingNode,
    double Inclination,
    double ArgumentOfPerihelion,
    double SemiMajorAxisAu,
    double Eccentricity,
    double MeanAnomaly);
