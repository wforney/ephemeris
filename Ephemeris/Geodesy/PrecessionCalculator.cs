// Updated: 2026-03-10
using Ephemeris.Geometry;

namespace Ephemeris.Geodesy;

/// <summary>
/// Calculates precession of the equinoxes — the slow, long-period wobble of Earth's rotation axis.
/// Uses the IAU 2006 / Lieske simplified precession angles.
/// </summary>
public static class PrecessionCalculator
{
    /// <summary>
    /// Calculates the general precession in longitude (ψ_A) for a given epoch.
    /// </summary>
    /// <param name="T">Julian centuries since J2000.0.</param>
    /// <returns>Precession in longitude in degrees.</returns>
    public static double GeneralPrecessionInLongitude(double T)
        => (5029.097222 * T) + (1.558970 * T * T) - (0.000344 * T * T * T);

    /// <summary>
    /// Calculates the precession angles (ζ_A, z_A, θ_A) used to transform mean equatorial coordinates
    /// from one epoch to another using the standard IAU rotation matrix method.
    /// All angles are in degrees.
    /// </summary>
    /// <param name="T">Julian centuries from J2000.0 to the starting epoch.</param>
    /// <param name="t">Julian centuries from the starting epoch to the target epoch.</param>
    /// <returns>
    /// A tuple of (ZetaA, zA, ThetaA) precession rotation angles in degrees.
    /// Apply using: r_target = R3(−zA) · R2(θA) · R3(−ζA) · r_source.
    /// </returns>
    public static (double ZetaA, double zA, double ThetaA) PrecessionAngles(double T, double t)
    {
        double zetaA  = (2306.2181 + (1.39656 * T) - (0.000139 * T * T)) * t
                      + (0.30188 - (0.000344 * T)) * t * t
                      + 0.017998 * t * t * t;
        double zA     = (2306.2181 + (1.39656 * T) - (0.000139 * T * T)) * t
                      + (1.09468 + (0.000066 * T)) * t * t
                      + 0.018203 * t * t * t;
        double thetaA = (2004.3109 - (0.85330 * T) - (0.000217 * T * T)) * t
                      - (0.42665 + (0.000217 * T)) * t * t
                      - 0.041775 * t * t * t;

        // Coefficients in arcseconds → degrees
        return (zetaA / 3600.0, zA / 3600.0, thetaA / 3600.0);
    }

    /// <summary>
    /// Precesses mean equatorial coordinates (RA/Dec) from J2000.0 to a target epoch.
    /// </summary>
    /// <param name="ra2000">Right ascension at J2000.0 in degrees.</param>
    /// <param name="dec2000">Declination at J2000.0 in degrees.</param>
    /// <param name="T">Julian centuries from J2000.0 to the target epoch.</param>
    /// <returns>Precessed <see cref="EquatorialCoordinates"/> in degrees at the target epoch.</returns>
    public static EquatorialCoordinates PrecessFromJ2000(double ra2000, double dec2000, double T)
    {
        (double zetaA, double zA, double thetaA) = PrecessionAngles(0.0, T);

        double zetaRad  = Ephemeris.Chronology.TimeUtils.ToRadians(zetaA);
        double zRad     = Ephemeris.Chronology.TimeUtils.ToRadians(zA);
        double thetaRad = Ephemeris.Chronology.TimeUtils.ToRadians(thetaA);
        double raRad    = Ephemeris.Chronology.TimeUtils.ToRadians(ra2000);
        double decRad   = Ephemeris.Chronology.TimeUtils.ToRadians(dec2000);

        double A = Math.Cos(decRad) * Math.Sin(raRad + zetaRad); // precession rotation matrix element
        double B = (Math.Cos(thetaRad) * Math.Cos(decRad) * Math.Cos(raRad + zetaRad))
                 - (Math.Sin(thetaRad) * Math.Sin(decRad));
        double C = (Math.Sin(thetaRad) * Math.Cos(decRad) * Math.Cos(raRad + zetaRad))
                 + (Math.Cos(thetaRad) * Math.Sin(decRad));

        double raPrecessed  = Ephemeris.Chronology.TimeUtils.NormalizeDegrees(
                                Ephemeris.Chronology.TimeUtils.ToDegrees(Math.Atan2(A, B)) + zA);
        // Clamp to [-1,1]: C is the sine of the precessed declination; floating-point can push it slightly outside range
        double decPrecessed = Ephemeris.Chronology.TimeUtils.ToDegrees(Math.Asin(Math.Clamp(C, -1.0, 1.0)));

        return new EquatorialCoordinates(raPrecessed, decPrecessed);
    }
}
