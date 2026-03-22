// Updated: 2026-03-22
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Ephemeris.UI.Avalonia.Controls;

/// <summary>
/// A compact toolbar that exposes toggle buttons and a magnitude slider for
/// controlling the display options of a <see cref="SkyGlControl"/>.
/// </summary>
/// <remarks>
/// <para>
/// Place this control in a layout row above or below the <see cref="SkyGlControl"/>.
/// Call <see cref="Attach"/> to bind the toolbar to a specific <see cref="SkyGlControl"/> instance.
/// </para>
/// <para>
/// Toggle buttons are wired to the following <see cref="SkyGlControl"/> properties:
/// <list type="bullet">
///   <item><description><c>Stars</c> — renders stars (mapped to <see cref="SkyGlControl.StarMagnitudeLimit"/>: 5.5 when on, -99 when off).</description></item>
///   <item><description><c>Constellations</c> → <see cref="SkyGlControl.ShowConstellations"/>.</description></item>
///   <item><description><c>Star Labels</c> → <see cref="SkyGlControl.ShowStarLabels"/>.</description></item>
///   <item><description><c>Planet Labels</c> → <see cref="SkyGlControl.ShowPlanetLabels"/>.</description></item>
///   <item><description><c>Horizon</c> → <see cref="SkyGlControl.ShowHorizonGrid"/>.</description></item>
///   <item><description><c>Sun Path</c> → <see cref="SkyGlControl.ShowSunPath"/>.</description></item>
///   <item><description><c>Moon Path</c> → <see cref="SkyGlControl.ShowMoonPath"/>.</description></item>
/// </list>
/// The magnitude slider controls <see cref="SkyGlControl.StarMagnitudeLimit"/> (when stars are enabled).
/// </para>
/// </remarks>
public partial class SkyDisplayToggleBar : UserControl
{
    /// <summary>Sentinel value written to <see cref="SkyGlControl.StarMagnitudeLimit"/> when star rendering is disabled.</summary>
    private const double DisabledStarMagnitude = -99.0;

    private SkyGlControl? _skyControl;
    private double _lastMagLimit = 5.5;

    /// <summary>
    /// Initialises the toolbar and wires up internal event handlers.
    /// </summary>
    public SkyDisplayToggleBar()
    {
        InitializeComponent();
        WireEvents();
    }

    /// <summary>
    /// Attaches the toolbar to a <see cref="SkyGlControl"/> so that toggle button
    /// state changes are forwarded to the control's display properties.
    /// </summary>
    /// <param name="skyControl">The sky control to drive.</param>
    public void Attach(SkyGlControl skyControl)
    {
        _skyControl = skyControl;
        SyncToControl();
    }

    // ─────────────────────────────────────────────────────────────────────
    // Internal wiring
    // ─────────────────────────────────────────────────────────────────────

    private void WireEvents()
    {
        StarsToggle.IsCheckedChanged         += OnStarsChanged;
        ConstellationsToggle.IsCheckedChanged += OnConstellationsChanged;
        StarLabelsToggle.IsCheckedChanged     += OnStarLabelsChanged;
        PlanetLabelsToggle.IsCheckedChanged   += OnPlanetLabelsChanged;
        HorizonGridToggle.IsCheckedChanged    += OnHorizonGridChanged;
        SunPathToggle.IsCheckedChanged        += OnSunPathChanged;
        MoonPathToggle.IsCheckedChanged       += OnMoonPathChanged;
        MagSlider.ValueChanged               += OnMagSliderChanged;
    }

    private void SyncToControl()
    {
        if (_skyControl is null) return;
        // Initialise toggles from control state
        StarsToggle.IsChecked         = _skyControl.StarMagnitudeLimit > DisabledStarMagnitude;
        ConstellationsToggle.IsChecked = _skyControl.ShowConstellations;
        StarLabelsToggle.IsChecked    = _skyControl.ShowStarLabels;
        PlanetLabelsToggle.IsChecked  = _skyControl.ShowPlanetLabels;
        HorizonGridToggle.IsChecked   = _skyControl.ShowHorizonGrid;
        SunPathToggle.IsChecked       = _skyControl.ShowSunPath;
        MoonPathToggle.IsChecked      = _skyControl.ShowMoonPath;
        MagSlider.Value               = _skyControl.StarMagnitudeLimit > 0 ? _skyControl.StarMagnitudeLimit : 5.5;
    }

    private void OnStarsChanged(object? sender, RoutedEventArgs e)
    {
        if (_skyControl is null) return;
        bool on = StarsToggle.IsChecked == true;
        _skyControl.StarMagnitudeLimit = on ? _lastMagLimit : DisabledStarMagnitude;
    }

    private void OnConstellationsChanged(object? sender, RoutedEventArgs e)
    {
        if (_skyControl is not null)
            _skyControl.ShowConstellations = ConstellationsToggle.IsChecked == true;
    }

    private void OnStarLabelsChanged(object? sender, RoutedEventArgs e)
    {
        if (_skyControl is not null)
            _skyControl.ShowStarLabels = StarLabelsToggle.IsChecked == true;
    }

    private void OnPlanetLabelsChanged(object? sender, RoutedEventArgs e)
    {
        if (_skyControl is not null)
            _skyControl.ShowPlanetLabels = PlanetLabelsToggle.IsChecked == true;
    }

    private void OnHorizonGridChanged(object? sender, RoutedEventArgs e)
    {
        if (_skyControl is not null)
            _skyControl.ShowHorizonGrid = HorizonGridToggle.IsChecked == true;
    }

    private void OnSunPathChanged(object? sender, RoutedEventArgs e)
    {
        if (_skyControl is not null)
            _skyControl.ShowSunPath = SunPathToggle.IsChecked == true;
    }

    private void OnMoonPathChanged(object? sender, RoutedEventArgs e)
    {
        if (_skyControl is not null)
            _skyControl.ShowMoonPath = MoonPathToggle.IsChecked == true;
    }

    private void OnMagSliderChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        double val = Math.Round(e.NewValue, 1);
        MagLabel.Text = val.ToString("F1");
        if (_skyControl is not null && StarsToggle.IsChecked == true)
        {
            _lastMagLimit = val;
            _skyControl.StarMagnitudeLimit = val;
        }
    }
}
