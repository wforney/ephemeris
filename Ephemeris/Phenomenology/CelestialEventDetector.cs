// Updated: 2026-03-22
using Ephemeris.Chronology;
using Ephemeris.Selenography;

namespace Ephemeris.Phenomenology;

/// <summary>
/// Scans date ranges for notable celestial events and returns them as a sorted list.
/// Detects lunar phases, seasonal turning points (equinoxes and solstices), and eclipses.
/// </summary>
/// <remarks>
/// <para>
/// This is a convenience wrapper around <see cref="EphemerisCalculator"/>,
/// <see cref="SeasonCalculator"/>, and <see cref="EclipseCalculator"/>.
/// All returned times are in UTC.
/// </para>
/// <para>
/// Accuracy mirrors the underlying calculators:
/// <list type="bullet">
///   <item><description>Lunar phases: ±30 minutes (iterative crossing detection).</description></item>
///   <item><description>Seasons: ±1 minute (Meeus Ch. 27 polynomial).</description></item>
///   <item><description>Eclipses: ±1 lunation time, ±30% magnitude (Meeus Ch. 54).</description></item>
/// </list>
/// </para>
/// </remarks>
public static class CelestialEventDetector
{
    /// <summary>Classifies the type of a detected celestial event.</summary>
    public enum EventType
    {
        /// <summary>Full Moon — lunar phase angle crosses 0°/360°.</summary>
        FullMoon,
        /// <summary>New Moon — lunar phase angle crosses 180°.</summary>
        NewMoon,
        /// <summary>Vernal (March) equinox — Sun crosses the celestial equator northward.</summary>
        VernalEquinox,
        /// <summary>Summer (June) solstice — Sun reaches maximum northerly declination.</summary>
        SummerSolstice,
        /// <summary>Autumnal (September) equinox — Sun crosses the celestial equator southward.</summary>
        AutumnEquinox,
        /// <summary>Winter (December) solstice — Sun reaches maximum southerly declination.</summary>
        WinterSolstice,
        /// <summary>Lunar eclipse (total, partial, or penumbral).</summary>
        LunarEclipse,
        /// <summary>Solar eclipse (total, annular, partial, or hybrid).</summary>
        SolarEclipse,
    }

    /// <summary>Describes a detected celestial event.</summary>
    /// <param name="Type">Type of event.</param>
    /// <param name="UtcTime">Approximate UTC time of the event.</param>
    /// <param name="Description">Human-readable description (includes eclipse subtype where applicable).</param>
    public record CelestialEvent(EventType Type, DateTime UtcTime, string Description)
        : IComparable<CelestialEvent>
    {
        /// <inheritdoc/>
        public int CompareTo(CelestialEvent? other) =>
            other is null ? 1 : UtcTime.CompareTo(other.UtcTime);
    }

    // Mean lunar period — used to step forward when searching for next phase
    private const double MeanLunationHours = 29.530588853 * 24.0;

    /// <summary>
    /// Scans for notable celestial events between <paramref name="startUtc"/> and
    /// <paramref name="endUtc"/> (both inclusive) and returns them ordered by time.
    /// </summary>
    /// <param name="startUtc">Start of the search window (UTC).</param>
    /// <param name="endUtc">End of the search window (UTC).</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{CelestialEvent}"/> sorted by ascending UTC time.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method collects:
    /// <list type="number">
    ///   <item><description>All Full Moons and New Moons in the window (stepped in ~29.5-day intervals).</description></item>
    ///   <item><description>All equinoxes and solstices whose calculated time falls in the window.</description></item>
    ///   <item><description>All solar and lunar eclipses whose time falls in the window, using <see cref="EclipseCalculator"/>.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static IReadOnlyList<CelestialEvent> Scan(DateTime startUtc, DateTime endUtc)
    {
        var events = new List<CelestialEvent>();

        // ── Lunar phases ───────────────────────────────────────────────────────
        CollectLunarPhases(startUtc, endUtc, events);

        // ── Seasons ────────────────────────────────────────────────────────────
        CollectSeasons(startUtc, endUtc, events);

        // ── Eclipses ───────────────────────────────────────────────────────────
        CollectEclipses(startUtc, endUtc, events);

        events.Sort();
        return events.AsReadOnly();
    }

