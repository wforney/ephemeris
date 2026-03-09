using Ephemeris;
using Ephemeris.Chronology;
using Ephemeris.Heliology;
using Ephemeris.Phenomenology;
using Ephemeris.Selenography;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Validates solar ephemeris accuracy against JPL Horizons reference values.
/// Reference: JPL Horizons Web Interface (https://ssd.jpl.nasa.gov/horizons/)
/// Observer: Geocentric (500@0), output: J2000 equatorial ICRF apparent RA/Dec
/// </summary>
public class SolarEphemerisTests
{
    // JPL Horizons geocentric apparent RA/Dec on 2000-Jan-01.5 (J2000.0)
    // RA = 18h 45m 39.8s = 281.4158°, Dec = -23.0166°  (≈ 281.4°, −23.0°)
    [Test]
    public async Task SunRa_AtJ2000_IsApproximatelyCorrect()
    {
        double T = 0.0;   // J2000.0
        var (ra, _, _) = SunEphemeris.ApparentEquatorialCoordinates(T);
        // Allow 0.5° tolerance for the simplified algorithm
        await Assert.That(Math.Abs(ra - 281.42)).IsLessThan(0.5);
    }

    [Test]
    public async Task SunDec_AtJ2000_IsApproximatelyCorrect()
    {
        double T = 0.0;
        var (_, dec, _) = SunEphemeris.ApparentEquatorialCoordinates(T);
        await Assert.That(Math.Abs(dec - (-23.02))).IsLessThan(0.5);
    }

    // JPL Horizons: 2024-Jun-21 12:00:00 UT
    // RA ≈ 90.2°  Dec ≈ +23.4° (summer solstice)
    [Test]
    public async Task SunRa_AtSummerSolstice2024_IsApproximatelyCorrect()
    {
        double jd = TimeZoneUtils.ToJulianDay(new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc));
        double T  = TimeUtils.JulianCentury(jd);
        var (ra, _, _) = SunEphemeris.ApparentEquatorialCoordinates(T);
        await Assert.That(Math.Abs(ra - 90.2)).IsLessThan(1.0);
    }

    [Test]
    public async Task SunDec_AtSummerSolstice2024_IsNearMaximum()
    {
        double jd = TimeZoneUtils.ToJulianDay(new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc));
        double T  = TimeUtils.JulianCentury(jd);
        var (_, dec, _) = SunEphemeris.ApparentEquatorialCoordinates(T);
        // Dec at summer solstice is near +23.44° (axial tilt)
        await Assert.That(Math.Abs(dec - 23.44)).IsLessThan(0.1);
    }

    [Test]
    public async Task SunDistance_IsNearOneAu()
    {
        double jd = TimeZoneUtils.ToJulianDay(new DateTime(2024, 6, 21, 12, 0, 0, DateTimeKind.Utc));
        double T  = TimeUtils.JulianCentury(jd);
        var (_, _, r) = SunEphemeris.ApparentEquatorialCoordinates(T);
        // Earth–Sun distance varies from 0.983 to 1.017 AU
        await Assert.That(r).IsGreaterThan(0.98);
        await Assert.That(r).IsLessThan(1.02);
    }

    [Test]
    public async Task SeasonCalculator_SpringEquinox2024_IsInMarch()
    {
        DateTime equinox = SeasonCalculator.Calculate(2024, SeasonCalculator.Season.SpringEquinox);
        await Assert.That(equinox.Year).IsEqualTo(2024);
        await Assert.That(equinox.Month).IsEqualTo(3);
        // 2024 vernal equinox was March 20
        await Assert.That(Math.Abs((equinox.Day - 20))).IsLessThan(2);
    }

    [Test]
    public async Task SeasonCalculator_SummerSolstice2024_IsInJune()
    {
        DateTime solstice = SeasonCalculator.Calculate(2024, SeasonCalculator.Season.SummerSolstice);
        await Assert.That(solstice.Year).IsEqualTo(2024);
        await Assert.That(solstice.Month).IsEqualTo(6);
        await Assert.That(Math.Abs((solstice.Day - 20))).IsLessThan(2);
    }
}
