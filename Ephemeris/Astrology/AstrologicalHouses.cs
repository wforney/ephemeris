// Updated: 2026-03-11
using Ephemeris.Chronology;

namespace Ephemeris.Astrology;

/// <summary>
/// Computes astrological house cusps for a given place and time using various house division systems.
/// </summary>
/// <remarks>
/// The foundation of all house systems computed here is the <em>Ascendant</em> (the ecliptic degree
/// rising on the eastern horizon) and the <em>Midheaven</em> (MC; the ecliptic degree on the upper meridian).
/// Both are computed from the Right Ascension of the Midheaven Circle (RAMC = GMST + observer longitude)
/// and the obliquity of the ecliptic using standard spherical-trigonometry formulae.
/// <para>
/// References: Meeus, <em>Astronomical Algorithms</em> (2nd ed.) Ch. 14;
/// Holden, <em>Astrological House Systems</em> (1977);
/// Koch &amp; Knappich, <em>Horoskop und Himmelshäuser</em> (1932).
/// </para>
/// </remarks>
public static class AstrologicalHouses
{
    /// <summary>
    /// Computes the twelve house cusps and four Angles for a given Julian Day, observer location,
    /// and house system.
    /// </summary>
    /// <param name="jd">Julian Day number (UTC).</param>
    /// <param name="longitude">Observer geographic longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer geographic latitude in degrees (north positive, clamped to ±89.9°).</param>
    /// <param name="system">House division system to use.</param>
    /// <returns>
    /// A <see cref="HouseCusps"/> record with all twelve cusps, four Angles, and the system identifier.
    /// </returns>
    /// <exception cref="NotSupportedException">
    /// Thrown for Koch, Campanus, and Regiomontanus systems which are not yet implemented.
    /// </exception>
    public static HouseCusps Calculate(double jd, double longitude, double latitude, HouseSystem system)
    {
        double T = TimeUtils.JulianCentury(jd);
        double obliquity = ObliquityOfEcliptic(T);
        double ramc = TimeUtils.NormalizeDegrees(TimeUtils.GMST(jd) + longitude);

        double mc  = ComputeMC(ramc, obliquity);
        double asc = ComputeAscendant(ramc, obliquity, latitude);
        double ic  = TimeUtils.NormalizeDegrees(mc + 180.0);
        double dsc = TimeUtils.NormalizeDegrees(asc + 180.0);

        double[] cusps = system switch
        {
            HouseSystem.Placidus      => ComputePlacidus(ramc, mc, asc, obliquity, latitude),
            HouseSystem.Equal         => ComputeEqual(asc),
            HouseSystem.WholeSigns    => ComputeWholeSigns(asc),
            HouseSystem.Porphyry      => ComputePorphyry(mc, asc, ic, dsc),
            HouseSystem.Koch          => throw new NotSupportedException("Koch house system is not yet implemented."),
            HouseSystem.Campanus      => throw new NotSupportedException("Campanus house system is not yet implemented."),
            HouseSystem.Regiomontanus => throw new NotSupportedException("Regiomontanus house system is not yet implemented."),
            _                         => throw new ArgumentOutOfRangeException(nameof(system), system, null),
        };

        return new HouseCusps(asc, mc, dsc, ic, cusps, system);
    }

    /// <summary>
    /// Returns the mean obliquity of the ecliptic for the given Julian century.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>Mean obliquity of ecliptic in degrees.</returns>
    /// <remarks>
    /// Meeus Ch. 22, Eq. 22.2 (IAU 1980 formula, two-term approximation):
    /// <code>ε = 23.439291° − 0.013004° · T</code>
    /// Consistent with the formula used in <c>PlanetEphemeris</c> and <c>SunEphemeris</c>.
    /// </remarks>
    internal static double ObliquityOfEcliptic(double T) => 23.439291 - (0.0130042 * T);

    /// <summary>
    /// Computes the Midheaven (MC) ecliptic longitude.
    /// </summary>
    /// <param name="ramcDeg">Right Ascension of the Midheaven Circle (RAMC) in degrees [0, 360).</param>
    /// <param name="obliquityDeg">Mean obliquity of the ecliptic in degrees.</param>
    /// <returns>MC ecliptic longitude in degrees [0, 360).</returns>
    /// <remarks>
    /// The Midheaven is the intersection of the ecliptic with the upper meridian:
    /// <code>MC = atan2(sin(RAMC), cos(RAMC) · cos(ε))</code>
    /// </remarks>
    internal static double ComputeMC(double ramcDeg, double obliquityDeg)
    {
        double ramcRad = TimeUtils.ToRadians(ramcDeg);
        double epsRad  = TimeUtils.ToRadians(obliquityDeg);
        double mc = TimeUtils.ToDegrees(
            Math.Atan2(Math.Sin(ramcRad), Math.Cos(ramcRad) * Math.Cos(epsRad)));
        return TimeUtils.NormalizeDegrees(mc);
    }

