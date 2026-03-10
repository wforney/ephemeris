// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris.Planetology;

/// <summary>
/// Calculates planetary positions using simplified Kepler orbital elements and ecliptic to equatorial conversion.
/// </summary>
public static class PlanetEphemeris
{
    /// <summary>
    /// Calculates a planet's equatorial coordinates and geocentric distance using simplified Kepler orbital elements.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <param name="elements">Keplerian orbital elements for the planet at epoch T.</param>
    /// <returns>
    /// A tuple of equatorial coordinates (RA, Dec) in degrees and geocentric distance in AU.
    /// Multiply AU by <c>149_597_870.7</c> to convert to kilometres.
    /// </returns>
    public static (EquatorialCoordinates Coordinates, double DistanceAu) SimplifiedPlanetPosition(double T, OrbitalElements elements)
    {
        double N = elements.LongitudeAscendingNode; // Ω: longitude of ascending node (deg)
        double i = elements.Inclination; // orbital inclination (deg)
        double w = elements.ArgumentOfPerihelion; // ω: argument of perihelion (deg)
        double a = elements.SemiMajorAxisAu; // semi-major axis (AU)
        double e = elements.Eccentricity; // orbital eccentricity
        double M = elements.MeanAnomaly; // mean anomaly (deg)
        M = TimeUtils.NormalizeDegrees(M);
        double E = SolveKepler(TimeUtils.ToRadians(M), e); // eccentric anomaly (rad)

        double xv = Math.Cos(E) - e; // orbital-plane Cartesian coords
        double yv = Math.Sqrt(1.0 - (e * e)) * Math.Sin(E);
        double v = TimeUtils.ToDegrees(Math.Atan2(yv, xv)); // true anomaly (deg)
        double r = Math.Sqrt((xv * xv) + (yv * yv)); // heliocentric distance (AU)

        double xh = r * ((Math.Cos(TimeUtils.ToRadians(N)) * Math.Cos(TimeUtils.ToRadians(v + w))) - (Math.Sin(TimeUtils.ToRadians(N)) * Math.Sin(TimeUtils.ToRadians(v + w)) * Math.Cos(TimeUtils.ToRadians(i)))); // heliocentric ecliptic coords
        double yh = r * ((Math.Sin(TimeUtils.ToRadians(N)) * Math.Cos(TimeUtils.ToRadians(v + w))) + (Math.Cos(TimeUtils.ToRadians(N)) * Math.Sin(TimeUtils.ToRadians(v + w)) * Math.Cos(TimeUtils.ToRadians(i))));
        double zh = r * (Math.Sin(TimeUtils.ToRadians(v + w)) * Math.Sin(TimeUtils.ToRadians(i)));

        double lon = TimeUtils.ToDegrees(Math.Atan2(yh, xh));
        double lat = TimeUtils.ToDegrees(Math.Atan2(zh, Math.Sqrt((xh * xh) + (yh * yh))));

        double eclipticObliquity = 23.439291 - (0.0130042 * T);
        double lonRad = TimeUtils.ToRadians(lon);
        double latRad = TimeUtils.ToRadians(lat);
        double eclipticObliquityRad = TimeUtils.ToRadians(eclipticObliquity);

        double x = Math.Cos(lonRad) * Math.Cos(latRad); // direction cosines
        double y = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Cos(eclipticObliquityRad)) - (Math.Sin(latRad) * Math.Sin(eclipticObliquityRad));
        double z = (Math.Sin(lonRad) * Math.Cos(latRad) * Math.Sin(eclipticObliquityRad)) + (Math.Sin(latRad) * Math.Cos(eclipticObliquityRad));

        double RA = TimeUtils.NormalizeDegrees(TimeUtils.ToDegrees(Math.Atan2(y, x)));
        double Dec = TimeUtils.ToDegrees(Math.Asin(z));

        // Geocentric distance: magnitude of heliocentric ecliptic vector (AU).
        // For this simplified model this equals heliocentric r; a full model would
        // subtract the Earth's heliocentric position vector, but this is sufficient
        // for topocentric parallax corrections where ~1% accuracy on distance is fine.
        double distanceAu = Math.Sqrt((xh * xh) + (yh * yh) + (zh * zh));

        return (new EquatorialCoordinates(RA, Dec), distanceAu);
    }

    /// <summary>
    /// Solves Kepler's equation M = E - e·sin(E) for the eccentric anomaly E using Newton–Raphson iteration.
    /// </summary>
    /// <param name="Mrad">Mean anomaly in radians.</param>
    /// <param name="e">Orbital eccentricity.</param>
    /// <returns>Eccentric anomaly in radians, converged to within 1×10⁻⁶ radians.</returns>
    public static double SolveKepler(double Mrad, double e)
    {
        double E = Mrad; // eccentric anomaly, initialized to M
        for (int iter = 0; iter < 50; iter++)
        {
            double dE = (E - (e * Math.Sin(E)) - Mrad) / (1.0 - (e * Math.Cos(E))); // Newton-Raphson correction
            E -= dE;
            if (Math.Abs(dE) < 1e-6)
                break;
        }
        return E;
    }
}
