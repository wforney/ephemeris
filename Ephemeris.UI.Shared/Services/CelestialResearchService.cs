// Updated: 2026-03-22
using Ephemeris;
using Ephemeris.Chronology;
using Ephemeris.Phenomenology;
using Ephemeris.UI.Models;

namespace Ephemeris.UI.Services;

/// <summary>
/// High-level service that wraps the Ephemeris core library to compute a combined
/// snapshot of celestial data (including the biblical calendar) for a single instant
/// and observer location.
/// </summary>
/// <remarks>
/// This service is the bridge between the MVVM layer and the core calculation engine.
/// All simulation overrides (freeze, reverse, extend daylight) should be applied
/// at this layer — they must not modify the core library.
/// <para>
/// Implements <see cref="ISingletonService"/> so that Scrutor assembly scanning
/// automatically registers it as a singleton via
/// <c>services.AddEphemerisServices()</c>.
/// </para>
/// </remarks>
public sealed class CelestialResearchService : ISingletonService
{
    /// <summary>
    /// Asynchronously computes all celestial data for the given observer and time.
    /// The calculation runs on a thread-pool thread so the UI thread is not blocked.
    /// </summary>
    /// <param name="timeUtc">The UTC date and time of the observation.</param>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>
    /// A <see cref="CelestialResearchData"/> snapshot for the requested moment,
    /// including the approximate biblical calendar date.
    /// </returns>
    /// <remarks>
    /// Biblical date computation calls <see cref="BiblicalCalendarHelper.GetBiblicalDate"/>
    /// with the Julian Day derived from <paramref name="timeUtc"/> via
    /// <see cref="TimeZoneUtils.ToJulianDay"/>.
    /// </remarks>
    public Task<CelestialResearchData> GetDataAsync(
        DateTime timeUtc,
        double longitude,
        double latitude,
        CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            double jd = TimeZoneUtils.ToJulianDay(timeUtc);
            var biblicalDate = BiblicalCalendarHelper.GetBiblicalDate(jd, longitude, latitude);

            return new CelestialResearchData
            {
                TimeUtc      = timeUtc,
                Longitude    = longitude,
                Latitude     = latitude,
                BiblicalDate = biblicalDate,
            };
        }, cancellationToken);
}
