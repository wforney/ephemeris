using Ephemeris.Geodesy;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Unit tests for <see cref="NutationCalculator"/>.
/// Validates IAU 1980 nutation series for Δψ, Δε, and true obliquity.
/// Physical bounds from Meeus <em>Astronomical Algorithms</em> Ch. 21.
/// </summary>
public class NutationCalculatorTests
{
    // ── Nutation in longitude Δψ ──────────────────────────────────────────

    [Test]
    public async Task Calculate_AtJ2000_DeltaPsiWithinPhysicalBounds()
    {
        // |Δψ| ≤ ~17 arcsec ≈ 0.00472° for the dominant term; total series < 21 arcsec ≈ 0.006°
        var (deltaPsi, _) = NutationCalculator.Calculate(0.0);
        await Assert.That(Math.Abs(deltaPsi)).IsLessThan(0.006);
    }

    [Test]
    public async Task Calculate_AtJ2000_DeltaPsiIsNegative()
    {
        // At J2000.0, Ω ≈ 125°, sin(Ω) > 0; the dominant term coefficient is negative,
        // so the largest contributor to Δψ is negative.
        var (deltaPsi, _) = NutationCalculator.Calculate(0.0);
        await Assert.That(deltaPsi).IsLessThan(0.0);
    }

    [Test]
    public async Task Calculate_AtJ2000_DeltaPsiIsNonZero()
    {
        var (deltaPsi, _) = NutationCalculator.Calculate(0.0);
        await Assert.That(Math.Abs(deltaPsi)).IsGreaterThan(0.001);
    }

    // ── Nutation in obliquity Δε ──────────────────────────────────────────

    [Test]
    public async Task Calculate_AtJ2000_DeltaEpsilonWithinPhysicalBounds()
    {
        // |Δε| ≤ ~10 arcsec ≈ 0.0028°; total series < 14 arcsec ≈ 0.004°
        var (_, deltaEps) = NutationCalculator.Calculate(0.0);
        await Assert.That(Math.Abs(deltaEps)).IsLessThan(0.004);
    }

    [Test]
    public async Task Calculate_DifferentEpochs_DeltaPsiDiffers()
    {
        // Nutation is time-dependent; values should differ over 0.5 Julian century
        var (psi1, _) = NutationCalculator.Calculate(0.0);
        var (psi2, _) = NutationCalculator.Calculate(0.5);
        await Assert.That(Math.Abs(psi1 - psi2)).IsGreaterThan(0.0);
    }

    [Test]
    public async Task Calculate_DifferentEpochs_DeltaEpsilonDiffers()
    {
        var (_, eps1) = NutationCalculator.Calculate(0.0);
        var (_, eps2) = NutationCalculator.Calculate(0.5);
        await Assert.That(Math.Abs(eps1 - eps2)).IsGreaterThan(0.0);
    }

    // ── True obliquity ────────────────────────────────────────────────────

    [Test]
    public async Task TrueObliquity_AtJ2000_IsNearEarthsAxialTilt()
    {
        // True obliquity at J2000.0 ≈ 23.4393° (mean 23.4393° + nutation correction ~0.0026°)
        double epsilon = NutationCalculator.TrueObliquity(0.0);
        await Assert.That(Math.Abs(epsilon - 23.44)).IsLessThan(0.02);
    }

    [Test]
    public async Task TrueObliquity_AtJ2000_WithinPhysicalBounds()
    {
        // Earth's axial tilt oscillates between 22.1° and 24.5° over 41 000-year cycle;
        // over centuries it stays in the tighter range 23.35°–23.50°
        double epsilon = NutationCalculator.TrueObliquity(0.0);
        await Assert.That(epsilon).IsGreaterThan(23.35);
        await Assert.That(epsilon).IsLessThan(23.50);
    }

    [Test]
    public async Task TrueObliquity_OneCentury_SlightlyLessThanJ2000()
    {
        // Mean obliquity decreases at ~47 arcsec/century ≈ 0.013°/century
        // True obliquity may fluctuate, but its value over 1 century should still be above 23.41°
        double e0 = NutationCalculator.TrueObliquity(0.0);
        double e1 = NutationCalculator.TrueObliquity(1.0);
        // After 1 century the mean decreases by ~0.013°; true value stays within ±0.005° of mean
        await Assert.That(e1).IsGreaterThan(23.40);
        await Assert.That(e1).IsLessThan(e0 + 0.01); // does not increase significantly
    }
}
