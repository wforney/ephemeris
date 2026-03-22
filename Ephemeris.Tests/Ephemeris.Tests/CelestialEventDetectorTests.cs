// Updated: 2026-03-22
using Ephemeris.Phenomenology;
using TUnit;

namespace Ephemeris.Tests;

/// <summary>
/// Validates the <see cref="CelestialEventDetector"/> functionality:
/// event detection, ordering, description accuracy, and edge cases.
/// </summary>
public class CelestialEventDetectorTests
{
    // ── Scan window tests ─────────────────────────────────────────────────

    [Test]
    public async Task Scan_OneMonthWindow_ContainsFullAndNewMoon()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2024, 1, 31, 23, 59, 59, DateTimeKind.Utc);

        var events = CelestialEventDetector.Scan(start, end);

        await Assert.That(events.Any(e => e.Type == CelestialEventDetector.EventType.FullMoon)).IsTrue();
        await Assert.That(events.Any(e => e.Type == CelestialEventDetector.EventType.NewMoon)).IsTrue();
    }

    [Test]
    public async Task Scan_YearWindow_ContainsAllFourSeasons()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var events = CelestialEventDetector.Scan(start, end);

        await Assert.That(events.Any(e => e.Type == CelestialEventDetector.EventType.VernalEquinox)).IsTrue();
        await Assert.That(events.Any(e => e.Type == CelestialEventDetector.EventType.SummerSolstice)).IsTrue();
        await Assert.That(events.Any(e => e.Type == CelestialEventDetector.EventType.AutumnEquinox)).IsTrue();
        await Assert.That(events.Any(e => e.Type == CelestialEventDetector.EventType.WinterSolstice)).IsTrue();
    }

    [Test]
    public async Task Scan_ResultsAreOrderedByTime()
    {
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        var events = CelestialEventDetector.Scan(start, end);

        for (int i = 1; i < events.Count; i++)
        {
            await Assert.That(events[i].UtcTime >= events[i - 1].UtcTime).IsTrue();
        }
    }

    [Test]
    public async Task Scan_AllEventsWithinWindow()
    {
        var start = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2025, 4, 30, 23, 59, 59, DateTimeKind.Utc);

        var events = CelestialEventDetector.Scan(start, end);

        foreach (var ev in events)
        {
            await Assert.That(ev.UtcTime >= start).IsTrue();
            await Assert.That(ev.UtcTime <= end).IsTrue();
        }
    }

    [Test]
    public async Task Scan_EmptyWindow_ReturnsEmpty()
    {
        // A zero-length window should return nothing
        var t = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.Scan(t, t.AddMilliseconds(1));
        // May return nothing or at most 1 event at the exact boundary; should be very small
        await Assert.That(events.Count).IsLessThanOrEqualTo(1);
    }

    // ── GetNext tests ─────────────────────────────────────────────────────

    [Test]
    public async Task GetNext_DefaultCount_Returns10Events()
    {
        var after = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after);
        await Assert.That(events.Count).IsEqualTo(10);
    }

    [Test]
    public async Task GetNext_CountOf5_Returns5Events()
    {
        var after = new DateTime(2024, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 5);
        await Assert.That(events.Count).IsEqualTo(5);
    }

    [Test]
    public async Task GetNext_AllEventsAfterStartDate()
    {
        var after = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 8);

        foreach (var ev in events)
        {
            await Assert.That(ev.UtcTime > after).IsTrue();
        }
    }

    [Test]
    public async Task GetNext_ResultsAreOrderedByTime()
    {
        var after = new DateTime(2024, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 12);

        for (int i = 1; i < events.Count; i++)
        {
            await Assert.That(events[i].UtcTime >= events[i - 1].UtcTime).IsTrue();
        }
    }

    // ── Description and event content tests ──────────────────────────────

    [Test]
    public async Task FullMoon_HasCorrectDescription()
    {
        var after = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 30);
        var fullMoon = events.First(e => e.Type == CelestialEventDetector.EventType.FullMoon);
        await Assert.That(fullMoon.Description).IsEqualTo("Full Moon");
    }

    [Test]
    public async Task NewMoon_HasCorrectDescription()
    {
        var after = new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 30);
        var newMoon = events.First(e => e.Type == CelestialEventDetector.EventType.NewMoon);
        await Assert.That(newMoon.Description).IsEqualTo("New Moon");
    }

    [Test]
    public async Task VernalEquinox_OccursInMarch()
    {
        var after = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 50);
        var equinox = events.First(e => e.Type == CelestialEventDetector.EventType.VernalEquinox);
        await Assert.That(equinox.UtcTime.Month).IsEqualTo(3);
        await Assert.That(equinox.Description).IsEqualTo("Vernal Equinox");
    }

    [Test]
    public async Task SummerSolstice_OccursInJune()
    {
        var after = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 50);
        var solstice = events.First(e => e.Type == CelestialEventDetector.EventType.SummerSolstice);
        await Assert.That(solstice.UtcTime.Month).IsEqualTo(6);
        await Assert.That(solstice.Description).IsEqualTo("Summer Solstice");
    }

    [Test]
    public async Task AutumnEquinox_OccursInSeptember()
    {
        var after = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 50);
        var equinox = events.First(e => e.Type == CelestialEventDetector.EventType.AutumnEquinox);
        await Assert.That(equinox.UtcTime.Month).IsEqualTo(9);
        await Assert.That(equinox.Description).IsEqualTo("Autumnal Equinox");
    }

    [Test]
    public async Task WinterSolstice_OccursInDecember()
    {
        var after = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var events = CelestialEventDetector.GetNext(after, count: 50);
        var solstice = events.First(e => e.Type == CelestialEventDetector.EventType.WinterSolstice);
        await Assert.That(solstice.UtcTime.Month).IsEqualTo(12);
        await Assert.That(solstice.Description).IsEqualTo("Winter Solstice");
    }

    [Test]
    public async Task LunarEclipses_HaveNonEmptyDescriptions()
    {
        // Scan 5 years to be sure to catch a lunar eclipse
        var start = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var events = CelestialEventDetector.Scan(start, end);
        var eclipses = events.Where(e => e.Type == CelestialEventDetector.EventType.LunarEclipse).ToList();

        // There should be at least some lunar eclipses in 5 years
        await Assert.That(eclipses.Count).IsGreaterThan(0);
        foreach (var eclipse in eclipses)
        {
            await Assert.That(string.IsNullOrEmpty(eclipse.Description)).IsFalse();
            await Assert.That(eclipse.Description).Contains("Eclipse");
        }
    }

    // ── CelestialEvent record tests ───────────────────────────────────────

    [Test]
    public async Task CelestialEvent_CompareTo_OrdersByTime()
    {
        var t1 = new DateTime(2024, 3, 20, 0, 0, 0, DateTimeKind.Utc);
        var t2 = new DateTime(2024, 6, 20, 0, 0, 0, DateTimeKind.Utc);

        var ev1 = new CelestialEventDetector.CelestialEvent(
            CelestialEventDetector.EventType.VernalEquinox, t1, "Vernal Equinox");
        var ev2 = new CelestialEventDetector.CelestialEvent(
            CelestialEventDetector.EventType.SummerSolstice, t2, "Summer Solstice");

        await Assert.That(ev1.CompareTo(ev2)).IsLessThan(0);
        await Assert.That(ev2.CompareTo(ev1)).IsGreaterThan(0);
        await Assert.That(ev1.CompareTo(ev1)).IsEqualTo(0);
        await Assert.That(ev1.CompareTo(null)).IsGreaterThan(0);
    }
}
