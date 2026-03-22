// Updated: 2026-03-22
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ephemeris.UI.Models;

namespace Ephemeris.UI.ViewModels;

/// <summary>
/// Manages two <see cref="SkyViewModel"/> instances — <see cref="Baseline"/> (unmodified
/// celestial motion) and <see cref="Simulation"/> (same time/location but with
/// <see cref="SimOverride"/> applied) — and keeps them in sync.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Baseline"/> is a <em>clone</em> of the source view-model, not the source
/// itself.  This ensures that the two <see cref="Ephemeris.UI.Avalonia.Controls.SkyGlControl"/>
/// instances each bind to their own <see cref="SkyViewModel"/> and only one timer drives
/// each view — avoiding doubled time progression when the parent workspace window's control
/// is already animating the same source view-model.
/// </para>
/// <para>
/// When <see cref="SyncTime"/> is <see langword="true"/> (the default), any change to the
/// source view-model's <see cref="SkyViewModel.SimTime"/>, <see cref="SkyViewModel.Longitude"/>,
/// or <see cref="SkyViewModel.Latitude"/> is mirrored to <see cref="Baseline"/>, and changes
/// to <see cref="Baseline"/> are in turn copied to <see cref="Simulation"/>.
/// </para>
/// <para>
/// All subscriptions are managed by <see cref="ObservableRecipient.OnActivated"/> /
/// <see cref="ObservableRecipient.OnDeactivated"/> to prevent leaking the view-model
/// after the comparison window is closed.  Set <c>IsActive = false</c> on window close
/// to detach all handlers.
/// </para>
/// </remarks>
public sealed partial class ComparisonViewModel : ObservableRecipient
{
    private readonly SkyViewModel _source;

    /// <summary>The unmodified baseline sky view-model (cloned from the source).</summary>
    public SkyViewModel Baseline { get; }

    /// <summary>The simulation sky view-model — same starting state as <see cref="Baseline"/> but
    /// with <see cref="SimOverride"/> applied by the bound
    /// <see cref="Ephemeris.UI.Avalonia.Controls.SkyGlControl"/>.</summary>
    public SkyViewModel Simulation { get; }

    /// <summary>Active simulation overrides for the <see cref="Simulation"/> sky panel.</summary>
    public SimulationOverride SimOverride { get; } = new();

    /// <summary>
    /// When <see langword="true"/>, changes to the source view-model time and location are
    /// automatically propagated to <see cref="Baseline"/> and on to <see cref="Simulation"/>.
    /// </summary>
    [ObservableProperty] private bool _syncTime = true;

    /// <summary>
    /// Initialises the comparison view-model from an existing <see cref="SkyViewModel"/>.
    /// Both <see cref="Baseline"/> and <see cref="Simulation"/> are seeded with the source's
    /// current state but are independent instances so their animation timers are independent.
    /// </summary>
    /// <param name="source">Source sky view-model whose time and location seed both panels.</param>
    public ComparisonViewModel(SkyViewModel source)
    {
        _source    = source;
        Baseline   = new SkyViewModel(source.Longitude, source.Latitude, source.SimTime);
        Simulation = new SkyViewModel(source.Longitude, source.Latitude, source.SimTime);
        IsActive   = true;
    }

    // ── Lifecycle ────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void OnActivated()
    {
        base.OnActivated();
        _source.PropertyChanged   += OnSourcePropertyChanged;
        Baseline.PropertyChanged  += OnBaselinePropertyChanged;
    }

    /// <inheritdoc/>
    protected override void OnDeactivated()
    {
        _source.PropertyChanged   -= OnSourcePropertyChanged;
        Baseline.PropertyChanged  -= OnBaselinePropertyChanged;
        base.OnDeactivated();
    }

    // ── Sync handlers ─────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors time and location changes from the original source view-model to
    /// <see cref="Baseline"/> so the comparison window stays in sync with the
    /// parent workspace window.
    /// </summary>
    private void OnSourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SkyViewModel.SimTime):
                Baseline.SimTime = _source.SimTime;
                break;
            case nameof(SkyViewModel.Longitude):
                Baseline.Longitude = _source.Longitude;
                break;
            case nameof(SkyViewModel.Latitude):
                Baseline.Latitude = _source.Latitude;
                break;
        }
    }

    /// <summary>
    /// When <see cref="SyncTime"/> is <see langword="true"/>, propagates time and location
    /// changes from <see cref="Baseline"/> to <see cref="Simulation"/>.
    /// </summary>
    private void OnBaselinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!SyncTime) return;

        switch (e.PropertyName)
        {
            case nameof(SkyViewModel.SimTime):
                Simulation.SimTime = Baseline.SimTime;
                break;
            case nameof(SkyViewModel.Longitude):
                Simulation.Longitude = Baseline.Longitude;
                break;
            case nameof(SkyViewModel.Latitude):
                Simulation.Latitude = Baseline.Latitude;
                break;
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>Clears all simulation overrides, reverting the simulation sky to normal motion.</summary>
    [RelayCommand]
    private void ResetSimulation() => SimOverride.Reset();

    /// <summary>
    /// Forces a one-shot copy of <see cref="Baseline"/> time and location to
    /// <see cref="Simulation"/>, regardless of the current <see cref="SyncTime"/> value.
    /// </summary>
    [RelayCommand]
    private void SyncNow()
    {
        Simulation.SimTime   = Baseline.SimTime;
        Simulation.Longitude = Baseline.Longitude;
        Simulation.Latitude  = Baseline.Latitude;
    }
}
