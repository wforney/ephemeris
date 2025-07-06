using Ephemeris.Chronology;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ephemeris.Heliology;

public static class SunEphemeris
{
    public static (double longitude, double radiusVector) HeliocentricLongitude(double T)
    {
        // Simplified high-accuracy VSOP87 coefficients for L0
        double L0 = 280.46646 + 36000.76983 * T + 0.0003032 * T * T;
        return (TimeUtils.NormalizeDegrees(L0), 1.00014 - 0.01671 * Math.Cos(TimeUtils.ToRadians(357.529 + 35999.050 * T)));
    }

    public static (double RA, double Dec) ApparentEquatorialCoordinates(double T)
    {
        var (L, _) = HeliocentricLongitude(T);

        // Mean obliquity of the ecliptic (deg)
        double epsilon0 = 23.43929111 - 0.0130042 * T;

        // Convert to radians
        double Lrad = TimeUtils.ToRadians(L);
        double epsRad = TimeUtils.ToRadians(epsilon0);

        // RA/Dec in degrees
        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(Math.Cos(epsRad) * Math.Sin(Lrad), Math.Cos(Lrad))));
        double Dec = TimeUtils.ToDegrees(Math.Asin(Math.Sin(epsRad) * Math.Sin(Lrad)));

        return (RA, Dec);
    }
}
