using Ephemeris.Chronology;

namespace Ephemeris.Planetology;

/// <summary>
/// Calculates planetary positions using simplified Kepler orbital elements and ecliptic to equatorial conversion.
/// </summary>
public static class PlanetEphemeris
{
    /// <summary>
    /// Calculates a planet's equatorial coordinates using simplified Kepler orbital elements.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <param name="N">Longitude of ascending node in degrees.</param>
    /// <param name="i">Inclination in degrees.</param>
    /// <param name="w">Argument of perihelion in degrees.</param>
    /// <param name="a">Semi-major axis in AU.</param>
    /// <param name="e">Eccentricity.</param>
    /// <param name="M">Mean anomaly in degrees.</param>
    /// <returns>A tuple of (RA, Dec) in degrees.</returns>
    public static (double RA, double Dec) SimplifiedPlanetPosition(double T, double N, double i, double w, double a, double e, double M)
    {
        M = TimeUtils.NormalizeDegrees(M);
        double E = SolveKepler(TimeUtils.ToRadians(M), e);

        double xv = Math.Cos(E) - e;
        double yv = Math.Sqrt(1.0 - (e * e)) * Math.Sin(E);
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

    /// <summary>
    /// Solves Kepler's equation M = E - e·sin(E) for the eccentric anomaly E using Newton–Raphson iteration.
    /// </summary>
    /// <param name="Mrad">Mean anomaly in radians.</param>
    /// <param name="e">Orbital eccentricity.</param>
    /// <returns>Eccentric anomaly in radians, converged to within 1×10⁻⁶ radians.</returns>
    public static double SolveKepler(double Mrad, double e)
    {
        double E = Mrad;
        for (int iter = 0; iter < 50; iter++)
        {
            double dE = (E - (e * Math.Sin(E)) - Mrad) / (1.0 - (e * Math.Cos(E)));
            E -= dE;
            if (Math.Abs(dE) < 1e-6)
                break;
        }
        return E;
    }
}
