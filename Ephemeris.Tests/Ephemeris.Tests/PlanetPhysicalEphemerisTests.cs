using Ephemeris.Planetology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Unit tests for <see cref="PlanetPhysicalEphemeris"/>.
/// Validates apparent magnitude, angular diameter, and elongation calculations.
/// Magnitude reference ranges from Meeus <em>Astronomical Algorithms</em> Appendix I.
/// </summary>
public class PlanetPhysicalEphemerisTests
{
    // ── ApparentMagnitude — realistic range checks ────────────────────────

    [Test]
    public async Task ApparentMagnitude_Jupiter_AtOpposition_IsReasonable()
    {
        // Jupiter near opposition: r≈5.2 AU, delta≈4.2 AU, phase≈0°
        // Expected ≈ -9.40 + 5·log10(5.2·4.2) ≈ −2.7 mag
        double mag = PlanetPhysicalEphemeris.ApparentMagnitude("Jupiter", 5.2, 4.2, 0.0);
        await Assert.That(mag).IsGreaterThan(-3.5);
        await Assert.That(mag).IsLessThan(-1.5);
    }

    [Test]
    public async Task ApparentMagnitude_Venus_AtGreatestElongation_IsReasonable()
    {
        // Venus at r=0.72 AU, delta=1.0 AU, phase≈0° (simplified)
        // Expected ≈ -4.34 + 5·log10(0.72) ≈ −5.0 mag
        double mag = PlanetPhysicalEphemeris.ApparentMagnitude("Venus", 0.72, 1.0, 0.0);
        await Assert.That(mag).IsGreaterThan(-6.0);
        await Assert.That(mag).IsLessThan(-3.5);
    }

    [Test]
    public async Task ApparentMagnitude_Mars_AtOpposition_IsReasonable()
    {
        // Mars near opposition: r≈1.38 AU, delta≈0.38 AU, phase≈0°
        double mag = PlanetPhysicalEphemeris.ApparentMagnitude("Mars", 1.38, 0.38, 0.0);
        await Assert.That(mag).IsGreaterThan(-3.0);
        await Assert.That(mag).IsLessThan(0.5);
    }

    [Test]
    public async Task ApparentMagnitude_Saturn_AtMeanDistance_IsReasonable()
    {
        // Saturn: r≈9.5 AU, delta≈8.5 AU, phase≈0°
        double mag = PlanetPhysicalEphemeris.ApparentMagnitude("Saturn", 9.5, 8.5, 0.0);
        await Assert.That(mag).IsGreaterThan(-1.5);
        await Assert.That(mag).IsLessThan(2.0);
    }

    [Test]
    public async Task ApparentMagnitude_Mercury_AtInferiorConjunction_IsReasonable()
    {
        // Mercury at r≈0.39 AU, delta≈0.61 AU, small phase
        double mag = PlanetPhysicalEphemeris.ApparentMagnitude("Mercury", 0.39, 0.61, 10.0);
        await Assert.That(mag).IsGreaterThan(-4.0);
        await Assert.That(mag).IsLessThan(1.0);
    }

    // ── ApparentMagnitude — monotonicity ──────────────────────────────────

    [Test]
    public async Task ApparentMagnitude_IncreasesWithGeocentricDistance()
    {
        // Farther from Earth → fainter (higher magnitude number)
        double near = PlanetPhysicalEphemeris.ApparentMagnitude("Jupiter", 5.2, 4.0, 0.0);
        double far  = PlanetPhysicalEphemeris.ApparentMagnitude("Jupiter", 5.2, 6.0, 0.0);
        await Assert.That(far).IsGreaterThan(near);
    }

    [Test]
    public async Task ApparentMagnitude_IncreasesWithPhaseAngle()
    {
        // Larger phase angle → less illuminated face → fainter (for Mars with positive coefficient)
        double small = PlanetPhysicalEphemeris.ApparentMagnitude("Mars", 1.5, 1.0,  10.0);
        double large = PlanetPhysicalEphemeris.ApparentMagnitude("Mars", 1.5, 1.0, 100.0);
        await Assert.That(large).IsGreaterThan(small);
    }

