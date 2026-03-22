// Updated: 2026-03-22
using Ephemeris.Chronology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Unit tests for <see cref="ProlepticDate"/>.
/// Reference values computed from Meeus, <em>Astronomical Algorithms</em> (2nd ed.), Ch. 7.
/// </summary>
public class ProlepticDateTests
{
    // ── Julian Day round-trip ────────────────────────────────────────────

    [Test]
    public async Task ToJulianDay_J2000Noon_Returns2451545()
    {
        // J2000.0 = 2000-Jan-01 12:00 TT → JD 2451545.0 (Meeus p.62)
        var d = new ProlepticDate(2000, 1, 1, 12.0);
        await Assert.That(Math.Abs(d.ToJulianDay() - 2451545.0)).IsLessThanOrEqualTo(0.001);
    }

    [Test]
    public async Task ToJulianDay_MeeusExample7a_Correct()
    {
        // Meeus Example 7.a: 1957-Oct-4.81 → JD 2436116.31
        var d = new ProlepticDate(1957, 10, 4, 0.81 * 24.0);
        await Assert.That(Math.Abs(d.ToJulianDay() - 2436116.31)).IsLessThanOrEqualTo(0.01);
    }

    [Test]
    public async Task ToJulianDay_MeeusExample7b_Correct()
    {
        // Meeus Example 7.b: 333-Jan-27.5 → JD 1842713.0
        var d = new ProlepticDate(333, 1, 27, 12.0);
        await Assert.That(Math.Abs(d.ToJulianDay() - 1842713.0)).IsLessThanOrEqualTo(1.0);
    }

    [Test]
    public async Task ToJulianDay_Bc701Aug1_IsInThePast()
    {
        // 701 BCE Aug 1 (Hezekiah scenario) should be a plausible historical JD
        var d = ProlepticDate.FromBce(701, 8, 1);
        double jd = d.ToJulianDay();
        // Roughly: 701 BCE ≈ JD 1503000; must be less than J2000 and greater than 0
        await Assert.That(jd).IsGreaterThan(0.0);
        await Assert.That(jd).IsLessThan(2451545.0);
    }

    [Test]
    public async Task ToJulianDay_Bc1406Jun21_IsEarlierThanBc701()
    {
        // 1406 BCE must be earlier (smaller JD) than 701 BCE
        var d1406 = ProlepticDate.FromBce(1406, 6, 21);
        var d701  = ProlepticDate.FromBce(701,  8,  1);
        await Assert.That(d1406.ToJulianDay()).IsLessThan(d701.ToJulianDay());
    }

    // ── FromJulianDay round-trip ─────────────────────────────────────────

    [Test]
    public async Task FromJulianDay_J2000_ReturnsYear2000Jan1()
    {
        var d = ProlepticDate.FromJulianDay(2451545.0);
        await Assert.That(d.Year).IsEqualTo(2000);
        await Assert.That(d.Month).IsEqualTo(1);
        await Assert.That(d.Day).IsEqualTo(1);
    }

    [Test]
    public async Task RoundTrip_ModernDate_IsReversible()
    {
        var original = new ProlepticDate(2024, 6, 21, 12.0);
        double jd     = original.ToJulianDay();
        var recovered = ProlepticDate.FromJulianDay(jd);

        await Assert.That(recovered.Year).IsEqualTo(2024);
        await Assert.That(recovered.Month).IsEqualTo(6);
        await Assert.That(recovered.Day).IsEqualTo(21);
    }

    [Test]
    public async Task RoundTrip_BceDate_IsReversible()
    {
        var original  = ProlepticDate.FromBce(701, 8, 1);
        double jd      = original.ToJulianDay();
        var recovered  = ProlepticDate.FromJulianDay(jd);

        await Assert.That(recovered.Year).IsEqualTo(original.Year);
        await Assert.That(recovered.Month).IsEqualTo(8);
        await Assert.That(recovered.Day).IsEqualTo(1);
    }

    // ── FromBce factory ──────────────────────────────────────────────────

    [Test]
    public async Task FromBce_1Bce_YieldsYear0()
    {
        var d = ProlepticDate.FromBce(1, 1, 1);
        await Assert.That(d.Year).IsEqualTo(0);
    }

    [Test]
    public async Task FromBce_701Bce_YieldsYearMinus700()
    {
        var d = ProlepticDate.FromBce(701, 8, 1);
        await Assert.That(d.Year).IsEqualTo(-700);
    }

    [Test]
    public async Task FromBce_1406Bce_YieldsYearMinus1405()
    {
        var d = ProlepticDate.FromBce(1406, 6, 21);
        await Assert.That(d.Year).IsEqualTo(-1405);
    }

    // ── Formatting ────────────────────────────────────────────────────────

    [Test]
    public async Task ToHistoricalString_BceDate_ContainsBceLabel()
    {
        var d = ProlepticDate.FromBce(701, 8, 1);
        string s = d.ToHistoricalString();
        await Assert.That(s).Contains("BCE");
        await Assert.That(s).Contains("701");
        await Assert.That(s).Contains("Aug");
    }

    [Test]
    public async Task ToHistoricalString_CeDate_ContainsCeLabel()
    {
        var d = new ProlepticDate(2024, 6, 21, 12.0);
        string s = d.ToHistoricalString();
        await Assert.That(s).Contains("CE");
        await Assert.That(s).Contains("2024");
        await Assert.That(s).Contains("Jun");
    }

    [Test]
    public async Task ToAstronomicalString_BceDate_HasNegativeYear()
    {
        var d = ProlepticDate.FromBce(701, 8, 1);
        string s = d.ToAstronomicalString();
        await Assert.That(s.StartsWith("-")).IsTrue();
    }

    // ── Comparison ────────────────────────────────────────────────────────

    [Test]
    public async Task CompareTo_EarlierDate_IsNegative()
    {
        var earlier = ProlepticDate.FromBce(1406, 6, 21);
        var later   = ProlepticDate.FromBce(701,  8,  1);
        await Assert.That(earlier.CompareTo(later)).IsLessThan(0);
    }

    [Test]
    public async Task Equality_SameDate_IsTrue()
    {
        var a = ProlepticDate.FromBce(701, 8, 1);
        var b = ProlepticDate.FromBce(701, 8, 1);
        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task LessThanOperator_EarlierDate_IsTrue()
    {
        var earlier = ProlepticDate.FromBce(1406, 6, 21);
        var later   = ProlepticDate.FromBce(701,  8,  1);
        await Assert.That(earlier < later).IsTrue();
    }

    // ── Validation ────────────────────────────────────────────────────────

    [Test]
    public async Task Constructor_InvalidMonth_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Task.FromResult(new ProlepticDate(2000, 13, 1)));
    }

    [Test]
    public async Task Constructor_InvalidDay_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Task.FromResult(new ProlepticDate(2000, 1, 32)));
    }

    [Test]
    public async Task FromBce_ZeroBceYear_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Task.FromResult(ProlepticDate.FromBce(0, 1, 1)));
    }

    [Test]
    public async Task Constructor_InvalidHour_OutOfRange_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Task.FromResult(new ProlepticDate(2000, 1, 1, 24.0)));
    }

    [Test]
    public async Task Constructor_InvalidHour_Negative_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Task.FromResult(new ProlepticDate(2000, 1, 1, -0.1)));
    }

    [Test]
    public async Task Constructor_InvalidHour_NaN_Throws()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Task.FromResult(new ProlepticDate(2000, 1, 1, double.NaN)));
    }
}
