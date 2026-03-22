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
using Ephemeris.UI.Avalonia.Controls;
using Ephemeris.UI.Messages;

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
    // TODO: replace with WorkspaceViewModel
    private readonly SkyViewModel _vm;
    private readonly SkyGlControl _glControl;

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

        _glControl = new SkyGlControl(_vm)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };

        GlHost.Content = _glControl;

        // Sync toolbar to initial VM state
        SyncToolbarFromVm();
        _vm.PropertyChanged += OnVmPropertyChanged;

        // ── Toolbar wire-up ───────────────────────────────────────────
        DateBox.LostFocus += (_, _) => ApplyDateTimeInput();
        DateBox.KeyDown   += OnDateTimeKeyDown;
        TimeBox.LostFocus += (_, _) => ApplyDateTimeInput();
        TimeBox.KeyDown   += OnDateTimeKeyDown;

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

        // ── Sidebar action buttons ────────────────────────────────────
        ScripturalEventsBtn.Click += OnScripturalEventsClick;
        ComparisonModeBtn.Click   += OnComparisonModeClick;
        NotesBtn.Click            += OnNotesClick;

        // ── GL control pointer / keyboard input ───────────────────────
        _glControl.PointerPressed      += OnPointerPressed;
        _glControl.PointerReleased     += OnPointerReleased;
        _glControl.PointerMoved        += OnPointerMoved;
        _glControl.PointerWheelChanged += OnPointerWheel;
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
    // Label overlay (identical to SkyViewWindow pattern)
    // ─────────────────────────────────────────────────────────────────────

    private void RefreshLabels()
    {
        LabelCanvas.Children.Clear();
        var bounds = _glControl.Bounds;
        foreach (var (screen, label, colorArgb) in _glControl.Labels)
        {
            if (screen.X < 0 || screen.Y < 0 ||
                screen.X > bounds.Width || screen.Y > bounds.Height) continue;
            var tb = new TextBlock
            {
                Text       = label,
                FontSize   = 11,
                Foreground = new SolidColorBrush(Color.FromArgb(
                    (byte)(colorArgb >> 24),
                    (byte)(colorArgb >> 16),
                    (byte)(colorArgb >>  8),
                    (byte) colorArgb)),
            };
            Canvas.SetLeft(tb, screen.X + 6);
            Canvas.SetTop(tb,  screen.Y - 8);
            LabelCanvas.Children.Add(tb);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sidebar data refresh
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Refreshes the sidebar data labels.
    /// Values are populated as stubs (—) until WorkspaceViewModel.CelestialData
    /// is wired in. TODO: replace with WorkspaceViewModel data bindings.
    /// </summary>
    private void RefreshSidebarData()
    {
        // Sync the current time display in the time bar
        CurrentTimeLabel.Text = $"UTC: {_vm.SimTime:yyyy-MM-dd HH:mm}";

        // TODO: replace with real data from WorkspaceViewModel.CelestialData
        // Sidebar text blocks remain at their default "Loading…" / "—" values
        // until a CelestialDataService is wired up.
        SunAlt.Text      = "Alt:  —";
        SunAz.Text       = "Az:   —";
        MoonPhase.Text   = "Phase: —";
        MoonAlt.Text     = "Alt:   —";
        MoonAz.Text      = "Az:    —";
        SunriseLabel.Text  = "Sunrise:   —";
        SunsetLabel.Text   = "Sunset:    —";
        MoonriseLabel.Text = "Moonrise:  —";
        MoonsetLabel.Text  = "Moonset:   —";
        NextFullMoonLabel.Text = "Next Full Moon: —";
    }

    // ─────────────────────────────────────────────────────────────────────
    // Toolbar helpers
    // ─────────────────────────────────────────────────────────────────────

    private void OnDateTimeKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) ApplyDateTimeInput();
    }

    private void SyncToolbarFromVm()
    {
        DateBox.Text    = _vm.SimTime.ToString("yyyy-MM-dd");
        TimeBox.Text    = _vm.SimTime.ToString("HH:mm");
        LonPicker.Value = (decimal)_vm.Longitude;
        LatPicker.Value = (decimal)_vm.Latitude;
        UpdatePlayPauseButton();
        CurrentTimeLabel.Text = $"UTC: {_vm.SimTime:yyyy-MM-dd HH:mm}";
    }

    private void ApplyDateTimeInput()
    {
        var dateText = DateBox.Text?.Trim() ?? string.Empty;
        var timeText = TimeBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(timeText)) timeText = "00:00";

        if (DateTime.TryParseExact(
                $"{dateText} {timeText}",
                "yyyy-MM-dd HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var dt))
        {
            _vm.SimTime = dt;
        }
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
                DateBox.Text = _vm.SimTime.ToString("yyyy-MM-dd");
                TimeBox.Text = _vm.SimTime.ToString("HH:mm");
                CurrentTimeLabel.Text = $"UTC: {_vm.SimTime:yyyy-MM-dd HH:mm}";
                break;
            case nameof(SkyViewModel.Longitude):
                LonPicker.Value = (decimal)_vm.Longitude;
                break;
            case nameof(SkyViewModel.Latitude):
                LatPicker.Value = (decimal)_vm.Latitude;
                break;
            case nameof(SkyViewModel.Playing):
                UpdatePlayPauseButton();
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Sidebar action buttons
    // ─────────────────────────────────────────────────────────────────────

    private async void OnScripturalEventsClick(object? sender, RoutedEventArgs e)
    {
        // TODO: open ScripturalEventLibraryWindow when implemented
        var dlg = new Window
        {
            Title   = "Scriptural Events",
            Width   = 480,
            Height  = 320,
            Content = new TextBlock
            {
                Text              = "Scriptural Event Library — coming soon.",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };
        await dlg.ShowDialog(this);
    }

    private async void OnComparisonModeClick(object? sender, RoutedEventArgs e)
    {
        // TODO: open side-by-side comparison view when implemented
        var dlg = new Window
        {
            Title   = "Comparison Mode",
            Width   = 480,
            Height  = 320,
            Content = new TextBlock
            {
                Text              = "Comparison Mode — coming soon.",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };
        await dlg.ShowDialog(this);
    }

    private async void OnNotesClick(object? sender, RoutedEventArgs e)
    {
        // TODO: show NotesPanel when implemented
        var dlg = new Window
        {
            Title   = "Notes",
            Width   = 480,
            Height  = 320,
            Content = new TextBlock
            {
                Text              = "Notes — coming soon.",
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            },
        };
        await dlg.ShowDialog(this);
    }

    // ─────────────────────────────────────────────────────────────────────
    // GL control input handling (same patterns as SkyViewWindow)
    // ─────────────────────────────────────────────────────────────────────

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_glControl).Properties.IsLeftButtonPressed)
        {
            _dragging  = true;
            _lastMouse = e.GetPosition(_glControl);
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e) =>
        _dragging = false;

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_dragging) return;
        var pos = e.GetPosition(_glControl);
        float dx = (float)(pos.X - _lastMouse.X);
        float dy = (float)(pos.Y - _lastMouse.Y);
        _vm.Yaw   = (_vm.Yaw   + dx * 0.4f + 360f) % 360f;
        _vm.Pitch = Math.Clamp(_vm.Pitch - dy * 0.3f, -10f, 90f);
        _lastMouse = pos;
    }

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e) =>
        _vm.FovDeg = Math.Clamp(_vm.FovDeg - (float)e.Delta.Y * 2f, 10f, 170f);

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
