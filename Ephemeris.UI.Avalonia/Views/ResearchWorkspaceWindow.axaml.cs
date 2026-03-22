// Updated: 2026-03-22
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Ephemeris.UI;
using Ephemeris.UI.Avalonia.Controls;
using Ephemeris.UI.Messages;
using Ephemeris.UI.Services;
using Ephemeris.UI.ViewModels;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Full-screen research workspace window for the Ephemeris Research App.
/// Combines a toolbar (date/time, location, scenario), the OpenGL sky canvas
/// (<see cref="SkyGlControl"/>), a data sidebar with celestial data labels,
/// and a time-bar transport control with variable playback speed.
/// </summary>
/// <remarks>
/// <para>
/// Currently uses <see cref="SkyViewModel"/> directly for the sky canvas and
/// playback commands. When <c>WorkspaceViewModel</c> is merged from the
/// companion PR it should be substituted here — search for
/// <c>TODO: replace with WorkspaceViewModel</c>.
/// </para>
/// <para>
/// The data sidebar values (Sun/Moon altitude, rise/set times, next full moon)
/// are stubs that display "—" until a proper <c>CelestialDataService</c> call
/// is wired in by <c>WorkspaceViewModel.CelestialData</c>.
/// </para>
/// </remarks>
public partial class ResearchWorkspaceWindow : Window,
    IRecipient<SimTimeChangedMessage>
{
    // TODO: replace with WorkspaceViewModel (kept for SkyGlControl which requires SkyViewModel)
    private readonly SkyViewModel _vm;
    private readonly WorkspaceViewModel _workspace;
    private readonly SkyGlControl _glControl;
    private readonly SkyChartControl _chartControl;

    // Speed table: seconds of simulation time advanced per real second.
    private static readonly int[] SpeedMultipliers = [1, 60, 3600, 86400];

    private static readonly string[] SpeedLabels =
    [
        "1× (real-time)",
        "1 min/sec",
        "1 hr/sec",
        "1 day/sec",
    ];

    // Animation timer — fires every 100 ms; advances sim time when Playing.
    private readonly DispatcherTimer _animTimer;

    // Prevents feedback loops when programmatically setting the date/time pickers.
    private bool _updatingFromVm;

    // Mouse drag state (for the sky GL control)
    private bool _dragging;
    private Point _lastMouse;

    /// <summary>
    /// Parameterless constructor required by Avalonia's runtime XAML loader.
    /// Defaults to longitude 0° (Greenwich meridian) and latitude 51.5° N (London).
    /// </summary>
    public ResearchWorkspaceWindow() : this(new SkyViewModel()) { }

    /// <summary>
    /// Initialises the research workspace with the supplied view-model.
    /// </summary>
    /// <param name="vm">
    /// The sky view-model providing observer location, simulated time, and
    /// playback commands.
    /// TODO: replace with WorkspaceViewModel when available.
    /// </param>
    public ResearchWorkspaceWindow(SkyViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        _workspace = new WorkspaceViewModel(
            new CelestialResearchService(),
            vm.Longitude,
            vm.Latitude,
            vm.SimTime);

        _glControl = new SkyGlControl(_vm)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };

        // TODO: replace with SkyGlControl once OpenGL rendering is confirmed working on all
        // target platforms.  The 2D chart is the reliable fallback — see SkyGlControl for
        // the full 3D OpenGL implementation that should take over as the primary sky view.
        _chartControl = new SkyChartControl(_vm)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };

        GlHost.Content = _chartControl;

        // Sync toolbar to initial VM state
        SyncToolbarFromVm();
        _vm.PropertyChanged += OnVmPropertyChanged;
        _workspace.PropertyChanged += OnWorkspacePropertyChanged;

        // ── Toolbar wire-up ───────────────────────────────────────────
        DatePicker.SelectedDateChanged += (_, _) => ApplyDateTimePickerValues();
        TimePicker.SelectedTimeChanged += (_, _) => ApplyDateTimePickerValues();

        LonPicker.ValueChanged += (_, _) =>
        {
            if (LonPicker.Value is { } lon) _vm.Longitude = (double)lon;
        };
        LatPicker.ValueChanged += (_, _) =>
        {
            if (LatPicker.Value is { } lat) _vm.Latitude = (double)lat;
        };

        // ── Time-bar wire-up ──────────────────────────────────────────
        StepBackDayBtn.Click  += (_, _) => _vm.StepBackCommand.Execute(null);
        StepBackHourBtn.Click += (_, _) => { _vm.SimTime = _vm.SimTime.AddHours(-1); };
        PlayPauseBtn.Click    += (_, _) => _vm.PlayPauseCommand.Execute(null);
        StepFwdHourBtn.Click  += (_, _) => { _vm.SimTime = _vm.SimTime.AddHours(1); };
        StepFwdDayBtn.Click   += (_, _) => _vm.StepForwardCommand.Execute(null);

        SpeedSlider.ValueChanged += (_, _) => UpdateSpeedLabel();
        UpdateSpeedLabel();
        RefreshSidebarData();
        _workspace.LoadDataCommand.Execute(null);

        // ── Sidebar action buttons ────────────────────────────────────
        ScripturalEventsBtn.Click += OnScripturalEventsClick;
        ComparisonModeBtn.Click   += OnComparisonModeClick;
        NotesBtn.Click            += OnNotesClick;

        // ── GL control pointer / keyboard input ───────────────────────
        _chartControl.PointerPressed      += OnPointerPressed;
        _chartControl.PointerReleased     += OnPointerReleased;
        _chartControl.PointerMoved        += OnPointerMoved;
        _chartControl.PointerWheelChanged += OnPointerWheel;
        KeyDown += OnKeyDown;

        // ── Body label overlay (same pattern as SkyViewWindow) ────────
        var labelTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        labelTimer.Tick += (_, _) => RefreshLabels();
        labelTimer.Start();

        // ── Animation timer ───────────────────────────────────────────
        _animTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _animTimer.Tick += OnAnimTick;
        _animTimer.Start();

        // ── Messaging ─────────────────────────────────────────────────
        WeakReferenceMessenger.Default.RegisterAll(this);

        Closed += (_, _) =>
        {
            labelTimer.Stop();
            _animTimer.Stop();
            _workspace.IsActive = false;
            WeakReferenceMessenger.Default.UnregisterAll(this);
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // IRecipient<SimTimeChangedMessage>
    // ─────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public void Receive(SimTimeChangedMessage message) =>
        Dispatcher.UIThread.Post(() => RefreshSidebarData());

    // ─────────────────────────────────────────────────────────────────────
    // Animation
    // ─────────────────────────────────────────────────────────────────────

    private void OnAnimTick(object? sender, EventArgs e)
    {
        if (!_vm.Playing)
        {
            return;
        }

        // Simulation time progression is driven by SkyGlControl's internal
        // timer via SkyViewModel.Playing. This window-level timer no longer
        // mutates _vm.SimTime to avoid double-advancing time at conflicting
        // step sizes. We can still use this tick to keep the sidebar UI in
        // sync while the control advances time.
        RefreshSidebarData();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Label overlay
    // ─────────────────────────────────────────────────────────────────────

    // SkyChartControl draws its own body labels directly onto the canvas, so
    // the LabelCanvas overlay has nothing to do here.
    // TODO: when the 3D SkyGlControl is activated, restore the label-snapshot
    //       logic that reads _glControl.Labels and adds TextBlock children to
    //       LabelCanvas (see SkyViewWindow.RefreshLabels for the pattern).
    private static void RefreshLabels() { }

    // ─────────────────────────────────────────────────────────────────────
    // WorkspaceViewModel change handler
    // ─────────────────────────────────────────────────────────────────────

    private void OnWorkspacePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(WorkspaceViewModel.CelestialData)
                           or nameof(WorkspaceViewModel.IsLoading))
            Dispatcher.UIThread.Post(RefreshSidebarData);
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sidebar data refresh
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the sidebar data labels from <see cref="WorkspaceViewModel.CelestialData"/>.
    /// Shows "…" while loading and "—" when no data is available.
    /// </summary>
    private void RefreshSidebarData()
    {
        // Sync the current time display in the time bar
        CurrentTimeLabel.Text = $"UTC: {_vm.SimTime:yyyy-MM-dd HH:mm}";

        var data = _workspace.CelestialData;
        if (data is null)
        {
            string ph = _workspace.IsLoading ? "…" : "—";
            SunAlt.Text            = $"Alt:  {ph}";
            SunAz.Text             = $"Az:   {ph}";
            MoonPhase.Text         = $"Phase: {ph}";
            MoonAlt.Text           = $"Alt:   {ph}";
            MoonAz.Text            = $"Az:    {ph}";
            SunriseLabel.Text      = $"Sunrise:   {ph}";
            SunsetLabel.Text       = $"Sunset:    {ph}";
            MoonriseLabel.Text     = $"Moonrise:  {ph}";
            MoonsetLabel.Text      = $"Moonset:   {ph}";
            NextFullMoonLabel.Text = $"Next Full Moon: {ph}";
            return;
        }

        SunAlt.Text = $"Alt:  {data.Sun.Altitude:F1}°";
        SunAz.Text  = $"Az:   {data.Sun.Azimuth:F1}°";

        double illum   = data.Moon.Illumination ?? 0.0;
        MoonPhase.Text = $"Phase: {illum * 100:F0}%";
        MoonAlt.Text   = $"Alt:   {data.Moon.Altitude:F1}°";
        MoonAz.Text    = $"Az:    {data.Moon.Azimuth:F1}°";

        SunriseLabel.Text      = $"Sunrise:   {data.Sunrise?.ToString("HH:mm") ?? "—"} UTC";
        SunsetLabel.Text       = $"Sunset:    {data.Sunset?.ToString("HH:mm") ?? "—"} UTC";
        MoonriseLabel.Text     = $"Moonrise:  {data.Moonrise?.ToString("HH:mm") ?? "—"} UTC";
        MoonsetLabel.Text      = $"Moonset:   {data.Moonset?.ToString("HH:mm") ?? "—"} UTC";
        NextFullMoonLabel.Text = $"Next Full Moon: {data.NextFullMoon?.ToString("yyyy-MM-dd") ?? "—"}";
    }

    // ─────────────────────────────────────────────────────────────────────
    // Toolbar helpers
    // ─────────────────────────────────────────────────────────────────────

    private void OnDateTimeKeyDown(object? sender, KeyEventArgs e)
    {
        // Native DatePicker/TimePicker handle all keyboard input internally.
    }

    private void SyncToolbarFromVm()
    {
        _updatingFromVm = true;
        DatePicker.SelectedDate = new DateTimeOffset(_vm.SimTime.Date, TimeSpan.Zero);
        TimePicker.SelectedTime = _vm.SimTime.TimeOfDay;
        _updatingFromVm = false;
        LonPicker.Value = (decimal)_vm.Longitude;
        LatPicker.Value = (decimal)_vm.Latitude;
        UpdatePlayPauseButton();
        CurrentTimeLabel.Text = $"UTC: {_vm.SimTime:yyyy-MM-dd HH:mm}";
    }

    private void ApplyDateTimePickerValues()
    {
        if (_updatingFromVm) return;
        if (DatePicker.SelectedDate is not { } date) return;
        var time = TimePicker.SelectedTime ?? TimeSpan.Zero;
        var dt = new DateTime(date.Year, date.Month, date.Day,
                              time.Hours, time.Minutes, 0, DateTimeKind.Utc);
        _vm.SimTime = dt;
    }

    private void UpdatePlayPauseButton() =>
        PlayPauseBtn.Content = _vm.Playing ? "⏸" : "▶";

    private void UpdateSpeedLabel()
    {
        int idx = (int)Math.Round(SpeedSlider.Value);
        idx = Math.Clamp(idx, 0, SpeedLabels.Length - 1);
        SpeedLabel.Text = SpeedLabels[idx];
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SkyViewModel.SimTime):
                _updatingFromVm = true;
                DatePicker.SelectedDate = new DateTimeOffset(_vm.SimTime.Date, TimeSpan.Zero);
                TimePicker.SelectedTime = _vm.SimTime.TimeOfDay;
                _updatingFromVm = false;
                CurrentTimeLabel.Text = $"UTC: {_vm.SimTime:yyyy-MM-dd HH:mm}";
                _workspace.SimTime = _vm.SimTime;
                break;
            case nameof(SkyViewModel.Longitude):
                LonPicker.Value = (decimal)_vm.Longitude;
                _workspace.Longitude = _vm.Longitude;
                break;
            case nameof(SkyViewModel.Latitude):
                LatPicker.Value = (decimal)_vm.Latitude;
                _workspace.Latitude = _vm.Latitude;
                break;
            case nameof(SkyViewModel.Playing):
                UpdatePlayPauseButton();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sidebar action buttons
    // ─────────────────────────────────────────────────────────────────────

    private async void OnScripturalEventsClick(object? sender, RoutedEventArgs e) =>
        await new ScripturalEventLibraryWindow(_vm).ShowDialog(this);

    private async void OnComparisonModeClick(object? sender, RoutedEventArgs e) =>
        await new ComparisonWindow(_vm).ShowDialog(this);

    private async void OnNotesClick(object? sender, RoutedEventArgs e) =>
        await new NotesPanel(_vm).ShowDialog(this);

    // ─────────────────────────────────────────────────────────────────────
    // Chart control input handling
    // ─────────────────────────────────────────────────────────────────────

    // TODO: when the 3D SkyGlControl is reactivated, restore yaw/pitch/FOV
    //       drag and wheel gestures — the 2D planisphere has no camera orientation.

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_chartControl).Properties.IsLeftButtonPressed)
        {
            _dragging  = true;
            _lastMouse = e.GetPosition(_chartControl);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        _dragging = false;

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        var pos = e.GetPosition(_chartControl);
        // TODO: map drag to zoom level or chart rotation for the 3D view.
        _lastMouse = pos;
    }

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        // TODO: wire scroll wheel to chart zoom level for the 3D view.
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space: _vm.PlayPauseCommand.Execute(null);   break;
            case Key.Left:  _vm.SimTime = _vm.SimTime.AddHours(-1); break;
            case Key.Right: _vm.SimTime = _vm.SimTime.AddHours(1);  break;
            case Key.F:     _vm.ResetToNowCommand.Execute(null);  break;
            case Key.Up:
                _vm.FovDeg = Math.Clamp(_vm.FovDeg - 5f, 10f, 170f);
                break;
            case Key.Down:
                _vm.FovDeg = Math.Clamp(_vm.FovDeg + 5f, 10f, 170f);
                break;
        }
    }
}
