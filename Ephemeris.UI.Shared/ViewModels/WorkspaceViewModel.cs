// Updated: 2026-03-22
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ephemeris.Chronology;
using Ephemeris.UI.Models;
using Ephemeris.UI.Services;

namespace Ephemeris.UI.ViewModels;

/// <summary>
/// View-model for the Research Workspace window.
/// Extends the basic observer/time state with support for loading named
/// <see cref="ScenarioModel"/> presets and for operating in <em>historical mode</em>
/// where the epoch is a <see cref="ProlepticDate"/> (BCE era) rather than a modern
/// <see cref="DateTime"/>.
/// </summary>
/// <remarks>
/// When <see cref="HistoricalDate"/> is set, <see cref="IsHistoricalMode"/> is
/// <see langword="true"/> and <see cref="DisplayDate"/> returns a historical string
/// (e.g. "701 BCE Aug 01"). All astronomical calculations should use
/// <see cref="CurrentJulianDay"/> as the epoch to ensure correctness for both modern
/// and pre-Common-Era dates.
/// </remarks>
public sealed partial class WorkspaceViewModel : ObservableObject
{
    private readonly ICelestialResearchService _svc;

    // ── Observable properties ─────────────────────────────────────────────

    /// <summary>Observer longitude in degrees (east positive).</summary>
    [ObservableProperty] private double _longitude;

    /// <summary>Observer latitude in degrees (north positive).</summary>
    [ObservableProperty] private double _latitude;

    /// <summary>
    /// Current simulated UTC time. Used only when <see cref="HistoricalDate"/> is
    /// <see langword="null"/> (i.e. not in historical mode).
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayDate), nameof(CurrentJulianDay))]
    private DateTime _simTime;

    /// <summary>
    /// Historical date set when a scenario with a BCE epoch is loaded.
    /// <see langword="null"/> when in modern-era mode.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHistoricalMode), nameof(DisplayDate), nameof(CurrentJulianDay))]
    private ProlepticDate? _historicalDate;

    /// <summary>Currently loaded scenario, or <see langword="null"/> if none.</summary>
    [ObservableProperty] private ScenarioModel? _activeScenario;

    /// <summary>Latest computed research data, or <see langword="null"/> before first calculation.</summary>
    [ObservableProperty] private CelestialResearchData? _researchData;

    // ── Derived properties ────────────────────────────────────────────────

    /// <summary>
    /// <see langword="true"/> when a historical (BCE) epoch is active.
    /// </summary>
    public bool IsHistoricalMode => HistoricalDate.HasValue;

    /// <summary>
    /// Human-readable date string for display in the toolbar.
    /// Returns the historical string when in historical mode, otherwise a UTC string.
    /// </summary>
    public string DisplayDate =>
        IsHistoricalMode
            ? HistoricalDate!.Value.ToHistoricalString()
            : SimTime.ToString("yyyy-MM-dd HH:mm UTC");

    /// <summary>
    /// Julian Day for the current epoch. Use this when calling Ephemeris core methods.
    /// Returns the JD from <see cref="HistoricalDate"/> when in historical mode,
    /// otherwise converts <see cref="SimTime"/> to JD.
    /// </summary>
    public double CurrentJulianDay =>
        IsHistoricalMode
            ? HistoricalDate!.Value.ToJulianDay()
            : Ephemeris.Chronology.TimeZoneUtils.ToJulianDay(SimTime);

    // ── Construction ──────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the workspace view-model.
    /// </summary>
    /// <param name="service">Celestial research service for position calculations.</param>
    /// <param name="longitude">Initial observer longitude (default 0°).</param>
    /// <param name="latitude">Initial observer latitude (default 51.5° N).</param>
    public WorkspaceViewModel(
        ICelestialResearchService service,
        double longitude = 0.0,
        double latitude  = 51.5)
    {
        _svc       = service;
        _longitude = longitude;
        _latitude  = latitude;
        _simTime   = DateTime.UtcNow;
    }

    // ── Commands ──────────────────────────────────────────────────────────

    /// <summary>
    /// Loads a <see cref="ScenarioModel"/> preset into the workspace.
    /// Sets observer coordinates, historical date (if any), and triggers a calculation.
    /// </summary>
    [RelayCommand]
    private async Task LoadScenarioAsync(ScenarioModel scenario)
    {
        ActiveScenario = scenario;
        Longitude      = scenario.Longitude;
        Latitude       = scenario.Latitude;

        if (scenario.HistoricalDate.HasValue)
        {
            HistoricalDate = scenario.HistoricalDate;
        }
        else
        {
            HistoricalDate = null;
            SimTime        = scenario.SuggestedUtcTime;
        }

        await RefreshDataAsync().ConfigureAwait(false);
    }

    /// <summary>Clears the active scenario and returns to modern-era mode.</summary>
    [RelayCommand]
    private void ClearScenario()
    {
        ActiveScenario = null;
        HistoricalDate = null;
        SimTime        = DateTime.UtcNow;
    }

    /// <summary>Advances the epoch by one day (works in both modern and historical mode).</summary>
    [RelayCommand]
    private async Task StepForwardAsync()
    {
        AdvanceByDays(1);
        await RefreshDataAsync().ConfigureAwait(false);
    }

    /// <summary>Rewinds the epoch by one day.</summary>
    [RelayCommand]
    private async Task StepBackAsync()
    {
        AdvanceByDays(-1);
        await RefreshDataAsync().ConfigureAwait(false);
    }

    /// <summary>Recalculates celestial positions for the current epoch.</summary>
    [RelayCommand]
    public async Task RefreshDataAsync()
    {
        var data = await _svc.GetDataForJulianDayAsync(
            CurrentJulianDay, Longitude, Latitude).ConfigureAwait(false);
        ResearchData = data;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Advances the current epoch (historical or modern) by the given number of days.</summary>
    private void AdvanceByDays(double days)
    {
        if (IsHistoricalMode)
        {
            double newJd = HistoricalDate!.Value.ToJulianDay() + days;
            HistoricalDate = ProlepticDate.FromJulianDay(newJd);
        }
        else
        {
            SimTime = SimTime.AddDays(days);
        }
    }
}
