// Updated: 2026-03-10
using Ephemeris.Chronology;

namespace Ephemeris.Geodesy;

/// <summary>
/// Computes atmospheric refraction corrections for celestial object altitudes.
/// Provides both forward (geometric → apparent) and inverse (apparent → geometric) corrections.
/// </summary>
public static class RefractionCalculator
{
    /// <summary>
    /// Minimum altitude below which refraction corrections are not applied.
    /// Objects below this geometric altitude are too close to the horizon for the formulae to be reliable.
    /// </summary>
    private const double MinAltitudeDeg = -1.0;

    /// <summary>
    /// Converts a geometric (true) altitude to an apparent (observed) altitude by applying Bennett's refraction formula.
    /// Refraction lifts objects above the geometric horizon; the returned value is always ≥ the input.
    /// </summary>
    /// <param name="geometricAltitudeDeg">Geometric (true) altitude in degrees [-90, 90].</param>
    /// <returns>
    /// Apparent altitude in degrees. Returns the input unmodified when below −1°.
    /// </returns>
    public static double GeometricToApparent(double geometricAltitudeDeg)
    {
        if (geometricAltitudeDeg < MinAltitudeDeg)
            return geometricAltitudeDeg;

        // Bennett's formula: correction R in arcminutes
        double refractionArcmin = 1.0 / Math.Tan(TimeUtils.ToRadians(
            geometricAltitudeDeg + (7.31 / (geometricAltitudeDeg + 4.4))));
        return geometricAltitudeDeg + (refractionArcmin / 60.0);
    }

    /// <summary>
    /// Converts an apparent (observed) altitude to a geometric (true) altitude by removing refraction using
    /// Saemundsson's inverse formula. More accurate than inverting Bennett's formula directly.
    /// </summary>
    /// <param name="apparentAltitudeDeg">Apparent (observed) altitude in degrees [-90, 90].</param>
    /// <returns>
    /// Geometric altitude in degrees. Returns the input unmodified when below −1°.
    /// </returns>
    public static double ApparentToGeometric(double apparentAltitudeDeg)
    {
        if (apparentAltitudeDeg < MinAltitudeDeg)
            return apparentAltitudeDeg;

        // Saemundsson's inverse formula: R in arcminutes, h_apparent in degrees
        double refractionArcmin = 1.02 / Math.Tan(TimeUtils.ToRadians(
            apparentAltitudeDeg + (10.3 / (apparentAltitudeDeg + 5.11))));
        return apparentAltitudeDeg - (refractionArcmin / 60.0);
    }
}
