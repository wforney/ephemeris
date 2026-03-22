// Updated: 2026-03-22
using Ephemeris.Chronology;

namespace Ephemeris.UI.Models;

/// <summary>
/// Represents a predefined research scenario — a named combination of observer location,
/// suggested modern UTC time, scripture reference, and (where applicable) a historical
/// <see cref="ProlepticDate"/> for BCE-era dates that .NET <see cref="DateTime"/> cannot represent.
/// </summary>
public sealed class ScenarioModel
{
    /// <summary>Human-readable name shown in the scenario picker.</summary>
    public required string Name { get; init; }

    /// <summary>Biblical chapter/verse reference, e.g. "Isaiah 38:8".</summary>
    public string ScriptureReference { get; init; } = string.Empty;

    /// <summary>One-to-two sentence description of the event.</summary>
    public string? Description { get; init; }

    /// <summary>Human-readable place name, e.g. "Jerusalem".</summary>
    public string LocationName { get; init; } = string.Empty;

    /// <summary>Observer longitude in degrees (east positive).</summary>
    public double Longitude { get; init; }

    /// <summary>Observer latitude in degrees (north positive).</summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Modern UTC proxy time for DateTime-based display or as fallback when
    /// <see cref="HistoricalDate"/> is not set.
    /// </summary>
    public DateTime SuggestedUtcTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Historical date in the BC/BCE era represented as a <see cref="ProlepticDate"/>.
    /// When set, the scenario operates in historical mode and
    /// <see cref="ToJulianDay"/> returns the JD of this date.
    /// </summary>
    public ProlepticDate? HistoricalDate { get; init; }

    /// <summary>
    /// Returns the Julian Day number to use as the calculation epoch.
    /// Prefers <see cref="HistoricalDate"/> when available; falls back to
    /// <see cref="SuggestedUtcTime"/>.
    /// </summary>
    public double ToJulianDay() =>
        HistoricalDate.HasValue
            ? HistoricalDate.Value.ToJulianDay()
            : TimeZoneUtils.ToJulianDay(SuggestedUtcTime);
}

/// <summary>
/// Provides the built-in scriptural event presets for the Ephemeris Research App.
/// </summary>
public static class BuiltInScenarios
{
    /// <summary>Hezekiah's Sundial — the shadow retreated ten steps on the stairway of Ahaz.</summary>
    /// <remarks>
    /// Observer: Jerusalem (31.8° N, 35.2° E). Approximate date: August 701 BCE.
    /// Reference: Meeus, Astronomical Algorithms (2nd ed.), Ch. 7.
    /// </remarks>
    public static ScenarioModel HezekiahSundial { get; } = new()
    {
        Name               = "Hezekiah's Sundial",
        ScriptureReference = "Isaiah 38:8 / 2 Kings 20:11",
        Description        = "The shadow on the stairway of Ahaz retreated ten steps as a sign to King Hezekiah (~701 BCE).",
        LocationName       = "Jerusalem",
        Longitude          = 35.2,
        Latitude           = 31.8,
        SuggestedUtcTime   = new DateTime(2000, 8, 1, 12, 0, 0, DateTimeKind.Utc),
        HistoricalDate     = ProlepticDate.FromBce(701, 8, 1),
    };

    /// <summary>Joshua's Long Day — the Sun and Moon stood still over Gibeon.</summary>
    /// <remarks>
    /// Observer: Gibeon (31.85° N, 35.18° E). Approximate date: June 1406 BCE.
    /// Reference: Meeus, Astronomical Algorithms (2nd ed.), Ch. 7.
    /// </remarks>
    public static ScenarioModel JoshuasLongDay { get; } = new()
    {
        Name               = "Joshua's Long Day",
        ScriptureReference = "Joshua 10:12-14",
        Description        = "The Sun and Moon stood still over Gibeon and the Valley of Aijalon (~1406 BCE).",
        LocationName       = "Gibeon",
        Longitude          = 35.2,
        Latitude           = 31.9,
        SuggestedUtcTime   = new DateTime(2000, 6, 21, 12, 0, 0, DateTimeKind.Utc),
        HistoricalDate     = ProlepticDate.FromBce(1406, 6, 21),
    };

    /// <summary>Returns all built-in scenarios in display order.</summary>
    public static IReadOnlyList<ScenarioModel> All { get; } =
        [HezekiahSundial, JoshuasLongDay];
}
