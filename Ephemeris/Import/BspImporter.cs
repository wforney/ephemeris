// Updated: 2026-05-29
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris.Import;

using SpiceSharpParser.Models;
using SpiceSharpParser.Parsers;

/// <summary>
/// Loads ephemeris data from SPICE .bsp kernel files using SpiceSharp-Parser.
/// </summary>
public static class BspImporter
{
    /// <summary>
    /// Loads a time series of ephemeris records from SPICE binary planet kernel (BSP) files.
    /// </summary>
    /// <param name="kernelPaths">A list of paths to SPICE kernel files to load.</param>
    /// <param name="target">The target body name (e.g., "SUN", "MOON", "EARTH").</param>
    /// <param name="observer">The observer body name (e.g., "EARTH").</param>
    /// <param name="startUtc">The starting UTC time.</param>
    /// <param name="intervalMinutes">The interval in minutes between successive calculations.</param>
    /// <param name="count">The number of records to generate.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A list of EphemerisRecord containing body positions from the SPICE kernel.</returns>
    public static List<EphemerisRecord> LoadFromSpiceSharp(
        List<string> kernelPaths,
        string target, string observer,
        DateTime startUtc, int intervalMinutes, int count,
        double longitude, double latitude)
    {
        // This assumes you’ve built SpiceSharp-Parser interfaces for SPICE kernel loading.
        var kernelDb = new SpiceKernelDatabase();
        foreach (var path in kernelPaths)
        {
            kernelDb.LoadKernel(path);
        }

        var records = new List<EphemerisRecord>();
        for (int i = 0; i < count; i++)
        {
            var dt = startUtc.AddMinutes(i * intervalMinutes);
            double et = kernelDb.ConvertUtcToEphemerisTime(dt);

            var state = kernelDb.GetPosition(target, et, "J2000", observer);
            var (coords, _) = CartesianToRaDec(state);

            double jd = TimeZoneUtils.ToJulianDay(dt);
            var hz = ObserverGeometry.EquatorialToHorizontal(coords.RightAscension, coords.Declination, jd, longitude, latitude);

            records.Add(new EphemerisRecord
            {
                TimeUtc = dt,
                Body = target,
                RightAscension = coords.RightAscension,
                Declination = coords.Declination,
                Azimuth = hz.Azimuth,
                Altitude = hz.Altitude
            });
        }

        return records;
    }

    private static (EquatorialCoordinates Coordinates, double DistanceAu) CartesianToRaDec(double[] vec)
    {
        double x = vec[0], y = vec[1], z = vec[2];
        double r = Math.Sqrt(x * x + y * y + z * z);
        double ra = Math.Atan2(y, x) * 180 / Math.PI;
        if (ra < 0) ra += 360;
        double dec = Math.Asin(z / r) * 180 / Math.PI;
        return (new EquatorialCoordinates(ra, dec), r);
    }
}
