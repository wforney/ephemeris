// Updated: 2026-03-22
using Avalonia.Controls;
using Avalonia.Interactivity;
using Ephemeris.UI.Models;
using Ephemeris.UI.ViewModels;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Modal window that presents the built-in scriptural event library.
/// Each card shows the event name, scripture reference, description, and location;
/// a "Load Event" button applies the scenario to the workspace.
/// </summary>
/// <remarks>
/// Iterates <see cref="ScenarioModel.BuiltInScenarios.All"/> dynamically via an
/// <c>ItemsControl</c> so that new scenarios added to the catalog appear automatically.
/// </remarks>
public partial class ScripturalEventLibraryWindow : Window
{
    private readonly WorkspaceViewModel _vm;

    /// <summary>
    /// Initialises the library window.
    /// </summary>
    /// <param name="vm">The research workspace view-model.</param>
    public ScripturalEventLibraryWindow(WorkspaceViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        ScenarioList.ItemsSource = ScenarioModel.BuiltInScenarios.All;
        CloseBtn.Click += (_, _) => Close();
    }

    private void OnLoadEventClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ScenarioModel scenario })
        {
            _vm.LoadScenarioCommand.Execute(scenario);
            Close();
        }
    }
}
