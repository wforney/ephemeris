using System.ComponentModel;
using Ephemeris.Phenomenology;
using ModelContextProtocol.Server;

namespace Ephemeris.MCP.Tools;

/// <summary>Tools for astronomical event calculations (rise/set, seasons).</summary>
[McpServerToolType]
public static class EventTools
{
    /// <summary>Get the UTC date and time of the next sunrise after the specified UTC date and time for an observer location.</summary>
    [McpServerTool(Name = "next_sunrise")]
    public static string NextSunrise(
        [Description("Starting UTC date/time in ISO 8601 format (e.g. '2024-06-01T00:00:00Z')")] string afterUtc,
        [Description("Observer longitude in degrees, east-positive (e.g. -118.2 for Los Angeles)")] double longitude,
        [Description("Observer latitude in degrees, north-positive (e.g. 34.05 for Los Angeles)")] double latitude)
    {
        var after = DateTime.Parse(afterUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var result = EphemerisCalculator.NextSunrise(after, longitude, latitude);
        return result.HasValue ? result.Value.ToString("o") : "No sunrise (circumpolar conditions)";
    }

    /// <summary>Get the UTC date and time of the next sunset after the specified UTC date and time for an observer location.</summary>
    [McpServerTool(Name = "next_sunset")]
    public static string NextSunset(
        [Description("Starting UTC date/time in ISO 8601 format (e.g. '2024-06-01T00:00:00Z')")] string afterUtc,
        [Description("Observer longitude in degrees, east-positive (e.g. -118.2 for Los Angeles)")] double longitude,
        [Description("Observer latitude in degrees, north-positive (e.g. 34.05 for Los Angeles)")] double latitude)
    {
        var after = DateTime.Parse(afterUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var result = EphemerisCalculator.NextSunset(after, longitude, latitude);
        return result.HasValue ? result.Value.ToString("o") : "No sunset (circumpolar conditions)";
    }

    /// <summary>Get the UTC date and time of the next moonrise after the specified UTC date and time for an observer location.</summary>
    [McpServerTool(Name = "next_moonrise")]
    public static string NextMoonrise(
        [Description("Starting UTC date/time in ISO 8601 format (e.g. '2024-06-01T00:00:00Z')")] string afterUtc,
        [Description("Observer longitude in degrees, east-positive (e.g. -118.2 for Los Angeles)")] double longitude,
        [Description("Observer latitude in degrees, north-positive (e.g. 34.05 for Los Angeles)")] double latitude)
    {
        var after = DateTime.Parse(afterUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var result = EphemerisCalculator.NextMoonrise(after, longitude, latitude);
        return result.HasValue ? result.Value.ToString("o") : "No moonrise (circumpolar conditions)";
    }

    /// <summary>Get the UTC date and time of the next moonset after the specified UTC date and time for an observer location.</summary>
    [McpServerTool(Name = "next_moonset")]
    public static string NextMoonset(
        [Description("Starting UTC date/time in ISO 8601 format (e.g. '2024-06-01T00:00:00Z')")] string afterUtc,
        [Description("Observer longitude in degrees, east-positive (e.g. -118.2 for Los Angeles)")] double longitude,
        [Description("Observer latitude in degrees, north-positive (e.g. 34.05 for Los Angeles)")] double latitude)
    {
        var after = DateTime.Parse(afterUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var result = EphemerisCalculator.NextMoonset(after, longitude, latitude);
        return result.HasValue ? result.Value.ToString("o") : "No moonset (circumpolar conditions)";
    }

    /// <summary>Get the UTC date and time of the next occurrence of an astronomical season (spring_equinox, summer_solstice, autumn_equinox, winter_solstice).</summary>
    [McpServerTool(Name = "next_season")]
    public static string NextSeason(
        [Description("Season name: spring_equinox, summer_solstice, autumn_equinox, or winter_solstice")] string season,
        [Description("Starting UTC date/time in ISO 8601 format (e.g. '2024-01-01T00:00:00Z')")] string afterUtc)
    {
        var after = DateTime.Parse(afterUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var seasonEnum = season.ToLowerInvariant() switch
        {
            "spring_equinox" => SeasonCalculator.Season.SpringEquinox,
            "summer_solstice" => SeasonCalculator.Season.SummerSolstice,
            "autumn_equinox" => SeasonCalculator.Season.AutumnEquinox,
            "winter_solstice" => SeasonCalculator.Season.WinterSolstice,
            _ => throw new ArgumentException($"Unknown season '{season}'. Valid values: spring_equinox, summer_solstice, autumn_equinox, winter_solstice.", nameof(season))
        };
        var result = EphemerisCalculator.NextSeason(seasonEnum, after);
        return result.ToString("o");
    }
}
