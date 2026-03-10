// Updated: 2026-03-10
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ephemeris.UI;

/// <summary>
/// Observable view-model backing <see cref="SkyViewForm"/>.
/// Holds all user-configurable state: observer position, simulated time,
/// camera orientation, and animation play state.
/// </summary>
public sealed partial class SkyViewModel : ObservableObject
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

    /// <summary>
    /// Initialises the view-model with observer coordinates and an optional start time.
    /// </summary>
    public SkyViewModel(double longitude = 0.0, double latitude = 51.5, DateTime initialTime = default)
    {
        _longitude = longitude;
        _latitude  = latitude;
        _simTime   = initialTime == default ? DateTime.UtcNow : initialTime;
        _pitch     = 20f;  // slightly above south horizon
        _yaw       = 180f; // facing south
    }

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
    internal void AdvanceTick() => SimTime = SimTime.AddMinutes(10);
}
