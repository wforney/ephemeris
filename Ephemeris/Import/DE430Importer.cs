using Ephemeris.Chronology;
using Ephemeris.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ephemeris.Import;

/// <summary>
/// Loads ephemeris data from DE430 binary ephemeris files.
/// </summary>
public static class DE430Importer
{
    /// <summary>
    /// Loads ephemeris records from a DE430 binary file.
    /// </summary>
    /// <param name="filePath">The path to the DE430 binary ephemeris file.</param>
    /// <param name="body">The name of the celestial body to store in each record.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>A list of EphemerisRecord parsed from the binary file, with horizontal coordinates computed.</returns>
    public static List<EphemerisRecord> LoadFromBinary(string filePath, string body, double longitude, double latitude)
    {
        var records = new List<EphemerisRecord>();

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        using var br = new BinaryReader(fs);

        while (br.BaseStream.Position < br.BaseStream.Length)
        {
            long ticks = br.ReadInt64();
            double ra = br.ReadDouble();
            double dec = br.ReadDouble();

            var utc = new DateTime(ticks, DateTimeKind.Utc);
            double jd = TimeZoneUtils.ToJulianDay(utc);
            var (az, alt) = ObserverGeometry.EquatorialToHorizontal(ra, dec, jd, longitude, latitude);

            records.Add(new EphemerisRecord
            {
                TimeUtc = utc,
                Body = body,
                RightAscension = ra,
                Declination = dec,
                Azimuth = az,
                Altitude = alt
            });
        }

        return records;
    }
}
