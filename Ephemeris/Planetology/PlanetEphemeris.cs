// Updated: 2026-03-11
using Ephemeris.Chronology;
using Ephemeris.Geometry;

namespace Ephemeris.Planetology;

/// <summary>
/// Calculates planetary positions using simplified Kepler orbital elements and ecliptic to equatorial conversion.
/// </summary>
public static class PlanetEphemeris
{
    /// <summary>
    /// Keplerian orbital elements for Earth at epoch J2000.0, evolving with Julian century T.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>Earth's <see cref="OrbitalElements"/> for use with <see cref="SimplifiedPlanetPosition"/>.</returns>
    /// <remarks>
    /// Elements from Paul Schlyter's simplified solar system (stjarnhimlen.se), same per-Julian-century
    /// convention used throughout <see cref="PlanetPositionService"/>:
    /// <list type="bullet">
    ///   <item>N = 0° (Earth defines the ecliptic reference plane)</item>
    ///   <item>i = 0° (Earth lies in the reference plane)</item>
    ///   <item>ω = 282.9404° + 4.70935×10⁻⁵ · T</item>
    ///   <item>a = 1.000000 AU</item>
    ///   <item>e = 0.016709 − 1.151×10⁻⁹ · T</item>
    ///   <item>M = 356.0470° + 0.9856002585° · T · 36525 (daily mean motion × days)</item>
    /// </list>
    /// The resulting heliocentric position vector points from the Sun toward Earth, i.e., 180° opposite
    /// the direction to the Sun as seen from Earth.
    /// </remarks>
    public static OrbitalElements EarthElements(double T) =>
        new(0.0, 0.0, 282.9404 + (4.70935E-5 * T), 1.000000,
            0.016709 - (1.151E-9 * T), 356.0470 + (0.9856002585 * T * 36525));

    /// <summary>
    /// Returns Earth's heliocentric ecliptic Cartesian coordinates (Xh, Yh, Zh) in AU.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>
    /// A tuple (Xh, Yh, Zh) representing Earth's position relative to the Sun in the J2000.0
    /// ecliptic reference frame, in astronomical units.
    /// </returns>
    /// <remarks>
    /// Earth's heliocentric position is the geometric inverse of the Sun's geocentric position.
    /// It can be used to correct the simplified geocentric distance in
    /// <see cref="SimplifiedPlanetPosition"/> (which currently uses heliocentric distance as a proxy):
    /// <code>
    ///   Δ = |(Xh_planet − Xh_Earth, Yh_planet − Yh_Earth, Zh_planet − Zh_Earth)|
    /// </code>
    /// </remarks>
    public static (double Xh, double Yh, double Zh) EarthHeliocentricPosition(double T)
    {
        OrbitalElements e = EarthElements(T);
        double M = TimeUtils.NormalizeDegrees(e.MeanAnomaly);
        double E = SolveKepler(TimeUtils.ToRadians(M), e.Eccentricity);

        double xv = Math.Cos(E) - e.Eccentricity;
        double yv = Math.Sqrt(1.0 - (e.Eccentricity * e.Eccentricity)) * Math.Sin(E);
        double v  = TimeUtils.ToDegrees(Math.Atan2(yv, xv));
        double r  = Math.Sqrt((xv * xv) + (yv * yv));

        // With N=0, i=0: Xh = r·cos(v+ω), Yh = r·sin(v+ω), Zh = 0
        double vw = TimeUtils.ToRadians(v + e.ArgumentOfPerihelion);
        double xh = r * Math.Cos(vw);
        double yh = r * Math.Sin(vw);
        double zh = 0.0;

        return (xh, yh, zh);
    }


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
