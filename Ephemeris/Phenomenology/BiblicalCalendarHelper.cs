// Updated: 2026-03-22
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Heliology;
using Ephemeris.Selenography;

namespace Ephemeris.Phenomenology;

/// <summary>
/// Provides approximate biblical (Hebrew luni-solar) calendar information derived from
/// celestial positions, for use in Biblical cosmology and Mazzaroth research.
/// </summary>
/// <remarks>
/// The biblical calendar is a luni-solar calendar in which months begin at the
/// sighting of the first crescent new moon, and the year begins with Nisan (month 1)
/// at the first new moon on or after the spring equinox.
/// <para>
/// All calculations are approximations based on mean synodic periods and Meeus algorithms.
/// They are intended for research and study purposes, not for liturgical determination.
/// </para>
/// </remarks>
public static class BiblicalCalendarHelper
{
    // Mean synodic period of the Moon (days) — Meeus Ch. 49
    private const double SynodicPeriod = 29.530588861;

    // Reference new moon JD (Jan 6, 2000 new moon) — Meeus Ch. 49, Table 49.a
    private const double ReferenceNewMoonJD = 2451550.09766;

    // Hebrew month names (1=Nisan, …, 12=Adar; 13=Adar II in leap year)
    private static readonly string[] s_monthNames =
    [
        "Nisan", "Iyyar", "Sivan", "Tammuz", "Av", "Elul",
        "Tishrei", "Cheshvan", "Kislev", "Tevet", "Shevat", "Adar", "Adar II",
    ];

    // Hebrew transliterations
    private static readonly string[] s_monthNamesHebrew =
    [
        "ניסן", "אייר", "סיון", "תמוז", "אב", "אלול",
        "תשרי", "חשוון", "כסלו", "טבת", "שבט", "אדר", "אדר ב׳",
    ];

    // Mazzaroth sign names (Western and Hebrew) indexed by 30° ecliptic longitude bands
    private static readonly string[] s_mazzarothWestern =
    [
        "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo",
        "Libra", "Scorpio", "Sagittarius", "Capricorn", "Aquarius", "Pisces",
    ];

    private static readonly string[] s_mazzarothHebrew =
    [
        "Taleh", "Shor", "Teomim", "Sartan", "Aryeh", "Betulah",
        "Moznayim", "Akrav", "Keshet", "Gedi", "Deli", "Dagim",
    ];

    /// <summary>
    /// Represents an approximate biblical (Hebrew luni-solar) calendar date with
    /// contextual information for Mazzaroth research.
    /// </summary>
    /// <param name="Year">Approximate Hebrew year (Julian year + 3760).</param>
    /// <param name="Month">Biblical month number: 1=Nisan/Abib, 2=Iyyar, … 12=Adar (13=Adar II in leap year).</param>
    /// <param name="MonthName">English transliteration of the Hebrew month name (e.g., "Nisan").</param>
    /// <param name="MonthNameHebrew">Hebrew script month name (e.g., "ניסן").</param>
    /// <param name="DayOfMonth">Approximate day within the lunar month (1–30).</param>
    /// <param name="IsNewMoonVisibility">
    /// <see langword="true"/> if the crescent new moon (Rosh Chodesh) is estimated to be
    /// visible at this location on this evening.
    /// </param>
    /// <param name="Season">Astronomical season: "Spring", "Summer", "Autumn", or "Winter".</param>
    /// <param name="SolarSign">Western zodiac name of the Mazzaroth sign the Sun currently occupies.</param>
    /// <param name="SolarSignHebrew">Hebrew name of the Mazzaroth sign (e.g., "Aryeh" for Leo).</param>
    /// <param name="Description">Human-readable summary combining all calendar fields.</param>
    public record BiblicalDate(
        int Year,
        int Month,
        string MonthName,
        string MonthNameHebrew,
        int DayOfMonth,
        bool IsNewMoonVisibility,
        string Season,
        string SolarSign,
        string SolarSignHebrew,
        string Description);

