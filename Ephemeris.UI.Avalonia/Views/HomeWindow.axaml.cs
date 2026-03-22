// Updated: 2026-03-22
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Home / launcher window — the application start screen.
/// Replaces <see cref="LauncherWindow"/> and provides:
/// <list type="bullet">
///   <item>New Research Session — opens <see cref="ResearchWorkspaceWindow"/> with default state.</item>
///   <item>Load Scriptural Event — opens <see cref="ScripturalEventLibraryWindow"/> then workspace.</item>
///   <item>Resume Previous Session — shows an open-file dialog for a <c>.json</c> session file.</item>
///   <item>Quick-start form — parses date, time, and location then opens the workspace.</item>
///   <item>Legacy 3D Sky View and Altitude Chart shortcuts.</item>
///   <item>Recent sessions panel — last three <c>.json</c> files from the sessions directory.</item>
/// </list>
/// </summary>
public partial class HomeWindow : Window
{
    private static readonly string SessionsDirectory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "EphemerisResearch",
            "sessions");

    /// <summary>Initialises the home window and populates the recent-sessions list.</summary>
    public HomeWindow()
    {
        InitializeComponent();

        NewSessionBtn.Click       += OnNewSessionClick;
        LoadScripturalBtn.Click   += OnLoadScripturalClick;
        ResumeSessionBtn.Click    += OnResumeSessionClick;
        LoadSkyBtn.Click          += OnLoadSkyClick;
        SkyViewBtn.Click          += OnSkyViewClick;
        AltitudeChartBtn.Click    += OnAltitudeChartClick;

        RecentSessionsList.DoubleTapped += OnRecentSessionDoubleTapped;

        PopulateRecentSessions();
    }

    // ── Primary action handlers ──────────────────────────────────────────────

    private void OnNewSessionClick(object? sender, RoutedEventArgs e)
    {
        var workspace = new ResearchWorkspaceWindow();
        workspace.Show();
    }

    private void OnLoadScripturalClick(object? sender, RoutedEventArgs e)
    {
        // Open a workspace and let the user pick a scriptural event from within it.
        // ResearchWorkspaceWindow's sidebar "📜 Scriptural Events" button opens the library
        // and applies the chosen scenario directly to its SkyViewModel.
        var workspace = new ResearchWorkspaceWindow();
        workspace.Show();
    }

    private async void OnResumeSessionClick(object? sender, RoutedEventArgs e)
    {
        var options = new FilePickerOpenOptions
        {
            Title = "Resume Research Session",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Session files")
                {
                    Patterns = ["*.json"]
                }
            ]
        };

        var files = await StorageProvider.OpenFilePickerAsync(options);
        if (files is { Count: > 0 })
        {
            OpenSessionFile(files[0].Path.LocalPath);
        }
    }

    private void OnLoadSkyClick(object? sender, RoutedEventArgs e)
    {
        var workspace = new ResearchWorkspaceWindow();
        // TODO: parse QuickDateInput.Text, QuickTimeInput.Text, QuickLocationInput.Text
        //       and pre-populate the workspace toolbar / load sky data.
        workspace.Show();
    }

    // ── Legacy shortcuts ─────────────────────────────────────────────────────

    private void OnSkyViewClick(object? sender, RoutedEventArgs e)
    {
        // TODO: persist and restore last-used observer coordinates instead of hard-coded defaults.
        const double defaultLongitude = 0.0;
        const double defaultLatitude  = 51.5;  // London
        var skyWindow = new SkyViewWindow(defaultLongitude, defaultLatitude, default);
        skyWindow.Show();
    }

    private void OnAltitudeChartClick(object? sender, RoutedEventArgs e)
    {
        // TODO: populate with a real EphemerisRecord dataset from the active session.
        var plotWindow = new EphemerisPlotWindow([], "Sun");
        plotWindow.Show();
    }

    // ── Recent sessions ──────────────────────────────────────────────────────

    private void OnRecentSessionDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (RecentSessionsList.SelectedItem is RecentSessionItem item)
        {
            OpenSessionFile(item.FilePath);
        }
    }

    private void PopulateRecentSessions()
    {
        if (!Directory.Exists(SessionsDirectory))
        {
            return;
        }

        var recentFiles = new DirectoryInfo(SessionsDirectory)
            .GetFiles("*.json")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Take(3)
            .Select(f => new RecentSessionItem(
                Name: Path.GetFileNameWithoutExtension(f.Name),
                DateDisplay: f.LastWriteTimeUtc.ToString("yyyy-MM-dd HH:mm"),
                FilePath: f.FullName))
            .ToList();

        RecentSessionsList.ItemsSource = recentFiles;
    }

    private void OpenSessionFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        var workspace = new ResearchWorkspaceWindow();
        // TODO: deserialise filePath into a SessionModel and pass it to the workspace.
        workspace.Show();
    }

    // ── Supporting data type ─────────────────────────────────────────────────

    /// <summary>View-model item for a single entry in the recent sessions list.</summary>
    /// <param name="Name">Human-readable session name (file name without extension).</param>
    /// <param name="DateDisplay">Formatted last-modified date/time string.</param>
    /// <param name="FilePath">Full path to the <c>.json</c> session file.</param>
    private record RecentSessionItem(string Name, string DateDisplay, string FilePath);
}
