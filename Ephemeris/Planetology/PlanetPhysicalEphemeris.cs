using Ephemeris.Chronology;

namespace Ephemeris.Planetology;

/// <summary>
/// Provides physical ephemeris quantities for planets: apparent magnitude, angular diameter, and elongation.
/// Magnitude coefficients from Meeus <em>Astronomical Algorithms</em> Appendix I and Ch. 41.
/// </summary>
public static class PlanetPhysicalEphemeris
{
    // (V0, G) — H-G magnitude system coefficients
    // V0: absolute magnitude at r=1 AU, delta=1 AU, phase=0°
    private static readonly Dictionary<string, (double V0, double EquatorialDiameterArcsecAt1AU)> s_data = new(StringComparer.OrdinalIgnoreCase)
    {
        ["mercury"] = (-0.36,  6.74),
        ["venus"]   = (-4.34, 16.92),
        ["mars"]    = (-1.51,  9.36),
        ["jupiter"] = (-9.40, 196.94),
        ["saturn"]  = (-8.88, 165.60),
        ["uranus"]  = (-7.19,  65.80),
        ["neptune"] = (-6.87,  62.20),
        ["pluto"]   = (-1.00,   2.07),
    };

    // Meeus Ch. 41 magnitude polynomial: V = V0 + 5·log10(r·Δ) + phase_term
    // Phase term coefficients for each planet (linear for outer, polynomial for inner)
    private static double PhaseCorrection(string planet, double phaseAngleDeg)
    {
        double i = phaseAngleDeg;
        return planet.ToLowerInvariant() switch
        {
            "mercury" => (6.3280e-2 * i) - (1.6336e-3 * i * i) + (3.3644e-5 * i * i * i) - (3.4265e-7 * i * i * i * i),
            "venus"   => (1.0820e-3 * i) + (1.8776e-5 * i * i),
            "mars"    => 2.852e-2 * i,
            "jupiter" => 1.400e-2 * i,
            "saturn"  => 1.500e-2 * i,   // ring contribution excluded (requires ring-plane tilt)
            "uranus"  => 0.0,
            "neptune" => 0.0,
            "pluto"   => 0.0,
            _ => 0.0,
        };
    }

    /// <summary>
    /// Calculates a planet's apparent visual magnitude.
    /// </summary>
    /// <param name="planet">Planet name (case-insensitive).</param>
    /// <param name="r">Heliocentric distance in AU.</param>
    /// <param name="delta">Geocentric distance in AU.</param>
    /// <param name="phaseAngleDeg">Sun–planet–observer phase angle in degrees.</param>
    /// <returns>Apparent visual magnitude (V-band). Lower is brighter.</returns>
    public static double ApparentMagnitude(string planet, double r, double delta, double phaseAngleDeg)
    {
        if (!s_data.TryGetValue(planet, out var data))
            throw new ArgumentException($"Unknown planet: {planet}", nameof(planet));

        return data.V0 + (5.0 * Math.Log10(r * delta)) + PhaseCorrection(planet, phaseAngleDeg);
    }

    /// <summary>
    /// Calculates a planet's apparent angular diameter as seen from Earth.
    /// </summary>
    /// <param name="planet">Planet name (case-insensitive).</param>
    /// <param name="delta">Geocentric distance in AU.</param>
    /// <returns>Equatorial angular diameter in arcseconds.</returns>
    public static double AngularDiameter(string planet, double delta)
    {
        if (!s_data.TryGetValue(planet, out var data))
            throw new ArgumentException($"Unknown planet: {planet}", nameof(planet));

        return data.EquatorialDiameterArcsecAt1AU / delta;
    }

    /// <summary>
    /// Calculates the elongation of a planet from the Sun as seen from Earth.
    /// This is equivalent to <see cref="CoordinateConverter.AngularSeparation"/> applied to the planet and Sun.
    /// </summary>
    /// <param name="planetRA">Planet right ascension in degrees.</param>
    /// <param name="planetDec">Planet declination in degrees.</param>
    /// <param name="sunRA">Sun right ascension in degrees.</param>
    /// <param name="sunDec">Sun declination in degrees.</param>
    /// <returns>Elongation angle in degrees [0, 180].</returns>
    public static double Elongation(double planetRA, double planetDec, double sunRA, double sunDec)
        => CoordinateConverter.AngularSeparation(planetRA, planetDec, sunRA, sunDec);

    /// <summary>
    /// Calculates the phase angle (Sun–planet–observer angle) given the three distances.
    /// </summary>
    /// <param name="heliocentricDistanceAu">Planet heliocentric distance in AU (r).</param>
    /// <param name="geocentricDistanceAu">Planet geocentric distance in AU (Δ).</param>
    /// <param name="earthSunDistanceAu">Earth–Sun distance in AU (R).</param>
    /// <returns>Phase angle in degrees [0, 180]. 0° = full (behind Sun), 180° = new (in front of Sun).</returns>
    public static double PhaseAngle(double heliocentricDistanceAu, double geocentricDistanceAu, double earthSunDistanceAu)
    {
        double r     = heliocentricDistanceAu;
        double delta = geocentricDistanceAu;
        double R     = earthSunDistanceAu;
        double cosI  = ((r * r) + (delta * delta) - (R * R)) / (2.0 * r * delta);
        return TimeUtils.ToDegrees(Math.Acos(Math.Clamp(cosI, -1.0, 1.0)));
    }

    /// <summary>
    /// Calculates the illuminated fraction of a planet's disk (Meeus Ch. 41).
    /// </summary>
    /// <param name="heliocentricDistanceAu">Planet heliocentric distance in AU (r).</param>
    /// <param name="geocentricDistanceAu">Planet geocentric distance in AU (Δ).</param>
    /// <param name="earthSunDistanceAu">Earth–Sun distance in AU (R).</param>
    /// <returns>
    /// Illumination fraction in [0, 1]. 1 = fully illuminated (opposition), 0 = dark (inferior conjunction).
    /// Outer planets always return values near 1; inner planets can drop below 0.5.
    /// </returns>
    public static double Illumination(double heliocentricDistanceAu, double geocentricDistanceAu, double earthSunDistanceAu)
    {
        double phaseAngleRad = TimeUtils.ToRadians(PhaseAngle(heliocentricDistanceAu, geocentricDistanceAu, earthSunDistanceAu));
        return (1.0 + Math.Cos(phaseAngleRad)) / 2.0;
    }
}
