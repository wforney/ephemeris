// Updated: 2026-03-22
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Ephemeris.Chronology;
using Ephemeris.UI.Messages;
using Ephemeris.UI.Models;
using Ephemeris.UI.Services;

namespace Ephemeris.UI.ViewModels;

/// <summary>
/// Full research workspace view-model for the Ephemeris Research App.
/// Manages observer position, simulation time, playback, scenario loading,
/// and on-demand retrieval of celestial data from <see cref="ICelestialResearchService"/>.
/// </summary>
/// <remarks>
/// Extends <see cref="ObservableRecipient"/> to participate in the
/// <see cref="WeakReferenceMessenger"/> bus.
/// Broadcasts <see cref="SimTimeChangedMessage"/> and <see cref="ObserverChangedMessage"/>
/// whenever the corresponding properties change, and auto-refreshes
/// <see cref="CelestialData"/> with a 300 ms debounce.
/// </remarks>
public sealed partial class WorkspaceViewModel : ObservableRecipient
{
    private readonly ICelestialResearchService _service;
    private CancellationTokenSource _debounceCts = new();

    // ── Observer & time ──────────────────────────────────────────────────────

    /// <summary>Observer longitude in degrees (East positive).</summary>
    [ObservableProperty]
    private double _longitude;

    /// <summary>Observer latitude in degrees (North positive).</summary>
    [ObservableProperty]
    private double _latitude;

    /// <summary>Current simulated UTC time. Used only when not in historical mode.</summary>
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

    /// <summary>Whether the time animation is currently playing.</summary>
    [ObservableProperty]
    private bool _playing;

    /// <summary><see langword="true"/> when a BCE/historical epoch is active.</summary>
    public bool IsHistoricalMode => HistoricalDate.HasValue;

    /// <summary>Human-readable date string for display in the toolbar.</summary>
    public string DisplayDate =>
        IsHistoricalMode
            ? HistoricalDate!.Value.ToHistoricalString()
            : SimTime.ToString("yyyy-MM-dd HH:mm UTC");

    /// <summary>
    /// Julian Day for the current epoch. Use this when calling Ephemeris core methods.
    /// </summary>
    public double CurrentJulianDay =>
        IsHistoricalMode
            ? HistoricalDate!.Value.ToJulianDay()
            : TimeZoneUtils.ToJulianDay(SimTime);

    // ── Workspace state ───────────────────────────────────────────────────────

    /// <summary>
    /// Animation playback speed multiplier (1 = real-time, 60 = 1 min/sec, 86400 = 1 day/sec).
    /// </summary>
    [ObservableProperty]
    private double _playbackSpeed = 60.0;

    /// <summary>
    /// Latest celestial data snapshot retrieved by <see cref="LoadDataCommand"/>,
    /// or <see langword="null"/> before the first load.
    /// </summary>
    [ObservableProperty]
    private CelestialResearchData? _celestialData;

    /// <summary>Human-readable status, e.g. "Loading…" or "Jerusalem, 701 BCE".</summary>
    [ObservableProperty]
    private string _statusMessage = "Ready";

    /// <summary>
    /// <see langword="true"/> while <see cref="LoadDataCommand"/> is executing.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Name of the currently active scenario preset, or <see langword="null"/> if none.
    /// </summary>
    [ObservableProperty]
    private string? _activeScenarioName;

    /// <summary>Active simulation overrides (freeze motion, altitude offset, extended daylight).</summary>
    public SimulationOverride Simulation { get; } = new();

    // ── Constructor ────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the workspace with an observer location and an optional start time.
    /// </summary>
    /// <param name="service">The celestial research service used by <see cref="LoadDataCommand"/>.</param>
    /// <param name="longitude">Initial observer longitude in degrees (East positive). Defaults to 0.</param>
    /// <param name="latitude">Initial observer latitude in degrees (North positive). Defaults to 51.5 (London).</param>
    /// <param name="initialTime">
    /// Initial simulation time (UTC).  If <see cref="DateTime.MinValue"/> or <c>default</c>,
    /// the current UTC clock is used.
    /// </param>
    public WorkspaceViewModel(
        ICelestialResearchService service,
        double longitude = 0.0,
        double latitude  = 51.5,
        DateTime initialTime = default)
    {
        _service   = service ?? throw new ArgumentNullException(nameof(service));
        _longitude = longitude;
        _latitude  = latitude;
        _simTime   = initialTime == default ? DateTime.UtcNow : initialTime;
        IsActive   = true; // register with WeakReferenceMessenger
    }

