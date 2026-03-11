// Updated: 2026-03-11
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris.Planetology;

/// <summary>
/// Calculates positions of selected minor planets using simplified Keplerian orbital elements.
/// </summary>
/// <remarks>
/// Supported bodies include the classical Big Four (Ceres, Pallas, Juno, Vesta), centaurs
/// (Chiron, Pholus, Nessus, Asbolus, Chariklo, Hylonome), trans-Neptunian objects and dwarf
/// planets (Eris, Haumea, Makemake, Sedna, Quaoar, Orcus), near-Earth and Mars-crossing
/// asteroids (Eros, Amor, Icarus), and a representative set of astrologically significant
/// main-belt bodies (Astraea, Hebe, Iris, Flora, Metis, Hygiea, Victoria, Eunomia, Psyche,
/// Fortuna, Proserpina, Harmonia, Isis, Sappho, Nemesis, Hidalgo).
/// <para>
/// Orbital elements are J2000.0 osculating elements (JD 2451545.0) sourced from the
/// JPL Small Body Database (SBDB) and the IAU Minor Planet Center (MPC).
/// </para>
/// <para>
/// Accuracy: approximately 1–5° for main-belt asteroids; 2–8° for centaurs; 5–15° for
/// high-eccentricity or long-period TNOs (Sedna, Eris).  For higher accuracy supply
/// current MPC osculating elements to <see cref="PlanetEphemeris.SimplifiedPlanetPosition"/>.
/// </para>
/// </remarks>
public static class AsteroidEphemeris
{
    /// <summary>
    /// Returns the canonical lower-case names of all asteroids supported by this class.
    /// </summary>
    public static IReadOnlyList<string> SupportedAsteroids { get; } =
    [
        // Classical Big Four — main asteroid belt, astrologically foundational
        "ceres", "pallas", "juno", "vesta",
        // Additional main-belt bodies commonly used in astrology
        "astraea", "hebe", "iris", "flora", "metis", "hygiea",
        "victoria", "eunomia", "psyche", "fortuna", "proserpina",
        "harmonia", "isis", "sappho", "nemesis",
        // Near-Earth / Mars-crossing
        "eros", "amor", "icarus",
        // Outer solar system — Hidalgo (comet-like orbit)
        "hidalgo",
        // Centaurs (Saturn–Uranus/Neptune crossers)
        "chiron", "pholus", "nessus", "asbolus", "chariklo", "hylonome",
        // Trans-Neptunian objects and dwarf planets
        "quaoar", "orcus", "haumea", "makemake", "eris", "sedna",
    ];

