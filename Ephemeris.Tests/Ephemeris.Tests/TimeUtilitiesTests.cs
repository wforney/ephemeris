using Ephemeris.Chronology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Unit tests for <see cref="TimeUtils"/>.
/// Validates Julian Day conversion, GMST, ΔT, angle normalization, and radian/degree helpers.
/// Reference: Meeus, <em>Astronomical Algorithms</em> (2nd ed.) Ch. 7 examples.
/// </summary>
public class TimeUtilitiesTests
{
    // ── Julian Day ────────────────────────────────────────────────────────

    [Test]
    public async Task JulianDay_AtJ2000Noon_Returns2451545()
    {
        // Meeus p. 61: JD of J2000.0 = 2000-Jan-01.5 = 2451545.0
        double jd = TimeUtils.JulianDay(2000, 1, 1, 12.0);
        await Assert.That(Math.Abs(jd - 2451545.0)).IsLessThan(0.0001);
    }

    [Test]
    public async Task JulianDay_AtJ2000Midnight_Returns2451544Point5()
    {
        double jd = TimeUtils.JulianDay(2000, 1, 1, 0.0);
        await Assert.That(Math.Abs(jd - 2451544.5)).IsLessThan(0.0001);
    }

    [Test]
    public async Task JulianDay_NoonVsMidnight_DiffersByHalf()
    {
        double jdNoon     = TimeUtils.JulianDay(2024, 6, 21, 12.0);
        double jdMidnight = TimeUtils.JulianDay(2024, 6, 21, 0.0);
        await Assert.That(Math.Abs(jdNoon - jdMidnight - 0.5)).IsLessThan(0.0001);
    }

    [Test]
    public async Task JulianDay_MeeusExample_IsCorrect()
    {
        // Meeus Ex. 7.a: 1957-Oct-04.81 → JD = 2436116.31
        double jd = TimeUtils.JulianDay(1957, 10, 4, 0.81 * 24.0);
        await Assert.That(Math.Abs(jd - 2436116.31)).IsLessThan(0.01);
    }

    [Test]
    public async Task JulianDay_ConsecutiveDays_DifferByOne()
    {
        double jd1 = TimeUtils.JulianDay(2024, 3, 20, 12.0);
        double jd2 = TimeUtils.JulianDay(2024, 3, 21, 12.0);
        await Assert.That(Math.Abs(jd2 - jd1 - 1.0)).IsLessThan(0.0001);
    }

    [Test]
    public async Task JulianDay_NewYear1900_IsPositive()
    {
        double jd = TimeUtils.JulianDay(1900, 1, 1, 0.0);
        await Assert.That(jd).IsGreaterThan(2400000.0);
    }

    // ── Julian Century ────────────────────────────────────────────────────

    [Test]
    public async Task JulianCentury_AtJ2000_ReturnsZero()
    {
        double T = TimeUtils.JulianCentury(2451545.0);
        await Assert.That(Math.Abs(T)).IsLessThan(1e-7);
    }

    [Test]
    public async Task JulianCentury_OneCenturyAfterJ2000_ReturnsOne()
    {
        double T = TimeUtils.JulianCentury(2451545.0 + 36525.0);
        await Assert.That(Math.Abs(T - 1.0)).IsLessThan(1e-7);
    }

    [Test]
    public async Task JulianCentury_OneCenturyBeforeJ2000_ReturnsMinusOne()
    {
        double T = TimeUtils.JulianCentury(2451545.0 - 36525.0);
        await Assert.That(Math.Abs(T + 1.0)).IsLessThan(1e-7);
    }

    // ── GMST ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GMST_AtJ2000_IsNear280Point46Degrees()
    {
        // At T=0 all higher-order terms vanish: GMST = 280.46061837°
        double gmst = TimeUtils.GMST(2451545.0);
        await Assert.That(Math.Abs(gmst - 280.4606)).IsLessThan(0.001);
    }

    [Test]
    public async Task GMST_IsAlwaysInRangeZeroTo360()
    {
        double gmst1 = TimeUtils.GMST(2451545.0);
        double gmst2 = TimeUtils.GMST(2451545.0 + 365.25);
        double gmst3 = TimeUtils.GMST(2451545.0 - 100.0);
        await Assert.That(gmst1).IsGreaterThan(0.0);
        await Assert.That(gmst1).IsLessThan(360.0);
        await Assert.That(gmst2).IsGreaterThan(0.0);
        await Assert.That(gmst2).IsLessThan(360.0);
        await Assert.That(gmst3).IsGreaterThan(0.0);
        await Assert.That(gmst3).IsLessThan(360.0);
    }