    // ── Property-change hooks ──────────────────────────────────────────────────

    partial void OnSimTimeChanged(DateTime value)
    {
        Messenger.Send(new SimTimeChangedMessage(value));
        _ = DebounceRefreshAsync();
    }

    partial void OnLongitudeChanged(double value)
    {
        Messenger.Send(new ObserverChangedMessage(new ObserverLocation(value, Latitude)));
        _ = DebounceRefreshAsync();
    }

    partial void OnLatitudeChanged(double value)
    {
        Messenger.Send(new ObserverChangedMessage(new ObserverLocation(Longitude, value)));
        _ = DebounceRefreshAsync();
    }

    // ── Commands ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Loads celestial data for the current <see cref="SimTime"/>, <see cref="Longitude"/>,
    /// and <see cref="Latitude"/> using <see cref="ICelestialResearchService.GetDataAsync"/>.
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task LoadDataAsync(CancellationToken ct)
    {
        IsLoading     = true;
        StatusMessage = "Loading…";
        try
        {
            CelestialData = await _service.GetDataAsync(SimTime, Longitude, Latitude, ct).ConfigureAwait(false);
            StatusMessage = $"{SimTime:yyyy-MM-dd HH:mm} UTC · {Latitude:F2}°N {Longitude:F2}°E";
        }
        catch (OperationCanceledException)
        {
            // Cancelled — do not update status; a fresh request is likely already queued.
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>Toggles the animation play state.</summary>
    [RelayCommand]
    private void PlayPause() => Playing = !Playing;

    /// <summary>Resets the simulated time to the current UTC clock and clears the active scenario.</summary>
    [RelayCommand]
    private void ResetToNow()
    {
        ActiveScenarioName = null;
        HistoricalDate = null;
        SimTime = DateTime.UtcNow;
    }

    /// <summary>Advances the simulation time by <paramref name="step"/>.</summary>
    /// <param name="step">The amount of time to add to <see cref="SimTime"/>.</param>
    [RelayCommand]
    private void StepForward(TimeSpan step) => SimTime = SimTime.Add(step);

    /// <summary>Rewinds the simulation time by <paramref name="step"/>.</summary>
    /// <param name="step">The amount of time to subtract from <see cref="SimTime"/>.</param>
    [RelayCommand]
    private void StepBack(TimeSpan step) => SimTime = SimTime.Add(-step);

    /// <summary>
    /// Applies a <see cref="ScenarioModel"/> preset: sets <see cref="SimTime"/>,
    /// <see cref="Longitude"/>, <see cref="Latitude"/>, and <see cref="ActiveScenarioName"/>.
    /// </summary>
    /// <param name="scenario">The preset to load.</param>
    [RelayCommand]
    private void LoadScenario(ScenarioModel scenario)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        Longitude          = scenario.Longitude;
        Latitude           = scenario.Latitude;
        ActiveScenarioName = scenario.Name;
        StatusMessage      = $"{scenario.LocationName} — {scenario.Name}";

        if (scenario.HistoricalDate.HasValue)
        {
            HistoricalDate = scenario.HistoricalDate;
        }
        else
        {
            HistoricalDate = null;
            SimTime        = scenario.SuggestedUtcTime;
        }
    }

    // ── Auto-refresh debounce ─────────────────────────────────────────────────

    /// <summary>
    /// Cancels any pending refresh, waits 300 ms, then calls <see cref="LoadDataAsync"/>
    /// with a fresh <see cref="CancellationToken"/> so rapid property changes result
    /// in only one network/calculation round-trip.
    /// </summary>
    private async Task DebounceRefreshAsync()
    {
        await _debounceCts.CancelAsync().ConfigureAwait(false);
        _debounceCts = new CancellationTokenSource();
        CancellationToken token = _debounceCts.Token;
        try
        {
            await Task.Delay(300, token).ConfigureAwait(false);
            await LoadDataAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // A newer property change arrived — the next debounce cycle will handle it.
        }
    }

    /// <summary>
    /// Advances the simulation time by one animation tick (10 minutes).
    /// Called by the animation timer when <see cref="Playing"/> is <see langword="true"/>.
    /// </summary>
    public void AdvanceTick() => SimTime = SimTime.AddMinutes(10);
}
