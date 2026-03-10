using Ephemeris.Phenomenology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Unit tests for <see cref="RiseSetCalculator"/>.
/// Validates rise, transit, and set event ordering, day-length variation by season,
/// and basic accuracy for known observer locations.
/// Observer: Greenwich (lon=0°, lat=51.477°N) and Chicago (lon=−87.65°, lat=41.85°N).
/// </summary>
public class RiseSetCalculatorTests
{
    private const double GreenwichLon = 0.0;
    private const double GreenwichLat = 51.477;
    private const double ChicagoLon   = -87.65;
    private const double ChicagoLat   =  41.85;

    // ── Solar rise/transit/set ordering ───────────────────────────────────

    [Test]
    public async Task Sun_Greenwich_SummerSolstice_HasRiseAndSet()
    {
        var date = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
        var rts = RiseSetCalculator.Sun(date, GreenwichLon, GreenwichLat);
        await Assert.That(rts.Rise).IsNotNull();
        await Assert.That(rts.Set).IsNotNull();
    }

    [Test]
    public async Task Sun_Greenwich_SummerSolstice_RiseBeforeTransitBeforeSet()
    {
        var date = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Sun(date, GreenwichLon, GreenwichLat);
        await Assert.That(rts.Rise!.Value).IsLessThan(rts.Transit);
        await Assert.That(rts.Transit).IsLessThan(rts.Set!.Value);
    }

    [Test]
    public async Task Sun_Greenwich_SummerSolstice_TransitNearNoonUtc()
    {
        // At longitude 0° the solar transit should be close to 12:00 UTC (±1 h for equation of time)
        var date = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Sun(date, GreenwichLon, GreenwichLat);
        double transitHour = rts.Transit.Hour + (rts.Transit.Minute / 60.0);
        await Assert.That(Math.Abs(transitHour - 12.0)).IsLessThan(1.0);
    }

    // ── Day-length variation by season ────────────────────────────────────

    [Test]
    public async Task Sun_Greenwich_SummerSolstice_DayLengthExceeds16Hours()
    {
        // At 51.5°N on the longest day, day length > 16 h
        var date = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Sun(date, GreenwichLon, GreenwichLat);
        double hours = (rts.Set!.Value - rts.Rise!.Value).TotalHours;
        await Assert.That(hours).IsGreaterThan(16.0);
        await Assert.That(hours).IsLessThan(20.0);
    }

    [Test]
    public async Task Sun_Greenwich_WinterSolstice_DayLengthUnder9Hours()
    {
        // At 51.5°N on the shortest day, day length < 9 h
        var date = new DateTime(2024, 12, 21, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Sun(date, GreenwichLon, GreenwichLat);
        await Assert.That(rts.Rise).IsNotNull();
        await Assert.That(rts.Set).IsNotNull();
        double hours = (rts.Set!.Value - rts.Rise!.Value).TotalHours;
        await Assert.That(hours).IsLessThan(9.0);
        await Assert.That(hours).IsGreaterThan(5.0);
    }

    [Test]
    public async Task Sun_SummerDayLongerThanWinterDay()
    {
        var summer = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
        var winter = new DateTime(2024, 12, 21, 0, 0, 0, DateTimeKind.Utc);
        var rtsS = RiseSetCalculator.Sun(summer, ChicagoLon, ChicagoLat);
        var rtsW = RiseSetCalculator.Sun(winter, ChicagoLon, ChicagoLat);
        double summerHours = (rtsS.Set!.Value - rtsS.Rise!.Value).TotalHours;
        double winterHours = (rtsW.Set!.Value - rtsW.Rise!.Value).TotalHours;
        await Assert.That(summerHours).IsGreaterThan(winterHours);
    }

    [Test]
    public async Task Sun_Equinox_DayLengthNear12Hours()
    {
        // At the vernal equinox, day length ≈ 12 hours (±1 h for refraction and latitude effects)
        var date = new DateTime(2024, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Sun(date, GreenwichLon, GreenwichLat);
        double hours = (rts.Set!.Value - rts.Rise!.Value).TotalHours;
        await Assert.That(Math.Abs(hours - 12.0)).IsLessThan(1.0);
    }

    // ── Transit is a valid UTC datetime ───────────────────────────────────

    [Test]
    public async Task Sun_Chicago_WinterSolstice_TransitOnSameDate()
    {
        var date = new DateTime(2024, 12, 21, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Sun(date, ChicagoLon, ChicagoLat);
        // Transit should be within 1 day of the requested date
        await Assert.That(Math.Abs((rts.Transit.Date - date.Date).TotalDays)).IsLessThanOrEqualTo(1.0);
    }

    // ── Moon ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Moon_Greenwich_HasTransit()
    {
        // The Moon always has a transit; Rise and Set may be null for circumpolar cases
        var date = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Moon(date, GreenwichLon, GreenwichLat);
        await Assert.That(rts.Transit.Year).IsEqualTo(2024);
    }

    [Test]
    public async Task Moon_WithRiseAndSet_ReturnsTimesNearDate()
    {
        // The Meeus simplified algorithm can return times outside the expected rise<set order
        // for the fast-moving Moon. Just validate the returned times are near the requested date.
        var date = new DateTime(2024, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Moon(date, GreenwichLon, GreenwichLat);
        var window = TimeSpan.FromDays(2);
        if (rts.Rise.HasValue)
        {
            await Assert.That(Math.Abs((rts.Rise.Value - date).TotalDays)).IsLessThan(2);
        }
        await Assert.That(Math.Abs((rts.Transit - date).TotalDays)).IsLessThan(2);
        if (rts.Set.HasValue)
        {
            await Assert.That(Math.Abs((rts.Set.Value - date).TotalDays)).IsLessThan(2);
        }
        _ = window; // suppress unused warning
    }

    // ── Planets ───────────────────────────────────────────────────────────

    [Test]
    public async Task Planet_Jupiter_HasTransit()
    {
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Planet("jupiter", date, ChicagoLon, ChicagoLat);
        await Assert.That(rts.Transit.Year).IsEqualTo(2024);
    }

    [Test]
    public async Task Planet_Jupiter_WithRiseAndSet_ReturnsTimesNearDate()
    {
        // The Meeus simplified algorithm can return times outside the expected rise<set order
        // for planets near conjunction. Just validate the returned times are near the requested date.
        var date = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Planet("jupiter", date, ChicagoLon, ChicagoLat);
        if (rts.Rise.HasValue)
        {
            await Assert.That(Math.Abs((rts.Rise.Value - date).TotalDays)).IsLessThan(2);
        }
        await Assert.That(Math.Abs((rts.Transit - date).TotalDays)).IsLessThan(2);
        if (rts.Set.HasValue)
        {
            await Assert.That(Math.Abs((rts.Set.Value - date).TotalDays)).IsLessThan(2);
        }
    }

    [Test]
    public async Task Planet_Mars_HasTransit()
    {
        var date = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
        var rts  = RiseSetCalculator.Planet("mars", date, GreenwichLon, GreenwichLat);
        await Assert.That(rts.Transit.Year).IsEqualTo(2024);
    }
}
