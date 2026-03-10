// Updated: 2026-03-09
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris.Import;

/// <summary>
/// Loads ephemeris data from SPICE .bsp kernel files via the native DAF/SPK reader.
/// </summary>
public static class BspImporter
{
    /// <summary>
    /// Loads a time series of ephemeris records from SPICE binary planet kernel (BSP) files
    /// using the native DAF/SPK reader.
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
    public static List<EphemerisRecord> LoadFromBspKernel(
        List<string> kernelPaths,
        string target, string observer,
        DateTime startUtc, int intervalMinutes, int count,
        double longitude, double latitude)
    {
        // Load each BSP kernel file into the native DAF/SPK reader via SpiceKernelDatabase.
        var kernelDb = new SpiceKernelDatabase();
        foreach (var path in kernelPaths)
        {
            kernelDb.LoadKernel(path);
        }

        List<EphemerisRecord> records = [];
        for (int i = 0; i < count; i++)
        {
            var dt = startUtc.AddMinutes(i * intervalMinutes);
            double et = kernelDb.ConvertUtcToEphemerisTime(dt); // SPICE ephemeris time (seconds since J2000.0)

            var state = kernelDb.GetPosition(target, et, "J2000", observer);
            var (coords, _) = CartesianToRaDec(state);

            double jd = TimeZoneUtils.ToJulianDay(dt);
            var horizontalPos = ObserverGeometry.EquatorialToHorizontal(coords.RightAscension, coords.Declination, jd, longitude, latitude);

            records.Add(new EphemerisRecord(
                TimeUtc: dt,
                Body: target,
                RightAscension: coords.RightAscension,
                Declination: coords.Declination,
                Azimuth: horizontalPos.Azimuth,
                Altitude: horizontalPos.Altitude,
                Illumination: null));
        }

        return records;
    }

    /// <summary>
    /// Converts an ICRF Cartesian position vector (km) to geocentric equatorial coordinates
    /// and distance.
    /// </summary>
    /// <param name="vec">Three-element array [x, y, z] in km, in the ICRF/J2000 frame.</param>
    /// <returns>
    /// <see cref="EquatorialCoordinates"/> (RA and Dec in degrees, J2000) and distance in km.
    /// </returns>
    /// <remarks>
    /// Spherical conversion from ICRF Cartesian:
    /// <code>
    ///   r   = √(x² + y² + z²)
    ///   RA  = atan2(y, x)          [normalised to [0°, 360°)]
    ///   Dec = arcsin(z / r)        [clamped to [−1, 1] for numerical safety]
    /// </code>
    /// The result is in the J2000.0 ICRS equatorial frame. No precession or nutation
    /// corrections are applied; the kernel already supplies ICRF coordinates.
    /// </remarks>
    private static (EquatorialCoordinates Coordinates, double DistanceAu) CartesianToRaDec(double[] vec)
    {
        double x = vec[0], y = vec[1], z = vec[2]; // ICRF Cartesian position (km)
        double r = Math.Sqrt(x * x + y * y + z * z); // distance (km)
        double ra = double.RadiansToDegrees(Math.Atan2(y, x));
        if (ra < 0) ra += 360;
        double dec = r > 0
            ? double.RadiansToDegrees(Math.Asin(Math.Clamp(z / r, -1.0, 1.0)))
            : 0.0;
        return (new EquatorialCoordinates(ra, dec), r);
    }
}
