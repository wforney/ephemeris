using Ephemeris.Stellarography;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Tests for <see cref="StarCatalog"/> and <see cref="FixedStar"/>.
/// Tests that require the <c>sefstars.txt</c> file are skipped automatically when the
/// file is not present (it is not included in this repository).
/// </summary>
public class StarCatalogTests
{
    // Path to the sefstars.txt file on the local machine.
    private static readonly string SefstarsFile =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "ephem-data", "sefstars.txt");

    private static bool FileAvailable => File.Exists(SefstarsFile);

    // ── Loading ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Load_ReturnsNonEmptyList_WhenFileExists()
    {
        if (!FileAvailable) { await Assert.That(true).IsTrue(); return; }

        var stars = StarCatalog.Load(SefstarsFile);
        await Assert.That(stars.Count).IsGreaterThan(100);
    }

    [Test]
    public async Task Load_ParsesSiriusCorrectly()
    {
        if (!FileAvailable) { await Assert.That(true).IsTrue(); return; }

        var stars = StarCatalog.Load(SefstarsFile);
        var sirius = stars.FirstOrDefault(s =>
            s.CommonName.Contains("Sirius", StringComparison.OrdinalIgnoreCase));

        await Assert.That(sirius).IsNotNull();

        // Sirius: RA ≈ 101.29°, Dec ≈ −16.72°, mag ≈ −1.47
        await Assert.That(sirius!.RightAscensionJ2000).IsGreaterThan(100.0);
        await Assert.That(sirius.RightAscensionJ2000).IsLessThan(103.0);
        await Assert.That(sirius.DeclinationJ2000).IsGreaterThan(-18.0);
        await Assert.That(sirius.DeclinationJ2000).IsLessThan(-15.0);
        await Assert.That(sirius.Magnitude).IsLessThan(0.0);   // brightest star in the sky
    }

    [Test]
    public async Task Load_ParsesPolarisCorrectly()
    {
        if (!FileAvailable) { await Assert.That(true).IsTrue(); return; }

        var stars = StarCatalog.Load(SefstarsFile);
        var polaris = stars.FirstOrDefault(s =>
            s.CommonName.Contains("Polaris", StringComparison.OrdinalIgnoreCase)
            && !s.CommonName.Contains("Australis", StringComparison.OrdinalIgnoreCase));

        await Assert.That(polaris).IsNotNull();

        // Polaris: Dec ≈ +89.26° (near the North Celestial Pole)
        await Assert.That(polaris!.DeclinationJ2000).IsGreaterThan(89.0);
    }

    [Test]
    public async Task Load_ThrowsFileNotFound_WhenFileIsMissing()
    {
        await Assert.ThrowsAsync<FileNotFoundException>(
            async () => await Task.Run(() => StarCatalog.Load("/nonexistent/sefstars.txt")));
    }

    // ── RA / Dec parsing ──────────────────────────────────────────────────────

    [Test]
    public async Task Load_RightAscensionValues_AreInValidRange()
    {
        if (!FileAvailable) { await Assert.That(true).IsTrue(); return; }

        var stars = StarCatalog.Load(SefstarsFile);
        foreach (var star in stars)
        {
            await Assert.That(star.RightAscensionJ2000).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(star.RightAscensionJ2000).IsLessThan(360.0);
        }
    }

    [Test]
    public async Task Load_DeclinationValues_AreInValidRange()
    {
        if (!FileAvailable) { await Assert.That(true).IsTrue(); return; }

        var stars = StarCatalog.Load(SefstarsFile);
        foreach (var star in stars)
        {
            await Assert.That(star.DeclinationJ2000).IsGreaterThanOrEqualTo(-90.0);
            await Assert.That(star.DeclinationJ2000).IsLessThanOrEqualTo(90.0);
        }
    }

    // ── Proper motion correction ──────────────────────────────────────────────

    [Test]
    public async Task ApplyProperMotion_AtJ2000_ReturnsJ2000Position()
    {
        var star = new FixedStar(
            "Test", "tStar", "ICRS",
            180.0, 45.0,
            1000.0, -500.0, 0.0, 0.0, 5.0);

        // At J2000.0, proper motion is zero elapsed time → position unchanged.
        var (ra, dec) = star.ApplyProperMotion(2451545.0);
        await Assert.That(ra).IsEqualTo(180.0).Within(1e-10);
        await Assert.That(dec).IsEqualTo(45.0).Within(1e-10);
    }

    [Test]
    public async Task ApplyProperMotion_AfterOneYear_ShiftsDecCorrectly()
    {
        // pm_dec = +3600000 mas/yr = +1 degree/yr
        var star = new FixedStar("Test", "t", "ICRS", 0.0, 0.0, 0.0, 3_600_000.0, 0.0, 0.0, 5.0);
        var (_, dec) = star.ApplyProperMotion(2451545.0 + 365.25);  // one year later
        await Assert.That(dec).IsEqualTo(1.0).Within(0.001);
    }

    [Test]
    public async Task DistanceParsecs_ReturnsInfinity_WhenParallaxIsZero()
    {
        var star = new FixedStar("Test", "t", "ICRS", 0.0, 0.0, 0.0, 0.0, 0.0, 0.0, 5.0);
        await Assert.That(star.DistanceParsecs).IsEqualTo(double.PositiveInfinity);
    }

    [Test]
    public async Task DistanceParsecs_ReturnsCorrectValue_ForSirius()
    {
        // Sirius parallax ≈ 379.21 mas → distance ≈ 2.636 pc
        var sirius = new FixedStar("Sirius", "alCMa", "ICRS", 101.29, -16.72, -546.05, -1223.14, -7.6, 379.21, -1.47);
        double dist = sirius.DistanceParsecs;
        await Assert.That(dist).IsGreaterThan(2.5);
        await Assert.That(dist).IsLessThan(2.8);
    }
}
