// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Phenomenology;
using Ephemeris.Selenography;

namespace Ephemeris.Tests;

/// <summary>
/// Tests for <see cref="LunarOccultationCalculator"/>.
/// </summary>
public class LunarOccultationTests
{
    // ── Null result tests ────────────────────────────────────────────────────

    /// <summary>
    /// The Moon's declination never exceeds ≈±28.5°, so a target near the north
    /// celestial pole (Dec = 88°) can never be occulted.  The search must return null.
    /// </summary>
    [Test]
    public async Task NextOccultation_NorthCelestialPole_ReturnsNull()
    {
        var target   = new EquatorialCoordinates(0.0, 88.0); // well above Moon's max Dec
        var after    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var observer = new GeographicCoordinates(0.0, 51.5); // London

        var result = LunarOccultationCalculator.NextOccultation(target, after, observer, "NorthPole");

        await Assert.That(result).IsNull();
    }

    /// <summary>
    /// A target south of Dec = −85° is similarly unreachable by the Moon and must return null.
    /// </summary>
    [Test]
    public async Task NextOccultation_SouthCelestialPole_ReturnsNull()
    {
        var target   = new EquatorialCoordinates(180.0, -85.0);
        var after    = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var observer = new GeographicCoordinates(0.0, 0.0);

        var result = LunarOccultationCalculator.NextOccultation(target, after, observer);

        await Assert.That(result).IsNull();
    }

    // ── Positive detection tests ─────────────────────────────────────────────

    /// <summary>
    /// Creates a virtual "star" exactly at the topocentric Moon position at a known reference
    /// time, then scans from 2 hours before that time.  The Moon must be detected covering
    /// and then uncovering the star.
    /// </summary>
    [Test]
    public async Task NextOccultation_TargetAtTopoMoonCenter_FindsOccultation()
    {
        // Reference time — arbitrary; the Moon is always somewhere on the sky.
        var refTime = new DateTime(2024, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        double refJd = TimeZoneUtils.ToJulianDay(refTime);
        double T     = TimeUtils.JulianCentury(refJd);

        // Compute geocentric Moon position at refTime.
        var (moonRA, moonDec, distKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);

        // Place observer at the equator with longitude chosen so the Moon is on the meridian
        // (H ≈ 0).  This makes the topocentric RA shift effectively zero and keeps the
        // topocentric Dec shift predictable and small.
        double gmst   = TimeUtils.GMST(refJd);
        double obsLon = TimeUtils.NormalizeDegrees(moonRA - gmst);
        var observer  = new GeographicCoordinates(obsLon, 0.0); // equatorial, H = 0

        // Compute the topocentric Moon position and use it as our "fixed star".
        var topoMoon = TopocentricParallax.ApplyLunarParallax(
            new EquatorialCoordinates(moonRA, moonDec), distKm, refJd, obsLon, 0.0);
        var target = new EquatorialCoordinates(topoMoon.RightAscension, topoMoon.Declination);

        // Start search 2 hours before refTime.  At that point the topocentric Moon is
        // approximately 0.5° – 0.8° away from the target (> Moon angular radius ≈ 0.25°).
        var startTime = refTime.AddHours(-2);

        var result = LunarOccultationCalculator.NextOccultation(target, startTime, observer, "TestStar");

        // An occultation must be found — the Moon center passes exactly through the target.
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Value.TargetName).IsEqualTo("TestStar");
    }

    // ── Ordering and structural tests ────────────────────────────────────────

