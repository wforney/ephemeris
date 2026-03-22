// Updated: 2026-03-22
using Ephemeris.Phenomenology;

namespace Ephemeris.UI.Services;

/// <summary>
/// Default implementation of <see cref="ICelestialResearchService"/> that delegates
/// event detection to the core <see cref="CelestialEventDetector"/>.
/// </summary>
/// <remarks>
/// All computation runs on a thread-pool thread via <c>Task.Run</c> so that calls
/// from the UI thread remain non-blocking.
/// </remarks>
public sealed class CelestialResearchService : ICelestialResearchService
{
    /// <inheritdoc/>
    /// <remarks>
    /// Delegates to <see cref="CelestialEventDetector.GetNext"/> on a background thread.
    /// Throws <see cref="OperationCanceledException"/> if <paramref name="ct"/> is signalled.
    /// </remarks>
    public Task<IReadOnlyList<CelestialEventDetector.CelestialEvent>> GetUpcomingEventsAsync(
        DateTime fromUtc, int count = 5, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.Run(() => CelestialEventDetector.GetNext(fromUtc, count), ct);
    }
}
