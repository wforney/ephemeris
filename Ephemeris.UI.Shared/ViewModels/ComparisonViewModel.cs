// Updated: 2026-03-22
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ephemeris.UI.ViewModels;

/// <summary>
/// Manages two <see cref="WorkspaceViewModel"/> instances — <see cref="Baseline"/> (unmodified
/// celestial motion) and <see cref="Simulation"/> (same time/location with
/// <see cref="WorkspaceViewModel.Simulation"/> overrides applied) — and keeps them in sync.
/// </summary>
/// <remarks>
/// When <see cref="SyncTime"/> is <see langword="true"/> (the default), any change to
/// <see cref="Baseline"/>.<see cref="WorkspaceViewModel.SimTime"/> is automatically copied
/// to <see cref="Simulation"/>.<see cref="WorkspaceViewModel.SimTime"/> so both sky views
/// always show the same moment.  Location is also kept in sync.
/// <para>
/// <see cref="ResetSimulationCommand"/> clears all overrides on the simulation workspace.
/// <see cref="SyncNowCommand"/> performs a one-shot copy of Baseline time and location
/// to Simulation regardless of the current <see cref="SyncTime"/> setting.
/// </para>
/// </remarks>
public sealed partial class ComparisonViewModel : ObservableRecipient
{
    /// <summary>The unmodified baseline workspace.</summary>
    public WorkspaceViewModel Baseline { get; }

    /// <summary>The workspace with <see cref="WorkspaceViewModel.Simulation"/> overrides applied.</summary>
    public WorkspaceViewModel Simulation { get; }

    /// <summary>
    /// When <see langword="true"/>, changes to <see cref="Baseline"/> time and location are
    /// automatically propagated to <see cref="Simulation"/>.
    /// </summary>
    [ObservableProperty] private bool _syncTime = true;

    /// <summary>
    /// Initialises the comparison view-model from an existing workspace.
    /// The provided workspace becomes <see cref="Baseline"/>; a copy of its current
    /// state is used to create a fresh <see cref="Simulation"/> workspace.
    /// </summary>
    /// <param name="source">Source workspace whose time and location seed both sides.</param>
    public ComparisonViewModel(WorkspaceViewModel source)
    {
        Baseline   = source;
        Simulation = new WorkspaceViewModel(source.Longitude, source.Latitude, source.SimTime);

        Baseline.PropertyChanged += OnBaselinePropertyChanged;
        IsActive = true;
    }

    // ── Sync handler ─────────────────────────────────────────────────────

    private void OnBaselinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!SyncTime) return;

        switch (e.PropertyName)
        {
            case nameof(WorkspaceViewModel.SimTime):
                Simulation.SimTime = Baseline.SimTime;
                break;
            case nameof(WorkspaceViewModel.Longitude):
                Simulation.Longitude = Baseline.Longitude;
                break;
            case nameof(WorkspaceViewModel.Latitude):
                Simulation.Latitude = Baseline.Latitude;
                break;
        }
    }

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>Clears all simulation overrides, reverting the simulation sky to normal motion.</summary>
    [RelayCommand]
    private void ResetSimulation() => Simulation.Simulation.Reset();

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