    [Test]
    public async Task GMST_AdvancesWithSiderealRate()
    {
        // One sidereal day ≈ 23h 56m 4.1s ≈ 0.99726958 solar days
        // GMST increases by ~360° per sidereal day
        double jd = 2451545.0;
        double siderealDay = 0.99726958;
        double gmst1 = TimeUtils.GMST(jd);
        double gmst2 = TimeUtils.GMST(jd + siderealDay);
        double diff = TimeUtils.NormalizeDegrees(gmst2 - gmst1);
        // Should have advanced by nearly one full revolution (close to 0° after normalisation)
        await Assert.That(diff).IsLessThan(1.0);
    }

    // ── ΔT ────────────────────────────────────────────────────────────────

    [Test]
    public async Task DeltaT_Year2000_IsApproximately64Point7Seconds()
    {
        // Modern era (≥ 2000): ΔT = 64.7 + 0.293*(year−2000) → at 2000 = 64.7 s
        double dt = TimeUtils.DeltaT(2000.0);
        await Assert.That(Math.Abs(dt - 64.7)).IsLessThan(0.01);
    }

    [Test]
    public async Task DeltaT_Year2024_IsGreaterThanYear2000()
    {
        // ΔT grows linearly in modern era
        double dt2000 = TimeUtils.DeltaT(2000.0);
        double dt2024 = TimeUtils.DeltaT(2024.0);
        await Assert.That(dt2024).IsGreaterThan(dt2000);
        await Assert.That(dt2024).IsLessThan(90.0);
    }

    [Test]
    public async Task DeltaT_HistoricalEra_IsPositive()
    {
        // Year 1800 (1600 ≤ year < 2000 branch)
        double dt = TimeUtils.DeltaT(1800.0);
        await Assert.That(dt).IsGreaterThan(0.0);
    }

    [Test]
    public async Task DeltaT_AncientEra_IsLarge()
    {
        // Year 500 CE (< 948 branch): polynomial gives thousands of seconds
        double dt = TimeUtils.DeltaT(500.0);
        await Assert.That(dt).IsGreaterThan(1000.0);
    }

    // ── NormalizeDegrees ─────────────────────────────────────────────────

    [Test]
    public async Task NormalizeDegrees_ZeroDegrees_ReturnsZero()
    {
        double result = TimeUtils.NormalizeDegrees(0.0);
        await Assert.That(result).IsEqualTo(0.0);
    }

    [Test]
    public async Task NormalizeDegrees_Exactly360_ReturnsZero()
    {
        double result = TimeUtils.NormalizeDegrees(360.0);
        await Assert.That(result).IsLessThan(0.0001);
    }

    [Test]
    public async Task NormalizeDegrees_NegativeAngle_WrapsToPositive()
    {
        // −90° → 270°
        double result = TimeUtils.NormalizeDegrees(-90.0);
        await Assert.That(Math.Abs(result - 270.0)).IsLessThan(0.0001);
    }

    [Test]
    public async Task NormalizeDegrees_LargePositiveAngle_WrapsDown()
    {
        // 720° → 0°
        double result = TimeUtils.NormalizeDegrees(720.0);
        await Assert.That(result).IsLessThan(0.0001);
    }

    [Test]
    public async Task NormalizeDegrees_NegativeMultipleOf360_WrapsCorrectly()
    {
        // −450° = −90° after one wrap → 270°
        double result = TimeUtils.NormalizeDegrees(-450.0);
        await Assert.That(Math.Abs(result - 270.0)).IsLessThan(0.0001);
    }

    [Test]
    public async Task NormalizeDegrees_180Degrees_Unchanged()
    {
        double result = TimeUtils.NormalizeDegrees(180.0);
        await Assert.That(Math.Abs(result - 180.0)).IsLessThan(0.0001);
    }

    // ── ToRadians / ToDegrees ─────────────────────────────────────────────

    [Test]
    public async Task ToRadians_90Degrees_IsHalfPi()
    {
        double rad = TimeUtils.ToRadians(90.0);
        await Assert.That(Math.Abs(rad - Math.PI / 2.0)).IsLessThan(1e-10);
    }

    [Test]
    public async Task ToDegrees_HalfPi_Is90Degrees()
    {
        double deg = TimeUtils.ToDegrees(Math.PI / 2.0);
        await Assert.That(Math.Abs(deg - 90.0)).IsLessThan(1e-9);
    }

    [Test]
    public async Task ToRadians_ToDegrees_RoundTrip()
    {
        double original = 137.5;
        double result = TimeUtils.ToDegrees(TimeUtils.ToRadians(original));
        await Assert.That(Math.Abs(result - original)).IsLessThan(1e-10);
    }

    [Test]
    public async Task ToDegrees_360_Is2Pi()
    {
        double rad = TimeUtils.ToRadians(360.0);
        await Assert.That(Math.Abs(rad - 2.0 * Math.PI)).IsLessThan(1e-10);
    }
}
