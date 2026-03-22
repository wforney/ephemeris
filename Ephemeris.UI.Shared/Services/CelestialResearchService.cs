// Updated: 2026-03-22
using Ephemeris.Phenomenology;

namespace Ephemeris.UI.Services;

/// <summary>
/// Default implementation of <see cref="ICelestialResearchService"/>.
/// Wraps the static <see cref="EphemerisCalculator"/> and <see cref="RiseSetCalculator"/>
/// APIs and offloads computation to a thread-pool thread so that UI threads are never blocked.
/// </summary>
/// <remarks>
/// This class implements <see cref="ISingletonService"/> so that Scrutor assembly scanning
/// automatically registers it as a singleton via <c>services.AddEphemerisServices()</c>.
/// </remarks>
public class CelestialResearchService : ICelestialResearchService, ISingletonService
{
    /// <inheritdoc />
    public Task<CelestialResearchData> GetDataAsync(
        DateTime utcTime,
        double longitude,
        double latitude,
        CancellationToken ct = default)
    {
        return Task.Run(() => Compute(utcTime, longitude, latitude), ct);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to <see cref="CelestialEventDetector.GetNext"/> on a background thread.
    /// </remarks>
    public Task<IReadOnlyList<CelestialEventDetector.CelestialEvent>> GetUpcomingEventsAsync(
        DateTime fromUtc, int count = 5, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => CelestialEventDetector.GetNext(fromUtc, count), ct);
    }

    /// <summary>
    /// Performs all synchronous celestial calculations for the given UTC instant and location.
    /// </summary>
    private static CelestialResearchData Compute(DateTime utcTime, double longitude, double latitude)
    {
        CelestialObservation sun  = EphemerisCalculator.GetSunPosition(utcTime, "UTC", longitude, latitude);
        CelestialObservation moon = EphemerisCalculator.GetMoonPosition(utcTime, "UTC", longitude, latitude);

        RiseSetCalculator.RiseTransitSet sunRst  = RiseSetCalculator.Sun(utcTime.Date, longitude, latitude);
        RiseSetCalculator.RiseTransitSet moonRst = RiseSetCalculator.Moon(utcTime.Date, longitude, latitude);

        DateTime nextFullMoon = EphemerisCalculator.NextFullMoon(utcTime);
        DateTime nextNewMoon  = EphemerisCalculator.NextNewMoon(utcTime);

        return new CelestialResearchData(
            Sun:          sun,
            Moon:         moon,
            Sunrise:      sunRst.Rise,
            Sunset:       sunRst.Set,
            Moonrise:     moonRst.Rise,
            Moonset:      moonRst.Set,
            NextFullMoon: nextFullMoon,
            NextNewMoon:  nextNewMoon);
    }
}
