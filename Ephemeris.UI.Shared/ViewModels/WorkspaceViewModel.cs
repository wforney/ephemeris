// Updated: 2026-03-22
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Ephemeris.UI.Messages;
using Ephemeris.UI.Models;

namespace Ephemeris.UI.ViewModels;

/// <summary>
/// Full-featured view-model for the research workspace.
/// Extends <see cref="SkyViewModel"/>'s properties with playback speed, scenario
/// management, status feedback, and celestial data access.
/// </summary>
/// <remarks>
/// Inherits <see cref="ObservableRecipient"/> via the CommunityToolkit.Mvvm base
/// and participates in the <see cref="WeakReferenceMessenger"/> bus just like
/// <see cref="SkyViewModel"/>.
/// </remarks>
public sealed partial class WorkspaceViewModel : ObservableRecipient
{
    // ─────────────────────────────────────────────────────────────────────
    // Observer and time (mirrors SkyViewModel)
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Observer longitude in degrees (east positive).</summary>
    [ObservableProperty] private double _longitude;

    /// <summary>Observer latitude in degrees (north positive).</summary>
    [ObservableProperty] private double _latitude;

    /// <summary>Current simulated UTC time.</summary>
    [ObservableProperty] private DateTime _simTime;

    /// <summary>Whether the time animation is currently playing.</summary>
    [ObservableProperty] private bool _playing;

    // ─────────────────────────────────────────────────────────────────────
    // Workspace-specific state
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Playback speed multiplier relative to real time.
    /// A value of 1.0 advances simulated time at roughly the same rate as
    /// wall-clock time; higher values fast-forward the sky.
    /// </summary>
    [ObservableProperty] private double _playbackSpeed = 1.0;

    /// <summary>Human-readable status message displayed in the UI status bar.</summary>
    [ObservableProperty] private string _statusMessage = "Ready";

    /// <summary>Whether a long-running background calculation is in progress.</summary>
    [ObservableProperty] private bool _isLoading;

    /// <summary>Display name of the currently loaded scenario, or <see langword="null"/>.</summary>
    [ObservableProperty] private string? _activeScenarioName;

    /// <summary>Latest computed celestial body positions for the current <see cref="SimTime"/>.</summary>
    [ObservableProperty] private IReadOnlyList<EphemerisRecord> _celestialData = [];

    // ─────────────────────────────────────────────────────────────────────
    // Constructor
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the workspace view-model with optional starting coordinates and time.
    /// </summary>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="initialTime">Initial simulated UTC time; defaults to <see cref="DateTime.UtcNow"/>.</param>
    public WorkspaceViewModel(
        double longitude = 0.0,
        double latitude = 51.5,
        DateTime initialTime = default)
    {
        _longitude = longitude;
        _latitude  = latitude;
        _simTime   = initialTime == default ? DateTime.UtcNow : initialTime;
        IsActive   = true; // register with WeakReferenceMessenger
    }

    // ─────────────────────────────────────────────────────────────────────
    // Messenger hooks (mirrors SkyViewModel behaviour)
    // ─────────────────────────────────────────────────────────────────────

    partial void OnSimTimeChanged(DateTime value) =>
        Messenger.Send(new SimTimeChangedMessage(value));

    partial void OnLongitudeChanged(double value) =>
        Messenger.Send(new ObserverChangedMessage(new ObserverLocation(value, Latitude)));

    partial void OnLatitudeChanged(double value) =>
        Messenger.Send(new ObserverChangedMessage(new ObserverLocation(Longitude, value)));

    // ─────────────────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Toggles the animation play state.</summary>
    [RelayCommand]
    private void PlayPause() => Playing = !Playing;

    /// <summary>Advances the simulation by one day.</summary>
    [RelayCommand]
    private void StepForward() => SimTime = SimTime.AddDays(1);

    /// <summary>Rewinds the simulation by one day.</summary>
    [RelayCommand]
    private void StepBack() => SimTime = SimTime.AddDays(-1);

    /// <summary>
    /// Loads a predefined scriptural event scenario into the workspace.
    /// </summary>
    /// <param name="scenario">The scenario to load.</param>
    /// <remarks>
    /// Updates observer longitude, latitude, and (if the scenario provides a valid
    /// suggested time) advances <see cref="SimTime"/> to the scenario's
    /// <see cref="ScenarioModel.SuggestedUtcTime"/>.
    /// Sets <see cref="ActiveScenarioName"/> and <see cref="StatusMessage"/>.
    /// </remarks>
    [RelayCommand]
    private void LoadScenario(ScenarioModel scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        Longitude          = scenario.Longitude;
        Latitude           = scenario.Latitude;
        ActiveScenarioName = scenario.Name;
        StatusMessage      = $"Loaded: {scenario.Name} — {scenario.LocationName}";

        if (scenario.SuggestedUtcTime != DateTime.MinValue)
            SimTime = scenario.SuggestedUtcTime;
    }

    /// <summary>Advances the simulation time by the given delta.</summary>
    /// <param name="delta">Amount of simulated time to add.</param>
    /// <remarks>
    /// Called by <see cref="Services.PlaybackEngine"/> on each timer interval.
    /// The <paramref name="delta"/> is computed as
    /// <c>tickInterval × PlaybackSpeed / 1000</c> by the engine.
    /// </remarks>
    public void AdvanceTick(TimeSpan delta) => SimTime = SimTime.Add(delta);
}
