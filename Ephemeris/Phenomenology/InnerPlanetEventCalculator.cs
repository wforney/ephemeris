// Updated: 2026-03-10
using Ephemeris.Chronology;
using Ephemeris.Heliology;
using Ephemeris.Planetology;

namespace Ephemeris.Phenomenology;

/// <summary>
/// Predicts greatest elongation events for Mercury and Venus — the maximum angular separation
/// of an inner planet from the Sun as seen from Earth.
/// </summary>
/// <remarks>
/// Greatest elongation is the most favorable time to observe an inner planet in twilight.
/// Mercury's greatest elongation varies from 18° to 28°; Venus from 45° to 47°.
/// </remarks>
public static class InnerPlanetEventCalculator
{
    private const double StepDays = 0.5;
    private const int MaxScanDays = 600; // Venus synodic period ≈ 584 days; must cover full cycle

    /// <summary>
    /// Finds the next greatest elongation of Mercury or Venus.
    /// </summary>
    /// <param name="planet">Either "mercury" or "venus" (case-insensitive).</param>
    /// <param name="after">Search start date (UTC).</param>
    /// <returns>
    /// A tuple of (Date, ElongationDeg, Direction) where Direction is "East" or "West",
    /// or <see langword="null"/> if no event is found within the search window.
    /// Eastern elongation = planet visible in evening sky; western = morning sky.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if planet is not "mercury" or "venus".</exception>
    public static (DateTime Date, double ElongationDeg, string Direction)? NextGreatestElongation(string planet, DateTime after)
    {
        string normalizedPlanet = planet.ToLowerInvariant();
        if (normalizedPlanet is not "mercury" and not "venus")
            throw new ArgumentException("Only Mercury and Venus have greatest elongation events.", nameof(planet));

        double jd0 = TimeUtils.JulianDay(after.Year, after.Month, after.Day, 12.0);

        double prevElon = Elongation(normalizedPlanet, jd0);
        double prevPrevElon = prevElon;

        for (double d = StepDays; d <= MaxScanDays; d += StepDays)
        {
            double jd = jd0 + d;
            double currElon = Elongation(normalizedPlanet, jd);

            // Local maximum: previous step was increasing, this step is decreasing
            if (d >= 2 * StepDays && prevElon > prevPrevElon && prevElon > currElon && prevElon > 10.0)
            {
                // Refine the peak time with a golden-section search over [jd-2*step, jd]
                double jdPeak = RefineMaximum(normalizedPlanet, jd - (2 * StepDays), jd);
                double peakElon = Elongation(normalizedPlanet, jdPeak);

                // Determine east/west direction via signed elongation
                double signedElon = PlanetaryEventCalculator.SignedElongation(normalizedPlanet, jdPeak);
                string direction = signedElon > 0 ? "East" : "West";

                return (TimeZoneUtils.FromJulianDay(jdPeak), peakElon, direction);
            }

            prevPrevElon = prevElon;
            prevElon = currElon;
        }

        return null;
    }

    private static double Elongation(string planet, double jd)
    {
        double T = TimeUtils.JulianCentury(jd);
        var (xg, yg, zg, _, rEarth) = PlanetaryEventCalculator.GeocentricXyz(planet, T);

        // Sun's geocentric direction from Earth (in ecliptic XYZ)
        var (sunLon, sunR) = SunEphemeris.HeliocentricLongitude(T);
        double sunLonRad = TimeUtils.ToRadians(sunLon);
        double xs = sunR * Math.Cos(sunLonRad);
        double ys = sunR * Math.Sin(sunLonRad);

        double delta = Math.Sqrt((xg * xg) + (yg * yg) + (zg * zg));
        if (delta < 1e-10) return 0.0;

        double cosE = ((xg * xs) + (yg * ys)) / (delta * sunR);
        return TimeUtils.ToDegrees(Math.Acos(Math.Clamp(cosE, -1.0, 1.0)));
    }

    /// <summary>
    /// Refines the peak elongation time within [jdLow, jdHigh] using golden-section maximization.
    /// </summary>
    private static double RefineMaximum(string planet, double jdLow, double jdHigh)
    {
        const double phi = 1.6180339887; // golden ratio
        const double resphi = 2.0 - phi; // = 1 - 1/phi

        double a = jdLow, b = jdHigh;
        double x1 = a + resphi * (b - a);
        double x2 = b - resphi * (b - a);
        double f1 = Elongation(planet, x1);
        double f2 = Elongation(planet, x2);

        for (int iter = 0; iter < 40; iter++)
        {
            if (f1 < f2)
            {
                a = x1; x1 = x2; f1 = f2;
                x2 = b - resphi * (b - a);
                f2 = Elongation(planet, x2);
            }
            else
            {
                b = x2; x2 = x1; f2 = f1;
                x1 = a + resphi * (b - a);
                f1 = Elongation(planet, x1);
            }
            if (b - a < 1e-4) // ~8 seconds precision
                break;
        }

        return (a + b) / 2.0;
    }
}
