// Updated: 2026-03-10
using Ephemeris.Geometry;

namespace Ephemeris.Stellarography;

/// <summary>
/// Computes the apparent equatorial position of a fixed star at an arbitrary epoch,
/// applying proper-motion correction and precession from J2000.0.
/// </summary>
public static class StarEphemeris
{
    /// <summary>
    /// Returns the apparent equatorial position of <paramref name="star"/> at the given
    /// Julian year, correcting for proper motion and precession from J2000.0.
    /// </summary>
    /// <param name="star">The star whose position is to be computed.</param>
    /// <param name="julianYear">
    /// Target epoch as a Julian year (e.g., 2000.0 = J2000.0, 2025.5 = mid-2025).
    /// </param>
    /// <returns>
    /// An <see cref="EquatorialCoordinates"/> (RA and Dec in degrees) at the requested epoch.
    /// </returns>
    /// <remarks>
    /// The conversion from Julian year to Julian Day uses:
    /// <code>JD = 2451545.0 + (julianYear − 2000.0) × 365.25</code>
    /// Proper motion is applied as a linear shift from J2000.0; precession uses the
    /// IAU 2006 rotation-matrix method implemented in
    /// <see cref="Geodesy.PrecessionCalculator.PrecessFromJ2000"/>.
    /// </remarks>
    public static EquatorialCoordinates ApparentPosition(FixedStar star, double julianYear)
    {
        ArgumentNullException.ThrowIfNull(star);

        double julianDay = 2451545.0 + (julianYear - 2000.0) * 365.25;
        return star.AtEpoch(julianDay);
    }

    /// <summary>
    /// Returns the apparent equatorial position of <paramref name="star"/> at the given
    /// Julian Day, correcting for proper motion and precession from J2000.0.
    /// </summary>
    /// <param name="star">The star whose position is to be computed.</param>
    /// <param name="julianDay">Target epoch as a Julian Day Number (ET/TT).</param>
    /// <returns>
    /// An <see cref="EquatorialCoordinates"/> (RA and Dec in degrees) at the requested epoch.
    /// </returns>
    public static EquatorialCoordinates ApparentPositionJd(FixedStar star, double julianDay)
    {
        ArgumentNullException.ThrowIfNull(star);

        return star.AtEpoch(julianDay);
    }

    /// <summary>
    /// Returns the J2000.0 equatorial position of <paramref name="star"/> without any
    /// epoch correction (proper motion or precession).
    /// </summary>
    /// <param name="star">The star whose catalog position is required.</param>
    /// <returns><see cref="EquatorialCoordinates"/> at J2000.0.</returns>
    public static EquatorialCoordinates CatalogPosition(FixedStar star)
    {
        ArgumentNullException.ThrowIfNull(star);

        return new EquatorialCoordinates(star.RightAscensionJ2000, star.DeclinationJ2000);
    }
}
