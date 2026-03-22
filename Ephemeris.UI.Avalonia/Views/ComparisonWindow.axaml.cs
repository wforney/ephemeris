// Updated: 2026-03-22
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Ephemeris.UI.Avalonia.Controls;
using Ephemeris.UI.ViewModels;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Side-by-side sky comparison window.
/// Shows a <em>Normal Sky</em> panel (baseline, unmodified motion) and a
/// <em>Modified Sky</em> panel (simulation with overrides) simultaneously so
/// researchers can directly compare the two views.
/// </summary>
/// <remarks>
/// <para>
/// Time and location controls in the header are bound to <see cref="ComparisonViewModel.Baseline"/>
/// and propagated to <see cref="ComparisonViewModel.Simulation"/> automatically when
/// <see cref="ComparisonViewModel.SyncTime"/> is <see langword="true"/>.
/// </para>
/// <para>
/// Create via <c>new ComparisonWindow(skyVm).Show()</c> from
/// <c>ResearchWorkspaceWindow.OnComparisonModeClick</c>.
/// </para>
/// </remarks>
public partial class ComparisonWindow : Window
{
    /// <summary>Label refresh interval — 50 ms ≈ 20 fps.</summary>
    private static readonly TimeSpan LabelRefreshInterval = TimeSpan.FromMilliseconds(50);

    private readonly ComparisonViewModel _vm;
    private readonly SkyGlControl _baselineGl;
    private readonly SkyGlControl _simulationGl;

    // Mouse drag state for each GL panel
    private bool _draggingBaseline;
    private bool _draggingSimulation;
    private Point _lastMouseBaseline;
    private Point _lastMouseSimulation;

    /// <summary>
    /// Parameterless constructor required by Avalonia's runtime XAML loader.
    /// Creates a default sky view-model so the designer / previewer can open the window.
    /// </summary>
    public ComparisonWindow() : this(new SkyViewModel()) { }

