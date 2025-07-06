using Ephemeris.Chronology;
using Ephemeris.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ephemeris.Import;

public static class DE430Importer
{
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
