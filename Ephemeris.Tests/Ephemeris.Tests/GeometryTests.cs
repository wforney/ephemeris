using Ephemeris.Geometry;
using Ephemeris.Chronology;

namespace Ephemeris.Tests;

/// <summary>Unit tests for <see cref="ObserverGeometry"/> and <see cref="CoordinateConverter"/>.</summary>
public class GeometryTests
{
    // Tolerance for angle comparisons (degrees)
    private const double AngleTol = 0.5;
    private const double TightTol = 0.001;

    // ── ObserverGeometry.EquatorialToHorizontal ──────────────────────────────

    [Test]
    public async Task EquatorialToHorizontal_PolarisDueNorth_AzimuthNearZero()
    {
        // Polaris: RA ≈ 37.95°, Dec ≈ 89.26°  (J2000)
        // From latitude 41.85° N (Chicago), Polaris culminates near due North,
        // altitude ≈ latitude when on the meridian.
        // Use JD 2451545.0 = J2000.0 as the epoch.
        double jd = 2451545.0;
        double lon = -87.65, lat = 41.85;
        double ra = 37.95, dec = 89.26;

        var (az, alt) = ObserverGeometry.EquatorialToHorizontal(ra, dec, jd, lon, lat, applyRefraction: false);

        // Altitude should be within a few degrees of geographic latitude
        await Assert.That(alt).IsGreaterThan(38.0);
        await Assert.That(alt).IsLessThan(50.0);
        // Azimuth should be within 10° of North (0° or 360°)
        double azNorthDist = Math.Min(az, 360.0 - az);
        await Assert.That(azNorthDist).IsLessThan(10.0);
    }

    [Test]
    public async Task EquatorialToHorizontal_RefractedAltitudeHigherThanGeometric()
    {
        // Refraction always adds positive correction for altitude > -1°
        double jd = 2451545.0;
        double ra = 100.0, dec = 20.0, lon = 0.0, lat = 45.0;

        var (_, altNoRefract) = ObserverGeometry.EquatorialToHorizontal(ra, dec, jd, lon, lat, applyRefraction: false);
        var (_, altWithRefract) = ObserverGeometry.EquatorialToHorizontal(ra, dec, jd, lon, lat, applyRefraction: true);

        if (altNoRefract > -1.0)
            await Assert.That(altWithRefract).IsGreaterThan(altNoRefract);
        else
            await Assert.That(altWithRefract).IsEqualTo(altNoRefract);
    }

    // ── ObserverGeometry.ApplyRefraction ─────────────────────────────────────

    [Test]
    public async Task ApplyRefraction_AtHorizon_AboutHalfDegreeCorrection()
    {
        // At 0° geometric altitude Bennett formula gives ≈ 34' ≈ 0.567°
        double corrected = ObserverGeometry.ApplyRefraction(0.0);
        await Assert.That(corrected).IsGreaterThan(0.4);
        await Assert.That(corrected).IsLessThan(0.7);
    }

    [Test]
    public async Task ApplyRefraction_AtZenith_NearlyZeroCorrection()
    {
        // At 90° altitude refraction is ~0.016' — negligible
        double corrected = ObserverGeometry.ApplyRefraction(90.0);
        await Assert.That(corrected - 90.0).IsLessThan(0.01);
    }

    [Test]
    public async Task ApplyRefraction_BelowMinus1Deg_NoCorrection()
    {
        double input = -5.0;
        double corrected = ObserverGeometry.ApplyRefraction(input);
        await Assert.That(corrected).IsEqualTo(input);
    }

    // ── CoordinateConverter ecliptic ↔ equatorial round-trip ────────────────

    [Test]
    public async Task EclipticToEquatorial_RoundTrip_WithinTolerance()
    {
        double lon = 120.0, lat = 5.0, T = 0.1;
        var (ra, dec) = CoordinateConverter.EclipticToEquatorial(lon, lat, T);
        var (lonBack, latBack) = CoordinateConverter.EquatorialToEcliptic(ra, dec, T);

        await Assert.That(Math.Abs(lonBack - lon)).IsLessThanOrEqualTo(TightTol);
        await Assert.That(Math.Abs(latBack - lat)).IsLessThanOrEqualTo(TightTol);
    }

    [Test]
    public async Task EclipticToEquatorial_VernalEquinox_RaZero()
    {
        // Ecliptic (0°, 0°) at T=0 should give RA≈0, Dec≈0
        var (ra, dec) = CoordinateConverter.EclipticToEquatorial(0.0, 0.0, 0.0);
        await Assert.That(Math.Abs(ra)).IsLessThanOrEqualTo(AngleTol);
        await Assert.That(Math.Abs(dec)).IsLessThanOrEqualTo(AngleTol);
    }

    // ── CoordinateConverter.AngularSeparation ─────────────────────────────

