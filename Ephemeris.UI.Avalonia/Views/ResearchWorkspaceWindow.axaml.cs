// Updated: 2026-03-22
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// The main research workspace window.
/// Provides a sidebar with celestial data and a toolbar for specifying date, time,
/// and observer location. Shows <see cref="Controls.EmptyStateControl"/> in the sidebar
/// until sky data has been loaded.
/// </summary>
public partial class ResearchWorkspaceWindow : Window
{
    /// <summary>Initialises the workspace with an optional pre-loaded session.</summary>
    public ResearchWorkspaceWindow()
    {
        InitializeComponent();
        LoadDataBtn.Click += OnLoadDataClick;
    }

    private void OnLoadDataClick(object? sender, RoutedEventArgs e)
    {
        // TODO: parse DateInput, TimeInput, LocationInput and populate CelestialDataList.
        // Once data arrives, toggle DataEmptyState.IsVisible = false and
        // DataScrollViewer.IsVisible = true.
    }
}
