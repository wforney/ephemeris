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
    public async Task SupportedAsteroids_HasSixBodies()
        => await Assert.That(AsteroidEphemeris.SupportedAsteroids.Count).IsEqualTo(6);

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
}
