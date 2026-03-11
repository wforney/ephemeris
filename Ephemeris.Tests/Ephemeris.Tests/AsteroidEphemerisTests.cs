// Updated: 2026-03-11
using Ephemeris.Chronology;
using Ephemeris.Planetology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Validates <see cref="AsteroidEphemeris"/> — orbital element retrieval, position range checks,
/// and error handling for unsupported bodies.
/// </summary>
public class AsteroidEphemerisTests
{
    private static double T2000 => 0.0; // J2000.0 epoch (T = 0)
    private static double T2024 => TimeUtils.JulianCentury(TimeZoneUtils.ToJulianDay(new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc)));

    // ── Supported body list ───────────────────────────────────────────────────────

    [Test]
    public async Task SupportedAsteroids_ContainsCeres()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids).Contains("ceres");

    [Test]
    public async Task SupportedAsteroids_ContainsChiron()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids).Contains("chiron");

    [Test]
    public async Task SupportedAsteroids_ContainsEris()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids).Contains("eris");

    [Test]
    public async Task SupportedAsteroids_HasThirtyFiveBodies()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids.Count).IsEqualTo(35);

    // ── Orbital elements ─────────────────────────────────────────────────────────

    [Test]
    public async Task Ceres_Elements_SemiMajorAxis_IsApproximately2p77AU()
    {
        var elements = AsteroidEphemeris.GetElements("ceres", T2000);
        await Assert.That(Math.Abs(elements.SemiMajorAxisAu - 2.7659)).IsLessThan(0.01);
    }

    [Test]
    public async Task Chiron_Elements_SemiMajorAxis_IsApproximately13p6AU()
    {
        var elements = AsteroidEphemeris.GetElements("chiron", T2000);
        await Assert.That(Math.Abs(elements.SemiMajorAxisAu - 13.65)).IsLessThan(0.1);
    }

    [Test]
    public async Task Eris_Elements_SemiMajorAxis_IsApproximately67p7AU()
    {
        var elements = AsteroidEphemeris.GetElements("eris", T2000);
        await Assert.That(Math.Abs(elements.SemiMajorAxisAu - 67.7)).IsLessThan(0.5);
    }

    [Test]
    public async Task GetElements_CaseInsensitive_Ceres()
    {
        var lower = AsteroidEphemeris.GetElements("ceres", T2000);
        var upper = AsteroidEphemeris.GetElements("CERES", T2000);
        await Assert.That(lower.SemiMajorAxisAu).IsEqualTo(upper.SemiMajorAxisAu);
    }

    [Test]
    public async Task GetElements_Unknown_ThrowsArgumentException()
    {
        await Assert.That(() => AsteroidEphemeris.GetElements("unknown_body", T2000))
            .Throws<ArgumentException>();
    }

    // ── Position range checks ─────────────────────────────────────────────────────

    [Test]
    public async Task Ceres_RA_IsInValidRange()
    {
        var (coords, _) = AsteroidEphemeris.GetPosition("ceres", T2024);
        await Assert.That(coords.RightAscension).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(coords.RightAscension).IsLessThan(360.0);
    }

    [Test]
    public async Task Pallas_RA_IsInValidRange()
    {
        var (coords, _) = AsteroidEphemeris.GetPosition("pallas", T2024);
        await Assert.That(coords.RightAscension).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(coords.RightAscension).IsLessThan(360.0);
    }

    [Test]
    public async Task Juno_RA_IsInValidRange()
    {
        var (coords, _) = AsteroidEphemeris.GetPosition("juno", T2024);
        await Assert.That(coords.RightAscension).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(coords.RightAscension).IsLessThan(360.0);
    }

    [Test]
    public async Task Vesta_RA_IsInValidRange()
    {
        var (coords, _) = AsteroidEphemeris.GetPosition("vesta", T2024);
        await Assert.That(coords.RightAscension).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(coords.RightAscension).IsLessThan(360.0);
    }

    [Test]
    public async Task Chiron_RA_IsInValidRange()
    {
        var (coords, _) = AsteroidEphemeris.GetPosition("chiron", T2024);
        await Assert.That(coords.RightAscension).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(coords.RightAscension).IsLessThan(360.0);
    }

    [Test]
    public async Task Eris_RA_IsInValidRange()
    {
        var (coords, _) = AsteroidEphemeris.GetPosition("eris", T2024);
        await Assert.That(coords.RightAscension).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(coords.RightAscension).IsLessThan(360.0);
    }

    [Test]
    public async Task Ceres_Dec_IsInValidRange()
    {
        var (coords, _) = AsteroidEphemeris.GetPosition("ceres", T2024);
        await Assert.That(coords.Declination).IsGreaterThanOrEqualTo(-90.0);
        await Assert.That(coords.Declination).IsLessThanOrEqualTo(90.0);
    }

    [Test]
    public async Task Ceres_Distance_IsInMainBelt()
    {
        // Ceres oscillates 2.55–2.98 AU; heliocentric r should be in that range
        var (_, dist) = AsteroidEphemeris.GetPosition("ceres", T2024);
        await Assert.That(dist).IsGreaterThan(2.0);
        await Assert.That(dist).IsLessThan(4.0);
    }

    [Test]
    public async Task Chiron_Distance_IsBetweenSaturnAndUranus()
    {
        // Chiron's heliocentric r ranges from ~8.5 to 18.8 AU
        var (_, dist) = AsteroidEphemeris.GetPosition("chiron", T2024);
        await Assert.That(dist).IsGreaterThan(6.0);
        await Assert.That(dist).IsLessThan(22.0);
    }

    // ── GetObservation ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetObservation_Ceres_ReturnsValidHorizontalCoords()
    {
        double jd = TimeZoneUtils.ToJulianDay(new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc));
        var obs = AsteroidEphemeris.GetObservation("ceres", jd, longitude: -77.0, latitude: 38.9);
        await Assert.That(obs.Azimuth).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(obs.Azimuth).IsLessThan(360.0);
        await Assert.That(obs.Altitude).IsGreaterThanOrEqualTo(-90.0);
        await Assert.That(obs.Altitude).IsLessThanOrEqualTo(90.0);
    }

    // ── New main-belt bodies ──────────────────────────────────────────────────────

    [Test]
    public async Task SupportedAsteroids_ContainsPsyche()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids).Contains("psyche");

    [Test]
    public async Task SupportedAsteroids_ContainsEros()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids).Contains("eros");

    [Test]
    public async Task Psyche_Elements_SemiMajorAxis_IsApproximately2p92AU()
    {
        var el = AsteroidEphemeris.GetElements("psyche", T2000);
        await Assert.That(Math.Abs(el.SemiMajorAxisAu - 2.923)).IsLessThan(0.01);
    }

    [Test]
    public async Task Eros_Distance_IsNearEarth()
    {
        // Eros semi-major axis ~1.46 AU; heliocentric distance should be 0.8–2.2 AU
        var (_, dist) = AsteroidEphemeris.GetPosition("eros", T2024);
        await Assert.That(dist).IsGreaterThan(0.5);
        await Assert.That(dist).IsLessThan(3.0);
    }

    [Test]
    public async Task Icarus_Elements_SemiMajorAxis_IsApproximately1p08AU()
    {
        var el = AsteroidEphemeris.GetElements("icarus", T2000);
        await Assert.That(Math.Abs(el.SemiMajorAxisAu - 1.078)).IsLessThan(0.01);
    }

    // ── New centaurs ──────────────────────────────────────────────────────────────

    [Test]
    public async Task SupportedAsteroids_ContainsPholus()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids).Contains("pholus");

    [Test]
    public async Task Pholus_Elements_SemiMajorAxis_IsApproximately20p4AU()
    {
        var el = AsteroidEphemeris.GetElements("pholus", T2000);
        await Assert.That(Math.Abs(el.SemiMajorAxisAu - 20.36)).IsLessThan(0.5);
    }

    [Test]
    public async Task Chariklo_Elements_SemiMajorAxis_IsApproximately15p8AU()
    {
        var el = AsteroidEphemeris.GetElements("chariklo", T2000);
        await Assert.That(Math.Abs(el.SemiMajorAxisAu - 15.80)).IsLessThan(0.5);
    }

    // ── New TNOs and dwarf planets ────────────────────────────────────────────────

    [Test]
    public async Task SupportedAsteroids_ContainsHaumea()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids).Contains("haumea");

    [Test]
    public async Task SupportedAsteroids_ContainsSedna()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids).Contains("sedna");

    [Test]
    public async Task Sedna_Elements_SemiMajorAxis_IsExtremelyLarge()
    {
        var el = AsteroidEphemeris.GetElements("sedna", T2000);
        await Assert.That(el.SemiMajorAxisAu).IsGreaterThan(400.0);
    }

    [Test]
    public async Task Haumea_Elements_SemiMajorAxis_IsApproximately43AU()
    {
        var el = AsteroidEphemeris.GetElements("haumea", T2000);
        await Assert.That(Math.Abs(el.SemiMajorAxisAu - 43.13)).IsLessThan(1.0);
    }

    [Test]
    public async Task AllSupportedAsteroids_ReturnValidRA()
    {
        // Smoke-test: every asteroid must return a valid RA
        foreach (string name in AsteroidEphemeris.SupportedAsteroids)
        {
            var (coords, _) = AsteroidEphemeris.GetPosition(name, T2024);
            await Assert.That(coords.RightAscension).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(coords.RightAscension).IsLessThan(360.0);
        }
    }
}
