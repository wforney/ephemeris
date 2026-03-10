using Ephemeris.Chronology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Unit tests for <see cref="TimeZoneUtils"/>.
/// Validates Julian Day ↔ DateTime conversions and timezone offset calculations.
/// </summary>
public class TimeZoneUtilsTests
{
    // ── ToJulianDay ───────────────────────────────────────────────────────

    [Test]
    public async Task ToJulianDay_AtJ2000_Returns2451545()
    {
        var dt = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        await Assert.That(Math.Abs(jd - 2451545.0)).IsLessThan(0.0001);
    }

    [Test]
    public async Task ToJulianDay_J2000Midnight_Returns2451544Point5()
    {
        var dt = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        await Assert.That(Math.Abs(jd - 2451544.5)).IsLessThan(0.0001);
    }

    [Test]
    public async Task ToJulianDay_NoonVsMidnight_DiffersByHalf()
    {
        var noon     = new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        var midnight = new DateTime(2024, 6, 21,  0, 0, 0, DateTimeKind.Utc);
        double diff = TimeZoneUtils.ToJulianDay(noon) - TimeZoneUtils.ToJulianDay(midnight);
        await Assert.That(Math.Abs(diff - 0.5)).IsLessThan(0.0001);
    }

    [Test]
    public async Task ToJulianDay_ConsistentWithTimeUtils()
    {
        // Both paths should produce the same JD for the same instant
        var dt = new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        double via1 = TimeZoneUtils.ToJulianDay(dt);
        double via2 = TimeUtils.JulianDay(2024, 6, 21, 12.0);
        await Assert.That(Math.Abs(via1 - via2)).IsLessThan(0.00001);
    }

    // ── FromJulianDay ─────────────────────────────────────────────────────

    [Test]
    public async Task FromJulianDay_AtJ2000_Returns2000Jan01Noon()
    {
        var dt = TimeZoneUtils.FromJulianDay(2451545.0);
        await Assert.That(dt.Year).IsEqualTo(2000);
        await Assert.That(dt.Month).IsEqualTo(1);
        await Assert.That(dt.Day).IsEqualTo(1);
        await Assert.That(dt.Hour).IsEqualTo(12);
        await Assert.That(dt.Minute).IsEqualTo(0);
    }

    [Test]
    public async Task FromJulianDay_QuarterDayOffset_HasCorrectHour()
    {
        // JD 2451545.25 = J2000 noon + 6 h = 2000-Jan-01 18:00 UTC
        var dt = TimeZoneUtils.FromJulianDay(2451545.25);
        await Assert.That(dt.Year).IsEqualTo(2000);
        await Assert.That(dt.Month).IsEqualTo(1);
        await Assert.That(dt.Day).IsEqualTo(1);
        await Assert.That(dt.Hour).IsEqualTo(18);
    }

    [Test]
    public async Task FromJulianDay_IsUtcKind()
    {
        var dt = TimeZoneUtils.FromJulianDay(2451545.0);
        await Assert.That(dt.Kind).IsEqualTo(DateTimeKind.Utc);
    }

    // ── Round-trip ────────────────────────────────────────────────────────

    [Test]
    public async Task FromJulianDay_ToJulianDay_RoundTrip_AccurateToOneSecond()
    {
        var original = new DateTime(2024, 3, 20, 15, 30, 45, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(original);
        var result = TimeZoneUtils.FromJulianDay(jd);
        double diffSec = Math.Abs((result - original).TotalSeconds);
        await Assert.That(diffSec).IsLessThanOrEqualTo(1.0);
    }

    [Test]
    public async Task ToJulianDay_FromJulianDay_RoundTrip_AccurateToOneSecond()
    {
        double original = 2451545.123456;
        var dt = TimeZoneUtils.FromJulianDay(original);
        double result = TimeZoneUtils.ToJulianDay(dt);
        // One second = 1/86400 days ≈ 0.0000116
        await Assert.That(Math.Abs(result - original)).IsLessThan(0.00002);
    }

    // ── Timezone conversions ──────────────────────────────────────────────

    [Test]
    public async Task ToLocal_UtcToEasternUS_StandardTime_AppliesUTCMinus5()
    {
        // 2024-Jan-15 12:00 UTC → Eastern Standard Time (UTC−5) → 07:00
        var utc = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var local = TimeZoneUtils.ToLocal(utc, "America/New_York");
        await Assert.That(local.Hour).IsEqualTo(7);
        await Assert.That(local.Minute).IsEqualTo(0);
    }

    [Test]
    public async Task ToLocal_UtcToCentralUS_StandardTime_AppliesUTCMinus6()
    {
        // 2024-Jan-15 12:00 UTC → Central Standard Time (UTC−6) → 06:00
        var utc = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        var local = TimeZoneUtils.ToLocal(utc, "America/Chicago");
        await Assert.That(local.Hour).IsEqualTo(6);
    }

    [Test]
    public async Task ToLocal_UtcToLondon_SummerTime_AppliesUTCPlus1()
    {
        // 2024-Jun-21 12:00 UTC → British Summer Time (UTC+1) → 13:00
        var utc = new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        var local = TimeZoneUtils.ToLocal(utc, "Europe/London");
        await Assert.That(local.Hour).IsEqualTo(13);
    }

    [Test]
    public async Task ToUtc_EasternUS_StandardTime_AppliesUTCMinus5()
    {
        // 07:00 EST (2024-Jan-15) → 12:00 UTC
        var local = new DateTime(2024, 1, 15, 7, 0, 0, DateTimeKind.Unspecified);
        var utc = TimeZoneUtils.ToUtc(local, "America/New_York");
        await Assert.That(utc.Hour).IsEqualTo(12);
    }

    [Test]
    public async Task ToLocal_ToUtc_RoundTrip()
    {
        var original = new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        var local     = TimeZoneUtils.ToLocal(original, "America/Chicago");
        var backToUtc = TimeZoneUtils.ToUtc(local, "America/Chicago");
        await Assert.That(Math.Abs((backToUtc - original).TotalMinutes)).IsLessThan(1.0);
    }
}
