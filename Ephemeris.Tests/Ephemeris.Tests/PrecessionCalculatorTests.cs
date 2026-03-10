using Ephemeris.Geodesy;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Unit tests for <see cref="PrecessionCalculator"/>.
/// Validates IAU 2006 precession angles, general precession in longitude, and RA/Dec precession.
/// Reference values from Meeus <em>Astronomical Algorithms</em> Ch. 21.
/// </summary>
public class PrecessionCalculatorTests
{
    // ── PrecessionAngles ──────────────────────────────────────────────────

    [Test]
    public async Task PrecessionAngles_AtZeroInterval_AreAllZero()
    {
        // T=0, t=0 → no time elapsed → no precession
        var (zetaA, zA, thetaA) = PrecessionCalculator.PrecessionAngles(0.0, 0.0);
        await Assert.That(Math.Abs(zetaA)).IsLessThan(0.0001);
        await Assert.That(Math.Abs(zA)).IsLessThan(0.0001);
        await Assert.That(Math.Abs(thetaA)).IsLessThan(0.0001);
    }

    [Test]
    public async Task PrecessionAngles_OneCentury_ZetaA_CorrectMagnitude()
    {
        // T=0, t=1: ζ_A = (2306.2181 + 0.30188 + 0.017998) / 3600 ≈ 0.64071°
        var (zetaA, _, _) = PrecessionCalculator.PrecessionAngles(0.0, 1.0);
        await Assert.That(Math.Abs(zetaA - 0.6407)).IsLessThan(0.001);
    }

    [Test]
    public async Task PrecessionAngles_OneCentury_zA_CorrectMagnitude()
    {
        // T=0, t=1: z_A = (2306.2181 + 1.09468 + 0.018203) / 3600 ≈ 0.64093°
        var (_, zA, _) = PrecessionCalculator.PrecessionAngles(0.0, 1.0);
        await Assert.That(Math.Abs(zA - 0.6409)).IsLessThan(0.001);
    }

    [Test]
    public async Task PrecessionAngles_OneCentury_ThetaA_CorrectMagnitude()
    {
        // T=0, t=1: θ_A = (2004.3109 − 0.42665 − 0.041775) / 3600 ≈ 0.55662°
        var (_, _, thetaA) = PrecessionCalculator.PrecessionAngles(0.0, 1.0);
        await Assert.That(Math.Abs(thetaA - 0.5566)).IsLessThan(0.001);
    }

    [Test]
    public async Task PrecessionAngles_AnglesMagnitudeIncreasesWithTime()
    {
        var (z1, _, th1) = PrecessionCalculator.PrecessionAngles(0.0, 0.5);
        var (z2, _, th2) = PrecessionCalculator.PrecessionAngles(0.0, 1.0);
        await Assert.That(z2).IsGreaterThan(z1);
        await Assert.That(th2).IsGreaterThan(th1);
    }

    // ── GeneralPrecessionInLongitude ──────────────────────────────────────

    [Test]
    public async Task GeneralPrecessionInLongitude_AtJ2000_IsZero()
    {
        double psi = PrecessionCalculator.GeneralPrecessionInLongitude(0.0);
        await Assert.That(Math.Abs(psi)).IsLessThan(0.0001);
    }

    [Test]
    public async Task GeneralPrecessionInLongitude_OneCentury_IsAbout5030Arcseconds()
    {
        // ψ_A ≈ 5029.097 + 1.559 ≈ 5030.66 arcseconds at T=1
        double psi = PrecessionCalculator.GeneralPrecessionInLongitude(1.0);
        await Assert.That(Math.Abs(psi - 5030.0)).IsLessThan(5.0);
    }

    [Test]
    public async Task GeneralPrecessionInLongitude_IsPositiveGoingForward()
    {
        double psi = PrecessionCalculator.GeneralPrecessionInLongitude(0.5);
        await Assert.That(psi).IsGreaterThan(0.0);
    }

    // ── PrecessFromJ2000 ──────────────────────────────────────────────────

    [Test]
    public async Task PrecessFromJ2000_AtT0_ReturnsOriginalCoordinates()
    {
        // At T=0 (no elapsed time) the precession rotation is the identity
        double ra = 100.0, dec = 25.0;
        var result = PrecessionCalculator.PrecessFromJ2000(ra, dec, 0.0);
        await Assert.That(Math.Abs(result.RightAscension - ra)).IsLessThan(0.001);
        await Assert.That(Math.Abs(result.Declination - dec)).IsLessThan(0.001);
    }

    [Test]
    public async Task PrecessFromJ2000_OneCentury_RAChangesNoticeably()
    {
        // One century of precession should shift RA by a measurable amount (several tenths of a degree)
        double ra = 100.0, dec = 25.0;
        var j2000 = PrecessionCalculator.PrecessFromJ2000(ra, dec, 0.0);
        var prec1 = PrecessionCalculator.PrecessFromJ2000(ra, dec, 1.0);
        double raShift = Math.Abs(prec1.RightAscension - j2000.RightAscension);
        await Assert.That(raShift).IsGreaterThan(0.1);
    }

    [Test]
    public async Task PrecessFromJ2000_DeclinationStaysWithinBounds()
    {
        // Precession never produces unphysical declination values
        var result = PrecessionCalculator.PrecessFromJ2000(100.0, 25.0, 1.0);
        await Assert.That(result.Declination).IsGreaterThan(-90.0);
        await Assert.That(result.Declination).IsLessThan(90.0);
    }

    [Test]
    public async Task PrecessFromJ2000_RAStaysInRange()
    {
        // Precessed RA should remain in [0, 360)
        var result = PrecessionCalculator.PrecessFromJ2000(350.0, 10.0, 1.0);
        await Assert.That(result.RightAscension).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(result.RightAscension).IsLessThan(360.0);
    }

    [Test]
    public async Task PrecessFromJ2000_PolarStar_DeclinationNearNinety()
    {
        // Polaris (RA≈37.95°, Dec≈89.26° at J2000) precesses but stays near the pole for 1 century
        var result = PrecessionCalculator.PrecessFromJ2000(37.95, 89.26, 1.0);
        await Assert.That(result.Declination).IsGreaterThan(80.0);
    }
}
