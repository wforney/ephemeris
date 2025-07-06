using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Heliology;
using Ephemeris.Selenography;

namespace Ephemeris;

public static class EphemerisCalculator
{
    public static (double RA, double Dec, double Az, double Alt, double Illumination) GetMoonPosition(int year, int month, int day, double hour, double longitude, double latitude)
    {
        double jd = TimeUtils.JulianDay(year, month, day, hour);
        double T = TimeUtils.JulianCentury(jd);
        (double RA, double Dec, double _) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        (double Az, double Alt) = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        double phaseAngle = MoonEphemeris.PhaseAngle(T);
        double illumination = MoonEphemeris.Illumination(phaseAngle);
        return (RA, Dec, Az, Alt, illumination);
    }

    public static (double RA, double Dec, double Az, double Alt) GetSunPosition(int year, int month, int day, double hour, double longitude, double latitude)
    {
        double jd = TimeUtils.JulianDay(year, month, day, hour);
        double T = TimeUtils.JulianCentury(jd);
        (double RA, double Dec) = SunEphemeris.ApparentEquatorialCoordinates(T);
        (double Az, double Alt) = ObserverGeometry.EquatorialToHorizontal(RA, Dec, jd, longitude, latitude);
        return (RA, Dec, Az, Alt);
    }
}
