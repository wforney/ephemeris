using Ephemeris.Chronology;

namespace Ephemeris.Geometry;

public static class ObserverGeometry
{
    public static (double Azimuth, double Altitude) EquatorialToHorizontal(double RA, double Dec, double jd, double longitude, double latitude)
    {
        double LST = TimeUtils.GMST(jd) + longitude;
        LST = TimeUtils.NormalizeDegrees(LST);

        double H = TimeUtils.NormalizeDegrees(LST - RA);

        double Hrad = TimeUtils.ToRadians(H);
        double Decrad = TimeUtils.ToRadians(Dec);
        double Latrad = TimeUtils.ToRadians(latitude);

        double Alt = Math.Asin((Math.Sin(Decrad) * Math.Sin(Latrad)) + (Math.Cos(Decrad) * Math.Cos(Latrad) * Math.Cos(Hrad)));
        double Az = Math.Acos((Math.Sin(Decrad) - (Math.Sin(Alt) * Math.Sin(Latrad))) / (Math.Cos(Alt) * Math.Cos(Latrad)));

        if (Math.Sin(Hrad) > 0)
        {
            Az = (2 * Math.PI) - Az;
        }

        return (TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Az)), TimeUtils.ToDegrees(Alt));
    }
}
