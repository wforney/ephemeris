// Updated: 2026-03-10
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
        if (!FileAvailable) { return; } // skip — file not available

        var stars = StarCatalog.Load(SefstarsFile);
        await Assert.That(stars.Count).IsGreaterThan(100);
    }

    [Test]
    public async Task Load_ParsesSiriusCorrectly()
    {
        if (!FileAvailable) { return; } // skip — file not available

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
        if (!FileAvailable) { return; } // skip — file not available

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
        if (!FileAvailable) { return; } // skip — file not available

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
        if (!FileAvailable) { return; } // skip — file not available

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

    // ── Built-in catalog ──────────────────────────────────────────────────────

    [Test]
    public async Task LoadBuiltIn_ReturnsHundredStars()
    {
        var catalog = StarCatalog.LoadBuiltIn();
        await Assert.That(catalog.Count).IsEqualTo(100);
    }

    [Test]
    public async Task LoadBuiltIn_ContainsSirius()
    {
        var catalog = StarCatalog.LoadBuiltIn();
        var sirius = catalog.FirstOrDefault(s =>
            s.CommonName.Equals("Sirius", StringComparison.OrdinalIgnoreCase));

        await Assert.That(sirius).IsNotNull();
        // Sirius: RA ≈ 101.29°, Dec ≈ −16.72°, mag ≈ −1.46
        await Assert.That(sirius!.RightAscensionJ2000).IsGreaterThan(100.0);
        await Assert.That(sirius.RightAscensionJ2000).IsLessThan(103.0);
        await Assert.That(sirius.DeclinationJ2000).IsGreaterThan(-18.0);
        await Assert.That(sirius.DeclinationJ2000).IsLessThan(-15.0);
        await Assert.That(sirius.Magnitude).IsLessThan(0.0);
    }

    [Test]
    public async Task GetBrighter_WithMag2_ReturnsBrightestStars()
    {
        var catalog = StarCatalog.LoadBuiltIn();
        var bright = StarCatalog.GetBrighter(catalog, 2.0).ToList();

        var names = bright.Select(s => s.CommonName).ToList();
        await Assert.That(names).Contains("Sirius");
        await Assert.That(names).Contains("Canopus");
        await Assert.That(names).Contains("Arcturus");
        await Assert.That(names).Contains("Vega");
        await Assert.That(names).Contains("Capella");
        await Assert.That(names).Contains("Rigel");
    }

    [Test]
    public async Task GetByName_CaseInsensitive()
    {
        var catalog = StarCatalog.LoadBuiltIn();
        var results = StarCatalog.GetByName(catalog, "sirius").ToList();

        await Assert.That(results.Count).IsGreaterThan(0);
        await Assert.That(results[0].CommonName).IsEqualTo("Sirius");
    }

    [Test]
    public async Task GetInRegion_AroundOrion()
    {
        // Orion center ≈ RA 83°, Dec +5°; radius 30° should capture Betelgeuse and Rigel
        var catalog = StarCatalog.LoadBuiltIn();
        var region = StarCatalog.GetInRegion(catalog, 83.0, 5.0, 30.0).ToList();

        var names = region.Select(s => s.CommonName).ToList();
        await Assert.That(names).Contains("Betelgeuse");
        await Assert.That(names).Contains("Rigel");
    }

    [Test]
    public async Task AtEpoch_J2000_MatchesJ2000Position()
    {
        var catalog = StarCatalog.LoadBuiltIn();
        var star = catalog[0]; // Sirius

        var coords = star.AtEpoch(2451545.0);
        // At J2000.0 exactly, result should match J2000 position within 0.001°
        await Assert.That(coords.RightAscension).IsEqualTo(star.RightAscensionJ2000).Within(0.001);
        await Assert.That(coords.Declination).IsEqualTo(star.DeclinationJ2000).Within(0.001);
    }

    [Test]
    public async Task AtEpoch_Shifts_WithTime()
    {
        var catalog = StarCatalog.LoadBuiltIn();
        // Sirius has large proper motion (-546 mas/yr in RA, -1223 mas/yr in Dec)
        var sirius = catalog.First(s => s.CommonName == "Sirius");

        var coordsJ2000 = sirius.AtEpoch(2451545.0);
        var coordsFuture = sirius.AtEpoch(2451545.0 + 365.25 * 25); // 25 years later

        // Position should have shifted measurably
        double deltaRa  = Math.Abs(coordsFuture.RightAscension - coordsJ2000.RightAscension);
        double deltaDec = Math.Abs(coordsFuture.Declination     - coordsJ2000.Declination);

        await Assert.That(deltaRa + deltaDec).IsGreaterThan(0.001);
    }

    [Test]
    public async Task SpectralType_IsPopulated_ForBuiltInStars()
    {
        var catalog = StarCatalog.LoadBuiltIn();
        var sirius = catalog.First(s => s.CommonName == "Sirius");

        await Assert.That(sirius.SpectralType).IsEqualTo("A1V");
    }
}
