// Updated: 2026-03-11
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris.Planetology;

/// <summary>
/// Calculates positions of selected minor planets using simplified Keplerian orbital elements.
/// </summary>
/// <remarks>
/// Supported bodies: (1) Ceres, (2) Pallas, (3) Juno, (4) Vesta, (2060) Chiron, (136199) Eris.
/// Orbital elements are referenced to the J2000.0 epoch (JD 2451545.0) and sourced from the
/// JPL Small Body Database and IAU Minor Planet Center records.
/// <para>
/// Accuracy: approximately 2–5° for main-belt asteroids; 1–3° for Chiron (slow-moving centaur);
/// 5–10° for Eris (high eccentricity and very long period).  For higher accuracy use
/// osculating elements from the current MPC catalog with <see cref="PlanetEphemeris.SimplifiedPlanetPosition"/>.
/// </para>
/// </remarks>
public static class AsteroidEphemeris
{
    /// <summary>
    /// Returns the names of all asteroids supported by this class.
    /// </summary>
    public static IReadOnlyList<string> SupportedAsteroids { get; } =
        ["ceres", "pallas", "juno", "vesta", "chiron", "eris"];

    /// <summary>
    /// Returns the Keplerian orbital elements for a named minor planet at epoch T (Julian centuries since J2000.0).
    /// </summary>
    /// <param name="asteroid">Asteroid name (case-insensitive): ceres, pallas, juno, vesta, chiron, or eris.</param>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns><see cref="OrbitalElements"/> for use with <see cref="PlanetEphemeris.SimplifiedPlanetPosition"/>.</returns>
    /// <exception cref="ArgumentException">Thrown for an unrecognised asteroid name.</exception>
    /// <remarks>
    /// Elements at J2000.0 (JD 2451545.0) from JPL Small Body Database / IAU MPC.
    /// Slow-varying element drift rates (N, i, w, e) are approximate and follow the same
    /// per-Julian-century convention used throughout <see cref="PlanetPositionService"/>.
    /// Mean anomaly uses the daily mean motion n multiplied by T × 36525 days/century.
    /// </remarks>
    public static OrbitalElements GetElements(string asteroid, double T) =>
        asteroid.ToLowerInvariant() switch
        {
            // (1) Ceres — dwarf planet; largest body in the main asteroid belt.
            "ceres" => new OrbitalElements(
                80.3272 + (3.1294E-5 * T), 10.5868 - (4.81E-6 * T), 73.1198 + (1.9023E-4 * T),
                2.7659254, 0.0760091 + (4.4E-8 * T), 95.9792 + (0.214135 * T * 36525)),

            // (2) Pallas — large C-type asteroid; unusually high orbital inclination (~35°).
            "pallas" => new OrbitalElements(
                173.0847 + (2.9E-5 * T), 34.8355 - (1.9E-6 * T), 310.0732 + (2.5E-5 * T),
                2.7724017, 0.2302540 - (1.2E-8 * T), 78.2209 + (0.213361 * T * 36525)),

            // (3) Juno — large S-type asteroid; historical candidate for planet status.
            "juno" => new OrbitalElements(
                169.8584 + (2.5E-5 * T), 12.9818 - (1.1E-6 * T), 247.8339 + (3.8E-5 * T),
                2.6682350, 0.2563449 - (2.6E-9 * T), 45.0791 + (0.225913 * T * 36525)),

            // (4) Vesta — second-largest asteroid; bright enough for naked-eye visibility at opposition.
            "vesta" => new OrbitalElements(
                103.8515 + (3.6E-5 * T), 7.1345 - (2.9E-7 * T), 151.1987 + (2.2E-5 * T),
                2.3614615, 0.0889177 + (1.6E-8 * T), 20.8659 + (0.271536 * T * 36525)),

            // (2060) Chiron — centaur body orbiting between Saturn and Uranus; widely used in astrology.
            "chiron" => new OrbitalElements(
                209.3680 + (3.1E-5 * T), 6.9350 + (1.2E-6 * T), 339.5940 + (4.1E-5 * T),
                13.6496967, 0.3815498 - (9.3E-9 * T), 339.3787 + (0.019848 * T * 36525)),

            // (136199) Eris — dwarf planet (scattered-disk TNO); largest known trans-Neptunian object.
            "eris" => new OrbitalElements(
                35.9511 + (2.0E-5 * T), 44.0404 - (3.0E-7 * T), 151.4300 + (5.0E-5 * T),
                67.6681, 0.4418000 - (1.0E-8 * T), 198.0000 + (0.001768 * T * 36525)),

            _ => throw new ArgumentException(
                $"Unknown asteroid '{asteroid}'. Supported names: {string.Join(", ", SupportedAsteroids)}.",
                nameof(asteroid))
        };

    /// <summary>
    /// Calculates an asteroid's geocentric equatorial coordinates and approximate distance.
    /// </summary>
    /// <param name="asteroid">Asteroid name (case-insensitive).</param>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>
    /// Equatorial coordinates (RA, Dec) in degrees and geocentric distance in AU.
    /// </returns>
    /// <remarks>
    /// Distance is heliocentric rather than true geocentric because Earth's
    /// heliocentric position is not subtracted; the error is at most ~1 AU for
    /// close-approaching bodies and typically negligible for distant objects.
    /// </remarks>
    public static (EquatorialCoordinates Coordinates, double DistanceAu) GetPosition(string asteroid, double T)
    {
        OrbitalElements elements = GetElements(asteroid, T);
        return PlanetEphemeris.SimplifiedPlanetPosition(T, elements);
    }

    /// <summary>
    /// Calculates an asteroid's position in both equatorial and horizontal coordinates for an observer.
    /// </summary>
    /// <param name="asteroid">Asteroid name (case-insensitive).</param>
    /// <param name="jd">Julian Day number (UTC).</param>
    /// <param name="longitude">Observer geographic longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer geographic latitude in degrees (north positive).</param>
    /// <returns>A <see cref="CelestialObservation"/> with (RA, Dec, Azimuth, Altitude) in degrees.</returns>
    public static CelestialObservation GetObservation(string asteroid, double jd, double longitude, double latitude)
    {
        double T = TimeUtils.JulianCentury(jd);
        var (equatorialPos, _) = GetPosition(asteroid, T);
        var horizontalPos = ObserverGeometry.EquatorialToHorizontal(
            equatorialPos.RightAscension, equatorialPos.Declination, jd, longitude, latitude);
        return new CelestialObservation(
            equatorialPos.RightAscension, equatorialPos.Declination,
            horizontalPos.Azimuth, horizontalPos.Altitude);
    }
}
