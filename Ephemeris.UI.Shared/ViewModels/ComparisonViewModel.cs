// Updated: 2026-03-22
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ephemeris.UI.Models;

namespace Ephemeris.UI.ViewModels;

/// <summary>
/// Manages two <see cref="SkyViewModel"/> instances — <see cref="Baseline"/> (unmodified
/// celestial motion) and <see cref="Simulation"/> (same time/location but with
/// <see cref="SimulationOverride"/> applied) — and keeps them in sync.
/// </summary>
/// <remarks>
/// When <see cref="SyncTime"/> is <see langword="true"/> (the default), any change to
/// <see cref="Baseline"/>.<see cref="SkyViewModel.SimTime"/>,
/// <see cref="SkyViewModel.Longitude"/>, or <see cref="SkyViewModel.Latitude"/>
/// is automatically copied to <see cref="Simulation"/> so both sky views always show
/// the same moment and location.
/// <para>
/// <see cref="ResetSimulationCommand"/> clears all <see cref="SimulationOverride"/> values.
/// <see cref="SyncNowCommand"/> performs a one-shot copy of Baseline state to Simulation
/// regardless of the current <see cref="SyncTime"/> setting.
/// </para>
/// </remarks>
public sealed partial class ComparisonViewModel : ObservableRecipient
{
    /// <summary>The unmodified baseline sky view-model (normal celestial motion).</summary>
    public SkyViewModel Baseline { get; }

    /// <summary>The simulation sky view-model — same sky but with <see cref="SimOverride"/> applied.</summary>
    public SkyViewModel Simulation { get; }

    /// <summary>Active simulation overrides for the <see cref="Simulation"/> sky panel.</summary>
    public SimulationOverride SimOverride { get; } = new();

    /// <summary>
    /// When <see langword="true"/>, changes to <see cref="Baseline"/> time and location are
    /// automatically propagated to <see cref="Simulation"/>.
    /// </summary>
    [ObservableProperty] private bool _syncTime = true;

    /// <summary>
    /// Initialises the comparison view-model from an existing <see cref="SkyViewModel"/>.
    /// The provided view-model becomes <see cref="Baseline"/>; a copy of its current state
    /// seeds the independent <see cref="Simulation"/> view-model.
    /// </summary>
    /// <param name="source">Source sky view-model whose time and location seed both panels.</param>
    public ComparisonViewModel(SkyViewModel source)
    {
        Baseline   = source;
        Simulation = new SkyViewModel(source.Longitude, source.Latitude, source.SimTime);

        Baseline.PropertyChanged += OnBaselinePropertyChanged;
        IsActive = true;
    }

    // ── Sync handler ─────────────────────────────────────────────────────

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
