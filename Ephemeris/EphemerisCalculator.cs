using Microsoft.Extensions.Logging;

namespace Ephemeris;

/// <summary>
/// EphemerisCalculator provides methods for calculating celestial positions and velocities.
/// </summary>
/// <param name="logger">The logger instance for logging information, warnings, and errors.</param>
public class EphemerisCalculator(ILogger<EphemerisCalculator> logger) : ISingletonService
{
    // Calculation of position and velocity vectors for moving objects (planets, asteroids, comets,
    // spacecraft, etc.)

    // Use of trigonometric expansions for the position of the Earth and planets.

    // Option to use precise orbital elements from an almanac.

    // Calculation of heliocentric, geocentric, and topocentric positions.

    // Optional corrections for parrallax and refraction.

    // Rectangular coordinates and velocity calculations.

    private const double Deg2Rad = Math.PI / 180.0;
    private const double Rad2Deg = 180.0 / Math.PI;

    private readonly Dictionary<string, double> _cache = [];
    private readonly Lock _lock = new();

    public double Acos(double v) => ToDegrees(Math.Acos(v));

    public double Asin(double v) => ToDegrees(Math.Asin(v));

    public double Atan(double v) => ToDegrees(Math.Atan(v));

    public double Atan2(double y, double x) => ToDegrees(Math.Atan2(y, x));

    public double CachedSin(params double[] angles)
    {
        string key = string.Join(",", angles);
        using (_lock.EnterScope())
        {
            if (_cache.TryGetValue(key, out double result))
            {
                return result;
            }

            result = 0;
            foreach (double a in angles)
            {
                result += Sin(a);
            }

            _cache[key] = result;
            return result;
        }
    }

    public double Cos(double deg) => Math.Cos(ToRadians(deg));

    // Greenwich Mean Sidereal Time (degrees)
    public double GMST(double jd)
    {
        double T = JulianCentury(jd);
        double GMST = 280.46061837 + (360.98564736629 * (jd - 2451545.0))
                    + (0.000387933 * T * T) - (T * T * T / 38710000.0);
        return Normalize(GMST);
    }

    // Julian Century since J2000.0
    public double JulianCentury(double jd) => (jd - 2451545.0) / 36525.0;

    // Julian Day (UT)
    public double JulianDay(int year, int month, int day, double hour = 0)
    {
        if (month <= 2)
        {
            year--;
            month += 12;
        }

        int A = year / 100;
        int B = 2 - A + (A / 4);

        double jd = Math.Floor(365.25 * (year + 4716))
                  + Math.Floor(30.6001 * (month + 1))
                  + day + (hour / 24.0) + B - 1524.5;

        return jd;
    }

    // Mean obliquity of the ecliptic (arcseconds → degrees)
    public double MeanObliquity(double T)
    {
        double seconds = 84381.448
                       - (46.8150 * T)
                       - (0.00059 * T * T)
                       + (0.001813 * T * T * T);

        return seconds / 3600.0;
    }

    public double Normalize(double deg)
    {
        deg %= 360;
        return deg < 0 ? deg + 360 : deg;
    }

    // Nutation in longitude and obliquity (very simplified version)
    public (double deltaPsi, double deltaEpsilon) Nutation(double T)
    {
        // Delaunay arguments (simplified for illustration)
        double omega = Normalize(125.04 - (1934.136 * T));  // Moon's ascending node
        double L = Normalize(280.47 + (36000.77 * T));      // Sun's mean longitude
        double L1 = Normalize(218.316 + (481267.881 * T));  // Moon's mean longitude

        double deltaPsi = (-17.20 * Sin(omega)) - (1.32 * Sin(2 * L))
                          - (0.23 * Sin(2 * L1)) + (0.21 * Sin(2 * omega));

        double deltaEpsilon = (9.20 * Cos(omega)) + (0.57 * Cos(2 * L))
                              + (0.10 * Cos(2 * L1)) - (0.09 * Cos(2 * omega));

        return (deltaPsi / 3600.0, deltaEpsilon / 3600.0); // arcsec to degrees
    }

    public double Sin(double deg) => Math.Sin(ToRadians(deg));

    public double Tan(double deg) => Math.Tan(ToRadians(deg));

    public double ToDegrees(double rad) => rad * Rad2Deg;

    public double ToRadians(double deg) => deg * Deg2Rad;

    // Sidereal Time with Nutation
    public double TrueSiderealTime(double jd)
    {
        double T = JulianCentury(jd);
        (double deltaPsi, double deltaEpsilon) = Nutation(T);
        double eps = MeanObliquity(T) + deltaEpsilon;

        return Normalize(GMST(jd) + (deltaPsi * Cos(eps)));
    }
}
