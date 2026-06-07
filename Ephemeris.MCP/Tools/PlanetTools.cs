using System.ComponentModel;
using ModelContextProtocol.Server;

namespace Ephemeris.MCP.Tools;

/// <summary>Tools for calculating planetary positions.</summary>
[McpServerToolType]
public static class PlanetTools
{
    private static readonly string[] s_validPlanets =
        ["mercury", "venus", "mars", "jupiter", "saturn", "uranus", "neptune", "pluto", "earth"];

    /// <summary>Get a planet's equatorial and horizontal coordinates for a UTC date/time and observer location. Supported planets: mercury, venus, mars, jupiter, saturn, uranus, neptune, pluto. "earth" returns heliocentric coordinates (direction from Sun to Earth), useful for orbital mechanics.</summary>
    [McpServerTool(Name = "get_planet_position")]
    public static object GetPlanetPosition(
        [Description("Planet name (case-insensitive): mercury, venus, mars, jupiter, saturn, uranus, neptune, pluto, or earth (earth returns heliocentric coordinates)")] string planet,
        [Description("Year (e.g. 2024)")] int year,
        [Description("Month (1–12)")] int month,
        [Description("Day of month (1–31)")] int day,
        [Description("Hour of day in UTC as a decimal (0–24, e.g. 6.0 = 06:00 UTC)")] double hour,
        [Description("Observer longitude in degrees, east-positive (e.g. -87.65 for Chicago)")] double longitude,
        [Description("Observer latitude in degrees, north-positive (e.g. 41.85 for Chicago)")] double latitude,
        [Description("Observer altitude above sea level in metres (default 0)")] double altitudeMeters = 0)
    {
        if (!s_validPlanets.Contains(planet.ToLowerInvariant()))
            throw new ArgumentException($"Unknown planet '{planet}'. Valid values: {string.Join(", ", s_validPlanets)}.", nameof(planet));

        // Convert numeric Y/M/D/H to a local DateTime (treated as UTC) plus UTC timezone
        var utcDateTime = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc).AddHours(hour);
        var obs = EphemerisCalculator.GetPlanetPosition(planet, utcDateTime, "UTC", longitude, latitude, altitudeMeters);
        return new
        {
            planet = planet.ToLowerInvariant(),
            right_ascension_deg = obs.RightAscension,
            declination_deg = obs.Declination,
            azimuth_deg = obs.Azimuth,
            altitude_deg = obs.Altitude,
        };
    }

    /// <summary>Calculate the angular separation in degrees between two celestial objects given their right ascension and declination.</summary>
    [McpServerTool(Name = "angular_separation")]
    public static object AngularSeparation(
        [Description("Right ascension of the first object in degrees")] double ra1,
        [Description("Declination of the first object in degrees")] double dec1,
        [Description("Right ascension of the second object in degrees")] double ra2,
        [Description("Declination of the second object in degrees")] double dec2)
    {
        double sep = EphemerisCalculator.AngularSeparation(ra1, dec1, ra2, dec2);
        return new { angular_separation_deg = sep };
    }
}