    /// <summary>
    /// Initialises the comparison window using <paramref name="sourceVm"/> as the baseline.
    /// </summary>
    /// <param name="sourceVm">
    /// Existing sky view-model whose time and location seed both the baseline and
    /// simulation panels.
    /// </param>
    public ComparisonWindow(SkyViewModel sourceVm)
    {
        InitializeComponent();

        _vm = new ComparisonViewModel(sourceVm);

        // Create the two OpenGL sky controls
        _baselineGl = new SkyGlControl(_vm.Baseline)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };
        _simulationGl = new SkyGlControl(_vm.Simulation)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };

        BaselineGlHost.Content   = _baselineGl;
        SimulationGlHost.Content = _simulationGl;

        // Initialise header controls from the baseline view-model
        SyncHeaderFromVm();
        _vm.Baseline.PropertyChanged += OnBaselinePropertyChanged;

        // Header controls — date/time text boxes
        DatePicker.LostFocus += (_, _) => ApplyDateTimePickerText();
        TimePicker.LostFocus += (_, _) => ApplyDateTimePickerText();
        DatePicker.KeyDown   += (_, e) => { if (e.Key == Key.Enter) ApplyDateTimePickerText(); };
        TimePicker.KeyDown   += (_, e) => { if (e.Key == Key.Enter) ApplyDateTimePickerText(); };

        LonPicker.ValueChanged += (_, _) =>
        {
            if (LonPicker.Value is { } lon) _vm.Baseline.Longitude = (double)lon;
        };
        LatPicker.ValueChanged += (_, _) =>
        {
            if (LatPicker.Value is { } lat) _vm.Baseline.Latitude = (double)lat;
        };

        // Time control buttons
        StepFirstBtn.Click += (_, _) => _vm.Baseline.SimTime = _vm.Baseline.SimTime.AddDays(-30);
        StepBackBtn.Click  += (_, _) => _vm.Baseline.StepBackCommand.Execute(null);
        PlayBtn.Click      += (_, _) => _vm.Baseline.PlayPauseCommand.Execute(null);
        StepFwdBtn.Click   += (_, _) => _vm.Baseline.StepForwardCommand.Execute(null);
        StepLastBtn.Click  += (_, _) => _vm.Baseline.SimTime = _vm.Baseline.SimTime.AddDays(30);
        NowBtn.Click       += (_, _) => _vm.Baseline.ResetToNowCommand.Execute(null);

        // Simulation controls
        FreezeMotionCheck.IsCheckedChanged += (_, _) =>
            _vm.SimOverride.MotionFrozen = FreezeMotionCheck.IsChecked == true;

        SunAltOffsetPicker.ValueChanged += (_, _) =>
        {
            if (SunAltOffsetPicker.Value is { } v)
                _vm.SimOverride.SunAltitudeOffsetDegrees = (double)v;
        };

        ExtendDaylightPicker.ValueChanged += (_, _) =>
        {
            if (ExtendDaylightPicker.Value is { } v)
                _vm.SimOverride.ExtendDaylightHours = (double)v;
        };

        ResetSimBtn.Click += (_, _) => _vm.ResetSimulationCommand.Execute(null);

        // Mouse drag and wheel on each panel
        _baselineGl.PointerPressed      += OnBaselinePointerPressed;
        _baselineGl.PointerReleased     += (_, _) => _draggingBaseline = false;
        _baselineGl.PointerMoved        += OnBaselinePointerMoved;
        _baselineGl.PointerWheelChanged += OnBaselinePointerWheel;

        _simulationGl.PointerPressed      += OnSimPointerPressed;
        _simulationGl.PointerReleased     += (_, _) => _draggingSimulation = false;
        _simulationGl.PointerMoved        += OnSimPointerMoved;
        _simulationGl.PointerWheelChanged += OnSimPointerWheel;

        KeyDown += OnKeyDown;

        // Refresh body-name labels at ~20 fps
        var labelTimer = new DispatcherTimer { Interval = LabelRefreshInterval };
        labelTimer.Tick += (_, _) => RefreshLabels();
        labelTimer.Start();
        Closed += (_, _) => labelTimer.Stop();
    }

    // ── Label refresh ────────────────────────────────────────────────────

    private void RefreshLabels()
    {
        RefreshLabelCanvas(BaselineLabelCanvas, _baselineGl);
        RefreshLabelCanvas(SimulationLabelCanvas, _simulationGl);
    }

    private static void RefreshLabelCanvas(Canvas canvas, SkyGlControl gl)
    {
        canvas.Children.Clear();
        foreach (var (screen, label, colorArgb) in gl.Labels)
        {
            if (screen.X < 0 || screen.Y < 0) continue;
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
            Canvas.SetTop(tb, screen.Y - 8);
            canvas.Children.Add(tb);
        }
    }

    // ── Header sync ──────────────────────────────────────────────────────

    private void SyncHeaderFromVm()
    {
        DatePicker.Text = _vm.Baseline.SimTime.ToString("yyyy-MM-dd");
        TimePicker.Text = _vm.Baseline.SimTime.ToString("HH:mm");
        LonPicker.Value = (decimal)_vm.Baseline.Longitude;
        LatPicker.Value = (decimal)_vm.Baseline.Latitude;
        PlayBtn.Content = _vm.Baseline.Playing ? "⏸" : "▶";
    }

    private void OnBaselinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SkyViewModel.SimTime):
                DatePicker.Text = _vm.Baseline.SimTime.ToString("yyyy-MM-dd");
                TimePicker.Text = _vm.Baseline.SimTime.ToString("HH:mm");
                break;
            case nameof(SkyViewModel.Longitude):
                LonPicker.Value = (decimal)_vm.Baseline.Longitude;
                break;
            case nameof(SkyViewModel.Latitude):
                LatPicker.Value = (decimal)_vm.Baseline.Latitude;
                break;
            case nameof(SkyViewModel.Playing):
                PlayBtn.Content = _vm.Baseline.Playing ? "⏸" : "▶";
                break;
        }
    }

    private void ApplyDateTimePickerText()
    {
        var dateStr = DatePicker.Text ?? string.Empty;
        var timeStr = TimePicker.Text ?? string.Empty;
        if (DateTime.TryParseExact($"{dateStr} {timeStr}", "yyyy-MM-dd HH:mm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var dt))
        {
            _vm.Baseline.SimTime = dt;
        }
    }

    // ── Input: Baseline panel ────────────────────────────────────────────

    private void OnBaselinePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_baselineGl).Properties.IsLeftButtonPressed)
        {
            _draggingBaseline  = true;
            _lastMouseBaseline = e.GetPosition(_baselineGl);
        }
    }

    private void OnBaselinePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_draggingBaseline) return;
        var pos = e.GetPosition(_baselineGl);
        float dx = (float)(pos.X - _lastMouseBaseline.X);
        float dy = (float)(pos.Y - _lastMouseBaseline.Y);
        _vm.Baseline.Yaw   = (_vm.Baseline.Yaw   + dx * 0.4f + 360f) % 360f;
        _vm.Baseline.Pitch = Math.Clamp(_vm.Baseline.Pitch - dy * 0.3f, -10f, 90f);
        _lastMouseBaseline = pos;
    }

    private void OnBaselinePointerWheel(object? sender, PointerWheelEventArgs e) =>
        _vm.Baseline.FovDeg = Math.Clamp(
            _vm.Baseline.FovDeg - (float)e.Delta.Y * 2f, 10f, 170f);

    // ── Input: Simulation panel ──────────────────────────────────────────

    private void OnSimPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(_simulationGl).Properties.IsLeftButtonPressed)
        {
            _draggingSimulation  = true;
            _lastMouseSimulation = e.GetPosition(_simulationGl);
        }
    }

    private void OnSimPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_draggingSimulation) return;
        var pos = e.GetPosition(_simulationGl);
        float dx = (float)(pos.X - _lastMouseSimulation.X);
        float dy = (float)(pos.Y - _lastMouseSimulation.Y);
        _vm.Simulation.Yaw   = (_vm.Simulation.Yaw   + dx * 0.4f + 360f) % 360f;
        _vm.Simulation.Pitch = Math.Clamp(_vm.Simulation.Pitch - dy * 0.3f, -10f, 90f);
        _lastMouseSimulation = pos;
    }

    private void OnSimPointerWheel(object? sender, PointerWheelEventArgs e) =>
        _vm.Simulation.FovDeg = Math.Clamp(
            _vm.Simulation.FovDeg - (float)e.Delta.Y * 2f, 10f, 170f);

    // ── Keyboard ─────────────────────────────────────────────────────────

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space: _vm.Baseline.PlayPauseCommand.Execute(null);   break;
            case Key.Left:  _vm.Baseline.StepBackCommand.Execute(null);    break;
            case Key.Right: _vm.Baseline.StepForwardCommand.Execute(null); break;
            case Key.F:     _vm.Baseline.ResetToNowCommand.Execute(null);  break;
        }
    }
}
