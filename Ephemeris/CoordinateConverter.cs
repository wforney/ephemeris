using Ephemeris.Chronology;

namespace Ephemeris;

public static class CoordinateConverter
{
    public static (double RA, double Dec) EclipticToEquatorial(double lon, double lat, double T)
    {
        double eps = 23.439291 - (0.0130042 * T);
        double lon_rad = TimeUtils.ToRadians(lon);
        double lat_rad = TimeUtils.ToRadians(lat);
        double eps_rad = TimeUtils.ToRadians(eps);

        double x = Math.Cos(lon_rad) * Math.Cos(lat_rad);
        double y = (Math.Sin(lon_rad) * Math.Cos(lat_rad) * Math.Cos(eps_rad)) - (Math.Sin(lat_rad) * Math.Sin(eps_rad));
        double z = (Math.Sin(lon_rad) * Math.Cos(lat_rad) * Math.Sin(eps_rad)) + (Math.Sin(lat_rad) * Math.Cos(eps_rad));

        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(y, x)));
        double Dec = TimeUtils.ToDegrees(Math.Asin(z));

        return (RA, Dec);
    }

    public static (double lon, double lat) EquatorialToEcliptic(double RA, double Dec, double T)
    {
        double eps = 23.439291 - (0.0130042 * T);
        double RA_rad = TimeUtils.ToRadians(RA);
        double Dec_rad = TimeUtils.ToRadians(Dec);
        double eps_rad = TimeUtils.ToRadians(eps);

        double x = Math.Cos(RA_rad) * Math.Cos(Dec_rad);
        double y = Math.Sin(RA_rad) * Math.Cos(Dec_rad);
        double z = Math.Sin(Dec_rad);

        double xe = x;
        double ye = (y * Math.Cos(eps_rad)) + (z * Math.Sin(eps_rad));
        double ze = (-y * Math.Sin(eps_rad)) + (z * Math.Cos(eps_rad));

        double lon = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(ye, xe)));
        double lat = TimeUtils.ToDegrees(Math.Asin(ze));

        return (lon, lat);
    }
}
