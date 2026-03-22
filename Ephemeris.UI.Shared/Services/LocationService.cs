// Updated: 2026-03-22
using System.Text.Json;

namespace Ephemeris.UI.Services;

/// <summary>
/// Provides the device's approximate geographic location via IP geolocation.
/// The result is fetched once on first access and cached for the process lifetime.
/// Falls back silently to <see langword="null"/> on network error or timeout.
/// </summary>
public static class LocationService
{
    // Lazily starts one HTTP request and caches the result for all callers.
    private static readonly Lazy<Task<(double Latitude, double Longitude)?>> _cached =
        new(() => FetchLocationAsync(), LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Warms up the geolocation cache in the background.
    /// Call once at application startup so the result is ready before the first sky view opens.
    /// </summary>
    public static void Prefetch() => _ = _cached.Value;

    /// <summary>
    /// Returns the device's approximate latitude and longitude from IP geolocation,
    /// or <see langword="null"/> if the location could not be determined.
    /// Subsequent calls return the same cached <see cref="Task{TResult}"/>.
    /// </summary>
    public static Task<(double Latitude, double Longitude)?> GetLocationAsync() => _cached.Value;

    private static async Task<(double Latitude, double Longitude)?> FetchLocationAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
            using var http = new HttpClient();
            var json = await http.GetStringAsync("https://ipinfo.io/json", cts.Token)
                                 .ConfigureAwait(false);

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("loc", out var locProp))
                return null;

            var loc = locProp.GetString();
            if (string.IsNullOrEmpty(loc))
                return null;

            var comma = loc.IndexOf(',');
            if (comma < 0)
                return null;

            if (!double.TryParse(loc.AsSpan(0, comma),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double lat))
                return null;

            if (!double.TryParse(loc.AsSpan(comma + 1),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double lon))
                return null;

            return (lat, lon);
        }
        catch
        {
            return null;
        }
    }
}
