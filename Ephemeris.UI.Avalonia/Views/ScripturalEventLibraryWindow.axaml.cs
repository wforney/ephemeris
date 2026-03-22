// Updated: 2026-03-22
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Displays the library of built-in scriptural event scenarios (e.g. Joshua's Long Day,
/// Hezekiah's Sundial). The user selects an event and presses <em>Open Event</em> to load
/// it into a <see cref="ResearchWorkspaceWindow"/>.
/// Shows <see cref="Controls.EmptyStateControl"/> when no scenarios are configured.
/// </summary>
public partial class ScripturalEventLibraryWindow : Window
{
    /// <summary>Gets the scenario the user confirmed, or <see langword="null"/> if cancelled.</summary>
    public object? SelectedScenario { get; private set; }

    /// <summary>Initialises the library window and populates the events list.</summary>
    public ScripturalEventLibraryWindow()
    {
        InitializeComponent();
        CancelBtn.Click += (_, _) => Close();
        OpenBtn.Click += OnOpenClick;
        EventsList.SelectionChanged += OnSelectionChanged;

        PopulateEvents();
    }

    private void PopulateEvents()
    {
        // TODO: replace with real BuiltInScenarios.All enumeration.
        var scenarios = Array.Empty<object>();

        EventsEmptyState.IsVisible = scenarios.Length == 0;
        EventsList.IsVisible       = scenarios.Length > 0;
        EventsList.ItemsSource     = scenarios;
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e) =>
        OpenBtn.IsEnabled = EventsList.SelectedItem is not null;

    private void OnOpenClick(object? sender, RoutedEventArgs e)
    {
        SelectedScenario = EventsList.SelectedItem;
        Close();
    }
}
