// Updated: 2026-03-22
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ephemeris.UI.Models;

/// <summary>
/// Holds user-controlled overrides that modify celestial motion in the simulation panel.
/// All properties default to zero / false so a fresh instance represents "no override".
/// </summary>
/// <remarks>
/// The simulation layer in <c>CelestialResearchService</c> reads these values and applies
/// them post-calculation; the core Ephemeris library is never modified.
/// </remarks>
public sealed partial class SimulationOverride : ObservableObject
{
    /// <summary>Whether any override is currently active.</summary>
    [ObservableProperty] private bool _isActive;

    /// <summary>When <see langword="true"/> celestial bodies are frozen at the moment the override was activated.</summary>
    [ObservableProperty] private bool _motionFrozen;

    /// <summary>Signed offset in degrees added to the Sun's altitude (positive = higher in sky).</summary>
    [ObservableProperty] private double _sunAltitudeOffsetDegrees;

    /// <summary>Additional hours of daylight appended after the computed sunset time.</summary>
    [ObservableProperty] private double _extendDaylightHours;

    /// <summary>
    /// Resets all overrides to their default (inactive) state.
    /// </summary>
    public void Reset()
    {
        IsActive                 = false;
        MotionFrozen             = false;
        SunAltitudeOffsetDegrees = 0.0;
        ExtendDaylightHours      = 0.0;
    }
}
