// Updated: 2026-03-10
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Ephemeris.UI.Avalonia.Controls;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// 3D sky view window — the Avalonia cross-platform equivalent of the WinForms
/// <c>SkyViewForm</c>.  Uses <see cref="SkyGlControl"/> (backed by
/// <c>Avalonia.OpenGL.Controls.OpenGlControlBase</c>) for OpenGL rendering
/// on Windows, Linux, and macOS.
/// </summary>
/// <remarks>
/// Input handling differences from the WinForms version:
/// <list type="bullet">
///   <item><description>Mouse drag uses Avalonia's <c>PointerPressed/Moved/Released</c> events.</description></item>
///   <item><description>Wheel zoom uses <c>PointerWheelChanged</c>.</description></item>
///   <item><description>Keyboard uses <c>KeyDown</c>.</description></item>
/// </list>
/// Date/time input uses a <c>TextBox</c> with <c>yyyy-MM-dd HH:mm</c> format; press Enter to apply.
/// </remarks>
public partial class SkyViewWindow : Window
{
    private readonly SkyViewModel _vm;
    private readonly SkyGlControl _glControl;

    // Mouse drag state
    private bool _dragging;
    private Point _lastMouse;

    /// <summary>
    /// Initialises the sky view window.
    /// </summary>
    /// <param name="longitude">Observer longitude in degrees (east positive).</param>
    /// <param name="latitude">Observer latitude in degrees (north positive).</param>
    /// <param name="initialTime">Initial UTC simulation time.</param>
    public SkyViewWindow(double longitude = 0.0, double latitude = 51.5, DateTime initialTime = default)
    {
        InitializeComponent();

        _vm         = new SkyViewModel(longitude, latitude, initialTime);
        _glControl  = new SkyGlControl(_vm)
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment   = VerticalAlignment.Stretch,
        };

        GlHost.Content = _glControl;

        // Initialise toolbar from view-model
        SyncToolbarFromVm();
        _vm.PropertyChanged += OnVmPropertyChanged;

        // Date/time text box: parse on lost focus or Enter
        DatePicker.LostFocus += (_, _) => ApplyDatePickerText();
        DatePicker.KeyDown   += (_, e) =>
        {
            if (e.Key == Key.Enter) ApplyDatePickerText();
        };

        LonPicker.ValueChanged += (_, _) => { if (LonPicker.Value is { } lon) _vm.Longitude = (double)lon; };
        LatPicker.ValueChanged += (_, _) => { if (LatPicker.Value is { } lat) _vm.Latitude  = (double)lat; };
        PlayBtn.Click += (_, _) => _vm.PlayPauseCommand.Execute(null);
        NowBtn.Click  += (_, _) => _vm.ResetToNowCommand.Execute(null);

        // GL control input events
        _glControl.PointerPressed      += OnPointerPressed;
        _glControl.PointerReleased     += OnPointerReleased;
        _glControl.PointerMoved        += OnPointerMoved;
        _glControl.PointerWheelChanged += OnPointerWheel;

        KeyDown += OnKeyDown;
    }

    private void ApplyDatePickerText()
    {
        if (DateTime.TryParseExact(DatePicker.Text, "yyyy-MM-dd HH:mm",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.AssumeUniversal |
            System.Globalization.DateTimeStyles.AdjustToUniversal,
            out var dt))
        {
            _vm.SimTime = dt;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // ViewModel sync
    // ─────────────────────────────────────────────────────────────────────

    private void SyncToolbarFromVm()
    {
        DatePicker.Text     = _vm.SimTime.ToString("yyyy-MM-dd HH:mm");
        LonPicker.Value     = (decimal)_vm.Longitude;
        LatPicker.Value     = (decimal)_vm.Latitude;
        PlayBtn.Content     = _vm.Playing ? "⏸ Pause" : "▶ Play";
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SkyViewModel.SimTime):
                DatePicker.Text = _vm.SimTime.ToString("yyyy-MM-dd HH:mm");
                break;
            case nameof(SkyViewModel.Longitude):
                LonPicker.Value = (decimal)_vm.Longitude;
                break;
            case nameof(SkyViewModel.Latitude):
                LatPicker.Value = (decimal)_vm.Latitude;
                break;
            case nameof(SkyViewModel.Playing):
                PlayBtn.Content = _vm.Playing ? "⏸ Pause" : "▶ Play";
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Input handling
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

    private void OnPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        _vm.FovDeg = Math.Clamp(_vm.FovDeg - (float)e.Delta.Y * 2f, 10f, 170f);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space: _vm.PlayPauseCommand.Execute(null);    break;
            case Key.Left:  _vm.StepBackCommand.Execute(null);     break;
            case Key.Right: _vm.StepForwardCommand.Execute(null);  break;
            case Key.F:     _vm.ResetToNowCommand.Execute(null);   break;
            case Key.Up:
                _vm.FovDeg = Math.Clamp(_vm.FovDeg - 5f, 10f, 170f);
                break;
            case Key.Down:
                _vm.FovDeg = Math.Clamp(_vm.FovDeg + 5f, 10f, 170f);
                break;
        }
    }
}
