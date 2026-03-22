// Updated: 2026-03-22
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ephemeris.UI.Models;
using Ephemeris.UI.Services;

namespace Ephemeris.UI.ViewModels;

/// <summary>
/// Observable view-model representing a single research workspace — an observer
/// location combined with a simulated time, optional playback, and an optional
/// <see cref="SimulationOverride"/> that modifies celestial motion post-calculation.
/// </summary>
/// <remarks>
/// Exposes a <see cref="SkyView"/> property (a <see cref="SkyViewModel"/>) that is
/// kept in sync with this view-model's <see cref="SimTime"/>, <see cref="Longitude"/>,
/// and <see cref="Latitude"/>.  <c>SkyGlControl</c> binds directly to
/// <see cref="SkyView"/> for OpenGL rendering.
/// </remarks>
public sealed partial class WorkspaceViewModel : ObservableRecipient
{
    /// <summary>Minutes advanced per animation tick at 1× playback speed.</summary>
    private const double TickMinutes = 10.0;

    /// <summary>Current simulated UTC time.</summary>
    [ObservableProperty] private DateTime _simTime;

    /// <summary>Observer longitude in degrees (east positive).</summary>
    [ObservableProperty] private double _longitude;

    /// <summary>Observer latitude in degrees (north positive).</summary>
    [ObservableProperty] private double _latitude;

    /// <summary>Whether time animation is currently playing.</summary>
    [ObservableProperty] private bool _playing;

    /// <summary>Multiplier applied to each animation tick (1.0 = real-time rate).</summary>
    [ObservableProperty] private double _playbackSpeed = 1.0;

    /// <summary>
    /// Latest computed celestial data for this workspace, or <see langword="null"/> before
    /// the first calculation completes.
    /// </summary>
    [ObservableProperty] private CelestialResearchData? _celestialData;

    /// <summary>
    /// Simulation overrides applied to the celestial calculations for this workspace.
    /// Defaults to an inactive (all-zero) override.
    /// </summary>
    public SimulationOverride Simulation { get; } = new();

    /// <summary>
    /// Underlying <see cref="SkyViewModel"/> used by <c>SkyGlControl</c> for rendering.
    /// Kept in sync with <see cref="SimTime"/>, <see cref="Longitude"/>, and
    /// <see cref="Latitude"/> via partial-method hooks.
    /// </summary>
    public SkyViewModel SkyView { get; }

    /// <summary>
    /// Initialises the workspace with observer coordinates and an optional start time.
    /// </summary>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="initialTime">
    /// Initial UTC simulation time; defaults to <see cref="DateTime.UtcNow"/> when not supplied.
    /// </param>
    public WorkspaceViewModel(double longitude = 0.0, double latitude = 51.5, DateTime initialTime = default)
    {
        _longitude = longitude;
        _latitude  = latitude;
        _simTime   = initialTime == default ? DateTime.UtcNow : initialTime;
        SkyView    = new SkyViewModel(_longitude, _latitude, _simTime);
        IsActive   = true;
    }

    // ── Partial hooks: keep SkyView in sync ──────────────────────────────

    partial void OnSimTimeChanged(DateTime value)  => SkyView.SimTime   = value;
    partial void OnLongitudeChanged(double value)  => SkyView.Longitude = value;
    partial void OnLatitudeChanged(double value)   => SkyView.Latitude  = value;
    partial void OnPlayingChanged(bool value)      => SkyView.Playing   = value;

    // ── Commands ─────────────────────────────────────────────────────────

    /// <summary>Loads a predefined scenario into this workspace.</summary>
    /// <param name="scenario">The scenario to apply.</param>
    [RelayCommand]
    private void LoadScenario(ScenarioModel scenario)
    {
        SimTime   = scenario.HistoricalDate;
        Longitude = scenario.Longitude;
        Latitude  = scenario.Latitude;

        Simulation.Reset();
        if (scenario.Override is { } ov)
        {
            Simulation.IsActive                 = ov.IsActive;
            Simulation.MotionFrozen             = ov.MotionFrozen;
            Simulation.SunAltitudeOffsetDegrees = ov.SunAltitudeOffsetDegrees;
            Simulation.ExtendDaylightHours      = ov.ExtendDaylightHours;
        }
    }

    /// <summary>Advances the simulation by one animation tick (<see cref="TickMinutes"/> minutes × playback speed).</summary>
    public void AdvanceTick()
    {
        if (Playing)
            SimTime = SimTime.AddMinutes(TickMinutes * PlaybackSpeed);
    }
}
