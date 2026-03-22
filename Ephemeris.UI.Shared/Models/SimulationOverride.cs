// Updated: 2026-03-22
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ephemeris.UI.Models;

/// <summary>
/// Holds optional simulation overrides that are applied post-calculation in the
/// <see cref="Ephemeris.UI.Services.CelestialResearchService"/> layer, without modifying
/// core ephemeris calculations.
/// </summary>
/// <remarks>
/// All overrides default to inactive / zero so that the normal computation path is taken
/// unless the researcher explicitly enables them.  Bind UI controls directly to these
/// observable properties.
/// </remarks>
public partial class SimulationOverride : ObservableObject
{
    /// <summary>Whether any simulation overrides are currently active.</summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// When <see langword="true"/>, celestial body positions are frozen at the
    /// time the override was activated (simulating stopped heavenly motion).
    /// </summary>
    [ObservableProperty]
    private bool _motionFrozen;

    /// <summary>
    /// Additional altitude offset applied to the Sun's computed altitude, in degrees.
    /// Positive values raise the Sun above its computed position; negative values lower it.
    /// Used to model events such as Hezekiah's sundial reversal.
    /// </summary>
    [ObservableProperty]
    private double _sunAltitudeOffsetDegrees;

    /// <summary>
    /// Number of hours by which daylight is extended beyond the computed sunset.
    /// Zero means normal day length.  Used to model Joshua's Long Day.
    /// </summary>
    [ObservableProperty]
    private double _extendDaylightHours;
}
