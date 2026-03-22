// Updated: 2026-03-22
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Side-by-side comparison window that displays normal celestial motion alongside a
/// simulated override (freeze, reverse, or extended daylight). Shows
/// <see cref="Controls.EmptyStateControl"/> in the simulated panel until at least one
/// override is activated.
/// </summary>
public partial class ComparisonWindow : Window
{
    /// <summary>Initialises the comparison window.</summary>
    public ComparisonWindow()
    {
        InitializeComponent();
        ApplyOverridesBtn.Click += OnApplyOverrides;

        FreezeMotionCheck.IsCheckedChanged  += OnOverrideCheckChanged;
        ReverseMotionCheck.IsCheckedChanged += OnOverrideCheckChanged;
        ExtendDaylightCheck.IsCheckedChanged += OnOverrideCheckChanged;
    }

    private void OnOverrideCheckChanged(object? sender, RoutedEventArgs e) =>
        RefreshSimulationState();

    private void OnApplyOverrides(object? sender, RoutedEventArgs e) =>
        RefreshSimulationState();

    private void RefreshSimulationState()
    {
        var hasOverrides = FreezeMotionCheck.IsChecked == true
                        || ReverseMotionCheck.IsChecked == true
                        || ExtendDaylightCheck.IsChecked == true;

        SimulationEmptyState.IsVisible = !hasOverrides;
        SimulationCanvas.IsVisible     = hasOverrides;

        // TODO: apply simulation overrides to the celestial engine and redraw canvas.
    }
}
