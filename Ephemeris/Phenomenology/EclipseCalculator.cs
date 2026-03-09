using Ephemeris.Chronology;
using Ephemeris.Heliology;
using Ephemeris.Selenography;

namespace Ephemeris.Phenomenology;

/// <summary>
/// Predicts solar and lunar eclipses using the Meeus Ch. 54 algorithm.
/// Eclipse predictions are approximate: ±1 lunation in time, ±30% in magnitude.
/// For precise eclipse circumstances, use a DE430-based SPICE calculation.
/// </summary>
public static class EclipseCalculator
{
    /// <summary>Describes the type and geometry of an eclipse event.</summary>
    public enum EclipseType
    {
        /// <summary>Total solar eclipse — Moon completely covers the Sun.</summary>
        TotalSolar,
        /// <summary>Annular solar eclipse — Moon is too small to cover the Sun completely.</summary>
        AnnularSolar,
        /// <summary>Partial solar eclipse — Moon partially obscures the solar disk.</summary>
        PartialSolar,
        /// <summary>Hybrid solar eclipse — switches between total and annular.</summary>
        HybridSolar,
        /// <summary>Total lunar eclipse — Moon fully enters Earth's umbra.</summary>
        TotalLunar,
        /// <summary>Partial lunar eclipse — Moon partially enters Earth's umbra.</summary>
        PartialLunar,
        /// <summary>Penumbral lunar eclipse — Moon enters Earth's penumbra only.</summary>
        PenumbralLunar,
    }

    /// <summary>Represents a predicted eclipse event.</summary>
    /// <param name="Type">Classification of the eclipse.</param>
    /// <param name="DateTime">Approximate UTC time of greatest eclipse.</param>
    /// <param name="Magnitude">
    ///   Eclipse magnitude: fraction of solar/lunar diameter eclipsed.
    ///   Values > 1.0 indicate total eclipse. Negative values for penumbral events.
    /// </param>
    /// <param name="Gamma">
    ///   Distance of the Moon's shadow axis from the Earth's center in units of equatorial radii.
    ///   Values where |gamma| &lt; 1.0 indicate central eclipses.
    /// </param>
    public record struct EclipseEvent(EclipseType Type, DateTime DateTime, double Magnitude, double Gamma);

    private const double MeanLunationDays = 29.530588853;

    /// <summary>
    /// Returns all solar eclipses visible anywhere on Earth in the specified UTC year range.
    /// </summary>
    /// <param name="startYear">First year to search (inclusive).</param>
    /// <param name="endYear">Last year to search (inclusive).</param>
    /// <returns>
    ///   A list of <see cref="EclipseEvent"/> instances for each solar eclipse, ordered by date.
    /// </returns>
    public static List<EclipseEvent> SolarEclipses(int startYear, int endYear)
        => FindEclipses(startYear, endYear, lunar: false);

    /// <summary>
    /// Returns all lunar eclipses in the specified UTC year range.
    /// </summary>
    /// <param name="startYear">First year to search (inclusive).</param>
    /// <param name="endYear">Last year to search (inclusive).</param>
    /// <returns>
    ///   A list of <see cref="EclipseEvent"/> instances for each lunar eclipse, ordered by date.
    /// </returns>
    public static List<EclipseEvent> LunarEclipses(int startYear, int endYear)
        => FindEclipses(startYear, endYear, lunar: true);

    /// <summary>
    /// Returns the next solar eclipse after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <returns>The next predicted solar eclipse, or <c>null</c> if none found within 5 years.</returns>
    public static EclipseEvent? NextSolarEclipse(DateTime after)
    {
        var events = SolarEclipses(after.Year, after.Year + 5);
        return events.FirstOrDefault(e => e.DateTime > after);
    }

    /// <summary>
    /// Returns the next lunar eclipse after the given UTC date.
    /// </summary>
    /// <param name="after">Starting UTC date.</param>
    /// <returns>The next predicted lunar eclipse, or <c>null</c> if none found within 5 years.</returns>
    public static EclipseEvent? NextLunarEclipse(DateTime after)
    {
        var events = LunarEclipses(after.Year, after.Year + 5);
        return events.FirstOrDefault(e => e.DateTime > after);
    }

    // --- Core eclipse search ---

    private static List<EclipseEvent> FindEclipses(int startYear, int endYear, bool lunar)
    {
        List<EclipseEvent> results = [];

        // Starting k: new moon (k integer) or full moon (k + 0.5) near startYear
        double k0 = (startYear - 2000.0) * 12.3685;
        double kEnd = (endYear + 1 - 2000.0) * 12.3685;

        for (double k = Math.Floor(k0); k <= kEnd; k += 1.0)
        {
            double kCandidate = lunar ? k + 0.5 : k;  // full moon for lunar, new moon for solar
            var evt = CheckEclipse(kCandidate, lunar);
            if (evt.HasValue)
                results.Add(evt.Value);
        }

        return [.. results.Where(e => e.DateTime.Year >= startYear && e.DateTime.Year <= endYear).OrderBy(e => e.DateTime)];
    }

