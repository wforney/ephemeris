// Updated: 2026-03-10
using Ephemeris.Geometry;
using Ephemeris.Stellarography;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Tests for <see cref="BrightStarCatalog"/>, <see cref="StarEphemeris"/>,
/// and the <see cref="EphemerisBatch.GenerateStarSeries"/> overloads.
/// </summary>
public class StarEphemerisTests
{
    // ── BrightStarCatalog ─────────────────────────────────────────────────────

    [Test]
    public async Task BrightStarCatalog_All_ReturnsHundredStars()
    {
        var catalog = BrightStarCatalog.All;
        await Assert.That(catalog.Count).IsEqualTo(100);
    }

    [Test]
    public async Task BrightStarCatalog_GetStar_FindsSirius()
    {
        var sirius = BrightStarCatalog.GetStar("Sirius");

        await Assert.That(sirius).IsNotNull();
        await Assert.That(sirius!.CommonName).IsEqualTo("Sirius");
        await Assert.That(sirius.RightAscensionJ2000).IsGreaterThan(100.0).And.IsLessThan(103.0);
        await Assert.That(sirius.DeclinationJ2000).IsGreaterThan(-18.0).And.IsLessThan(-15.0);
        await Assert.That(sirius.Magnitude).IsLessThan(0.0);
    }

    [Test]
    public async Task BrightStarCatalog_GetStar_CaseInsensitive()
    {
        var star1 = BrightStarCatalog.GetStar("sirius");
        var star2 = BrightStarCatalog.GetStar("SIRIUS");
        var star3 = BrightStarCatalog.GetStar("Sirius");

        await Assert.That(star1).IsNotNull();
        await Assert.That(star2).IsNotNull();
        await Assert.That(star3).IsNotNull();
        await Assert.That(star1!.CommonName).IsEqualTo(star3!.CommonName);
        await Assert.That(star2!.CommonName).IsEqualTo(star3.CommonName);
    }

    [Test]
    public async Task BrightStarCatalog_GetStar_FindsByBayerDesignation()
    {
        // alCMa = α Canis Majoris = Sirius
        var sirius = BrightStarCatalog.GetStar("alCMa");

        await Assert.That(sirius).IsNotNull();
        await Assert.That(sirius!.CommonName).IsEqualTo("Sirius");
    }

    [Test]
    public async Task BrightStarCatalog_GetStar_ReturnsNull_ForUnknownStar()
    {
        var result = BrightStarCatalog.GetStar("NotARealStar");
        await Assert.That(result).IsNull();
    }