    [Test]
    public async Task AngularSeparation_SamePoint_Zero()
    {
        double sep = CoordinateConverter.AngularSeparation(120.0, 30.0, 120.0, 30.0);
        await Assert.That(sep).IsEqualTo(0.0);
    }

    [Test]
    public async Task AngularSeparation_Antipodal_180Degrees()
    {
        // Two points that are exactly antipodal on the sphere
        double sep = CoordinateConverter.AngularSeparation(0.0, 90.0, 0.0, -90.0);
        await Assert.That(Math.Abs(sep - 180.0)).IsLessThanOrEqualTo(TightTol);
    }

    [Test]
    public async Task AngularSeparation_KnownPair_WithinTolerance()
    {
        // Sirius: RA=101.287°, Dec=-16.716°
        // Betelgeuse: RA=88.793°, Dec=7.407°
        // Known angular separation ≈ 27.2°
        double sep = CoordinateConverter.AngularSeparation(101.287, -16.716, 88.793, 7.407);
        await Assert.That(Math.Abs(sep - 27.2)).IsLessThan(0.5);
    }

    // ── ObserverGeometry.ParallacticAngle ────────────────────────────────────

    [Test]
    public async Task ParallacticAngle_TransitSouthOfZenith_ReturnsZero()
    {
        // Object south of zenith (dec=20° < lat=45°) at transit (H=0).
        // Zenith is north of the object, north pole is also north → angle = 0°.
        // denominator = tan(45°)·cos(20°) − sin(20°) = 0.940 − 0.342 = 0.598 > 0
        // q = atan2(0, 0.598) = 0°
        double q = ObserverGeometry.ParallacticAngle(0.0, 20.0, 45.0);
        await Assert.That(Math.Abs(q)).IsLessThanOrEqualTo(TightTol);
    }

    [Test]
    public async Task ParallacticAngle_TransitNorthOfZenith_Returns180Degrees()
    {
        // Object north of zenith (dec=60° > lat=45°) at transit (H=0).
        // Zenith is south of the object, north pole is north → angle = 180°.
        // denominator = tan(45°)·cos(60°) − sin(60°) = 0.500 − 0.866 = −0.366 < 0
        // q = atan2(0, −0.366) = ±180°
        double q = ObserverGeometry.ParallacticAngle(0.0, 60.0, 45.0);
        await Assert.That(Math.Abs(Math.Abs(q) - 180.0)).IsLessThanOrEqualTo(TightTol);
    }

    [Test]
    public async Task ParallacticAngle_ObjectAtZenith_ReturnsZero()
    {
        // Degenerate case: dec = lat, H = 0 → object exactly at zenith.
        // Both sin(H) = 0 and denominator = tan(lat)·cos(lat) − sin(lat) = 0.
        // Special-case guard returns 0°.
        double q = ObserverGeometry.ParallacticAngle(0.0, 45.0, 45.0);
        await Assert.That(q).IsEqualTo(0.0);
    }

    [Test]
    public async Task ParallacticAngle_WestOfMeridian_PositiveAngle()
    {
        // H > 0 (object west of meridian) with object south of zenith → q > 0.
        double q = ObserverGeometry.ParallacticAngle(45.0, 20.0, 45.0);
        await Assert.That(q).IsGreaterThan(0.0);
    }

    [Test]
    public async Task ParallacticAngle_EastOfMeridian_NegativeAngle()
    {
        // H < 0 (object east of meridian) with object south of zenith → q < 0.
        double q = ObserverGeometry.ParallacticAngle(-45.0, 20.0, 45.0);
        await Assert.That(q).IsLessThan(0.0);
    }

    [Test]
    public async Task ParallacticAngle_OppositeHourAngles_OppositeSign()
    {
        // ParallacticAngle(+H, ...) = −ParallacticAngle(−H, ...) by symmetry.
        double qWest = ObserverGeometry.ParallacticAngle(30.0, 25.0, 50.0);
        double qEast = ObserverGeometry.ParallacticAngle(-30.0, 25.0, 50.0);
        await Assert.That(Math.Abs(qWest + qEast)).IsLessThanOrEqualTo(TightTol);
    }

    [Test]
    public async Task ParallacticAngle_KnownValue_WithinTolerance()
    {
        // lat=50°, dec=30°, H=45°:
        // sin(45°) = 0.7071, tan(50°) = 1.1918, cos(30°) = 0.8660
        // sin(30°) = 0.5000, cos(45°) = 0.7071
        // denom = 1.1918 × 0.8660 − 0.5 × 0.7071 = 1.0321 − 0.3536 = 0.6786
        // q = atan2(0.7071, 0.6786) ≈ 46.15°
        double q = ObserverGeometry.ParallacticAngle(45.0, 30.0, 50.0);
        await Assert.That(Math.Abs(q - 46.15)).IsLessThan(0.1);
    }
}
