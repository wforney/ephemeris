namespace Ephemeris.Chronology;

public static class TimeUtils
{
    private const double J2000 = 2451545.0;

    public static double DeltaT(double year)
    {
        double y = year;
        // Polynomial fit: approximate delta-T (in seconds)
        if (y < 948)
        {
            double u = (y - 2000) / 100;
            return 2177 + (497 * u) + (44.1 * u * u);
        }
        else if (y < 1600)
        {
            double u = (y - 1000) / 100;
            return 102 + (102 * u) + (25.3 * u * u);
        }
        else if (y < 2000)
        {
            double t = y - 2000;
            return 62.92 + (0.32217 * t) + (0.005589 * t * t);
        }
        else
        {
            double t = y - 2000;
            return 64.7 + (0.293 * t);
        }
    }

    public static double GMST(double jd)
    {
        double T = JulianCentury(jd);
        double gmst = 280.46061837 + (360.98564736629 * (jd - J2000)) +
                      (0.000387933 * T * T) - (T * T * T / 38710000.0);
        return NormalizeDegrees(gmst);
    }

    public static double JulianCentury(double jd) => (jd - J2000) / 36525.0;

    public static double JulianDay(int year, int month, int day, double hour = 0.0)
    {
        if (month <= 2)
        {
            year--;
            month += 12;
        }

        int A = year / 100;
        int B = 2 - A + (A / 4);

        return Math.Floor(365.25 * (year + 4716)) +
               Math.Floor(30.6001 * (month + 1)) +
               day + (hour / 24.0) + B - 1524.5;
    }

    public static double NormalizeDegrees(double angle)
    {
        angle %= 360.0;
        if (angle < 0)
        {
            angle += 360.0;
        }

        return angle;
    }

    public static double ToDegrees(double rad) => rad * 180.0 / Math.PI;

    public static double ToRadians(double deg) => deg * Math.PI / 180.0;
}
