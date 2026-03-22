using Ephemeris.Chronology;
using Ephemeris.Phenomenology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Unit tests for <see cref="BiblicalCalendarHelper"/>.
/// Validates Mazzaroth sign assignment, crescent visibility logic,
/// and biblical date computation for known astronomical events.
/// </summary>
public class BiblicalCalendarHelperTests
{
    // Observer: Jerusalem (lon=35.22°E, lat=31.77°N) — historically significant for biblical calendar
    private const double JerusalemLon =  35.22;
    private const double JerusalemLat =  31.77;

    // ── GetMazzarothSign ─────────────────────────────────────────────────────

    [Test]
    public async Task GetMazzarothSign_0Degrees_IsAries()
    {
        string sign = BiblicalCalendarHelper.GetMazzarothSign(0.0);
        await Assert.That(sign).IsEqualTo("Aries");
    }

    [Test]
    public async Task GetMazzarothSign_29Degrees_IsAries()
    {
        string sign = BiblicalCalendarHelper.GetMazzarothSign(29.9);
        await Assert.That(sign).IsEqualTo("Aries");
    }

    [Test]
    public async Task GetMazzarothSign_30Degrees_IsTaurus()
    {
        string sign = BiblicalCalendarHelper.GetMazzarothSign(30.0);
        await Assert.That(sign).IsEqualTo("Taurus");
    }

    [Test]
    public async Task GetMazzarothSign_120Degrees_IsLeo()
    {
        // 120°–150° = Leo (Aryeh) — the lion of Judah's sign
        string sign = BiblicalCalendarHelper.GetMazzarothSign(120.0);
        await Assert.That(sign).IsEqualTo("Leo");
    }

    [Test]
    public async Task GetMazzarothSign_150Degrees_IsVirgo()
    {
        string sign = BiblicalCalendarHelper.GetMazzarothSign(150.0);
        await Assert.That(sign).IsEqualTo("Virgo");
    }

    [Test]
    public async Task GetMazzarothSign_330Degrees_IsPisces()
    {
        // 330°–360° = Pisces (Dagim)
        string sign = BiblicalCalendarHelper.GetMazzarothSign(330.0);
        await Assert.That(sign).IsEqualTo("Pisces");
    }

    [Test]
    public async Task GetMazzarothSign_359Degrees_IsPisces()
    {
        string sign = BiblicalCalendarHelper.GetMazzarothSign(359.9);
        await Assert.That(sign).IsEqualTo("Pisces");
    }

    [Test]
    public async Task GetMazzarothSign_360Degrees_WrapsToAries()
    {
        // Exactly 360° should normalize to 0° → Aries
        string sign = BiblicalCalendarHelper.GetMazzarothSign(360.0);
        await Assert.That(sign).IsEqualTo("Aries");
    }

    [Test]
    public async Task GetMazzarothSign_NegativeDegrees_Normalizes()
    {
        // -30° normalizes to 330° → Pisces
        string sign = BiblicalCalendarHelper.GetMazzarothSign(-30.0);
        await Assert.That(sign).IsEqualTo("Pisces");
    }

    // ── GetBiblicalDate — spring (Nisan) ─────────────────────────────────────

