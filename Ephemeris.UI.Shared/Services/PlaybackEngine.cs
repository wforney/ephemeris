// Updated: 2026-03-22
using Ephemeris.UI.ViewModels;

namespace Ephemeris.UI.Services;

/// <summary>
/// Timer-driven engine that advances <see cref="WorkspaceViewModel.SimTime"/> at the
/// rate specified by <see cref="WorkspaceViewModel.PlaybackSpeed"/>.
/// </summary>
/// <remarks>
/// <para>
/// The engine fires a <see cref="System.Timers.Timer"/> at ~50 ms intervals.
/// On each tick, when <see cref="WorkspaceViewModel.Playing"/> is <see langword="true"/>,
/// the simulated time is advanced by:
/// <code>
///   ΔT = 50 ms × PlaybackSpeed / 1000   (in simulated seconds)
/// </code>
/// </para>
/// <para>
/// The <see cref="SynchronizationContext"/> from the calling thread (typically the
/// UI thread) is captured in the constructor.  Property mutations are posted back
/// through that context so Avalonia / WinForms data-binding continues to work
/// without cross-thread exceptions.
/// </para>
/// </remarks>
public sealed class PlaybackEngine : IDisposable
{
    private const double TickIntervalMs = 50.0;

    private readonly WorkspaceViewModel _vm;
    private readonly SynchronizationContext? _syncContext;
    private readonly System.Timers.Timer _timer;
    private bool _disposed;

    // ─────────────────────────────────────────────────────────────────────
    // Construction / lifecycle
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the playback engine and binds it to <paramref name="vm"/>.
    /// </summary>
    /// <param name="vm">The workspace view-model whose <see cref="WorkspaceViewModel.SimTime"/> will be advanced.</param>
    /// <remarks>
    /// Captures <see cref="SynchronizationContext.Current"/> at construction time so that
    /// timer callbacks are dispatched to the UI thread.  Always construct on the UI thread.
    /// </remarks>
    public PlaybackEngine(WorkspaceViewModel vm)
    {
        ArgumentNullException.ThrowIfNull(vm);

        _vm          = vm;
        _syncContext = SynchronizationContext.Current;

        _timer            = new System.Timers.Timer(TickIntervalMs);
        _timer.AutoReset  = true;
        _timer.Elapsed   += OnTick;
    }

    /// <summary>Starts the internal timer so that ticks begin advancing simulation time.</summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _timer.Start();
    }

    /// <summary>Stops the internal timer; simulation time freezes.</summary>
    public void Stop()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _timer.Stop();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Elapsed -= OnTick;
        _timer.Dispose();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Timer callback
    // ─────────────────────────────────────────────────────────────────────

    private void OnTick(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (!_vm.Playing) return;

        // Compute how far to advance in simulated seconds.
        // ΔT(sim) = tick_interval_ms × PlaybackSpeed / 1000
        var delta = TimeSpan.FromSeconds(TickIntervalMs * _vm.PlaybackSpeed / 1000.0);

        void Advance() => _vm.AdvanceTick(delta);

        if (_syncContext is not null)
            _syncContext.Post(_ => Advance(), null);
        else
            Advance();
    }
}
