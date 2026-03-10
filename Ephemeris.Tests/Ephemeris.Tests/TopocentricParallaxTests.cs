// Updated: 2026-03-10
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

    // ─────────────────── Sun parallax ───────────────────────────────────────

    /// <summary>
    /// The Sun's topocentric parallax (solar parallax) must be ≤ 8.794″ ≈ 0.00244°.
    /// This is the largest possible solar parallax (body at horizon, mean distance).
    /// </summary>
    [Test]
    public async Task SunParallax_IsLessThanMaxSolarParallax()
    {
        // Mean Sun distance: 1 AU = 149_597_870.7 km → solar parallax = 8.794″ = 0.002443°
        const double meanSunDistanceKm = 149_597_870.7;
        double jd = 2451545.0; // J2000.0
        var geocentric = new EquatorialCoordinates(280.0, 0.0);

        // Act
        var topocentric = TopocentricParallax.ApplyParallax(
            geocentric, meanSunDistanceKm, jd, longitude: -77.0, latitude: 38.9);

        double deltaRA = Math.Abs(topocentric.RightAscension - geocentric.RightAscension);
        if (deltaRA > 180) deltaRA = 360 - deltaRA;
        double deltaDec = Math.Abs(topocentric.Declination - geocentric.Declination);
        double magnitude = Math.Sqrt((deltaRA * deltaRA) + (deltaDec * deltaDec));

        // Solar parallax upper bound: 0.00244°
        await Assert.That(magnitude).IsLessThanOrEqualTo(0.00244);
    }

    /// <summary>
    /// When a body is at the zenith (hour angle = 0, Dec = observer latitude),
    /// the parallax shift is nearly zero because the observer is directly below the body.
    /// </summary>
    [Test]
    public async Task BodyAtZenith_HasNegligibleParallaxShift()
    {
        // Place Sun directly overhead: LST = RA, Dec = latitude
        double jd = 2451545.0;
        double latitude = 45.0;
        double gmst = TimeUtils.GMST(jd);
        double targetRA = 100.0;
        double longitude = TimeUtils.NormalizeDegrees(targetRA - gmst); // ensures H ≈ 0

        var geocentric = new EquatorialCoordinates(targetRA, latitude); // Dec = lat → near zenith
        const double sunDistanceKm = 149_597_870.7;

        // Act
        var topocentric = TopocentricParallax.ApplyParallax(
            geocentric, sunDistanceKm, jd, longitude, latitude);

        double deltaRA = Math.Abs(topocentric.RightAscension - geocentric.RightAscension);
        if (deltaRA > 180) deltaRA = 360 - deltaRA;
        double deltaDec = Math.Abs(topocentric.Declination - geocentric.Declination);

        await Assert.That(deltaRA).IsLessThan(0.00001);
        await Assert.That(deltaDec).IsLessThan(0.00244); // still within full solar parallax bound
    }

    // ─────────────────── Planet parallax ────────────────────────────────────

    /// <summary>
    /// Mars at opposition is ~0.524 AU from Earth (perihelion opposition).
    /// Its parallax can reach up to ~23″ ≈ 0.0064°.
    /// The shift must be non-zero and within physical bounds.
    /// </summary>
    [Test]
    public async Task MarsAtOpposition_ParallaxIsNonZeroAndBounded()
    {
        // Mars near perihelion opposition: ~0.524 AU
        const double marsOppositionKm = 0.524 * 149_597_870.7;
        double jd = 2451545.0;
        var geocentric = new EquatorialCoordinates(280.0, -25.0);
        double longitude = -77.0;
        double latitude = 38.9;

        var topocentric = TopocentricParallax.ApplyParallax(
            geocentric, marsOppositionKm, jd, longitude, latitude);

        double deltaRA = Math.Abs(topocentric.RightAscension - geocentric.RightAscension);
        if (deltaRA > 180) deltaRA = 360 - deltaRA;
        double deltaDec = Math.Abs(topocentric.Declination - geocentric.Declination);
        double magnitude = Math.Sqrt((deltaRA * deltaRA) + (deltaDec * deltaDec));

        await Assert.That(magnitude).IsGreaterThan(0.0);
        // Mars parallax upper bound: ~23″ ≈ 0.0064°
        await Assert.That(magnitude).IsLessThanOrEqualTo(0.0064);
    }

    /// <summary>
    /// A body farther away (e.g., Jupiter at 4 AU) must produce a smaller
    /// parallax shift than a closer body (Mars at 0.524 AU) for the same geometry.
    /// </summary>
    [Test]
    public async Task FartherBody_HasSmallerParallaxThanCloserBody()
    {
        double jd = 2451545.0;
        var geocentric = new EquatorialCoordinates(200.0, 10.0);
        double longitude = -77.0;
        double latitude = 38.9;

        const double marsKm    = 0.524 * 149_597_870.7;
        const double jupiterKm = 4.2   * 149_597_870.7;

        var topoMars    = TopocentricParallax.ApplyParallax(geocentric, marsKm,    jd, longitude, latitude);
        var topoJupiter = TopocentricParallax.ApplyParallax(geocentric, jupiterKm, jd, longitude, latitude);

        double deltaRaMars    = Math.Abs(topoMars.RightAscension    - geocentric.RightAscension);
        double deltaDecMars   = Math.Abs(topoMars.Declination       - geocentric.Declination);
        double magnitudeMars  = Math.Sqrt((deltaRaMars * deltaRaMars) + (deltaDecMars * deltaDecMars));

        double deltaRaJup    = Math.Abs(topoJupiter.RightAscension  - geocentric.RightAscension);
        double deltaDecJup   = Math.Abs(topoJupiter.Declination     - geocentric.Declination);
        double magnitudeJup  = Math.Sqrt((deltaRaJup * deltaRaJup)  + (deltaDecJup * deltaDecJup));

        await Assert.That(magnitudeMars).IsGreaterThan(magnitudeJup);
    }
}
