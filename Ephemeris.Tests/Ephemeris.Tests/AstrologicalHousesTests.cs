// Updated: 2026-03-11
using Ephemeris.Astrology;
using Ephemeris.Chronology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Validates <see cref="AstrologicalHouses"/> — Ascendant/MC geometry, angle relationships,
/// house cusp ranges, and system-specific structural properties.
/// </summary>
public class AstrologicalHousesTests
{
    // Reference location: Washington D.C. (38.9°N, 77.0°W) at J2000.0 (2000-Jan-01 12:00 UTC)
    private static double Jd2000 => TimeZoneUtils.ToJulianDay(new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc));
    private const double WashingtonLon = -77.0;
    private const double WashingtonLat = 38.9;

    // ── Angles geometry ───────────────────────────────────────────────────────────

    [Test]
    public async Task Calculate_Ascendant_IsInValidRange()
    {
        var cusps = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        await Assert.That(cusps.Ascendant).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(cusps.Ascendant).IsLessThan(360.0);
    }

    [Test]
    public async Task Calculate_Midheaven_IsInValidRange()
    {
        var cusps = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        await Assert.That(cusps.Midheaven).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(cusps.Midheaven).IsLessThan(360.0);
    }

    [Test]
    public async Task Calculate_Descendant_IsAscendantPlus180()
    {
        var cusps = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        double expected = (cusps.Ascendant + 180.0) % 360.0;
        await Assert.That(Math.Abs(cusps.Descendant - expected)).IsLessThan(0.001);
    }

    [Test]
    public async Task Calculate_ImumCoeli_IsMidheavenPlus180()
    {
        var cusps = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        double expected = (cusps.Midheaven + 180.0) % 360.0;
        await Assert.That(Math.Abs(cusps.ImumCoeli - expected)).IsLessThan(0.001);
    }

    [Test]
    public async Task Calculate_Cusps_Count_Is12()
    {
        var cusps = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        await Assert.That(cusps.Cusps.Count).IsEqualTo(12);
    }

    // ── Angle vs cusp consistency ─────────────────────────────────────────────────

    [Test]
    public async Task Calculate_Cusps0_EqualsAscendant()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Porphyry);
        await Assert.That(Math.Abs(h.Cusps[0] - h.Ascendant)).IsLessThan(0.001);
    }

    [Test]
    public async Task Calculate_Cusps3_EqualsImumCoeli()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Porphyry);
        await Assert.That(Math.Abs(h.Cusps[3] - h.ImumCoeli)).IsLessThan(0.001);
    }

    [Test]
    public async Task Calculate_Cusps6_EqualsDescendant()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Porphyry);
        await Assert.That(Math.Abs(h.Cusps[6] - h.Descendant)).IsLessThan(0.001);
    }

    [Test]
    public async Task Calculate_Cusps9_EqualsMidheaven()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Porphyry);
        await Assert.That(Math.Abs(h.Cusps[9] - h.Midheaven)).IsLessThan(0.001);
    }

    // ── Equal House ──────────────────────────────────────────────────────────────

    [Test]
    public async Task EqualHouse_Cusps_AreSeparatedBy30Degrees()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Equal);
        for (int i = 0; i < 11; i++)
        {
            double sep = (h.Cusps[i + 1] - h.Cusps[i] + 360.0) % 360.0;
            await Assert.That(Math.Abs(sep - 30.0)).IsLessThan(0.001);
        }
    }

    [Test]
    public async Task EqualHouse_House1_EqualsAscendant()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Equal);
        await Assert.That(Math.Abs(h.Cusps[0] - h.Ascendant)).IsLessThan(0.001);
    }

    // ── Whole Signs ──────────────────────────────────────────────────────────────

    [Test]
    public async Task WholeSigns_House1_IsMultipleOf30()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.WholeSigns);
        double remainder = h.Cusps[0] % 30.0;
        await Assert.That(remainder).IsLessThan(0.001);
    }

    [Test]
    public async Task WholeSigns_Cusps_AreSeparatedBy30Degrees()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.WholeSigns);
        for (int i = 0; i < 11; i++)
        {
            double sep = (h.Cusps[i + 1] - h.Cusps[i] + 360.0) % 360.0;
            await Assert.That(Math.Abs(sep - 30.0)).IsLessThan(0.001);
        }
    }

    [Test]
    public async Task WholeSigns_House1_ContainsAscendant()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.WholeSigns);
        // ASC must lie within [Cusps[0], Cusps[0]+30°)
        double offset = (h.Ascendant - h.Cusps[0] + 360.0) % 360.0;
        await Assert.That(offset).IsLessThan(30.0);
    }

    // ── Porphyry ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Porphyry_AllCusps_AreInValidRange()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Porphyry);
        foreach (double c in h.Cusps)
        {
            await Assert.That(c).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(c).IsLessThan(360.0);
        }
    }

    [Test]
    public async Task Porphyry_OppositeHouses_Are180Apart()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Porphyry);
        // H1 ↔ H7, H4 ↔ H10 are always exactly 180° apart
        double sep17 = Math.Abs((h.Cusps[6] - h.Cusps[0] + 360.0) % 360.0 - 180.0);
        double sep4_10 = Math.Abs((h.Cusps[9] - h.Cusps[3] + 360.0) % 360.0 - 180.0);
        await Assert.That(sep17).IsLessThan(0.01);
        await Assert.That(sep4_10).IsLessThan(0.01);
    }

    // ── Placidus ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Placidus_AllCusps_AreInValidRange()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        foreach (double c in h.Cusps)
        {
            await Assert.That(c).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(c).IsLessThan(360.0);
        }
    }

    [Test]
    public async Task Placidus_House1_EqualsAscendant()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        await Assert.That(Math.Abs(h.Cusps[0] - h.Ascendant)).IsLessThan(0.001);
    }

    [Test]
    public async Task Placidus_House10_EqualsMidheaven()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        await Assert.That(Math.Abs(h.Cusps[9] - h.Midheaven)).IsLessThan(0.001);
    }

    [Test]
    public async Task Placidus_HouseSystem_RecordedCorrectly()
    {
        var h = AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Placidus);
        await Assert.That(h.HouseSystem).IsEqualTo(HouseSystem.Placidus);
    }

    // ── Unimplemented systems ─────────────────────────────────────────────────────

    [Test]
    public async Task Koch_ThrowsNotSupportedException()
    {
        await Assert.That(() => AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Koch))
            .Throws<NotSupportedException>();
    }

    [Test]
    public async Task Campanus_ThrowsNotSupportedException()
    {
        await Assert.That(() => AstrologicalHouses.Calculate(Jd2000, WashingtonLon, WashingtonLat, HouseSystem.Campanus))
            .Throws<NotSupportedException>();
    }

    // ── MC formula spot check ─────────────────────────────────────────────────────
    // At J2000.0 and longitude 0°, GMST ≈ 280.46°, so RAMC ≈ 280.46°.
    // MC = atan2(sin(280.46°), cos(280.46°)*cos(23.44°)) should be near 280° (Capricorn).

    [Test]
    public async Task ComputeMC_RAMC280_IsNearCapricorn()
    {
        double mc = AstrologicalHouses.ComputeMC(280.46, 23.44);
        // MC should be close to 280° ± 5° (around 10° Capricorn)
        await Assert.That(Math.Abs(mc - 280.0) % 360.0).IsLessThan(10.0);
    }

    [Test]
    public async Task ComputeAscendant_ProducesValidResult()
    {
        double asc = AstrologicalHouses.ComputeAscendant(280.46, 23.44, 38.9);
        await Assert.That(asc).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(asc).IsLessThan(360.0);
    }
}
