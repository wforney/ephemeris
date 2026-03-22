// Updated: 2026-03-22
using Ephemeris.Phenomenology;

namespace Ephemeris.UI.Services;

/// <summary>
/// Default implementation of <see cref="ICelestialResearchService"/>.
/// Wraps the static <see cref="EphemerisCalculator"/> and <see cref="RiseSetCalculator"/>
/// APIs and offloads computation to a thread-pool thread so that UI threads are never blocked.
/// </summary>
/// <remarks>
/// This class implements <see cref="ISingletonService"/> so that Scrutor's assembly scan
/// in <see cref="Ephemeris.ServiceCollectionExtensions.AddEphemerisServices"/> will register
/// it as a singleton in the DI container when the UI.Shared assembly is included in the scan.
/// </remarks>
public class CelestialResearchService : ICelestialResearchService, ISingletonService
{
    /// <inheritdoc />
    /// <remarks>
    /// All calculation work is dispatched to <see cref="Task.Run"/> to keep UI threads
    /// responsive. The <paramref name="ct"/> is passed through to enable cancellation of
    /// queued work before results are returned.
    /// </remarks>
    public Task<CelestialResearchData> GetDataAsync(
        DateTime utcTime,
        double longitude,
        double latitude,
        CancellationToken ct = default)
    {
        return Task.Run(() => Compute(utcTime, longitude, latitude), ct);
    }

    /// <summary>
    /// Performs all synchronous celestial calculations for the given UTC instant and location.
    /// </summary>
    /// <param name="utcTime">UTC date and time of the observation.</param>
    /// <param name="longitude">Observer longitude in degrees (East positive).</param>
    /// <param name="latitude">Observer latitude in degrees (North positive).</param>
    /// <returns>A fully populated <see cref="CelestialResearchData"/> record.</returns>
    private static CelestialResearchData Compute(DateTime utcTime, double longitude, double latitude)
    {
        // Sun and Moon positions — pass "UTC" because utcTime is already in UTC.
        CelestialObservation sun  = EphemerisCalculator.GetSunPosition(utcTime, "UTC", longitude, latitude);
        CelestialObservation moon = EphemerisCalculator.GetMoonPosition(utcTime, "UTC", longitude, latitude);

        // Today's rise and set times (uses the calendar date at 0h UTC).
        RiseSetCalculator.RiseTransitSet sunRst  = RiseSetCalculator.Sun(utcTime.Date, longitude, latitude);
        RiseSetCalculator.RiseTransitSet moonRst = RiseSetCalculator.Moon(utcTime.Date, longitude, latitude);

        // Next lunar phase events after the query instant.
        DateTime nextFullMoon = EphemerisCalculator.NextFullMoon(utcTime);
        DateTime nextNewMoon  = EphemerisCalculator.NextNewMoon(utcTime);

        // Biblical calendar data derived from Julian Day.
        double jd = TimeZoneUtils.ToJulianDay(utcTime);
        BiblicalCalendarHelper.BiblicalDate? biblicalDate = BiblicalCalendarHelper.GetBiblicalDate(jd, longitude, latitude);

        return new CelestialResearchData(
            Sun:          sun,
            Moon:         moon,
            Sunrise:      sunRst.Rise,
            Sunset:       sunRst.Set,
            Moonrise:     moonRst.Rise,
            Moonset:      moonRst.Set,
            NextFullMoon: nextFullMoon,
            NextNewMoon:  nextNewMoon,
            BiblicalDate: biblicalDate);
    }
}
