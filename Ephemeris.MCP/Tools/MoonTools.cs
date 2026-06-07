using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Ephemeris.MCP.Tools;

/// <summary>Tools for calculating the Moon's position and related events.</summary>
[McpServerToolType]
public static class MoonTools
{
    /// <summary>Get the Moon's equatorial and horizontal coordinates and illuminated fraction for a UTC date/time and observer location.</summary>
    [McpServerTool(Name = "get_moon_position")]
    public static object GetMoonPosition(
        [Description("Year (e.g. 2024)")] int year,
        [Description("Month (1–12)")] int month,
        [Description("Day of month (1–31)")] int day,
        [Description("Hour of day in UTC as a decimal (0–24, e.g. 20.0 = 20:00 UTC)")] double hour,
        [Description("Observer longitude in degrees, east-positive (e.g. -118.2 for Los Angeles)")] double longitude,
        [Description("Observer latitude in degrees, north-positive (e.g. 34.05 for Los Angeles)")] double latitude,
        [Description("Observer altitude above sea level in metres (default 0)")] double altitudeMeters = 0)
    {
        var obs = EphemerisCalculator.GetMoonPosition(year, month, day, hour, longitude, latitude, altitudeMeters);
        return new
        {
            right_ascension_deg = obs.RightAscension,
            declination_deg = obs.Declination,
            azimuth_deg = obs.Azimuth,
            altitude_deg = obs.Altitude,
            illumination_fraction = obs.Illumination,
        };
    }

    /// <summary>Get the UTC date and time of the next full moon after the specified UTC date and time.</summary>
    [McpServerTool(Name = "next_full_moon")]
    public static string NextFullMoon(
        [Description("Starting UTC date/time in ISO 8601 format (e.g. '2024-06-01T00:00:00Z')")] string afterUtc)
    {
        var after = DateTime.Parse(afterUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var result = EphemerisCalculator.NextFullMoon(after);
        return result.ToString("o");
    }

    /// <summary>Get the UTC date and time of the next new moon after the specified UTC date and time.</summary>
    [McpServerTool(Name = "next_new_moon")]
    public static string NextNewMoon(
        [Description("Starting UTC date/time in ISO 8601 format (e.g. '2024-06-01T00:00:00Z')")] string afterUtc)
    {
        var after = DateTime.Parse(afterUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
        var result = EphemerisCalculator.NextNewMoon(after);
        return result.ToString("o");
    }
}
