using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Ephemeris.MCP.Tools;

/// <summary>Tools for calculating the Sun's position and related events.</summary>
[McpServerToolType]
public static class SunTools
{
    /// <summary>Get the Sun's equatorial and horizontal coordinates for a UTC date/time and observer location.</summary>
    [McpServerTool(Name = "get_sun_position")]
    public static object GetSunPosition(
        [Description("Year (e.g. 2024)")] int year,
        [Description("Month (1–12)")] int month,
        [Description("Day of month (1–31)")] int day,
        [Description("Hour of day in UTC as a decimal (0–24, e.g. 14.5 = 14:30 UTC)")] double hour,
        [Description("Observer longitude in degrees, east-positive (e.g. -118.2 for Los Angeles)")] double longitude,
        [Description("Observer latitude in degrees, north-positive (e.g. 34.05 for Los Angeles)")] double latitude,
        [Description("Observer altitude above sea level in metres (default 0)")] double altitudeMeters = 0)
    {
        var obs = EphemerisCalculator.GetSunPosition(year, month, day, hour, longitude, latitude, altitudeMeters);
        return new
        {
            right_ascension_deg = obs.RightAscension,
            declination_deg = obs.Declination,
            azimuth_deg = obs.Azimuth,
            altitude_deg = obs.Altitude,
        };
    }
}