    /// <summary>
    /// Computes the Ascendant ecliptic longitude.
    /// </summary>
    /// <param name="ramcDeg">Right Ascension of the Midheaven Circle (RAMC) in degrees [0, 360).</param>
    /// <param name="obliquityDeg">Mean obliquity of the ecliptic in degrees.</param>
    /// <param name="latitudeDeg">Observer geographic latitude in degrees (north positive).</param>
    /// <returns>Ascendant ecliptic longitude in degrees [0, 360).</returns>
    /// <remarks>
    /// The Ascendant is the ecliptic degree crossing the eastern horizon:
    /// <code>
    /// ASC = atan2(−cos(RAMC), sin(RAMC) · cos(ε) + tan(φ) · sin(ε))
    /// </code>
    /// Geographic poles (|φ| = 90°) produce a singularity; latitude is clamped to ±89.9°.
    /// </remarks>
    internal static double ComputeAscendant(double ramcDeg, double obliquityDeg, double latitudeDeg)
    {
        double ramcRad = TimeUtils.ToRadians(ramcDeg);
        double epsRad  = TimeUtils.ToRadians(obliquityDeg);
        // Clamp to avoid tan(φ) singularity at poles
        double phiRad  = TimeUtils.ToRadians(Math.Clamp(latitudeDeg, -89.9, 89.9));

        double asc = TimeUtils.ToDegrees(Math.Atan2(
            -Math.Cos(ramcRad),
            (Math.Sin(ramcRad) * Math.Cos(epsRad)) + (Math.Tan(phiRad) * Math.Sin(epsRad))));
        return TimeUtils.NormalizeDegrees(asc);
    }

    // ── Equal House ──────────────────────────────────────────────────────────────
    // Each house is exactly 30° wide, starting from the Ascendant (house 1 cusp).
    private static double[] ComputeEqual(double ascDeg)
    {
        double[] cusps = new double[12];
        for (int i = 0; i < 12; i++)
            cusps[i] = TimeUtils.NormalizeDegrees(ascDeg + (i * 30.0));
        return cusps;
    }

    // ── Whole Signs ───────────────────────────────────────────────────────────────
    // House 1 begins at the 0° boundary of the zodiac sign that contains the Ascendant.
    private static double[] ComputeWholeSigns(double ascDeg)
    {
        double house1Start = Math.Floor(ascDeg / 30.0) * 30.0;
        double[] cusps = new double[12];
        for (int i = 0; i < 12; i++)
            cusps[i] = TimeUtils.NormalizeDegrees(house1Start + (i * 30.0));
        return cusps;
    }

    // ── Porphyry ─────────────────────────────────────────────────────────────────
    // Each of the four quadrants (defined by MC, ASC, IC, DSC) is trisected into equal arcs.
    // Quadrant arcs go forward (increasing ecliptic longitude):
    //   MC → ASC, ASC → IC, IC → DSC, DSC → MC (wrapping)
    private static double[] ComputePorphyry(double mc, double asc, double ic, double dsc)
    {
        double arcMcAsc = ArcForward(mc, asc);   // H11, H12 are inside this quadrant
        double arcAscIc = ArcForward(asc, ic);   // H2,  H3  are inside this quadrant
        double arcIcDsc = ArcForward(ic, dsc);   // H5,  H6  are inside this quadrant
        double arcDscMc = ArcForward(dsc, mc);   // H8,  H9  are inside this quadrant

        double[] cusps = new double[12];
        cusps[0]  = asc;
        cusps[1]  = TimeUtils.NormalizeDegrees(asc + (arcAscIc / 3.0));
        cusps[2]  = TimeUtils.NormalizeDegrees(asc + (arcAscIc * 2.0 / 3.0));
        cusps[3]  = ic;
        cusps[4]  = TimeUtils.NormalizeDegrees(ic + (arcIcDsc / 3.0));
        cusps[5]  = TimeUtils.NormalizeDegrees(ic + (arcIcDsc * 2.0 / 3.0));
        cusps[6]  = dsc;
        cusps[7]  = TimeUtils.NormalizeDegrees(dsc + (arcDscMc / 3.0));
        cusps[8]  = TimeUtils.NormalizeDegrees(dsc + (arcDscMc * 2.0 / 3.0));
        cusps[9]  = mc;
        cusps[10] = TimeUtils.NormalizeDegrees(mc + (arcMcAsc / 3.0));
        cusps[11] = TimeUtils.NormalizeDegrees(mc + (arcMcAsc * 2.0 / 3.0));
        return cusps;
    }