    /// <summary>
    /// Determines approximate biblical calendar information for a given Julian Day and observer location.
    /// </summary>
    /// <param name="julianDay">Julian Day number (fractional, UTC).</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>
    /// A <see cref="BiblicalDate"/> containing the approximate Hebrew year, biblical month,
    /// lunar day, season, Mazzaroth sign, and crescent visibility for the given moment.
    /// </returns>
    /// <remarks>
    /// Algorithm overview:
    /// <list type="number">
    ///   <item>Convert JD to a Julian Century T for celestial calculations.</item>
    ///   <item>Compute the Sun's true ecliptic longitude to determine season and Mazzaroth sign.</item>
    ///   <item>Compute the Moon's mean elongation D to determine the day within the lunar month.</item>
    ///   <item>Find the approximate Julian Day of the most recent new moon (mean synodic reference, Meeus Ch. 49).</item>
    ///   <item>Find Nisan 1 = first new moon JD on or after the spring equinox for the current year.</item>
    ///   <item>Count lunations from Nisan 1 to the current new moon to derive the biblical month number.</item>
    ///   <item>Hebrew year ≈ Julian year + 3760 (the traditional anno mundi offset).</item>
    /// </list>
    /// <para>Accuracy: Month boundaries are within ±1 day of observational calendars for dates near J2000.</para>
    /// </remarks>
    public static BiblicalDate GetBiblicalDate(double julianDay, double longitude, double latitude)
    {
        double T = (julianDay - 2451545.0) / 36525.0;

        // Sun's true ecliptic longitude (used for Mazzaroth sign and season)
        double sunLon = SunTrueEclipticLongitude(T);
        string solarSign = GetMazzarothSign(sunLon);
        string solarSignHebrew = GetMazzarothSignHebrew(sunLon);

        // Season from Sun's ecliptic longitude (0°=spring equinox, 90°=summer solstice, etc.)
        string season = GetSeasonFromSunLongitude(sunLon);

        // Moon's mean elongation D (degrees from Sun) — Meeus Ch. 47 mean argument
        double D = MoonMeanElongation(T);
        double moonAgeDays = D / 360.0 * SynodicPeriod;

        // Most recent new moon JD (mean approximation)
        double currentNewMoonJD = julianDay - moonAgeDays;

        // Gregorian year of the current JD
        DateTime dt = TimeZoneUtils.FromJulianDay(julianDay);
        int year = dt.Year;

        // Hebrew year ≈ Julian year + 3760
        // The Hebrew year starts at Tishrei (month 7), roughly September/October.
        // Nisan (month 1) falls in the Gregorian spring, so for dates after Nisan
        // in any Gregorian year, the Hebrew year = Gregorian year + 3760.
        // For dates before Nisan (January–March), it is from the Hebrew year that started the previous autumn.
        int hebrewYear = year + 3760;

        // Find Nisan 1 (first new moon on or after the spring equinox)
        // Use the current year's spring equinox as the anchor.
        double nisan1JD = FindNisan1(year);
        double nisan1JDPrev = FindNisan1(year - 1);

        // Determine which Nisan 1 to count from (current year or previous)
        if (currentNewMoonJD < nisan1JD)
        {
            // We're before this year's Nisan — count from previous year's Nisan
            nisan1JD = nisan1JDPrev;
            hebrewYear -= 1;
        }

        // Number of complete lunations elapsed since Nisan 1
        int monthsElapsed = (int)Math.Round((currentNewMoonJD - nisan1JD) / SynodicPeriod);
        if (monthsElapsed < 0) monthsElapsed = 0;

        // Biblical month (1-based, 1=Nisan, wraps at 13)
        int month = (monthsElapsed % 13) + 1;

        // Clamp month name index (0-based, up to 12 for Adar II)
        int monthNameIdx = Math.Min(month - 1, s_monthNames.Length - 1);
        string monthName = s_monthNames[monthNameIdx];
        string monthNameHebrew = s_monthNamesHebrew[monthNameIdx];

        // Day of the lunar month (1-based)
        int dayOfMonth = (int)Math.Floor(moonAgeDays) + 1;
        dayOfMonth = Math.Clamp(dayOfMonth, 1, 30);

        // Crescent visibility
        bool crescent = IsCrescentVisible(julianDay, longitude, latitude);

        string ordinal = OrdinalSuffix(month);

        string description = $"Hebrew Year {hebrewYear}, Month {ordinal} ({monthName}), Day {dayOfMonth}. " +
                             $"Season: {season}. Sun in {solarSign} ({solarSignHebrew}). " +
                             (crescent ? "Crescent moon visible (Rosh Chodesh)." : "Crescent moon not yet visible.");

        return new BiblicalDate(
            Year: hebrewYear,
            Month: month,
            MonthName: monthName,
            MonthNameHebrew: monthNameHebrew,
            DayOfMonth: dayOfMonth,
            IsNewMoonVisibility: crescent,
            Season: season,
            SolarSign: solarSign,
            SolarSignHebrew: solarSignHebrew,
            Description: description);
    }

