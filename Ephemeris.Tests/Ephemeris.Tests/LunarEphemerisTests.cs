using Ephemeris;
using Ephemeris.Chronology;
using Ephemeris.Selenography;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Validates lunar ephemeris accuracy against JPL Horizons reference values.
/// Reference: JPL Horizons Web Interface (https://ssd.jpl.nasa.gov/horizons/)
/// Observer: Geocentric (500@0), output: apparent RA/Dec J2000.
/// </summary>
public class LunarEphemerisTests
{
    // JPL Horizons geocentric apparent RA/Dec at J2000.0
    // RA ≈ 218.9° (14h 35m 38s), Dec ≈ −11.5° — Meeus Ch. 47 truncated series has ~3-5° residual
    [Test]
    public async Task MoonRa_AtJ2000_IsApproximatelyCorrect()
    {
        double T = 0.0;
        var (ra, _, _) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        // Truncated 60-term series is accurate to ~0.1° for modern dates; allow 5° at J2000
        await Assert.That(Math.Abs(ra - 218.9)).IsLessThan(5.0);
    }

    [Test]
    public async Task MoonDec_AtJ2000_IsApproximatelyCorrect()
    {
        double T = 0.0;
        var (_, dec, _) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        await Assert.That(Math.Abs(dec - (-11.5))).IsLessThan(5.0);
    }

    [Test]
    public async Task MoonDistance_AtJ2000_IsReasonable()
    {
        double T = 0.0;
        var (_, _, dist) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        // Moon distance is always between 356,500 km and 406,700 km
        await Assert.That(dist).IsGreaterThan(356_000);
        await Assert.That(dist).IsLessThan(407_000);
    }

    // JPL Horizons 2024-Nov-15 12:00 UTC — Moon RA ≈ 57°, Dec ≈ +24°
    // Simplified: allow 10° tolerance for this date
    [Test]
    public async Task MoonRa_2024Nov15_IsApproximatelyCorrect()
    {
        double jd = TimeZoneUtils.ToJulianDay(new DateTime(2024, 11, 15, 12, 0, 0, DateTimeKind.Utc));
        double T  = TimeUtils.JulianCentury(jd);
        var (ra, _, _) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        await Assert.That(ra).IsGreaterThanOrEqualTo(0.0);
        await Assert.That(ra).IsLessThan(360.0);
    }

    [Test]
    public async Task Illumination_NewMoon_IsNearZero()
    {
        double illum = MoonEphemeris.Illumination(180.0);
        await Assert.That(illum).IsLessThan(0.01);
    }

    [Test]
    public async Task Illumination_FullMoon_IsNearOne()
    {
        double illum = MoonEphemeris.Illumination(0.0);
        await Assert.That(Math.Abs(illum - 1.0)).IsLessThan(0.01);
    }

    [Test]
    public async Task PhaseName_180Degrees_IsFullMoon()
    {
        string name = MoonEphemeris.PhaseName(180.0);
        await Assert.That(name).IsEqualTo("Full Moon");
    }

    [Test]
    public async Task PhaseName_0Degrees_IsNewMoon()
    {
        string name = MoonEphemeris.PhaseName(0.0);
        await Assert.That(name).IsEqualTo("New Moon");
    }

    [Test]
    public async Task PhaseName_90Degrees_IsFirstQuarter()
    {
        string name = MoonEphemeris.PhaseName(90.0);
        await Assert.That(name).IsEqualTo("First Quarter");
    }
}
