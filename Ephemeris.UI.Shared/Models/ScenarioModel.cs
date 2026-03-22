// Updated: 2026-03-22
namespace Ephemeris.UI.Models;

/// <summary>
/// Represents a predefined scriptural or historical celestial event scenario.
/// </summary>
/// <remarks>
/// Scenarios bundle a location, approximate time, and descriptive metadata so
/// the research UI can jump directly to a meaningful observation point without
/// manual data entry.
/// </remarks>
public sealed class ScenarioModel
{
    /// <summary>Display name for the event (e.g. "Hezekiah's Sundial").</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Scripture passage reference (e.g. "2 Kings 20:8–11").</summary>
    public string ScriptureReference { get; init; } = string.Empty;

    /// <summary>Brief description of the celestial event.</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Suggested simulation start time in UTC.
    /// </summary>
    /// <remarks>
    /// BCE dates cannot be represented by <see cref="DateTime"/>; the value here is a
    /// proleptic-Gregorian proxy that positions the scenario in the correct era.
    /// Use <see cref="DateTime.MinValue"/> as a sentinel meaning "set manually".
    /// </remarks>
    public DateTime SuggestedUtcTime { get; init; }

    /// <summary>Observer longitude in degrees (east positive).</summary>
    public double Longitude { get; init; }

    /// <summary>Observer latitude in degrees (north positive).</summary>
    public double Latitude { get; init; }

    /// <summary>Human-readable location description (e.g. "Jerusalem (~701 BCE)").</summary>
    public string LocationName { get; init; } = string.Empty;

    /// <summary>
    /// Built-in scenario catalog drawn from scripture.
    /// </summary>
    public static class BuiltInScenarios
    {
        /// <summary>
        /// Hezekiah's Sundial — the sun's shadow reversed ten steps (2 Kings 20:8–11).
        /// </summary>
        /// <remarks>
        /// Observer location: Jerusalem (35.2137 °E, 31.7683 °N).
        /// Approximate year: 701 BCE.  DateTime cannot represent BCE dates;
        /// <see cref="SuggestedUtcTime"/> is set to <see cref="DateTime.MinValue"/>
        /// as a signal to prompt the user to enter the date manually.
        /// </remarks>
        public static readonly ScenarioModel HezekiahSundial = new()
        {
            Name               = "Hezekiah's Sundial",
            ScriptureReference = "2 Kings 20:8–11",
            Description        = "Sun reversed 10 degrees",
            SuggestedUtcTime   = DateTime.MinValue,   // ~701 BCE — set manually
            Longitude          = 35.2137,
            Latitude           = 31.7683,
            LocationName       = "Jerusalem (~701 BCE)",
        };

        /// <summary>
        /// Joshua's Long Day — the sun and moon stood still over Gibeon (Joshua 10:12–14).
        /// </summary>
        /// <remarks>
        /// Observer location: Gibeon (35.2698 °E, 31.8515 °N).
        /// Approximate year: 1406 BCE.  <see cref="SuggestedUtcTime"/> is
        /// <see cref="DateTime.MinValue"/> for the same reason as
        /// <see cref="HezekiahSundial"/>.
        /// </remarks>
        public static readonly ScenarioModel JoshuasLongDay = new()
        {
            Name               = "Joshua's Long Day",
            ScriptureReference = "Joshua 10:12–14",
            Description        = "Sun and moon stood still",
            SuggestedUtcTime   = DateTime.MinValue,   // ~1406 BCE — set manually
            Longitude          = 35.2698,
            Latitude           = 31.8515,
            LocationName       = "Gibeon (~1406 BCE)",
        };

        /// <summary>All built-in scenarios in display order.</summary>
        public static IReadOnlyList<ScenarioModel> All { get; } =
        [
            HezekiahSundial,
            JoshuasLongDay,
        ];
    }
}