    [Test]
    public async Task BrightStarCatalog_GetStar_ThrowsOnNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await Task.Run(() => BrightStarCatalog.GetStar(null!)));
    }

    [Test]
    public async Task BrightStarCatalog_Search_PartialNameMatch()
    {
        var results = BrightStarCatalog.Search("pol").ToList();

        // Should find at least Pollux and Polaris
        var names = results.Select(s => s.CommonName).ToList();
        await Assert.That(names).Contains("Pollux");
        await Assert.That(names).Contains("Polaris");
    }

    [Test]
    public async Task BrightStarCatalog_GetBrighter_ReturnsBrightestStars()
    {
        var bright = BrightStarCatalog.GetBrighter(1.5).ToList();

        var names = bright.Select(s => s.CommonName).ToList();
        await Assert.That(names).Contains("Sirius");
        await Assert.That(names).Contains("Canopus");
        await Assert.That(names).Contains("Arcturus");
        await Assert.That(names).Contains("Vega");
    }

    [Test]
    public async Task BrightStarCatalog_All_ContainsMagnitudeRange()
    {
        // All stars should have magnitudes within a reasonable range
        foreach (var star in BrightStarCatalog.All)
        {
            await Assert.That(star.Magnitude).IsGreaterThan(-5.0);
            await Assert.That(star.Magnitude).IsLessThan(4.5);
        }
    }

    // ── StarEphemeris ─────────────────────────────────────────────────────────

    [Test]
    public async Task StarEphemeris_ApparentPosition_AtJ2000_MatchesCatalog()
    {
        var sirius = BrightStarCatalog.GetStar("Sirius")!;

        // At J2000.0 (year 2000.0), apparent position should match J2000 catalog within 0.001°
        var pos = StarEphemeris.ApparentPosition(sirius, 2000.0);

        await Assert.That(pos.RightAscension).IsEqualTo(sirius.RightAscensionJ2000).Within(0.001);
        await Assert.That(pos.Declination).IsEqualTo(sirius.DeclinationJ2000).Within(0.001);
    }

    [Test]
    public async Task StarEphemeris_ApparentPosition_ShiftsWithTime()
    {
        var sirius = BrightStarCatalog.GetStar("Sirius")!;

        var pos2000 = StarEphemeris.ApparentPosition(sirius, 2000.0);
        var pos2025 = StarEphemeris.ApparentPosition(sirius, 2025.0);

        // Sirius has large proper motion (−546 mas/yr RA, −1223 mas/yr Dec)
        // 25 years × ~1.2 arcsec/yr Dec ≈ 30 arcsec total shift in Dec
        double dDec = Math.Abs(pos2025.Declination - pos2000.Declination);
        await Assert.That(dDec).IsGreaterThan(0.001); // at least 3.6 arcsec
    }

    [Test]
    public async Task StarEphemeris_ApparentPosition_ReturnsValidRA()
    {
        foreach (var star in BrightStarCatalog.GetBrighter(2.0))
        {
            var pos = StarEphemeris.ApparentPosition(star, 2025.0);
            await Assert.That(pos.RightAscension).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(pos.RightAscension).IsLessThan(360.0);
        }
    }

    [Test]
    public async Task StarEphemeris_ApparentPosition_ReturnsValidDec()
    {
        foreach (var star in BrightStarCatalog.GetBrighter(2.0))
        {
            var pos = StarEphemeris.ApparentPosition(star, 2025.0);
            await Assert.That(pos.Declination).IsGreaterThanOrEqualTo(-90.0);
            await Assert.That(pos.Declination).IsLessThanOrEqualTo(90.0);
        }
    }

    [Test]
    public async Task StarEphemeris_CatalogPosition_MatchesJ2000Fields()
    {
        var vega = BrightStarCatalog.GetStar("Vega")!;

        var pos = StarEphemeris.CatalogPosition(vega);

        await Assert.That(pos.RightAscension).IsEqualTo(vega.RightAscensionJ2000).Within(1e-10);
        await Assert.That(pos.Declination).IsEqualTo(vega.DeclinationJ2000).Within(1e-10);
    }

    [Test]
    public async Task StarEphemeris_ApparentPositionJd_AtJ2000_MatchesCatalog()
    {
        var vega = BrightStarCatalog.GetStar("Vega")!;

        var pos = StarEphemeris.ApparentPositionJd(vega, 2451545.0); // JD of J2000.0

        await Assert.That(pos.RightAscension).IsEqualTo(vega.RightAscensionJ2000).Within(0.001);
        await Assert.That(pos.Declination).IsEqualTo(vega.DeclinationJ2000).Within(0.001);
    }

    [Test]
    public async Task StarEphemeris_ApparentPosition_ThrowsOnNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await Task.Run(() => StarEphemeris.ApparentPosition(null!, 2025.0)));
    }

    // ── EphemerisBatch.GenerateStarSeries ─────────────────────────────────────

    [Test]
    public async Task GenerateStarSeries_ByFixedStar_ReturnsCorrectCount()
    {
        var sirius = BrightStarCatalog.GetStar("Sirius")!;
        var start = new DateTime(2025, 6, 21, 0, 0, 0, DateTimeKind.Utc);

        var records = EphemerisBatch.GenerateStarSeries(sirius, start, 60, 24, 0.0, 51.5).ToList();

        await Assert.That(records.Count).IsEqualTo(24);
    }

    [Test]
    public async Task GenerateStarSeries_ByStarName_ReturnsCorrectCount()
    {
        var start = new DateTime(2025, 6, 21, 0, 0, 0, DateTimeKind.Utc);

        var records = EphemerisBatch.GenerateStarSeries("Vega", start, 60, 12, -74.0, 40.7).ToList();

        await Assert.That(records.Count).IsEqualTo(12);
    }

    [Test]
    public async Task GenerateStarSeries_BodyName_MatchesStarName()
    {
        var sirius = BrightStarCatalog.GetStar("Sirius")!;
        var start = new DateTime(2025, 6, 21, 0, 0, 0, DateTimeKind.Utc);

        var records = EphemerisBatch.GenerateStarSeries(sirius, start, 60, 1, 0.0, 0.0).ToList();

        await Assert.That(records[0].Body).IsEqualTo("Sirius");
    }

    [Test]
    public async Task GenerateStarSeries_MagnitudeField_IsPopulated()
    {
        var sirius = BrightStarCatalog.GetStar("Sirius")!;
        var start = new DateTime(2025, 6, 21, 0, 0, 0, DateTimeKind.Utc);

        var records = EphemerisBatch.GenerateStarSeries(sirius, start, 60, 1, 0.0, 0.0).ToList();

        await Assert.That(records[0].Magnitude).IsNotNull();
        await Assert.That(records[0].Magnitude!.Value).IsLessThan(0.0); // Sirius mag ≈ -1.46
    }

    [Test]
    public async Task GenerateStarSeries_RA_IsInValidRange()
    {
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var records = EphemerisBatch.GenerateStarSeries("Arcturus", start, 60, 24, -87.65, 41.85).ToList();

        foreach (var r in records)
        {
            await Assert.That(r.RightAscension).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(r.RightAscension).IsLessThan(360.0);
        }
    }

    [Test]
    public async Task GenerateStarSeries_ByName_ThrowsForUnknownStar()
    {
        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await Assert.ThrowsAsync<ArgumentException>(
            async () => await Task.Run(
                () => EphemerisBatch.GenerateStarSeries("FictionalStar", start, 60, 1, 0.0, 0.0).ToList()));
    }

    [Test]
    public async Task GenerateStarSeries_TimeUtc_MatchesInputInterval()
    {
        var start = new DateTime(2025, 6, 21, 0, 0, 0, DateTimeKind.Utc);
        var star = BrightStarCatalog.GetStar("Deneb")!;

        var records = EphemerisBatch.GenerateStarSeries(star, start, 30, 5, -79.0, 43.7).ToList();

        await Assert.That(records[0].TimeUtc).IsEqualTo(start);
        await Assert.That(records[1].TimeUtc).IsEqualTo(start.AddMinutes(30));
        await Assert.That(records[4].TimeUtc).IsEqualTo(start.AddMinutes(120));
    }

    [Test]
    public async Task GenerateStarSeries_Illumination_IsNull()
    {
        var star = BrightStarCatalog.GetStar("Polaris")!;
        var start = new DateTime(2025, 3, 20, 0, 0, 0, DateTimeKind.Utc);

        var records = EphemerisBatch.GenerateStarSeries(star, start, 60, 2, 0.0, 45.0).ToList();

        await Assert.That(records[0].Illumination).IsNull();
    }
}