    /// <summary>
    /// Returns the next <paramref name="count"/> celestial events after
    /// <paramref name="afterUtc"/>, ordered by ascending time.
    /// </summary>
    /// <param name="afterUtc">Search for events strictly after this UTC time.</param>
    /// <param name="count">Maximum number of events to return (default 10).</param>
    /// <returns>
    /// An <see cref="IReadOnlyList{CelestialEvent}"/> of up to <paramref name="count"/> events.
    /// </returns>
    /// <remarks>
    /// The method expands the search window by 30-day increments until enough events are found
    /// or a maximum search horizon of 5 years is reached.
    /// </remarks>
    public static IReadOnlyList<CelestialEvent> GetNext(DateTime afterUtc, int count = 10)
    {
        var result = new List<CelestialEvent>(count);
        // Use a HashSet for O(1) duplicate detection instead of O(n) Any()
        var seen = new HashSet<(DateTime, EventType)>();
        DateTime windowEnd = afterUtc.AddDays(30);
        DateTime maxHorizon = afterUtc.AddDays(365 * 5);

        while (result.Count < count && windowEnd <= maxHorizon)
        {
            var batch = Scan(afterUtc.AddSeconds(1), windowEnd);
            foreach (var ev in batch)
            {
                if (result.Count >= count)
                    break;
                if (seen.Add((ev.UtcTime, ev.Type)))
                    result.Add(ev);
            }
            afterUtc = windowEnd;
            windowEnd = windowEnd.AddDays(30);
        }

        result.Sort();
        return result.AsReadOnly();
    }

    // ── Private helpers ────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the normalized lunar phase angle in degrees [0, 360).
    /// </summary>
    /// <remarks>
    /// Convention used by <see cref="MoonEphemeris"/>:
    /// <list type="bullet">
    ///   <item><description>phase ≈ 0° — Full Moon (Moon opposite Sun).</description></item>
    ///   <item><description>phase ≈ 180° — New Moon (Moon near Sun).</description></item>
    /// </list>
    /// <see cref="MoonEphemeris.Illumination"/> returns 0 at phase=180° and 1 at phase=0°,
    /// confirming this convention.
    /// </remarks>
    private static double GetLunarPhase(DateTime utc)
    {
        double jd = TimeZoneUtils.ToJulianDay(utc);
        double T  = TimeUtils.JulianCentury(jd);
        double phase = MoonEphemeris.PhaseAngle(T);
        return ((phase % 360) + 360) % 360;
    }

    /// <summary>
    /// Finds the next Full Moon after <paramref name="after"/>.
    /// Full Moon is detected when the normalized phase jumps from near 0° to near 359°.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="EphemerisCalculator.NextFullMoon"/>, which detects the Full Moon
    /// via the condition <c>prevPhase &lt; 180 &amp;&amp; phase ≥ 180</c>.
    /// Under the <see cref="MoonEphemeris.PhaseAngle"/> convention (phase ≈ 0° at Full Moon,
    /// phase ≈ 180° at New Moon), the phase DECREASES from 180° toward 0° in the first half
    /// of the lunation. When the Moon is exactly at Full Moon (phase = 0°), the next step
    /// shows phase ≈ 359° — a discontinuous jump from near 0° to near 359°.
    /// The condition <c>prevPhase &lt; 180 &amp;&amp; phase ≥ 180</c> reliably catches this
    /// jump (prevPhase ≈ 1°, phase ≈ 359°) and does NOT trigger at the New Moon passage.
    /// </remarks>
    private static DateTime FindNextFullMoon(DateTime after)
        => EphemerisCalculator.NextFullMoon(after);

    /// <summary>
    /// Finds the next New Moon after <paramref name="after"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// New Moon is detected when the normalized phase decreases through 180°
    /// (condition: prevPhase &gt; 180 &amp;&amp; phase ≤ 180).
    /// </para>
    /// <para>
    /// Note: <see cref="EphemerisCalculator.NextNewMoon"/> is NOT used here because
    /// it relies on a condition (`prevPhase &gt; 300 &amp;&amp; phase &lt; 60`) that
    /// never fires under the <see cref="MoonEphemeris.PhaseAngle"/> convention used
    /// in this library (where 0° = Full Moon and 180° = New Moon).
    /// </para>
    /// </remarks>
    private static DateTime FindNextNewMoon(DateTime after)
    {
        var dt = after.AddHours(1);
        double prevPhase = GetLunarPhase(dt);

        // At most scan 35 days (one full lunation + margin)
        for (int i = 0; i < 35 * 24; i++)
        {
            dt = dt.AddHours(1);
            double phase = GetLunarPhase(dt);

            // New moon: phase decreasing through 180°
            // prevPhase is in (180, 360) and phase dropped to <= 180
            if (prevPhase > 180 && phase <= 180)
                return dt.AddMinutes(-30);

            prevPhase = phase;
        }

        // Fallback: should never reach here in practice
        return after.AddDays(29.5);
    }

