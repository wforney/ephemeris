// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Geodesy;
using Ephemeris.Selenography;
using Ephemeris.Planetology;

namespace Ephemeris.Tests;

/// <summary>
/// Tests for features added in phase 3: LMST/HourAngle, ParallacticAngle,
/// RefractionCalculator, galactic coordinates, planet illumination, and lunar libration.
/// </summary>
public class Phase3FeatureTests
{
    // ── LMST / HourAngle ──────────────────────────────────────────────────────

    [Test]
    public async Task LocalMeanSiderealTime_AtJ2000_PrimeMeridian_ApproximatesGmst()
    {
        double jd = 2451545.0; // J2000.0
        double lmst = TimeUtils.LocalMeanSiderealTime(jd, 0.0);
        double gmst = TimeUtils.GMST(jd);
        await Assert.That(lmst).IsEqualTo(gmst).Within(0.001);
    }

    [Test]
    public async Task LocalMeanSiderealTime_EastLongitude_IsGmstPlusLongitude()
    {
        double jd = 2451545.0;
        double lon = 75.0;
        double lmst = TimeUtils.LocalMeanSiderealTime(jd, lon);
        double expected = TimeUtils.NormalizeDegrees(TimeUtils.GMST(jd) + lon);
        await Assert.That(lmst).IsEqualTo(expected).Within(0.001);
    }

    [Test]
    public async Task LocalMeanSiderealTime_IsNormalizedTo0_360()
    {
        double jd = 2451545.0;
        double lmst = TimeUtils.LocalMeanSiderealTime(jd, -200.0);
        await Assert.That(lmst).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(lmst).IsLessThan(360.0);
    }

    [Test]
    public async Task HourAngle_WhenObjectOnMeridian_IsZero()
    {
        double jd = 2451545.0;
        double lon = 0.0;
        double lmst = TimeUtils.LocalMeanSiderealTime(jd, lon);
        double ha = TimeUtils.HourAngle(jd, lon, lmst); // RA = LMST → HA = 0
        await Assert.That(ha).IsEqualTo(0.0).Within(0.001);
    }

    // ── ParallacticAngle ──────────────────────────────────────────────────────

    [Test]
    public async Task ParallacticAngle_AtTransit_IsZero()
    {
        // At hour angle = 0 (transit), q = 0 for objects south of observer
        double q = ObserverGeometry.ParallacticAngle(0.0, 0.0, 45.0);
        await Assert.That(q).IsEqualTo(0.0).Within(1e-9);
    }

    [Test]
    public async Task ParallacticAngle_EastOfMeridian_IsNegative()
    {
        // HA < 0 (east of meridian, i.e. HA ≈ 350° represented as 350° but functionally −10°)
        // Use HA = 330° (= −30° east), Dec = 30°, Lat = 51°
        double q = ObserverGeometry.ParallacticAngle(330.0, 30.0, 51.0);
        await Assert.That(q).IsLessThan(0.0);
    }

    [Test]
    public async Task ParallacticAngle_WestOfMeridian_IsPositive()
    {
        double q = ObserverGeometry.ParallacticAngle(30.0, 30.0, 51.0);
        await Assert.That(q).IsGreaterThan(0.0);
    }

    [Test]
    public async Task ParallacticAngle_IsWithinPm180Degrees()
    {
        double q = ObserverGeometry.ParallacticAngle(45.0, -10.0, 35.0);
        await Assert.That(q).IsGreaterThanOrEqualTo(-180.0);
        await Assert.That(q).IsLessThanOrEqualTo(180.0);
    }

    // ── RefractionCalculator ──────────────────────────────────────────────────

    [Test]
    public async Task GeometricToApparent_PositiveAltitude_IncreasesAltitude()
    {
        double geometric = 30.0;
        double apparent = RefractionCalculator.GeometricToApparent(geometric);
        await Assert.That(apparent).IsGreaterThan(geometric);
    }

    [Test]
    public async Task GeometricToApparent_Below_NegativeOne_ReturnsUnchanged()
    {
        double alt = -5.0;
        await Assert.That(RefractionCalculator.GeometricToApparent(alt)).IsEqualTo(alt).Within(1e-10);
    }

    [Test]
    public async Task ApparentToGeometric_PositiveAltitude_DecreasesAltitude()
    {
        double apparent = 30.0;
        double geometric = RefractionCalculator.ApparentToGeometric(apparent);
        await Assert.That(geometric).IsLessThan(apparent);
    }

    [Test]
    public async Task Refraction_RoundTrip_WithinTolerance()
    {
        // geometric → apparent → geometric should be within 0.001°
        double original = 15.0;
        double apparent = RefractionCalculator.GeometricToApparent(original);
        double recovered = RefractionCalculator.ApparentToGeometric(apparent);
        await Assert.That(recovered).IsEqualTo(original).Within(0.001);
    }

    [Test]
    public async Task Refraction_At90Degrees_IsNearZero()
    {
        // At zenith, refraction should be essentially 0
        double corr = RefractionCalculator.GeometricToApparent(90.0) - 90.0;
        await Assert.That(Math.Abs(corr)).IsLessThan(0.005); // < 0.005° at zenith
    }