    // ── ApparentMagnitude — error handling ───────────────────────────────

    [Test]
    public async Task ApparentMagnitude_UnknownPlanet_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await Task.Run(() =>
                PlanetPhysicalEphemeris.ApparentMagnitude("Nibiru", 1.0, 1.0, 0.0)));
    }

    // ── AngularDiameter ───────────────────────────────────────────────────

    [Test]
    public async Task AngularDiameter_Jupiter_AtTypicalDistance_IsCorrect()
    {
        // Jupiter at delta=4.2 AU → 196.94 / 4.2 ≈ 46.9 arcsec
        double diam = PlanetPhysicalEphemeris.AngularDiameter("Jupiter", 4.2);
        await Assert.That(Math.Abs(diam - 46.9)).IsLessThan(0.5);
    }

    [Test]
    public async Task AngularDiameter_DecreasesWithDistance()
    {
        double near = PlanetPhysicalEphemeris.AngularDiameter("Jupiter", 4.0);
        double far  = PlanetPhysicalEphemeris.AngularDiameter("Jupiter", 6.0);
        await Assert.That(far).IsLessThan(near);
    }

    [Test]
    public async Task AngularDiameter_JupiterLargerThanMarsAtSameDistance()
    {
        // Jupiter's equatorial diameter coefficient (196.94) >> Mars (9.36)
        double jup = PlanetPhysicalEphemeris.AngularDiameter("Jupiter", 5.0);
        double mar = PlanetPhysicalEphemeris.AngularDiameter("Mars", 5.0);
        await Assert.That(jup).IsGreaterThan(mar);
    }

    [Test]
    public async Task AngularDiameter_IsPositive()
    {
        double diam = PlanetPhysicalEphemeris.AngularDiameter("Saturn", 9.0);
        await Assert.That(diam).IsGreaterThan(0.0);
    }

    [Test]
    public async Task AngularDiameter_UnknownPlanet_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await Task.Run(() =>
                PlanetPhysicalEphemeris.AngularDiameter("Nibiru", 1.0)));
    }

    // ── Elongation ────────────────────────────────────────────────────────

    [Test]
    public async Task Elongation_SamePosition_IsZero()
    {
        double sep = PlanetPhysicalEphemeris.Elongation(100.0, 25.0, 100.0, 25.0);
        await Assert.That(sep).IsEqualTo(0.0);
    }

    [Test]
    public async Task Elongation_AntipodalOnEquator_Is180()
    {
        // (180°, 0°) vs (0°, 0°) → 180° separation on the equator
        double sep = PlanetPhysicalEphemeris.Elongation(180.0, 0.0, 0.0, 0.0);
        await Assert.That(Math.Abs(sep - 180.0)).IsLessThan(0.001);
    }

    [Test]
    public async Task Elongation_QuarterCircle_Is90Degrees()
    {
        // (90°, 0°) vs (0°, 0°) on the equator → 90°
        double sep = PlanetPhysicalEphemeris.Elongation(90.0, 0.0, 0.0, 0.0);
        await Assert.That(Math.Abs(sep - 90.0)).IsLessThan(0.001);
    }

    [Test]
    public async Task Elongation_AlwaysNonNegative()
    {
        double sep = PlanetPhysicalEphemeris.Elongation(45.0, 30.0, 200.0, -10.0);
        await Assert.That(sep).IsGreaterThan(0.0);
    }

    [Test]
    public async Task Elongation_NeverExceeds180()
    {
        double sep = PlanetPhysicalEphemeris.Elongation(45.0, 30.0, 200.0, -10.0);
        await Assert.That(sep).IsLessThanOrEqualTo(180.0);
    }
}