    /// <summary>
    /// Determines which Mazzaroth (Hebrew zodiac) sign the Sun currently occupies based on
    /// its ecliptic longitude.
    /// </summary>
    /// <param name="sunLongitudeDegrees">Sun's ecliptic longitude in degrees [0, 360).</param>
    /// <returns>
    /// The Western name of the zodiac sign (e.g., "Aries", "Taurus") — each sign spans 30°
    /// starting from ecliptic longitude 0° (vernal equinox).
    /// </returns>
    /// <remarks>
    /// The twelve Mazzaroth signs are the Hebrew equivalents of the Western zodiac constellations.
    /// Each sign occupies a 30° arc of the ecliptic:
    /// <code>
    ///   0°–30°  : Aries   (Taleh)       180°–210°: Libra       (Moznayim)
    ///  30°–60°  : Taurus  (Shor)        210°–240°: Scorpio     (Akrav)
    ///  60°–90°  : Gemini  (Teomim)      240°–270°: Sagittarius (Keshet)
    ///  90°–120° : Cancer  (Sartan)      270°–300°: Capricorn   (Gedi)
    /// 120°–150° : Leo     (Aryeh)       300°–330°: Aquarius    (Deli)
    /// 150°–180° : Virgo   (Betulah)     330°–360°: Pisces      (Dagim)
    /// </code>
    /// </remarks>
    public static string GetMazzarothSign(double sunLongitudeDegrees)
    {
        double normalized = ((sunLongitudeDegrees % 360) + 360) % 360;
        int index = (int)(normalized / 30.0);
        index = Math.Clamp(index, 0, 11);
        return s_mazzarothWestern[index];
    }

