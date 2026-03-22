// Updated: 2026-03-22
using Avalonia.Controls;
using Avalonia.Interactivity;
using Ephemeris.UI.Models;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Modal window that presents the built-in scriptural event library.
/// Each card shows the event name, scripture reference, description, and location;
/// a "Load Event" button applies the scenario to the sky view-model and closes the window.
/// </summary>
/// <remarks>
/// Iterates <see cref="BuiltInScenarios.All"/> dynamically via an
/// <c>ItemsControl</c> so that new scenarios added to the catalog appear automatically.
/// </remarks>
public partial class ScripturalEventLibraryWindow : Window
{
    private readonly SkyViewModel _vm;

    /// <summary>
    /// Initialises the library window.
    /// </summary>
    /// <param name="vm">The sky view-model to update when a scenario is loaded.</param>
    public ScripturalEventLibraryWindow(SkyViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        ScenarioList.ItemsSource = BuiltInScenarios.All;
        CloseBtn.Click += (_, _) => Close();
    }

    private void OnLoadEventClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ScenarioModel scenario })
        {
            _vm.Longitude = scenario.Longitude;
            _vm.Latitude  = scenario.Latitude;
            _vm.SimTime   = scenario.SuggestedUtcTime;
            Close();
        }
    }
}
