// Updated: 2026-03-22
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Ephemeris.Phenomenology;
using Ephemeris.UI.Messages;
using Ephemeris.UI.Services;

namespace Ephemeris.UI;

/// <summary>
/// Observable view-model backing the sky view on all platforms.
/// Holds all user-configurable state: observer position, simulated time,
/// camera orientation, and animation play state.
/// </summary>
/// <remarks>
/// Extends <see cref="ObservableRecipient"/> to participate in the
/// <see cref="CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger"/> bus.
/// Sends <see cref="SimTimeChangedMessage"/> and <see cref="ObserverChangedMessage"/>
/// so decoupled recipients can track state across sessions without holding a direct reference.
/// </remarks>
public sealed partial class SkyViewModel : ObservableRecipient
{
    /// <summary>Observer longitude in degrees (east positive).</summary>
    [ObservableProperty] private double _longitude;

    /// <summary>Observer latitude in degrees (north positive).</summary>
    [ObservableProperty] private double _latitude;

    /// <summary>Current simulated UTC time.</summary>
    [ObservableProperty] private DateTime _simTime;

    /// <summary>Camera yaw — rotation around the vertical axis, in degrees.</summary>
    [ObservableProperty] private float _yaw;

    /// <summary>Camera pitch — tilt from horizon in degrees (0 = horizon, 90 = zenith).</summary>
    [ObservableProperty] private float _pitch;

    /// <summary>Vertical field of view in degrees.</summary>
    [ObservableProperty] private float _fovDeg = 90f;

    /// <summary>Whether the time animation is currently playing.</summary>
    [ObservableProperty] private bool _playing;

    /// <summary>Upcoming notable celestial events, refreshed on demand.</summary>
    [ObservableProperty]
    private IReadOnlyList<CelestialEventDetector.CelestialEvent> _upcomingEvents = [];

    private readonly ICelestialResearchService _researchService;

    /// <summary>
    /// Initialises the view-model with observer coordinates and an optional start time.
    /// </summary>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="initialTime">Initial UTC simulation time (defaults to <see cref="DateTime.UtcNow"/>).</param>
    /// <param name="researchService">
    /// Optional <see cref="ICelestialResearchService"/> for event detection.
    /// Defaults to <see cref="CelestialResearchService"/> when <see langword="null"/>.
    /// </param>
    public SkyViewModel(double longitude = 0.0, double latitude = 51.5, DateTime initialTime = default,
        ICelestialResearchService? researchService = null)
    {
        _longitude = longitude;
        _latitude  = latitude;
        _simTime   = initialTime == default ? DateTime.UtcNow : initialTime;
        _pitch     = 20f;  // slightly above south horizon
        _yaw       = 180f; // facing south
        IsActive   = true; // register with WeakReferenceMessenger
        _researchService = researchService ?? new CelestialResearchService();
    }

    // Partial hooks invoked by source-generated property setters.
    // Camera-only properties (Yaw, Pitch, FovDeg, Playing) are not broadcast —
    // they are purely local UI state of no interest to other components.

    partial void OnSimTimeChanged(DateTime value) =>
        Messenger.Send(new SimTimeChangedMessage(value));

    partial void OnLongitudeChanged(double value) =>
        Messenger.Send(new ObserverChangedMessage(new ObserverLocation(value, Latitude)));

    partial void OnLatitudeChanged(double value) =>
        Messenger.Send(new ObserverChangedMessage(new ObserverLocation(Longitude, value)));

    /// <summary>Toggles the animation play state.</summary>
    [RelayCommand]
    private void PlayPause() => Playing = !Playing;

    /// <summary>Resets the simulated time to the current UTC clock.</summary>
    [RelayCommand]
    private void ResetToNow() => SimTime = DateTime.UtcNow;

    /// <summary>Advances the simulation by one day.</summary>
    [RelayCommand]
    private void StepForward() => SimTime = SimTime.AddDays(1);

    /// <summary>Rewinds the simulation by one day.</summary>
    [RelayCommand]
    private void StepBack() => SimTime = SimTime.AddDays(-1);

    /// <summary>Advances the simulation time by one animation tick (10 minutes).</summary>
    public void AdvanceTick() => SimTime = SimTime.AddMinutes(10);

    /// <summary>Rewinds the simulation time by one animation tick (10 minutes).</summary>
    public void RewindTick() => SimTime = SimTime.AddMinutes(-10);

    /// <summary>
    /// Asynchronously refreshes <see cref="UpcomingEvents"/> from <see cref="SimTime"/>.
    /// </summary>
    /// <param name="count">Maximum number of events to fetch (default 5).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task that completes once <see cref="UpcomingEvents"/> is updated.</returns>
    public async Task RefreshUpcomingEventsAsync(int count = 5, CancellationToken ct = default)
    {
        var events = await _researchService.GetUpcomingEventsAsync(SimTime, count, ct).ConfigureAwait(false);
        UpcomingEvents = events;
    }
}
