// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Selenography;

namespace Ephemeris.Phenomenology;

/// <summary>
/// Predicts lunar occultations of stars and planets as seen from a specific observer location.
/// An occultation occurs when the Moon's limb covers a celestial target.
/// </summary>
/// <remarks>
/// The algorithm:
/// <list type="number">
///   <item><description>Scans forward in 30-minute steps computing the topocentric angular
///     separation between the Moon and the target.</description></item>
///   <item><description>Detects crossings of the Moon's angular radius (~0.25°).</description></item>
///   <item><description>Refines ingress (disappearance) and egress (reappearance) times
///     via binary search to ~10 second precision.</description></item>
/// </list>
/// Topocentric parallax is applied to the Moon (up to ~1° correction), making the
/// event times observer-dependent. The simplified 60-term Meeus lunar series is used;
/// absolute timing accuracy is typically within 1–5 minutes.
/// </remarks>
public static class LunarOccultationCalculator
{
    /// <summary>Maximum days to scan forward before giving up.</summary>
    private const int MaxScanDays = 30;

    /// <summary>Step size for the initial scan (0.5 hours in days).</summary>
    private const double ScanStepDays = 0.5 / 24.0;

    /// <summary>Mean lunar radius in kilometres (IAU 2012 value).</summary>
    private const double MoonRadiusKm = 1737.4;

    /// <summary>
    /// Finds the next occultation of a target by the Moon within a 30-day window,
    /// as seen from the given observer location.
    /// </summary>
    /// <param name="target">Fixed equatorial coordinates of the target (RA/Dec in degrees).</param>
    /// <param name="after">UTC time to start searching from.</param>
    /// <param name="observer">Observer's geographic coordinates.</param>
    /// <param name="targetName">Optional display name of the target (star or planet name).</param>
    /// <returns>
    /// An <see cref="OccultationEvent"/> describing the next disappearance and reappearance
    /// times, or <see langword="null"/> if no occultation occurs within 30 days.
    /// </returns>
    public static OccultationEvent? NextOccultation(
        EquatorialCoordinates target,
        DateTime after,
        GeographicCoordinates observer,
        string targetName = "")
    {
        double jdStart  = TimeZoneUtils.ToJulianDay(after);
        double jdEnd    = jdStart + MaxScanDays;

        double prevJd = jdStart;

        // If already occulted at the start of the search window, Disappearance will remain null.
        bool inside = Separation(jdStart, target, observer) <= MoonAngularRadius(jdStart);
        DateTime? disappearance = null;

        for (double jd = jdStart + ScanStepDays; jd <= jdEnd; jd += ScanStepDays)
        {
            double sep = Separation(jd, target, observer);
            double rad = MoonAngularRadius(jd);

            if (!inside && sep <= rad)
            {
                // Target entering occultation — binary-search for ingress
                inside = true;
                disappearance = RefineContact(prevJd, jd, target, observer, entering: true);
            }
            else if (inside && sep > rad)
            {
                // Target leaving occultation — binary-search for egress
                DateTime reappearance = RefineContact(prevJd, jd, target, observer, entering: false);
                return new OccultationEvent(disappearance, reappearance, targetName);
            }

            prevJd = jd;
        }

        // No complete occultation found in window
        return null;
    }

    // ─── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the topocentric angular separation (degrees) between the Moon and the target
    /// at the given Julian Day, as seen from the observer.
    /// </summary>
    private static double Separation(double jd, EquatorialCoordinates target, GeographicCoordinates observer)
    {
        double T = TimeUtils.JulianCentury(jd);
        var (moonRA, moonDec, distanceKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);

        // Apply topocentric parallax — shifts Moon position by up to ~1° near the horizon.
        var topo = TopocentricParallax.ApplyLunarParallax(
            new EquatorialCoordinates(moonRA, moonDec),
            distanceKm,
            jd,
            observer.Longitude,
            observer.Latitude,
            observer.AltitudeMeters);

        return CoordinateConverter.AngularSeparation(
            topo.RightAscension, topo.Declination,
            target.RightAscension, target.Declination);
    }

    /// <summary>
    /// Returns the Moon's angular radius in degrees at the given Julian Day.
    /// The angular radius varies between ~0.245° (apogee) and ~0.278° (perigee).
    /// </summary>
    private static double MoonAngularRadius(double jd)
    {
        double T = TimeUtils.JulianCentury(jd);
        var (_, _, distanceKm) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        return double.RadiansToDegrees(Math.Asin(MoonRadiusKm / distanceKm));
    }

    /// <summary>
    /// Refines the contact time (ingress or egress) via binary search between
    /// <paramref name="jdLow"/> and <paramref name="jdHigh"/> to ~10 second precision.
    /// </summary>
    /// <param name="jdLow">Lower bound Julian Day (before the crossing).</param>
    /// <param name="jdHigh">Upper bound Julian Day (after the crossing).</param>
    /// <param name="target">Target equatorial coordinates.</param>
    /// <param name="observer">Observer geographic coordinates.</param>
    /// <param name="entering"><see langword="true"/> for ingress (disappearance);
    /// <see langword="false"/> for egress (reappearance).</param>
    /// <returns>UTC <see cref="DateTime"/> of the contact.</returns>
    private static DateTime RefineContact(
        double jdLow, double jdHigh,
        EquatorialCoordinates target, GeographicCoordinates observer,
        bool entering)
    {
        // 50 bisection iterations → precision ≈ ScanStepDays / 2^50 ≈ 0.001 ms (well below 1 second)
        for (int i = 0; i < 50; i++)
        {
            double jdMid = (jdLow + jdHigh) / 2.0;
            double sep   = Separation(jdMid, target, observer);
            double rad   = MoonAngularRadius(jdMid);

            // For ingress: we want the last moment when sep > rad (outside)
            // For egress:  we want the first moment when sep > rad (outside)
            if ((sep <= rad) == entering)
                jdHigh = jdMid;
            else
                jdLow  = jdMid;
        }

        double jdContact = (jdLow + jdHigh) / 2.0;
        return JdToUtc(jdContact);
    }

    /// <summary>Converts a Julian Day number to UTC <see cref="DateTime"/>.</summary>
    private static DateTime JdToUtc(double jd) => TimeZoneUtils.FromJulianDay(jd);
}
