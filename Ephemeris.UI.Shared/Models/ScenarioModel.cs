// Updated: 2026-03-22
namespace Ephemeris.UI.Models;

/// <summary>
/// An immutable preset that encodes a scriptural or historical celestial event,
/// pre-populated with a suggested date, observer location, and descriptive metadata.
/// </summary>
/// <param name="Name">Short display name, e.g. "Hezekiah's Sundial".</param>
/// <param name="ScriptureReference">Biblical chapter/verse reference, e.g. "Isaiah 38:8".</param>
/// <param name="Description">One-to-two sentence description of the event.</param>
/// <param name="SuggestedUtcTime">
/// Suggested UTC date/time for the simulation.
/// <para>
/// <b>Note:</b> .NET <see cref="DateTime"/> does not support BC/BCE dates.
/// Ancient scenario dates use CE placeholder values (e.g., year 701 represents 701 BCE)
/// until BC/BCE support is added — see GitHub issue #85.
/// </para>
/// </param>
/// <param name="Longitude">Observer longitude in degrees (East positive).</param>
/// <param name="Latitude">Observer latitude in degrees (North positive).</param>
/// <param name="LocationName">Human-readable place name, e.g. "Jerusalem".</param>
public record ScenarioModel(
    string Name,
    string ScriptureReference,
    string Description,
    DateTime SuggestedUtcTime,
    double Longitude,
    double Latitude,
    string LocationName);

/// <summary>
/// Provides the built-in scriptural event presets for the Ephemeris Research App.
/// </summary>
public static class BuiltInScenarios
{
    /// <summary>
    /// Hezekiah's Sundial — the shadow retreated ten steps on the stairway of Ahaz.
    /// </summary>
    /// <remarks>
    /// Observer: Jerusalem (31.8° N, 35.2° E).
    /// Approximate date: August 701 BCE.
    /// <b>Placeholder date:</b> <c>new DateTime(701, 8, 1)</c> represents 701 BCE.
    /// BC/BCE date support is tracked in GitHub issue #85.
    /// </remarks>
    public static ScenarioModel HezekiahSundial => new(
        Name:             "Hezekiah's Sundial",
        ScriptureReference: "Isaiah 38:8 / 2 Kings 20:11",
        Description:      "The shadow on the stairway of Ahaz retreated ten steps as a sign to King Hezekiah. " +
                          "Simulates the sky over Jerusalem circa 701 BCE.",
        // Placeholder: year 701 CE represents 701 BCE — see issue #85 for BC/BCE support.
        SuggestedUtcTime: new DateTime(701, 8, 1, 12, 0, 0, DateTimeKind.Utc),
        Longitude:        35.2,   // Jerusalem, approx.
        Latitude:         31.8,
        LocationName:     "Jerusalem");

    /// <summary>
    /// Joshua's Long Day — the Sun stood still over Gibeon and the Moon over the Valley of Aijalon.
    /// </summary>
    /// <remarks>
    /// Observer: Gibeon (31.85° N, 35.18° E).
    /// Approximate date: ~1406 BCE.
    /// <b>Placeholder date:</b> <c>new DateTime(1406, 8, 1)</c> represents 1406 BCE.
    /// BC/BCE date support is tracked in GitHub issue #85.
    /// </remarks>
    public static ScenarioModel JoshuasLongDay => new(
        Name:             "Joshua's Long Day",
        ScriptureReference: "Joshua 10:12-14",
        Description:      "The Sun stood still over Gibeon and the Moon over the Valley of Aijalon " +
                          "while Israel fought the Amorites. Simulates the sky over Gibeon circa 1406 BCE.",
        // Placeholder: year 1406 CE represents 1406 BCE — see issue #85 for BC/BCE support.
        SuggestedUtcTime: new DateTime(1406, 8, 1, 12, 0, 0, DateTimeKind.Utc),
        Longitude:        35.18,  // Gibeon, approx.
        Latitude:         31.85,
        LocationName:     "Gibeon");

    /// <summary>
    /// All built-in scenario presets, in display order.
    /// </summary>
    public static IReadOnlyList<ScenarioModel> All =>
    [
        HezekiahSundial,
        JoshuasLongDay,
    ];
}