    /// <summary>
    /// Returns the Keplerian orbital elements for a named minor planet at epoch T (Julian centuries since J2000.0).
    /// </summary>
    /// <param name="asteroid">Asteroid name (case-insensitive). See <see cref="SupportedAsteroids"/>.</param>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns><see cref="OrbitalElements"/> for use with <see cref="PlanetEphemeris.SimplifiedPlanetPosition"/>.</returns>
    /// <exception cref="ArgumentException">Thrown for an unrecognised asteroid name.</exception>
    /// <remarks>
    /// Elements are J2000.0 osculating values from the JPL Small Body Database / IAU MPC.
    /// Constructor parameter order: (Ω, i, ω, a, e, M₀ + n·T·36525)
    /// where Ω = longitude of ascending node (°), i = inclination (°),
    /// ω = argument of perihelion (°), a = semi-major axis (AU), e = eccentricity,
    /// and n = mean daily motion (°/day), derived from Kepler's third law: n ≈ 0.9856°/a^1.5.
    /// Linear element drift terms are omitted for most bodies; their contribution over
    /// astrological time-scales (decades) is negligible compared to the method's intrinsic error.
    /// </remarks>
    public static OrbitalElements GetElements(string asteroid, double T) =>
        asteroid.ToLowerInvariant() switch
        {
            // ── Classical Big Four ────────────────────────────────────────────────

            // (1) Ceres — largest main-belt body, classified as a dwarf planet since 2006.
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

            // ── Additional main-belt bodies ───────────────────────────────────────

            // (5) Astraea — star-maiden; orbits just interior to the outer main belt.
            "astraea" => new OrbitalElements(
                141.596, 5.368, 357.824,
                2.5733, 0.1905, 241.49 + (0.2389 * T * 36525)),

            // (6) Hebe — cupbearer of the gods; large S-type asteroid.
            "hebe" => new OrbitalElements(
                138.758, 14.768, 239.654,
                2.4248, 0.2018, 147.63 + (0.2612 * T * 36525)),

            // (7) Iris — rainbow goddess; one of the largest S-type asteroids.
            "iris" => new OrbitalElements(
                259.582, 5.516, 145.479,
                2.3861, 0.2298, 261.38 + (0.2675 * T * 36525)),

            // (8) Flora — goddess of flowers; progenitor of the Flora asteroid family.
            "flora" => new OrbitalElements(
                110.882, 5.887, 285.085,
                2.2018, 0.1563, 247.83 + (0.3020 * T * 36525)),

            // (9) Metis — goddess of wisdom; one of the larger S-type main-belt asteroids.
            "metis" => new OrbitalElements(
                68.847, 5.576, 6.069,
                2.3863, 0.1227, 333.48 + (0.2675 * T * 36525)),

            // (10) Hygiea — goddess of health; fourth-largest main-belt asteroid (C-type).
            "hygiea" => new OrbitalElements(
                283.215, 3.843, 311.965,
                3.1421, 0.1177, 113.95 + (0.1770 * T * 36525)),

            // (12) Victoria — goddess of victory; used by J.C. Adams in orbital studies.
            "victoria" => new OrbitalElements(
                235.531, 8.368, 69.220,
                2.3334, 0.2206, 152.73 + (0.2720 * T * 36525)),

            // (15) Eunomia — goddess of order; largest S-type asteroid in the inner belt.
            "eunomia" => new OrbitalElements(
                292.876, 11.757, 97.922,
                2.6435, 0.1870, 354.27 + (0.2292 * T * 36525)),

            // (16) Psyche — soul; metallic M-type asteroid; target of NASA Psyche mission.
            "psyche" => new OrbitalElements(
                150.024, 3.098, 228.030,
                2.9229, 0.1344, 149.13 + (0.1972 * T * 36525)),

            // (19) Fortuna — goddess of fortune; prominent C-type inner main-belt asteroid.
            "fortuna" => new OrbitalElements(
                211.141, 1.573, 182.548,
                2.4421, 0.1578, 119.43 + (0.2581 * T * 36525)),

            // (26) Proserpina — Persephone; queen of the underworld.
            "proserpina" => new OrbitalElements(
                45.886, 3.564, 193.788,
                2.6560, 0.0878, 73.39 + (0.2275 * T * 36525)),

            // (40) Harmonia — goddess of harmony and concord; daughter of Ares and Aphrodite.
            "harmonia" => new OrbitalElements(
                94.480, 4.256, 267.524,
                2.2664, 0.0473, 60.74 + (0.2892 * T * 36525)),

            // (42) Isis — Egyptian mother goddess; large C-type main-belt asteroid.
            "isis" => new OrbitalElements(
                214.616, 8.535, 289.981,
                2.4427, 0.2250, 27.77 + (0.2580 * T * 36525)),

            // (80) Sappho — Greek lyric poet of Lesbos; inner main-belt S-type asteroid.
            "sappho" => new OrbitalElements(
                216.087, 8.668, 208.836,
                2.2967, 0.2006, 261.49 + (0.2831 * T * 36525)),

            // (128) Nemesis — goddess of retribution; outer main-belt C-type asteroid.
            "nemesis" => new OrbitalElements(
                175.155, 6.251, 0.118,
                2.7499, 0.1280, 340.26 + (0.2162 * T * 36525)),

            // ── Near-Earth / Mars-crossing asteroids ──────────────────────────────

            // (433) Eros — first near-Earth asteroid discovered; target of NEAR Shoemaker mission.
            "eros" => new OrbitalElements(
                304.299, 10.829, 178.895,
                1.4579, 0.2228, 208.50 + (0.5601 * T * 36525)),

            // (1221) Amor — archetype of Amor-class near-Earth asteroids.
            "amor" => new OrbitalElements(
                171.386, 11.878, 26.430,
                1.9193, 0.4353, 85.27 + (0.3703 * T * 36525)),

            // (1566) Icarus — famous for extremely close approaches to the Sun (q ≈ 0.19 AU).
            "icarus" => new OrbitalElements(
                87.983, 22.838, 31.220,
                1.0780, 0.8268, 175.62 + (0.8810 * T * 36525)),

            // ── Outer belt / comet-like ───────────────────────────────────────────

            // (944) Hidalgo — comet-like orbit with aphelion near Saturn; used in mundane astrology.
            "hidalgo" => new OrbitalElements(
                21.100, 42.518, 54.037,
                5.7404, 0.6573, 53.33 + (0.07167 * T * 36525)),

            // ── Centaurs ──────────────────────────────────────────────────────────

            // (2060) Chiron — first-discovered centaur; widely used in astrological interpretation.
            "chiron" => new OrbitalElements(
                209.3680 + (3.1E-5 * T), 6.9350 + (1.2E-6 * T), 339.5940 + (4.1E-5 * T),
                13.6496967, 0.3815498 - (9.3E-9 * T), 339.3787 + (0.019848 * T * 36525)),

            // (5145) Pholus — second-discovered centaur; associated with turning points and excess.
            "pholus" => new OrbitalElements(
                119.376, 24.667, 354.189,
                20.361, 0.5720, 190.19 + (0.01072 * T * 36525)),

            // (7066) Nessus — third major centaur; themes of cycles, karma, and consequences.
            "nessus" => new OrbitalElements(
                179.057, 15.659, 169.638,
                24.614, 0.5199, 68.31 + (0.008073 * T * 36525)),

            // (8405) Asbolus — centaur with high eccentricity; divination and foresight themes.
            "asbolus" => new OrbitalElements(
                107.054, 17.621, 257.422,
                17.942, 0.6198, 125.84 + (0.01297 * T * 36525)),

            // (10199) Chariklo — largest known centaur; first minor planet found with ring system.
            "chariklo" => new OrbitalElements(
                300.398, 23.414, 241.299,
                15.800, 0.1717, 124.47 + (0.01569 * T * 36525)),

            // (10370) Hylonome — centaur; linked mythologically to tragic love.
            "hylonome" => new OrbitalElements(
                289.032, 4.137, 192.270,
                25.248, 0.2521, 59.69 + (0.007773 * T * 36525)),

            // ── Trans-Neptunian objects and dwarf planets ─────────────────────────

            // (50000) Quaoar — Kuiper Belt Object; creation deity of the Tongva people.
            "quaoar" => new OrbitalElements(
                188.803, 7.988, 163.001,
                43.616, 0.0339, 283.28 + (0.003421 * T * 36525)),

            // (90482) Orcus — plutino (3:2 Neptune resonance); Etruscan underworld deity.
            "orcus" => new OrbitalElements(
                268.607, 20.573, 72.197,
                39.475, 0.2270, 171.94 + (0.003971 * T * 36525)),

            // (136108) Haumea — rapidly rotating dwarf planet; Hawaiian creation goddess.
            "haumea" => new OrbitalElements(
                121.902, 28.218, 239.508,
                43.129, 0.1954, 214.84 + (0.003477 * T * 36525)),

            // (136472) Makemake — dwarf planet in Kuiper Belt; Rapa Nui creator god.
            "makemake" => new OrbitalElements(
                79.382, 28.963, 294.838,
                45.430, 0.1600, 126.74 + (0.003215 * T * 36525)),

            // (136199) Eris — most massive known dwarf planet (scattered disc); goddess of strife.
            "eris" => new OrbitalElements(
                35.9511 + (2.0E-5 * T), 44.0404 - (3.0E-7 * T), 151.4300 + (5.0E-5 * T),
                67.6681, 0.4418000 - (1.0E-8 * T), 198.0000 + (0.001768 * T * 36525)),

            // (90377) Sedna — extreme scattered-disc object; perihelion beyond 76 AU.
            "sedna" => new OrbitalElements(
                144.514, 11.929, 311.027,
                506.84, 0.8450, 357.29 + (0.0000864 * T * 36525)),

            _ => throw new ArgumentException(
                $"Unknown asteroid '{asteroid}'. See AsteroidEphemeris.SupportedAsteroids for the full list.",
                nameof(asteroid))
        };

    /// <summary>
    /// Calculates an asteroid's geocentric equatorial coordinates and true geocentric distance
    /// by subtracting Earth's heliocentric position vector.
    /// </summary>
    /// <param name="asteroid">Asteroid name (case-insensitive).</param>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>
    /// Geocentric equatorial coordinates (RA, Dec) in degrees and geocentric distance in AU.
    /// </returns>
    /// <remarks>
    /// Delegates to <see cref="PlanetEphemeris.GeocentricPosition"/>, which subtracts Earth's
    /// heliocentric ecliptic position from the asteroid's heliocentric ecliptic position before
    /// converting to equatorial coordinates (Meeus Ch. 33).  This is important for near-Earth
    /// objects and Mars-crossing asteroids where the heliocentric and geocentric positions can
    /// differ by up to ~1 AU; for main-belt and outer-solar-system bodies the correction is
    /// smaller but still improves RA/Dec accuracy by several tenths of a degree.
    /// </remarks>
    public static (EquatorialCoordinates Coordinates, double DistanceAu) GetPosition(string asteroid, double T)
    {
        OrbitalElements elements = GetElements(asteroid, T);
        return PlanetEphemeris.GeocentricPosition(T, elements);
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
