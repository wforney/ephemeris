// Updated: 2026-03-22
using Ephemeris.Chronology;

namespace Ephemeris.UI.Models;

/// <summary>
/// Represents a predefined research scenario — a named combination of observer location,
/// suggested modern UTC time, and (where applicable) a historical <see cref="ProlepticDate"/>
/// for pre-BCE events that .NET <see cref="DateTime"/> cannot represent.
/// </summary>
/// <remarks>
/// When <see cref="HistoricalDate"/> is set the historical Julian Day is used as the
/// calculation epoch instead of <see cref="SuggestedUtcTime"/>.
/// Obtain a Julian Century for the Ephemeris engine via
/// <c>TimeUtils.JulianCentury(scenario.HistoricalDate.Value.ToJulianDay())</c>.
/// </remarks>
public sealed class ScenarioModel
{
    /// <summary>Human-readable name shown in the scenario picker.</summary>
    public required string Name { get; init; }

    /// <summary>Optional short description displayed below the name.</summary>
    public string? Description { get; init; }

    /// <summary>Observer longitude in degrees (east positive).</summary>
    public double Longitude { get; init; }

    /// <summary>Observer latitude in degrees (north positive).</summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Modern UTC proxy time for <see cref="DateTime"/>-based display when
    /// <see cref="HistoricalDate"/> is <see langword="null"/>, or as a fallback display
    /// reference when it is set.
    /// </summary>
    public DateTime SuggestedUtcTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Historical date in the BC/BCE era represented as a <see cref="ProlepticDate"/>.
    /// When this is set the scenario operates in <em>historical mode</em> and
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
            : Ephemeris.Chronology.TimeZoneUtils.ToJulianDay(SuggestedUtcTime);
}

/// <summary>
/// Provides the built-in research scenarios shipped with the application.
/// </summary>
public static class BuiltInScenarios
{
    /// <summary>
    /// Hezekiah's Sundial — Isaiah 38 / 2 Kings 20.
    /// The Sun's shadow retreated ten steps on Ahaz's sundial (~701 BCE),
    /// observed from Jerusalem (lon 35.2°E, lat 31.8°N).
    /// </summary>
    /// <remarks>
    /// Historical date: 1 August 701 BCE (proleptic Gregorian), Jerusalem noon.
    /// Reference: Meeus, <em>Astronomical Algorithms</em> (2nd ed.), Ch. 7.
    /// </remarks>
    public static ScenarioModel HezekiahSundial { get; } = new()
    {
        Name           = "Hezekiah's Sundial",
        Description    = "Isaiah 38 / 2 Kings 20 — shadow retreated ten steps (701 BCE)",
        Longitude      = 35.2,
        Latitude       = 31.8,
        SuggestedUtcTime = new DateTime(2000, 8, 1, 12, 0, 0, DateTimeKind.Utc), // proxy
        HistoricalDate = ProlepticDate.FromBce(701, 8, 1),
    };

    /// <summary>
    /// Joshua's Long Day — Joshua 10:12–14.
    /// The Sun and Moon stood still over Gibeon and the Valley of Aijalon (~1406 BCE),
    /// observed from Gibeon (lon 35.2°E, lat 31.9°N).
    /// </summary>
    /// <remarks>
    /// Historical date: 21 June 1406 BCE (proleptic Gregorian), near summer solstice.
    /// Reference: Meeus, <em>Astronomical Algorithms</em> (2nd ed.), Ch. 7.
    /// </remarks>
    public static ScenarioModel JoshuasLongDay { get; } = new()
    {
        Name           = "Joshua's Long Day",
        Description    = "Joshua 10:12–14 — Sun and Moon stood still (~1406 BCE)",
        Longitude      = 35.2,
        Latitude       = 31.9,
        SuggestedUtcTime = new DateTime(2000, 6, 21, 12, 0, 0, DateTimeKind.Utc), // proxy
        HistoricalDate = ProlepticDate.FromBce(1406, 6, 21),
    };

    /// <summary>Returns all built-in scenarios in display order.</summary>
    public static IReadOnlyList<ScenarioModel> All { get; } =
        [HezekiahSundial, JoshuasLongDay];
}