    /// <summary>
    /// Collects Full Moon and New Moon events in [<paramref name="start"/>, <paramref name="end"/>].
    /// </summary>
    /// <remarks>
    /// Steps forward by roughly one lunation at a time, calling
    /// <see cref="FindNextFullMoon"/> and <see cref="FindNextNewMoon"/> from the start of the window.
    /// </remarks>
    private static void CollectLunarPhases(DateTime start, DateTime end, List<CelestialEvent> events)
    {
        // Advance step: 28 days (one day short of min lunation ~29.27 days).
        // This ensures we always land at least ~1.27 days BEFORE the next event,
        // so the search function never starts after the event has already passed.
        const double stepHours = 28.0 * 24.0; // 672 hours = 28 days

        // Full moons: step back two lunations to avoid missing the first one in window
        var cursor = start.AddHours(-MeanLunationHours * 2);
        while (true)
        {
            var fm = FindNextFullMoon(cursor);
            if (fm > end) break;
            if (fm >= start)
                events.Add(new CelestialEvent(EventType.FullMoon, fm, "Full Moon"));
            cursor = fm.AddHours(stepHours);
        }

        // New moons: step back two lunations so we don't miss the first one in window
        cursor = start.AddHours(-MeanLunationHours * 2);
        while (true)
        {
            var nm = FindNextNewMoon(cursor);
            if (nm > end) break;
            if (nm >= start)
                events.Add(new CelestialEvent(EventType.NewMoon, nm, "New Moon"));
            cursor = nm.AddHours(stepHours);
        }
    }

    /// <summary>
    /// Collects equinox and solstice events in [<paramref name="start"/>, <paramref name="end"/>].
    /// </summary>
    /// <remarks>
    /// Iterates over every year in the range and checks all four seasons.
    /// Uses <see cref="SeasonCalculator.Calculate"/> for each year.
    /// </remarks>
    private static void CollectSeasons(DateTime start, DateTime end, List<CelestialEvent> events)
    {
        for (int year = start.Year; year <= end.Year; year++)
        {
            CheckSeason(year, SeasonCalculator.Season.SpringEquinox, EventType.VernalEquinox, "Vernal Equinox", start, end, events);
            CheckSeason(year, SeasonCalculator.Season.SummerSolstice, EventType.SummerSolstice, "Summer Solstice", start, end, events);
            CheckSeason(year, SeasonCalculator.Season.AutumnEquinox, EventType.AutumnEquinox, "Autumnal Equinox", start, end, events);
            CheckSeason(year, SeasonCalculator.Season.WinterSolstice, EventType.WinterSolstice, "Winter Solstice", start, end, events);
        }
    }

    /// <summary>
    /// Calculates a specific season event for <paramref name="year"/> and adds it to
    /// <paramref name="events"/> if its time falls within the search window.
    /// </summary>
    private static void CheckSeason(int year, SeasonCalculator.Season season, EventType eventType,
        string description, DateTime start, DateTime end, List<CelestialEvent> events)
    {
        var dt = SeasonCalculator.Calculate(year, season);
        if (dt >= start && dt <= end)
            events.Add(new CelestialEvent(eventType, dt, description));
    }

    /// <summary>
    /// Collects solar and lunar eclipse events in [<paramref name="start"/>, <paramref name="end"/>].
    /// </summary>
    /// <remarks>
    /// Uses <see cref="EclipseCalculator.SolarEclipses"/> and
    /// <see cref="EclipseCalculator.LunarEclipses"/> for the year range.
    /// Eclipse subtype (e.g., "Total Solar Eclipse") is included in the description.
    /// </remarks>
    private static void CollectEclipses(DateTime start, DateTime end, List<CelestialEvent> events)
    {
        int startYear = start.Year;
        int endYear = end.Year;

        foreach (var eclipse in EclipseCalculator.SolarEclipses(startYear, endYear))
        {
            if (eclipse.DateTime >= start && eclipse.DateTime <= end)
            {
                string description = FormatEclipseDescription(eclipse.Type);
                events.Add(new CelestialEvent(EventType.SolarEclipse, eclipse.DateTime, description));
            }
        }

        foreach (var eclipse in EclipseCalculator.LunarEclipses(startYear, endYear))
        {
            if (eclipse.DateTime >= start && eclipse.DateTime <= end)
            {
                string description = FormatEclipseDescription(eclipse.Type);
                events.Add(new CelestialEvent(EventType.LunarEclipse, eclipse.DateTime, description));
            }
        }
    }

    /// <summary>
    /// Converts an <see cref="EclipseCalculator.EclipseType"/> to a human-readable description.
    /// </summary>
    private static string FormatEclipseDescription(EclipseCalculator.EclipseType type) =>
        type switch
        {
            EclipseCalculator.EclipseType.TotalSolar    => "Total Solar Eclipse",
            EclipseCalculator.EclipseType.AnnularSolar  => "Annular Solar Eclipse",
            EclipseCalculator.EclipseType.PartialSolar  => "Partial Solar Eclipse",
            EclipseCalculator.EclipseType.HybridSolar   => "Hybrid Solar Eclipse",
            EclipseCalculator.EclipseType.TotalLunar    => "Total Lunar Eclipse",
            EclipseCalculator.EclipseType.PartialLunar  => "Partial Lunar Eclipse",
            EclipseCalculator.EclipseType.PenumbralLunar => "Penumbral Lunar Eclipse",
            _ => type.ToString(),
        };
}
