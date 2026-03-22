// Updated: 2026-03-22
namespace Ephemeris.UI.Models;

/// <summary>
/// Represents a predefined scriptural or historical observing scenario.
/// </summary>
/// <remarks>
/// Scenarios encode a historical moment, observer location, and optional simulation
/// overrides so researchers can instantly load events such as Hezekiah's Sundial or
/// Joshua's Long Day.  They are immutable records; the mutable override state lives
/// in <see cref="SimulationOverride"/>.
/// </remarks>
public sealed record ScenarioModel
{
    /// <summary>Display name for the scenario (e.g. "Joshua's Long Day").</summary>
    public required string Name { get; init; }

    /// <summary>Historical UTC date and time for the event.</summary>
    public required DateTime HistoricalDate { get; init; }

    /// <summary>Observer longitude in degrees (east positive).</summary>
    public required double Longitude { get; init; }

    /// <summary>Observer latitude in degrees (north positive).</summary>
    public required double Latitude { get; init; }

    /// <summary>Optional simulation overrides to apply when the scenario loads.</summary>
    public SimulationOverride? Override { get; init; }
}