    private static EclipseEvent? CheckEclipse(double k, bool lunar)
    {
        // Meeus Ch. 49 — time of new/full moon
        double T = k / 1236.85;
        double T2 = T * T;
        double T3 = T2 * T;
        double T4 = T2 * T2;

        // Julian Ephemeris Day of the mean phase
        double JDE = 2451550.09766 + (MeanLunationDays * k)
                   + (0.00015437 * T2)
                   - (0.000000150 * T3)
                   + (0.00000000073 * T4);

        double M   = TimeUtils.NormalizeDegrees(2.5534  + (29.10535669 * k) - (0.0000218 * T2) - (0.00000011 * T3));
        double Mp  = TimeUtils.NormalizeDegrees(201.5643 + (385.81693528 * k) + (0.0107438 * T2) + (0.00001239 * T3) - (0.000000058 * T4));
        double F   = TimeUtils.NormalizeDegrees(160.7108 + (390.67050274 * k) - (0.0016341 * T2) - (0.00000227 * T3) + (0.000000011 * T4));
        double Om  = TimeUtils.NormalizeDegrees(124.7746 - (1.5637558 * k) + (0.0020691 * T2) + (0.00000215 * T3));

        // Quick check: if |sin(F)| > 0.36, no eclipse is possible (Moon too far from node)
        double sinF = Math.Sin(TimeUtils.ToRadians(F));
        if (Math.Abs(sinF) > 0.36)
            return null;

        double e = 1.0 - (0.002516 * T) - (0.0000074 * T2);

        double MRad  = TimeUtils.ToRadians(M);
        double MpRad = TimeUtils.ToRadians(Mp);
        double FRad  = TimeUtils.ToRadians(F);
        double OmRad = TimeUtils.ToRadians(Om);

        // Corrections to JDE (Meeus Eq. 49.5 — solar eclipse corrections at new moon)
        double F1 = TimeUtils.ToRadians(F - 0.02665 * Math.Sin(OmRad));
        double A1 = TimeUtils.ToRadians(299.77 + (0.107408 * k) - (0.009173 * T2));

        double dJDE;
        if (!lunar)
        {
            dJDE = -0.40720 * Math.Sin(MpRad)
                 + 0.17241 * e * Math.Sin(MRad)
                 + 0.01608 * Math.Sin(2 * MpRad)
                 + 0.01039 * Math.Sin(2 * FRad)
                 + 0.00739 * e * Math.Sin(MpRad - MRad)
                 - 0.00514 * e * Math.Sin(MpRad + MRad)
                 + 0.00208 * e * e * Math.Sin(2 * MRad)
                 - 0.00111 * Math.Sin(MpRad - 2 * FRad)
                 - 0.00057 * Math.Sin(MpRad + 2 * FRad)
                 + 0.00056 * e * Math.Sin(2 * MpRad + MRad)
                 - 0.00042 * Math.Sin(3 * MpRad)
                 + 0.00042 * e * Math.Sin(MRad + 2 * FRad)
                 + 0.00038 * e * Math.Sin(MRad - 2 * FRad)
                 - 0.00024 * e * Math.Sin(2 * MpRad - MRad)
                 - 0.00017 * Math.Sin(OmRad)
                 - 0.00007 * Math.Sin(MpRad + 2 * MRad)
                 + 0.00004 * Math.Sin(2 * MpRad - 2 * FRad)
                 + 0.00004 * Math.Sin(3 * MRad)
                 + 0.00003 * Math.Sin(MpRad + MRad - 2 * FRad)
                 + 0.00003 * Math.Sin(2 * MpRad + 2 * FRad)
                 - 0.00003 * Math.Sin(MpRad + MRad + 2 * FRad)
                 + 0.00003 * Math.Sin(MpRad - MRad + 2 * FRad)
                 - 0.00002 * Math.Sin(MpRad - MRad - 2 * FRad)
                 - 0.00002 * Math.Sin(3 * MpRad + MRad)
                 + 0.00002 * Math.Sin(4 * MpRad);
        }
        else
        {
            dJDE = -0.40614 * Math.Sin(MpRad)
                 + 0.17302 * e * Math.Sin(MRad)
                 + 0.01614 * Math.Sin(2 * MpRad)
                 + 0.01043 * Math.Sin(2 * FRad)
                 + 0.00734 * e * Math.Sin(MpRad - MRad)
                 - 0.00515 * e * Math.Sin(MpRad + MRad)
                 + 0.00209 * e * e * Math.Sin(2 * MRad)
                 - 0.00111 * Math.Sin(MpRad - 2 * FRad)
                 - 0.00057 * Math.Sin(MpRad + 2 * FRad)
                 + 0.00056 * e * Math.Sin(2 * MpRad + MRad)
                 - 0.00042 * Math.Sin(3 * MpRad)
                 + 0.00042 * e * Math.Sin(MRad + 2 * FRad)
                 + 0.00038 * e * Math.Sin(MRad - 2 * FRad)
                 - 0.00024 * e * Math.Sin(2 * MpRad - MRad)
                 - 0.00017 * Math.Sin(OmRad)
                 - 0.00007 * Math.Sin(MpRad + 2 * MRad)
                 + 0.00004 * Math.Sin(2 * MpRad - 2 * FRad)
                 + 0.00004 * Math.Sin(3 * MRad)
                 + 0.00003 * Math.Sin(MpRad + MRad - 2 * FRad)
                 + 0.00003 * Math.Sin(2 * MpRad + 2 * FRad)
                 - 0.00003 * Math.Sin(MpRad + MRad + 2 * FRad)
                 + 0.00003 * Math.Sin(MpRad - MRad + 2 * FRad)
                 - 0.00002 * Math.Sin(MpRad - MRad - 2 * FRad)
                 - 0.00002 * Math.Sin(3 * MpRad + MRad)
                 + 0.00002 * Math.Sin(4 * MpRad);
        }

        double JDEmax = JDE + dJDE;

        // Ascending/descending node parameter (Meeus Eq. 54.2)
        double gamma = (0.0028 - (0.0004 * Math.Cos(MRad)) + (0.0003 * Math.Cos(MpRad))) * Math.Cos(FRad)
                     + ((0.0003 * Math.Cos(MRad)) - (0.0099 * Math.Sin(FRad) * Math.Cos(FRad)));

        // Umbral/penumbral magnitudes (Meeus Eq. 54.3)
        double u = 0.0059 + (0.0046 * Math.Cos(MRad)) - (0.0182 * Math.Cos(MpRad)) + (0.0004 * Math.Cos(2 * MpRad)) - (0.00005 * Math.Cos(FRad));

        // Eclipse condition (Meeus Table 54.a)
        double absF = Math.Abs(Math.Sin(F1));
        if (lunar)
        {
            if (absF > 1.0412)
                return null; // no lunar eclipse

            double magnitude;
            EclipseType type;
            if (absF <= 0.9972)
            {
                magnitude = 1.0128 - u - Math.Abs(gamma);
                type = magnitude > 0 ? EclipseType.TotalLunar : EclipseType.PartialLunar;
            }
            else
            {
                magnitude = 1.5573 + u - Math.Abs(gamma);
                type = magnitude < 0 ? EclipseType.PenumbralLunar : EclipseType.PartialLunar;
            }

            return new EclipseEvent(type, JulianDayToDateTime(JDEmax), magnitude, gamma);
        }
        else
        {
            // Solar eclipse
            if (absF > 0.9972 && absF > (1.5433 + u))
                return null; // no solar eclipse

            double magnitude = (1.5433 + u - Math.Abs(gamma)) / (0.5461 + (2 * u));
            EclipseType type;
            if (Math.Abs(gamma) < 0.9972)
            {
                type = u < 0 ? EclipseType.TotalSolar
                     : u > 0.0047 ? EclipseType.AnnularSolar
                     : EclipseType.HybridSolar;
            }
            else
            {
                type = EclipseType.PartialSolar;
            }

            return new EclipseEvent(type, JulianDayToDateTime(JDEmax), magnitude, gamma);
        }
    }

    private static DateTime JulianDayToDateTime(double jd)
    {
        double z = Math.Floor(jd + 0.5);
        double f = jd + 0.5 - z;
        double a = z >= 2299161
            ? Math.Floor((z - 1867216.25) / 36524.25)
            : z;
        if (z >= 2299161)
            a = z + 1 + a - Math.Floor(a / 4);
        double b = a + 1524;
        double c = Math.Floor((b - 122.1) / 365.25);
        double d = Math.Floor(365.25 * c);
        double e = Math.Floor((b - d) / 30.6001);
        int day   = (int)(b - d - Math.Floor(30.6001 * e));
        int month = e < 14 ? (int)e - 1 : (int)e - 13;
        int year  = month > 2 ? (int)c - 4716 : (int)c - 4715;
        int hour  = (int)(f * 24);
        int min   = (int)((f * 24 - hour) * 60);
        int sec   = (int)(((f * 24 - hour) * 60 - min) * 60);
        try { return new DateTime(year, month, day, hour, min, sec, DateTimeKind.Utc); }
        catch { return DateTime.MinValue; }
    }
}
