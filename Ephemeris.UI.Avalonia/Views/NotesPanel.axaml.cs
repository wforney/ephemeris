// Updated: 2026-03-22
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Ephemeris.UI;
using Ephemeris.UI.Models;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Research notes panel — a lightweight window for taking notes during a session.
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description>Session name is bound to a local <see cref="SessionModel"/>.</description></item>
///   <item><description>"Save Time Marker" appends a UTC timestamp to the notes.</description></item>
///   <item><description>"Save Session" serialises the session to a JSON file via <see cref="SessionModel.SaveAsync"/>.</description></item>
///   <item><description>"Export Notes" writes a plain-text summary to a <c>.txt</c> file.</description></item>
/// </list>
/// </remarks>
public partial class NotesPanel : Window
{
    private readonly SkyViewModel _vm;
    private readonly SessionModel _session;

    /// <summary>
    /// Initialises the notes panel.
    /// </summary>
    /// <param name="vm">The sky view-model providing observer location and simulated time.</param>
    public NotesPanel(SkyViewModel vm)
    {
        InitializeComponent();

        _vm = vm;

        // Build a session snapshot from the current VM state
        _session = new SessionModel
        {
            SimTime   = vm.SimTime,
            Longitude = vm.Longitude,
            Latitude  = vm.Latitude,
        };

        // Wire text boxes to the session model (two-way, manual sync)
        SessionNameBox.Text = _session.Name;
        NotesBox.Text       = _session.Notes;

        SessionNameBox.TextChanged += (_, _) => _session.Name  = SessionNameBox.Text ?? string.Empty;
        NotesBox.TextChanged       += (_, _) => _session.Notes = NotesBox.Text       ?? string.Empty;

        SaveMarkerBtn.Click   += OnSaveMarkerClick;
        SaveSessionBtn.Click  += OnSaveSessionClick;
        ExportNotesBtn.Click  += OnExportNotesClick;
    }

    // ─────────────────────────────────────────────────────────────────────
    // Button handlers
    // ─────────────────────────────────────────────────────────────────────

    private void OnSaveMarkerClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var timestamp = DateTime.UtcNow;
        var marker = $"\n[Marker: {timestamp:yyyy-MM-dd HH:mm} UTC]";

        NotesBox.Text = (NotesBox.Text ?? string.Empty) + marker;
        MarkerTimestampLabel.Text = $"timestamp: {timestamp:yyyy-MM-dd HH:mm} UTC";
    }

    private async void OnSaveSessionClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title                    = "Save Session",
            SuggestedFileName        = _session.Name,
            DefaultExtension         = "json",
            FileTypeChoices          =
            [
                new FilePickerFileType("Session JSON") { Patterns = ["*.json"] },
                new FilePickerFileType("All Files")    { Patterns = ["*"] },
            ],
        }).ConfigureAwait(true);

        if (file is null) return;

        try
        {
            _session.SimTime   = _vm.SimTime;
            _session.Longitude = _vm.Longitude;
            _session.Latitude  = _vm.Latitude;

            await using var stream = await file.OpenWriteAsync().ConfigureAwait(true);
            await _session.SaveAsync(stream).ConfigureAwait(true);
            Title = $"Research Notes — saved {DateTime.UtcNow:HH:mm:ss UTC}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to save session:\n{ex.Message}").ConfigureAwait(true);
        }
    }

    private async void OnExportNotesClick(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        var topLevel = GetTopLevel(this);
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title                    = "Export Notes",
            SuggestedFileName        = _session.Name,
            DefaultExtension         = "txt",
            FileTypeChoices          =
            [
                new FilePickerFileType("Text Files") { Patterns = ["*.txt"] },
                new FilePickerFileType("All Files")  { Patterns = ["*"] },
            ],
        }).ConfigureAwait(true);

        if (file is null) return;

        try
        {
            var text = BuildExportText();
            await using var stream = await file.OpenWriteAsync().ConfigureAwait(true);
            stream.SetLength(0);
            using var writer = new global::System.IO.StreamWriter(stream);
            await writer.WriteAsync(text).ConfigureAwait(true);
            await writer.FlushAsync().ConfigureAwait(true);
            Title = $"Research Notes — exported {DateTime.UtcNow:HH:mm:ss UTC}";
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Failed to export notes:\n{ex.Message}").ConfigureAwait(true);
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────

    private string BuildExportText()
    {
        var lon = _vm.Longitude;
        var lat = _vm.Latitude;

        var lonHemisphere = lon >= 0 ? "E" : "W";
        var latHemisphere = lat >= 0 ? "N" : "S";

        return $"""
            Session: {_session.Name}
            Exported: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC
            Sim Time: {_vm.SimTime:yyyy-MM-dd HH:mm} UTC
            Location: {Math.Abs(lon):F4}° {lonHemisphere}, {Math.Abs(lat):F4}° {latHemisphere}

            Notes:
            {_session.Notes}
            """;
    }

    private async Task ShowErrorAsync(string message)
    {
        var dialog = new Window
        {
            Title  = "Error",
            Width  = 360,
            Height = 160,
            Content = new TextBlock
            {
                Text         = message,
                Margin       = new global::Avalonia.Thickness(16),
                TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            },
        };
        await dialog.ShowDialog(this).ConfigureAwait(true);
    }
}