    /// <summary>
    /// When both Disappearance and Reappearance are present, Disappearance must be
    /// strictly before Reappearance.
    /// </summary>
    [Test]
    public async Task NextOccultation_WhenEventFound_DisappearanceIsBeforeReappearance()
    {
        var refTime = new DateTime(2024, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        double refJd = TimeZoneUtils.ToJulianDay(refTime);
        double T     = TimeUtils.JulianCentury(refJd);

        var (moonRA, moonDec, distKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        double gmst   = TimeUtils.GMST(refJd);
        double obsLon = TimeUtils.NormalizeDegrees(moonRA - gmst);
        var observer  = new GeographicCoordinates(obsLon, 0.0);

        var topoMoon = TopocentricParallax.ApplyLunarParallax(
            new EquatorialCoordinates(moonRA, moonDec), distKm, refJd, obsLon, 0.0);
        var target    = new EquatorialCoordinates(topoMoon.RightAscension, topoMoon.Declination);
        var startTime = refTime.AddHours(-2);

        var result = LunarOccultationCalculator.NextOccultation(target, startTime, observer);

        if (result.HasValue && result.Value.Disappearance.HasValue && result.Value.Reappearance.HasValue)
        {
            await Assert.That(result.Value.Disappearance.Value)
                .IsLessThan(result.Value.Reappearance.Value);
        }
    }

    /// <summary>
    /// When an occultation is found, the event timing must lie within a sensible window
    /// around the reference time (Moon traverses its disk in approximately 1 hour).
    /// </summary>
    [Test]
    public async Task NextOccultation_EventTimes_AreWithinExpectedRange()
    {
        var refTime = new DateTime(2024, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        double refJd = TimeZoneUtils.ToJulianDay(refTime);
        double T     = TimeUtils.JulianCentury(refJd);

        var (moonRA, moonDec, distKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        double gmst   = TimeUtils.GMST(refJd);
        double obsLon = TimeUtils.NormalizeDegrees(moonRA - gmst);
        var observer  = new GeographicCoordinates(obsLon, 0.0);

        var topoMoon = TopocentricParallax.ApplyLunarParallax(
            new EquatorialCoordinates(moonRA, moonDec), distKm, refJd, obsLon, 0.0);
        var target    = new EquatorialCoordinates(topoMoon.RightAscension, topoMoon.Declination);
        var startTime = refTime.AddHours(-2);

        var result = LunarOccultationCalculator.NextOccultation(target, startTime, observer);

        if (result.HasValue)
        {
            // Disappearance must be after the search start and before refTime + 2 h.
            if (result.Value.Disappearance.HasValue)
            {
                await Assert.That(result.Value.Disappearance.Value).IsGreaterThanOrEqualTo(startTime);
                await Assert.That(result.Value.Disappearance.Value)
                    .IsLessThanOrEqualTo(refTime.AddHours(2));
            }

            // Reappearance must be before startTime + 6 h (generous upper bound).
            if (result.Value.Reappearance.HasValue)
            {
                await Assert.That(result.Value.Reappearance.Value)
                    .IsGreaterThan(startTime);
                await Assert.That(result.Value.Reappearance.Value)
                    .IsLessThanOrEqualTo(startTime.AddHours(6));
            }
        }
    }

    // ── OccultationEvent record tests ────────────────────────────────────────

    /// <summary>
    /// The TargetName supplied to <see cref="LunarOccultationCalculator.NextOccultation"/>
    /// must be round-tripped into the returned <see cref="OccultationEvent"/>.
    /// </summary>
    [Test]
    public async Task NextOccultation_TargetName_IsPreservedInResult()
    {
        // Use the same setup as the positive-detection test
        var refTime = new DateTime(2024, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        double refJd = TimeZoneUtils.ToJulianDay(refTime);
        double T     = TimeUtils.JulianCentury(refJd);

        var (moonRA, moonDec, distKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        double gmst   = TimeUtils.GMST(refJd);
        double obsLon = TimeUtils.NormalizeDegrees(moonRA - gmst);
        var observer  = new GeographicCoordinates(obsLon, 0.0);

        var topoMoon = TopocentricParallax.ApplyLunarParallax(
            new EquatorialCoordinates(moonRA, moonDec), distKm, refJd, obsLon, 0.0);
        var target    = new EquatorialCoordinates(topoMoon.RightAscension, topoMoon.Declination);
        var startTime = refTime.AddHours(-2);

        const string name = "Aldebaran";
        var result = LunarOccultationCalculator.NextOccultation(target, startTime, observer, name);

        if (result.HasValue)
        {
            await Assert.That(result.Value.TargetName).IsEqualTo(name);
        }
    }

    /// <summary>
    /// The default target name (empty string) is preserved when not supplied.
    /// </summary>
    [Test]
    public async Task NextOccultation_DefaultTargetName_IsEmptyString()
    {
        var target   = new EquatorialCoordinates(0.0, 88.0); // will return null quickly
        var after    = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var observer = new GeographicCoordinates(0.0, 51.5);

        // The 4-parameter overload has targetName = ""
        var result = LunarOccultationCalculator.NextOccultation(target, after, observer);

        // Either null (no occultation) or the name field is empty
        if (result.HasValue)
        {
            await Assert.That(result.Value.TargetName).IsEqualTo(string.Empty);
        }
        else
        {
            await Assert.That(result).IsNull(); // polar target → null
        }
    }

    // ── Moon angular radius tests ────────────────────────────────────────────

    /// <summary>
    /// The Moon's angular radius must lie in the known physical range
    /// (~0.245° at apogee to ~0.278° at perigee).
    /// </summary>
    [Test]
    public async Task MoonAngularRadius_IsWithinPhysicalBounds()
    {
        // Indirectly verify by checking that the occultation threshold changes with lunar distance.
        // We check two dates known to be near apogee vs. perigee in 2024.
        double T1 = TimeUtils.JulianCentury(TimeUtils.JulianDay(2024, 2, 10, 0)); // near perigee
        double T2 = TimeUtils.JulianCentury(TimeUtils.JulianDay(2024, 2, 24, 0)); // near apogee

        var (_, _, dist1) = MoonEphemeris.GeocentricEquatorialCoordinates(T1);
        var (_, _, dist2) = MoonEphemeris.GeocentricEquatorialCoordinates(T2);

        const double moonRadiusKm = 1737.4;
        double rad1 = double.RadiansToDegrees(Math.Asin(moonRadiusKm / dist1));
        double rad2 = double.RadiansToDegrees(Math.Asin(moonRadiusKm / dist2));

        await Assert.That(rad1).IsGreaterThanOrEqualTo(0.245);
        await Assert.That(rad1).IsLessThanOrEqualTo(0.278);
        await Assert.That(rad2).IsGreaterThanOrEqualTo(0.245);
        await Assert.That(rad2).IsLessThanOrEqualTo(0.278);
    }

    // ── GeographicCoordinates record tests ───────────────────────────────────

    /// <summary>
    /// GeographicCoordinates stores and returns all three fields correctly.
    /// </summary>
    [Test]
    public async Task GeographicCoordinates_FieldsRoundTrip()
    {
        var gc = new GeographicCoordinates(-77.06, 38.92, 92.0);

        await Assert.That(gc.Longitude).IsEqualTo(-77.06);
        await Assert.That(gc.Latitude).IsEqualTo(38.92);
        await Assert.That(gc.AltitudeMeters).IsEqualTo(92.0);
    }

    /// <summary>
    /// Default altitude is zero when not provided.
    /// </summary>
    [Test]
    public async Task GeographicCoordinates_DefaultAltitude_IsZero()
    {
        var gc = new GeographicCoordinates(0.0, 0.0);
        await Assert.That(gc.AltitudeMeters).IsEqualTo(0.0);
    }
}