    // ── Galactic Coordinates ──────────────────────────────────────────────────

    [Test]
    public async Task EquatorialToGalactic_GalacticCenter_GivesNearZeroZero()
    {
        // Galactic center J2000: RA ≈ 266.417°, Dec ≈ −28.936°
        var (l, b) = CoordinateConverter.EquatorialToGalactic(new EquatorialCoordinates(266.417, -28.936));
        await Assert.That(l).IsEqualTo(0.0).Within(1.0);
        await Assert.That(b).IsEqualTo(0.0).Within(1.0);
    }

    [Test]
    public async Task EquatorialToGalactic_NGP_GivesLatitude90()
    {
        // NGP: RA = 192.859°, Dec = 27.128°
        var (_, b) = CoordinateConverter.EquatorialToGalactic(new EquatorialCoordinates(192.859, 27.128));
        await Assert.That(b).IsEqualTo(90.0).Within(0.5);
    }

    [Test]
    public async Task GalacticToEquatorial_GalacticCenter_GivesCorrectEquatorial()
    {
        var eq = CoordinateConverter.GalacticToEquatorial(0.0, 0.0);
        await Assert.That(eq.RightAscension).IsEqualTo(266.4).Within(1.0);
        await Assert.That(eq.Declination).IsEqualTo(-28.9).Within(1.0);
    }

    [Test]
    public async Task GalacticCoordinates_RoundTrip_WithinHalfDegree()
    {
        // Arbitrary test point
        double ra = 120.0, dec = 45.0;
        var (l, b) = CoordinateConverter.EquatorialToGalactic(new EquatorialCoordinates(ra, dec));
        var back = CoordinateConverter.GalacticToEquatorial(l, b);
        await Assert.That(back.RightAscension).IsEqualTo(ra).Within(0.5);
        await Assert.That(back.Declination).IsEqualTo(dec).Within(0.5);
    }

    // ── Planet Illumination ───────────────────────────────────────────────────

    [Test]
    public async Task PlanetIllumination_JupiterAtOpposition_IsNearFull()
    {
        // At opposition r ≈ Δ, R ≈ Δ−r or similar; cos(i)→1 → k→1
        // Jupiter at opposition: r≈5.0 AU, Δ≈4.0 AU, R≈1.0 AU
        double k = PlanetPhysicalEphemeris.Illumination(5.0, 4.0, 1.0);
        await Assert.That(k).IsGreaterThan(0.99);
    }

    [Test]
    public async Task PlanetIllumination_Venus_CanBeLessThanHalf()
    {
        // Venus near inferior conjunction: r≈0.72 AU, Δ≈0.28 AU, R≈1.0 AU
        double k = PlanetPhysicalEphemeris.Illumination(0.72, 0.28, 1.0);
        await Assert.That(k).IsLessThan(0.5);
    }

    [Test]
    public async Task PlanetIllumination_IsInRange0To1()
    {
        double k = PlanetPhysicalEphemeris.Illumination(1.52, 0.52, 1.0);
        await Assert.That(k).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(k).IsLessThanOrEqualTo(1.0);
    }

    [Test]
    public async Task PhaseAngle_AtOpposition_IsNearZero()
    {
        // Simplified opposition: planet at (r, Δ=r-1, R=1) all collinear, planet behind Earth from Sun
        // cos(i) = (r² + (r-1)² - 1²) / (2·r·(r-1)) → approaches 1 for large r
        double i = PlanetPhysicalEphemeris.PhaseAngle(5.0, 4.0, 1.0);
        await Assert.That(i).IsLessThan(15.0); // outer planets have small phase angles
    }

    // ── Lunar Libration ──────────────────────────────────────────────────────

    [Test]
    public async Task LunarLibration_ReturnsValuesInExpectedRange()
    {
        double T = TimeUtils.JulianCentury(TimeUtils.JulianDay(2000, 1, 1, 12.0));
        var (lLon, lLat) = MoonEphemeris.Libration(T);
        await Assert.That(lLon).IsGreaterThanOrEqualTo(-8.5);
        await Assert.That(lLon).IsLessThanOrEqualTo(8.5);
        await Assert.That(lLat).IsGreaterThanOrEqualTo(-7.5);
        await Assert.That(lLat).IsLessThanOrEqualTo(7.5);
    }

    [Test]
    public async Task LunarLibration_DifferentDates_ProduceDifferentValues()
    {
        double T1 = TimeUtils.JulianCentury(TimeUtils.JulianDay(2000, 1, 1));
        double T2 = TimeUtils.JulianCentury(TimeUtils.JulianDay(2000, 7, 1));
        var (lon1, _) = MoonEphemeris.Libration(T1);
        var (lon2, _) = MoonEphemeris.Libration(T2);
        await Assert.That(Math.Abs(lon1 - lon2)).IsGreaterThan(0.1);
    }

    [Test]
    public async Task LunarLibration_J2000_IsFinite()
    {
        double T = 0.0;
        var (lLon, lLat) = MoonEphemeris.Libration(T);
        await Assert.That(double.IsFinite(lLon)).IsTrue();
        await Assert.That(double.IsFinite(lLat)).IsTrue();
    }
}
