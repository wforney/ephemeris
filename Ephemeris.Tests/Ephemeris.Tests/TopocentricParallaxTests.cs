// Updated: 2026-03-09
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Selenography;

namespace Ephemeris.Tests;

public class TopocentricParallaxTests
{
    /// <summary>
    /// Meeus Ch. 40 reference case (2003-Aug-28 03h17m UT, USNO Washington DC).
    /// Geocentric: RA = 339.530°, Dec = −15.771°, distance = 368409.7 km.
    /// Moon is ~32° east of the meridian → positive RA shift, negative Dec shift.
    /// Expected topocentric RA ≈ 339.959°, Dec ≈ −16.552° (within 0.05° tolerance).
    /// </summary>
    [Test]
    public async Task MeeusReferenceCase_ReturnsExpectedTopocentricCoordinates()
    {
        // Arrange
        double jd = 2452879.63542; // 2003-Aug-28 03h17m UT
        var geocentric = new EquatorialCoordinates(339.530, -15.771);
        double distanceKm = 368409.7;
        double longitude = -77.0657; // USNO Washington DC
        double latitude = 38.9215;
        double altitudeMeters = 92;

        // Act
        var topocentric = TopocentricParallax.ApplyLunarParallax(
            geocentric, distanceKm, jd, longitude, latitude, altitudeMeters);

        // Assert — within 0.05° tolerance (exact values depend on GMST precision)
        await Assert.That(topocentric.RightAscension).IsEqualTo(339.959).Within(0.05);
        await Assert.That(topocentric.Declination).IsEqualTo(-16.552).Within(0.05);
    }

    /// <summary>
    /// The total parallax correction magnitude must be between 0° and 1.1° for
    /// any realistic Moon distance and observer position.
    /// </summary>
    [Test]
    public async Task ParallaxCorrectionMagnitude_IsWithinExpectedRange()
    {
        // Arrange — Moon near the horizon for maximum parallax
        double jd = 2452879.63542;
        var geocentric = new EquatorialCoordinates(339.530, -15.771);
        double distanceKm = 368409.7;
        double longitude = -77.0657;
        double latitude = 38.9215;

        // Act
        var topocentric = TopocentricParallax.ApplyLunarParallax(
            geocentric, distanceKm, jd, longitude, latitude);

        // Assert — correction magnitude √(ΔRA² + ΔDec²) between 0° and 1.1°
        double deltaRA = topocentric.RightAscension - geocentric.RightAscension;
        // Handle wrap-around near 360°/0°
        if (deltaRA > 180) deltaRA -= 360;
        if (deltaRA < -180) deltaRA += 360;
        double deltaDec = topocentric.Declination - geocentric.Declination;
        double magnitude = Math.Sqrt((deltaRA * deltaRA) + (deltaDec * deltaDec));

        await Assert.That(magnitude).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(magnitude).IsLessThanOrEqualTo(1.1);
    }

    /// <summary>
    /// Higher observer altitude moves further from Earth's centre, producing a
    /// measurably different parallax correction than sea level.
    /// </summary>
    [Test]
    public async Task HigherAltitude_ProducesDifferentCorrection()
    {
        // Arrange
        double jd = 2452879.63542;
        var geocentric = new EquatorialCoordinates(339.530, -15.771);
        double distanceKm = 368409.7;
        double longitude = -77.0657;
        double latitude = 38.9215;

        // Act
        var atSeaLevel = TopocentricParallax.ApplyLunarParallax(
            geocentric, distanceKm, jd, longitude, latitude, 0);
        var atHighAltitude = TopocentricParallax.ApplyLunarParallax(
            geocentric, distanceKm, jd, longitude, latitude, 10_000);

        // Assert — the two results must differ measurably
        double raDiff = Math.Abs(atSeaLevel.RightAscension - atHighAltitude.RightAscension);
        double decDiff = Math.Abs(atSeaLevel.Declination - atHighAltitude.Declination);
        double totalDiff = raDiff + decDiff;

        await Assert.That(totalDiff).IsGreaterThan(0.0);
    }

    /// <summary>
    /// When the Moon is near the observer's meridian (hour angle ≈ 0), the RA
    /// correction Δα is small because sin(H) ≈ 0.
    /// </summary>
    [Test]
    public async Task MoonNearMeridian_HasSmallRACorrection()
    {
        // Arrange — place the observer's longitude such that LST ≈ RA, giving H ≈ 0
        double jd = 2452879.63542;
        double distanceKm = 384400; // mean lunar distance
        double latitude = 45.0;

        // GMST at this JD is ~338° — set longitude so LST = GMST + lon ≈ RA
        // We want LST ≈ RA, so pick RA = GMST + longitude
        double gmst = Ephemeris.Chronology.TimeUtils.GMST(jd);
        double targetRA = 100.0;
        double longitude = TimeUtils.NormalizeDegrees(targetRA - gmst);

        var geocentric = new EquatorialCoordinates(targetRA, 20.0);

        // Act
        var topocentric = TopocentricParallax.ApplyLunarParallax(
            geocentric, distanceKm, jd, longitude, latitude);

        // Assert — ΔRA should be very small (< 0.01°) when H ≈ 0
        double deltaRA = Math.Abs(topocentric.RightAscension - geocentric.RightAscension);
        if (deltaRA > 180) deltaRA = 360 - deltaRA;

        await Assert.That(deltaRA).IsLessThan(0.01);
    }
}
