// Updated: 2026-03-22
using Ephemeris.Chronology;
using Ephemeris.Geometry;
using Ephemeris.Heliology;
using Ephemeris.Selenography;

namespace Ephemeris.UI.Services;

/// <summary>
/// Data transfer type returned by <see cref="ICelestialResearchService"/>.
/// Carries the computed horizontal positions for the Sun, Moon, and a formatted display string.
/// </summary>
/// <param name="SunAzimuth">Sun azimuth in degrees [0, 360).</param>
/// <param name="SunAltitude">Sun altitude in degrees [−90, 90].</param>
/// <param name="MoonAzimuth">Moon azimuth in degrees [0, 360).</param>
/// <param name="MoonAltitude">Moon altitude in degrees [−90, 90].</param>
/// <param name="JulianDay">Julian Day of the calculation epoch.</param>
public readonly record struct CelestialResearchData(
    double SunAzimuth,
    double SunAltitude,
    double MoonAzimuth,
    double MoonAltitude,
    double JulianDay);

/// <summary>
/// Service contract for high-level celestial position queries used by research view-models.
/// </summary>
/// <remarks>
/// Two overloads are provided:
/// <list type="bullet">
///   <item>Modern era: pass a <see cref="DateTime"/> (UTC) — uses <c>TimeZoneUtils.ToJulianDay</c>.</item>
///   <item>Historical era: pass a Julian Day directly — works for any era including BC.</item>
/// </list>
/// </remarks>
public interface ICelestialResearchService
{
    /// <summary>
    /// Computes celestial positions for a modern UTC time.
    /// </summary>
    Task<CelestialResearchData> GetDataAsync(
        DateTime utcTime,
        double longitude,
        double latitude,
        CancellationToken ct = default);

    /// <summary>
    /// Computes celestial positions for an arbitrary Julian Day — supports BCE dates.
    /// </summary>
    Task<CelestialResearchData> GetDataForJulianDayAsync(
        double julianDay,
        double longitude,
        double latitude,
        CancellationToken ct = default);
}

/// <summary>
/// Default implementation of <see cref="ICelestialResearchService"/> that delegates
/// directly to the Ephemeris core library.
/// </summary>
/// <remarks>
/// Uses <see cref="SunEphemeris.ApparentEquatorialCoordinates"/> and
/// <see cref="MoonEphemeris.ApparentEquatorialCoordinates"/> with
/// <see cref="ObserverGeometry.EquatorialToHorizontal"/> for the observer conversion.
/// Works for any era because the core algorithms accept Julian Century <c>T</c> which
/// is a continuous value computed from Julian Day.
/// </remarks>
public sealed class CelestialResearchService : ICelestialResearchService
{
    /// <inheritdoc/>
    public Task<CelestialResearchData> GetDataAsync(
        DateTime utcTime,
        double longitude,
        double latitude,
        CancellationToken ct = default)
    {
        double jd = TimeZoneUtils.ToJulianDay(utcTime);
        return GetDataForJulianDayAsync(jd, longitude, latitude, ct);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Algorithm:
    /// <list type="number">
    ///   <item>Compute Julian Century: <c>T = (JD − 2451545.0) / 36525</c>.</item>
    ///   <item>Obtain Sun RA/Dec via <see cref="SunEphemeris.ApparentEquatorialCoordinates"/>.</item>
    ///   <item>Obtain Moon RA/Dec via <see cref="MoonEphemeris.ApparentEquatorialCoordinates"/>.</item>
    ///   <item>Convert to horizontal (Az/Alt) via <see cref="ObserverGeometry.EquatorialToHorizontal"/>.</item>
    /// </list>
    /// Because the entire pipeline is parameterised by <c>T</c> (Julian Century since J2000),
    /// this overload works for any era — including dates thousands of years before Christ.
    /// </remarks>
    public Task<CelestialResearchData> GetDataForJulianDayAsync(
        double julianDay,
        double longitude,
        double latitude,
        CancellationToken ct = default)
    {
        double T = TimeUtils.JulianCentury(julianDay);

        var (sunRA, sunDec, _) = SunEphemeris.ApparentEquatorialCoordinates(T);
        var sunH = ObserverGeometry.EquatorialToHorizontal(sunRA, sunDec, julianDay, longitude, latitude, applyRefraction: false);

        var (moonRA, moonDec, _) = MoonEphemeris.GeocentricEquatorialCoordinates(T);
        var moonH = ObserverGeometry.EquatorialToHorizontal(moonRA, moonDec, julianDay, longitude, latitude, applyRefraction: false);

        var data = new CelestialResearchData(
            sunH.Azimuth, sunH.Altitude,
            moonH.Azimuth, moonH.Altitude,
            julianDay);

        return Task.FromResult(data);
    }
}
