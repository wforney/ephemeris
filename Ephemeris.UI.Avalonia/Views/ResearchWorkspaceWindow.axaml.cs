// Updated: 2026-03-22
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Ephemeris.UI.Models;
using Ephemeris.UI.Services;
using Ephemeris.UI.ViewModels;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Research Workspace window — provides scenario loading, historical BCE date support,
/// and celestial position readouts for the Ephemeris Research App.
/// </summary>
/// <remarks>
/// In <em>historical mode</em> (when a scenario with a <c>HistoricalDate</c> is loaded):
/// <list type="bullet">
///   <item>The date text box is hidden and replaced by a formatted historical string.</item>
///   <item>A "📜 Historical Mode" badge is shown in the status bar.</item>
///   <item>Step-forward/back buttons advance/rewind the Julian Day directly.</item>
/// </list>
/// </remarks>
public partial class ResearchWorkspaceWindow : Window
{
    private readonly WorkspaceViewModel _vm;

    /// <summary>
    /// Parameterless constructor required by Avalonia's XAML loader.
    /// Creates a default <see cref="CelestialResearchService"/> instance.
    /// </summary>
    public ResearchWorkspaceWindow() : this(new CelestialResearchService()) { }

    /// <summary>
    /// Initialises the window with the supplied <see cref="ICelestialResearchService"/>.
    /// </summary>
    public ResearchWorkspaceWindow(ICelestialResearchService service)
    {
        InitializeComponent();

        _vm = new WorkspaceViewModel(service);

        // Populate scenario picker
        ScenarioPicker.ItemsSource    = BuiltInScenarios.All;
        ScenarioPicker.DisplayMemberBinding = new global::Avalonia.Data.Binding(nameof(ScenarioModel.Name));

        SyncFromVm();
        _vm.PropertyChanged += OnVmPropertyChanged;

        // Wire up toolbar controls
        DatePicker.LostFocus += (_, _) => ApplyDateText();
        DatePicker.KeyDown   += (_, e) => { if (e.Key == Key.Enter) ApplyDateText(); };

        LonPicker.ValueChanged += (_, _) => { if (LonPicker.Value is { } v) _vm.Longitude = (double)v; };
        LatPicker.ValueChanged += (_, _) => { if (LatPicker.Value is { } v) _vm.Latitude  = (double)v; };

        StepBackBtn.Click  += async (_, _) => await _vm.StepBackCommand.ExecuteAsync(null);
        StepFwdBtn.Click   += async (_, _) => await _vm.StepForwardCommand.ExecuteAsync(null);

        ScenarioPicker.SelectionChanged += async (_, _) =>
        {
            if (ScenarioPicker.SelectedItem is ScenarioModel s)
                await _vm.LoadScenarioCommand.ExecuteAsync(s);
        };

        ClearScenarioBtn.Click += (_, _) =>
        {
            _vm.ClearScenarioCommand.Execute(null);
            ScenarioPicker.SelectedItem = null;
        };

        // Trigger initial calculation
        _ = _vm.RefreshDataCommand.ExecuteAsync(null);
    }

    // ─────────────────────────────────────────────────────────────────────
    // ViewModel synchronisation
    // ─────────────────────────────────────────────────────────────────────

    private void SyncFromVm()
    {
        bool hist = _vm.IsHistoricalMode;

        // Date display
        DateDisplayLabel.Text    = _vm.DisplayDate;
        DatePicker.IsVisible     = !hist;
        DateDisplayLabel.IsVisible = hist;

        if (!hist)
            DatePicker.Text = _vm.SimTime.ToString("yyyy-MM-dd HH:mm");

        LonPicker.Value = (decimal)_vm.Longitude;
        LatPicker.Value = (decimal)_vm.Latitude;

        // Historical mode badge
        HistoricalBadge.IsVisible = hist;

        // Scenario info card
        bool hasScenario = _vm.ActiveScenario is not null;
        ScenarioInfoBorder.IsVisible = hasScenario;
        if (hasScenario)
        {
            ScenarioNameLabel.Text = _vm.ActiveScenario!.Name;
            ScenarioDescLabel.Text = _vm.ActiveScenario.Description ?? string.Empty;
        }

        // Celestial data
        if (_vm.ResearchData is { } d)
        {
            SunLabel.Text  = $"Az: {d.SunAzimuth:F1}°   Alt: {d.SunAltitude:F1}°";
            MoonLabel.Text = $"Az: {d.MoonAzimuth:F1}°   Alt: {d.MoonAltitude:F1}°";
            StatusLabel.Text = $"JD {d.JulianDay:F2}";
        }
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // All changes → full re-sync (simple and safe for this window size)
        SyncFromVm();
    }

    private void ApplyDateText()
    {
        if (DateTime.TryParseExact(
                DatePicker.Text,
                "yyyy-MM-dd HH:mm",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal |
                System.Globalization.DateTimeStyles.AdjustToUniversal,
                out var dt))
        {
            _vm.SimTime = dt;
            _ = _vm.RefreshDataCommand.ExecuteAsync(null);
        }
    }
}
