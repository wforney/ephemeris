// Updated: 2026-03-22
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Heliology;
using Ephemeris.Phenomenology;
using Ephemeris.Selenography;

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
    public Task<CelestialResearchData> GetDataForJulianDayAsync(
        double julianDay,
        double longitude,
        double latitude,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => ComputeForJulianDay(julianDay, longitude, latitude), ct);
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

    /// <summary>
    /// Computes celestial positions for any Julian Day (including BCE/BC dates).
    /// Uses core ephemeris algorithms parameterised by Julian Century T so the epoch
    /// is not limited to the range of <see cref="DateTime"/>.
    /// </summary>
    private static CelestialResearchData ComputeForJulianDay(double julianDay, double longitude, double latitude)
    {
        double T = TimeUtils.JulianCentury(julianDay);

        var (sunRA, sunDec, _)   = SunEphemeris.ApparentEquatorialCoordinates(T);
        HorizontalCoordinates sunH = ObserverGeometry.EquatorialToHorizontal(sunRA, sunDec, julianDay, longitude, latitude);

        var (moonRA, moonDec, _) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        HorizontalCoordinates moonH = ObserverGeometry.EquatorialToHorizontal(moonRA, moonDec, julianDay, longitude, latitude);
        double moonIllumination = MoonEphemeris.PhaseAngle(T) / 180.0;

        return new CelestialResearchData(
            Sun:          new CelestialObservation(sunRA, sunDec, sunH.Azimuth, sunH.Altitude),
            Moon:         new CelestialObservation(moonRA, moonDec, moonH.Azimuth, moonH.Altitude, moonIllumination),
            Sunrise:      null,
            Sunset:       null,
            Moonrise:     null,
            Moonset:      null,
            NextFullMoon: null,
            NextNewMoon:  null);
    }
}
