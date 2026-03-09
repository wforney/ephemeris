// Updated: 2026-03-09
using Ephemeris.Geometry;

namespace Ephemeris.Stellarography;

/// <summary>
/// Represents one entry from the fixed-star catalog file (<c>sefstars.txt</c>).
/// All angular values are in decimal degrees at epoch J2000.0 (ICRS unless noted).
/// </summary>
/// <param name="CommonName">Human-readable common name (e.g., "Sirius").</param>
/// <param name="BayerDesignation">Bayer designation string (e.g., "alCMa").</param>
/// <param name="Frame">Coordinate reference frame ("ICRS", "FK5", or "2000").</param>
/// <param name="RightAscensionJ2000">Right ascension at J2000.0 in decimal degrees [0, 360).</param>
/// <param name="DeclinationJ2000">Declination at J2000.0 in decimal degrees [−90, 90].</param>
/// <param name="ProperMotionRaCosD">
/// Proper motion in RA multiplied by cos(Dec), in mas/yr.
/// To convert to an RA shift divide by cos(Dec) at the target epoch.
/// </param>
/// <param name="ProperMotionDec">Proper motion in declination, in mas/yr.</param>
/// <param name="RadialVelocityKmS">Radial velocity in km/s (positive = receding).</param>
/// <param name="ParallaxMas">Annual trigonometric parallax in milli-arcseconds.</param>
/// <param name="Magnitude">Visual (V-band) apparent magnitude.</param>
/// <param name="SpectralType">Spectral classification string (e.g. "A1V", "G2V"); empty when not available.</param>
public record FixedStar(
    string CommonName,
    string BayerDesignation,
    string Frame,
    double RightAscensionJ2000,
    double DeclinationJ2000,
    double ProperMotionRaCosD,
    double ProperMotionDec,
    double RadialVelocityKmS,
    double ParallaxMas,
    double Magnitude,
    string SpectralType = "")
{
    /// <summary>
    /// Distance in parsecs derived from the parallax.
    /// Returns <see cref="double.PositiveInfinity"/> when parallax is zero.
    /// </summary>
    public double DistanceParsecs =>
        ParallaxMas > 0 ? 1000.0 / ParallaxMas : double.PositiveInfinity;

    /// <summary>
    /// Returns equatorial coordinates corrected for both proper motion and precession at the target epoch.
    /// For epochs within a few years of J2000, proper motion dominates; precession accumulates at
    /// ~50 arcseconds per year and becomes significant for multi-decade extrapolation.
    /// </summary>
    /// <param name="julianDay">Target Julian Day (ET/TT approximated as UTC for typical use).</param>
    /// <returns>Precessed and proper-motion-corrected <see cref="EquatorialCoordinates"/> in degrees.</returns>
    public EquatorialCoordinates AtEpoch(double julianDay)
    {
        // Step 1: apply proper motion (linear shift from J2000.0)
        EquatorialCoordinates pmCorrected = ApplyProperMotion(julianDay);

        // Step 2: apply precession from J2000.0 to target epoch
        double julianCenturies = (julianDay - 2451545.0) / 36525.0; // Julian centuries since J2000.0
        return Ephemeris.Geodesy.PrecessionCalculator.PrecessFromJ2000(
            pmCorrected.RightAscension,
            pmCorrected.Declination,
            julianCenturies);
    }

    /// <summary>
    /// Applies proper-motion correction to this star's position for the given Julian Day.
    /// </summary>
    /// <param name="julianDay">Target Julian Day (ET/TT).</param>
    /// <returns>Corrected <see cref="EquatorialCoordinates"/> in decimal degrees.</returns>
    public EquatorialCoordinates ApplyProperMotion(double julianDay)
    {
        // Elapsed years since J2000.0 (JD 2451545.0).
        double dtYears = (julianDay - 2451545.0) / 365.25;

        double decRad = double.DegreesToRadians(DeclinationJ2000);

        // pm_ra in the catalog is already μα·cos(δ) in mas/yr.
        // Convert to degrees/yr and divide by cos(δ) to get Δα in degrees/yr.
        double cosDec = Math.Cos(decRad);

        double raShift =
            cosDec > 1e-10
                ? (ProperMotionRaCosD / 3_600_000.0) / cosDec * dtYears
                : 0.0;

        double decShift = (ProperMotionDec / 3_600_000.0) * dtYears;

        double ra  = (RightAscensionJ2000 + raShift + 360.0) % 360.0;
        double dec = Math.Clamp(DeclinationJ2000 + decShift, -90.0, 90.0);

        return new EquatorialCoordinates(ra, dec);
    }
}
