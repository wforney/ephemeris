using Ephemeris.Chronology;

namespace Ephemeris.Planetology;

public static class PlanetEphemeris
{
    public static (double RA, double Dec) SimplifiedPlanetPosition(double T, double N, double i, double w, double a, double e, double M)
    {
        M = TimeUtils.NormalizeDegrees(M);
        double E = M + (180 / Math.PI * e * Math.Sin(TimeUtils.ToRadians(M)) * (1 + (e * Math.Cos(TimeUtils.ToRadians(M)))));

        double xv = Math.Cos(TimeUtils.ToRadians(E)) - e;
        double yv = Math.Sqrt(1.0 - (e * e)) * Math.Sin(TimeUtils.ToRadians(E));
        double v = TimeUtils.ToDegrees(Math.Atan2(yv, xv));
        double r = Math.Sqrt((xv * xv) + (yv * yv));

        double xh = r * ((Math.Cos(TimeUtils.ToRadians(N)) * Math.Cos(TimeUtils.ToRadians(v + w))) - (Math.Sin(TimeUtils.ToRadians(N)) * Math.Sin(TimeUtils.ToRadians(v + w)) * Math.Cos(TimeUtils.ToRadians(i))));
        double yh = r * ((Math.Sin(TimeUtils.ToRadians(N)) * Math.Cos(TimeUtils.ToRadians(v + w))) + (Math.Cos(TimeUtils.ToRadians(N)) * Math.Sin(TimeUtils.ToRadians(v + w)) * Math.Cos(TimeUtils.ToRadians(i))));
        double zh = r * (Math.Sin(TimeUtils.ToRadians(v + w)) * Math.Sin(TimeUtils.ToRadians(i)));

        double lon = TimeUtils.ToDegrees(Math.Atan2(yh, xh));
        double lat = TimeUtils.ToDegrees(Math.Atan2(zh, Math.Sqrt((xh * xh) + (yh * yh))));

        double eps = 23.439291 - (0.0130042 * T);
        double lonRad = TimeUtils.ToRadians(lon);
        double latRad = TimeUtils.ToRadians(lat);
        double epsRad = TimeUtils.ToRadians(eps);

        double x = Math.Cos(lonRad) * Math.Cos(latRad);
        double y = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Cos(epsRad)) - (Math.Sin(latRad) * Math.Sin(epsRad));
        double z = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Sin(epsRad)) + (Math.Sin(latRad) * Math.Cos(epsRad));

        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(y, x)));
        double Dec = TimeUtils.ToDegrees(Math.Asin(z));

        return (RA, Dec);
    }
}