    // Returns the forward ecliptic arc from angle a to angle b (going in increasing longitude, mod 360°).
    private static double ArcForward(double a, double b)
    {
        double arc = b - a;
        if (arc < 0)
            arc += 360.0;
        return arc;
    }

    // ── Placidus ──────────────────────────────────────────────────────────────────
    // Semi-arc-based system: locate each intermediate cusp by iterating the ecliptic longitude
    // that satisfies the oblique-ascension condition for that house fraction.
    private static double[] ComputePlacidus(double ramcDeg, double mc, double asc, double obliquityDeg, double latitudeDeg)
    {
        double ic  = TimeUtils.NormalizeDegrees(mc + 180.0);
        double dsc = TimeUtils.NormalizeDegrees(asc + 180.0);

        // Upper semi-arc houses (between MC and ASC): target OASC offsets of 60° and 120° from RAMC.
        double h11 = PlacidusIntermediate(ramcDeg + 60.0,  obliquityDeg, latitudeDeg);
        double h12 = PlacidusIntermediate(ramcDeg + 120.0, obliquityDeg, latitudeDeg);

        // Lower semi-arc houses (between ASC and IC): target OASC offsets of 240° and 300° from RAMC.
        double h2  = PlacidusIntermediate(ramcDeg + 240.0, obliquityDeg, latitudeDeg);
        double h3  = PlacidusIntermediate(ramcDeg + 300.0, obliquityDeg, latitudeDeg);

        // Opposite cusps are always exactly 180° away.
        double h5 = TimeUtils.NormalizeDegrees(h11 + 180.0);
        double h6 = TimeUtils.NormalizeDegrees(h12 + 180.0);
        double h8 = TimeUtils.NormalizeDegrees(h2 + 180.0);
        double h9 = TimeUtils.NormalizeDegrees(h3 + 180.0);

        return [asc, h2, h3, ic, h5, h6, dsc, h8, h9, mc, h11, h12];
    }

    /// <summary>
    /// Iterative solver for a Placidus intermediate house cusp given a target Oblique Ascension Under the Pole.
    /// </summary>
    /// <param name="targetOasc">Target oblique ascension (RAMC + fractional-arc offset) in degrees.</param>
    /// <param name="obliquityDeg">Obliquity of ecliptic in degrees.</param>
    /// <param name="latitudeDeg">Geographic latitude in degrees.</param>
    /// <returns>Ecliptic longitude of the intermediate cusp in degrees [0, 360).</returns>
    /// <remarks>
    /// Algorithm iterates the ecliptic longitude λ until the oblique ascension of the
    /// corresponding ecliptic point matches <paramref name="targetOasc"/>:
    /// <code>
    ///   Dec   = arcsin(sin(λ) · sin(ε))
    ///   AD    = arcsin(clamp(tan(φ) · tan(Dec), −1, 1))   [Ascensional Difference]
    ///   RA    = targetOasc + AD
    ///   λ_new = atan2(sin(RA)·cos(ε) + tan(Dec)·sin(ε), cos(RA))
    /// </code>
    /// Convergence is typically reached within 3–5 iterations.
    /// Falls back to the Equal-house value when |tan(φ)·tan(Dec)| > 1 (circumpolar condition
    /// for latitudes above about 66°).
    /// </remarks>
    internal static double PlacidusIntermediate(double targetOasc, double obliquityDeg, double latitudeDeg)
    {
        double epsRad    = TimeUtils.ToRadians(obliquityDeg);
        double phiRad    = TimeUtils.ToRadians(Math.Clamp(latitudeDeg, -89.9, 89.9));
        double targetRad = TimeUtils.ToRadians(TimeUtils.NormalizeDegrees(targetOasc));

        double lambda = targetRad; // initial guess: Equal-house offset

        for (int iter = 0; iter < 20; iter++)
        {
            double decRad = Math.Asin(Math.Clamp(Math.Sin(lambda) * Math.Sin(epsRad), -1.0, 1.0));

            // Ascensional Difference: arcsin(tan(φ)·tan(Dec)).
            // |arg| > 1 means the degree is circumpolar at this latitude — fall back to initial guess.
            double adArg = Math.Tan(phiRad) * Math.Tan(decRad);
            if (Math.Abs(adArg) > 1.0)
                return TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(targetRad));

            double adRad = Math.Asin(adArg);

            double raRad = targetRad + adRad;
            double lambdaNew = Math.Atan2(
                (Math.Sin(raRad) * Math.Cos(epsRad)) + (Math.Tan(decRad) * Math.Sin(epsRad)),
                Math.Cos(raRad));

            lambdaNew = TimeUtils.ToRadians(
                TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(lambdaNew)));

            if (Math.Abs(lambdaNew - lambda) < 1e-6)
            {
                lambda = lambdaNew;
                break;
            }

            lambda = lambdaNew;
        }

        return TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(lambda));
    }
}