    /// <summary>
    /// Estimates whether the crescent new moon (Rosh Chodesh) is visible at the given
    /// observer location on the evening of the specified Julian Day.
    /// </summary>
    /// <param name="julianDay">Julian Day number (fractional, UTC) of the evening to check.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <returns>
    /// <see langword="true"/> if the crescent is estimated to be visible;
    /// <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    /// Visibility criteria (simplified Yallop / Odeh method):
    /// <list type="number">
    ///   <item>Moon age (elapsed time since conjunction) must be less than 2.5 days.</item>
    ///   <item>Moon altitude above the true horizon at civil sunset must exceed 5°.</item>
    /// </list>
    /// Moon age is derived from the Moon's mean elongation D (Meeus Ch. 47):
    /// <code>
    ///   D        = 297.8501921 + 445267.1114034 × T  (degrees, normalized)
    ///   moon_age = D / 360 × 29.530589               (days)
    /// </code>
    /// Sunset time is obtained from <see cref="RiseSetCalculator.Sun"/> and the Moon's
    /// altitude at that moment is computed via <see cref="ObserverGeometry.EquatorialToHorizontal"/>.
    /// <para>
    /// This is an approximation. For liturgical crescent determination, use a dedicated
    /// Hillazonot or Odeh visibility model with topocentric corrections.
    /// </para>
    /// </remarks>
    public static bool IsCrescentVisible(double julianDay, double longitude, double latitude)
    {
        double T = (julianDay - 2451545.0) / 36525.0;

        // Moon age in days within the current synodic month
        double D = MoonMeanElongation(T);
        double moonAgeDays = D / 360.0 * SynodicPeriod;

        // Criterion 1: Moon must be young (< 2.5 days past new moon)
        if (moonAgeDays >= 2.5)
            return false;

        // Criterion 2: Moon altitude at sunset must exceed 5°
        // Get sunset time for this date
        DateTime date = TimeZoneUtils.FromJulianDay(julianDay).Date;
        var rts = RiseSetCalculator.Sun(DateTime.SpecifyKind(date, DateTimeKind.Utc), longitude, latitude);

        if (!rts.Set.HasValue)
            return false; // circumpolar — no defined sunset

        double sunsetJD = TimeZoneUtils.ToJulianDay(rts.Set.Value);

        // Moon's equatorial coordinates at sunset
        double Tsunset = (sunsetJD - 2451545.0) / 36525.0;
        var (moonRA, moonDec, _) = MoonEphemeris.GeocentricEquatorialCoordinates(Tsunset);

        // Moon's altitude at sunset
        var horizontal = ObserverGeometry.EquatorialToHorizontal(moonRA, moonDec, sunsetJD, longitude, latitude);
        return horizontal.Altitude > 5.0;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns the Moon's mean elongation D (degrees, [0, 360)) from Julian Century T.
    /// </summary>
    /// <remarks>Meeus Ch. 47, mean argument: D = 297.8501921 + 445267.1114034 × T.</remarks>
    private static double MoonMeanElongation(double T) =>
        TimeUtils.NormalizeDegrees(297.8501921 + (445267.1114034 * T));

    /// <summary>Returns the Hebrew Mazzaroth sign name for the given Sun ecliptic longitude.</summary>
    private static string GetMazzarothSignHebrew(double sunLongitudeDegrees)
    {
        double normalized = ((sunLongitudeDegrees % 360) + 360) % 360;
        int index = Math.Clamp((int)(normalized / 30.0), 0, 11);
        return s_mazzarothHebrew[index];
    }

    /// <summary>Returns a short English ordinal suffix string for a month number (1→"1st", 2→"2nd", etc.).</summary>
    private static string OrdinalSuffix(int n) => n switch
    {
        1 => "1st", 2 => "2nd", 3 => "3rd", _ => $"{n}th",
    };

    /// <summary>Computes the Sun's true ecliptic longitude (degrees) from Julian Century T.</summary>
    /// <remarks>
    /// Meeus Ch. 25: L0 (mean longitude) + C (equation of center), without nutation/aberration.
    /// Accuracy ~0.01° — sufficient for 30° sign boundaries.
    /// </remarks>
    private static double SunTrueEclipticLongitude(double T)
    {
        double L0 = TimeUtils.NormalizeDegrees(280.46646 + (36000.76983 * T) + (0.0003032 * T * T));
        double M  = TimeUtils.NormalizeDegrees(357.52911 + (35999.05029 * T) - (0.0001537 * T * T));
        double Mrad = TimeUtils.ToRadians(M);

        double C = ((1.914602 - (0.004817 * T) - (0.000014 * T * T)) * Math.Sin(Mrad))
                 + ((0.019993 - (0.000101 * T)) * Math.Sin(2 * Mrad))
                 + (0.000289 * Math.Sin(3 * Mrad));

        return TimeUtils.NormalizeDegrees(L0 + C);
    }

    /// <summary>Returns the astronomical season name from the Sun's ecliptic longitude.</summary>
    private static string GetSeasonFromSunLongitude(double sunLon)
    {
        double lon = ((sunLon % 360) + 360) % 360;
        return lon switch
        {
            < 90.0  => "Spring",
            < 180.0 => "Summer",
            < 270.0 => "Autumn",
            _       => "Winter",
        };
    }

    /// <summary>
    /// Finds the Julian Day of Nisan 1 (first new moon on or after the spring equinox)
    /// for the given Gregorian year.
    /// </summary>
    /// <remarks>
    /// Uses the mean synodic period reference (Meeus Ch. 49) to find the first new moon
    /// not earlier than the spring equinox computed by <see cref="SeasonCalculator"/>.
    /// </remarks>
    private static double FindNisan1(int year)
    {
        DateTime springEquinox = SeasonCalculator.Calculate(year, SeasonCalculator.Season.SpringEquinox);
        double equinoxJD = TimeZoneUtils.ToJulianDay(springEquinox);

        // Find the new moon lunation number closest to the equinox
        long k = (long)Math.Floor((equinoxJD - ReferenceNewMoonJD) / SynodicPeriod);
        double newMoonJD = ReferenceNewMoonJD + (k * SynodicPeriod);

        // Advance to the first new moon on or after the equinox
        while (newMoonJD < equinoxJD)
        {
            k++;
            newMoonJD = ReferenceNewMoonJD + (k * SynodicPeriod);
        }

        return newMoonJD;
    }
}
