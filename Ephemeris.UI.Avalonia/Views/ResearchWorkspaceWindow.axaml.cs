// Updated: 2026-03-22
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ephemeris.UI.Services;
using Ephemeris.UI.ViewModels;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Main research workspace window.
/// Hosts the sky view, playback toolbar, and provides access to the scriptural event
/// library and research notes panel via toolbar buttons.
/// </summary>
/// <remarks>
/// Wires a <see cref="PlaybackEngine"/> to <see cref="WorkspaceViewModel"/> so that
/// time advances automatically when <see cref="WorkspaceViewModel.Playing"/> is
/// <see langword="true"/>.
/// </remarks>
public partial class ResearchWorkspaceWindow : Window
{
    private readonly WorkspaceViewModel _vm;
    private readonly PlaybackEngine _playbackEngine;

    /// <summary>
    /// Parameterless constructor required by Avalonia's runtime XAML loader.
    /// Defaults to longitude 0° / latitude 51.5° N (London).
    /// </summary>
    public ResearchWorkspaceWindow() : this(new WorkspaceViewModel()) { }

    /// <summary>
    /// Initialises the workspace window with an existing view-model.
    /// </summary>
    /// <param name="vm">The research workspace view-model.</param>
    public ResearchWorkspaceWindow(WorkspaceViewModel vm)
    {
        InitializeComponent();

        _vm             = vm;
        _playbackEngine = new PlaybackEngine(vm);

        // Populate toolbar from view-model
        SyncToolbarFromVm();
        _vm.PropertyChanged += OnVmPropertyChanged;

        // Toolbar wiring
        LonPicker.ValueChanged   += (_, _) => { if (LonPicker.Value is { } v)   _vm.Longitude      = (double)v; };
        LatPicker.ValueChanged   += (_, _) => { if (LatPicker.Value is { } v)   _vm.Latitude       = (double)v; };
        SpeedPicker.ValueChanged += (_, _) => { if (SpeedPicker.Value is { } v) _vm.PlaybackSpeed  = (double)v; };

        DatePicker.LostFocus += (_, _) => ApplyDatePickerText();
        DatePicker.KeyDown   += (_, e) =>
        {
            if (e.Key == Key.Enter) ApplyDatePickerText();
        };

        StepBackBtn.Click  += (_, _) => _vm.StepBackCommand.Execute(null);
        PlayPauseBtn.Click += (_, _) => _vm.PlayPauseCommand.Execute(null);
        StepFwdBtn.Click   += (_, _) => _vm.StepForwardCommand.Execute(null);

        ScripturalEventsBtn.Click += OnScripturalEventsClick;
        NotesBtn.Click            += OnNotesClick;

        // Start the playback engine; it only advances time when Playing == true.
        _playbackEngine.Start();

        Closed += (_, _) =>
        {
            _playbackEngine.Dispose();
            _vm.PropertyChanged -= OnVmPropertyChanged;
        };
    }

    // ─────────────────────────────────────────────────────────────────────
    // Feature button handlers
    // ─────────────────────────────────────────────────────────────────────

    /// <summary>Opens the Scriptural Event Library as a modal dialog.</summary>
    private void OnScripturalEventsClick(object? sender, RoutedEventArgs e) =>
        new ScripturalEventLibraryWindow(_vm).ShowDialog(this);

    /// <summary>Opens the Research Notes panel as a modal dialog.</summary>
    private void OnNotesClick(object? sender, RoutedEventArgs e) =>
        new NotesPanel(_vm).ShowDialog(this);

    // ─────────────────────────────────────────────────────────────────────
    // ViewModel sync
    // ─────────────────────────────────────────────────────────────────────

    private void SyncToolbarFromVm()
    {
        DatePicker.Text       = _vm.SimTime.ToString("yyyy-MM-dd HH:mm");
        LonPicker.Value       = (decimal)_vm.Longitude;
        LatPicker.Value       = (decimal)_vm.Latitude;
        SpeedPicker.Value     = (decimal)_vm.PlaybackSpeed;
        PlayPauseBtn.Content  = _vm.Playing ? "⏸ Pause" : "▶ Play";
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(WorkspaceViewModel.SimTime):
                DatePicker.Text = _vm.SimTime.ToString("yyyy-MM-dd HH:mm");
                break;
            case nameof(WorkspaceViewModel.Longitude):
                LonPicker.Value = (decimal)_vm.Longitude;
                break;
            case nameof(WorkspaceViewModel.Latitude):
                LatPicker.Value = (decimal)_vm.Latitude;
                break;
            case nameof(WorkspaceViewModel.Playing):
                PlayPauseBtn.Content = _vm.Playing ? "⏸ Pause" : "▶ Play";
                break;
            case nameof(WorkspaceViewModel.PlaybackSpeed):
                SpeedPicker.Value = (decimal)_vm.PlaybackSpeed;
                break;
        }
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
}