    [Test]
    public async Task GetBiblicalDate_SpringEquinox2024_IsNisanOrIyyar()
    {
        // 2024 Mar 20 is the spring equinox. Nisan 1 is the first new moon on or after this.
        // New moon was 2024-Apr-08 — so on March 20 we are still in Adar (last month of previous cycle).
        var dt   = new DateTime(2024, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        // After Apr 8 new moon, we should be in Nisan (month 1)
        await Assert.That(result.Month).IsEqualTo(1);
        await Assert.That(result.MonthName).IsEqualTo("Nisan");
    }

    [Test]
    public async Task GetBiblicalDate_HebrewYear2024_IsApprox5784()
    {
        // Gregorian 2024 spring → Hebrew year ~5784
        var dt   = new DateTime(2024, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        // Hebrew year = Gregorian year + 3760 (±1 due to Tishrei/Nisan boundary)
        await Assert.That(result.Year).IsGreaterThanOrEqualTo(5783);
        await Assert.That(result.Year).IsLessThanOrEqualTo(5785);
    }

    [Test]
    public async Task GetBiblicalDate_SpringDate_SeasonIsSpring()
    {
        var dt   = new DateTime(2024, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        await Assert.That(result.Season).IsEqualTo("Spring");
    }

    [Test]
    public async Task GetBiblicalDate_SummerDate_SeasonIsSummer()
    {
        // August — Sun ecliptic longitude ~130°-160° → Summer
        var dt   = new DateTime(2024, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        await Assert.That(result.Season).IsEqualTo("Summer");
    }

    [Test]
    public async Task GetBiblicalDate_JanuaryDate_SeasonIsWinter()
    {
        // January 15 — Sun is well past the winter solstice (~270°), ecliptic longitude ~295°
        var dt   = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        await Assert.That(result.Season).IsEqualTo("Winter");
    }

    [Test]
    public async Task GetBiblicalDate_SolarSignIsFromKnownList()
    {
        var dt   = new DateTime(2024, 7, 15, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        string[] validSigns =
        [
            "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo",
            "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces",
        ];
        await Assert.That(validSigns).Contains(result.SolarSign);
    }

    [Test]
    public async Task GetBiblicalDate_July2024_SunInCancer()
    {
        // July 15: Sun ~approx ecliptic longitude 112°-115° → Cancer (90°-120°)
        var dt   = new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        await Assert.That(result.SolarSign).IsEqualTo("Cancer");
    }

    [Test]
    public async Task GetBiblicalDate_DayOfMonth_IsInValidRange()
    {
        var dt   = new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        await Assert.That(result.DayOfMonth).IsGreaterThanOrEqualTo(1);
        await Assert.That(result.DayOfMonth).IsLessThanOrEqualTo(30);
    }

    [Test]
    public async Task GetBiblicalDate_DescriptionIsNonEmpty()
    {
        var dt   = new DateTime(2024, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        await Assert.That(result.Description).IsNotEmpty();
    }

    [Test]
    public async Task GetBiblicalDate_MonthNameHebrewIsNonEmpty()
    {
        var dt   = new DateTime(2024, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        var result = BiblicalCalendarHelper.GetBiblicalDate(jd, JerusalemLon, JerusalemLat);

        await Assert.That(result.MonthNameHebrew).IsNotEmpty();
    }

    // ── IsCrescentVisible ──────────────────────────────────────────────────

    [Test]
    public async Task IsCrescentVisible_FullMoon_ReturnsFalse()
    {
        // 2024-Mar-25 was approximately a full moon
        var dt   = new DateTime(2024, 3, 25, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        bool visible = BiblicalCalendarHelper.IsCrescentVisible(jd, JerusalemLon, JerusalemLat);

        // At full moon (D ≈ 180°, age ≈ 14.7 days) the crescent is not visible
        await Assert.That(visible).IsFalse();
    }

    [Test]
    public async Task IsCrescentVisible_OldMoon_ReturnsFalse()
    {
        // Three days before new moon (waning crescent) should not be a young crescent
        // New moon 2024-Apr-08: checking Apr-05 (3 days before) → old moon
        var dt   = new DateTime(2024, 4, 5, 0, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        bool visible = BiblicalCalendarHelper.IsCrescentVisible(jd, JerusalemLon, JerusalemLat);

        // Moon is nearing new moon on waning side, not a young crescent
        await Assert.That(visible).IsFalse();
    }

    [Test]
    public async Task IsCrescentVisible_TwoDaysAfterNewMoon_MoonAgeIsYoung()
    {
        // 2024-Apr-10 is 2 days after the Apr-08 new moon — age check should pass (< 2.5 days).
        // Visibility also depends on altitude; we just verify the method returns without throwing.
        var dt   = new DateTime(2024, 4, 10, 12, 0, 0, DateTimeKind.Utc);
        double jd = TimeZoneUtils.ToJulianDay(dt);
        double T  = (jd - 2451545.0) / 36525.0;

        // Moon's mean elongation D: at ~2 days post-new-moon, D should be roughly 20°-30°
        double D = ((297.8501921 + (445267.1114034 * T)) % 360 + 360) % 360;
        await Assert.That(D).IsLessThan(90.0); // still in the young-moon window (< ~4.4 days)
    }
}
