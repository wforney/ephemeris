// Updated: 2026-03-10
using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using Ephemeris.UI.Messages;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Application launcher window.
/// Allows the user to open either the 3D sky view or the 2D altitude chart.
/// Implements <see cref="IRecipient{TMessage}"/> for <see cref="ObserverChangedMessage"/>
/// and <see cref="SimTimeChangedMessage"/> so that observer location and simulation time
/// are remembered between sky view sessions without coupling the two windows directly.
/// </summary>
public partial class LauncherWindow : Window,
    IRecipient<ObserverChangedMessage>,
    IRecipient<SimTimeChangedMessage>
{
    // Last-used state — restored to SkyViewWindow on next open
    private double _lastLongitude = 0.0;
    private double _lastLatitude  = 51.5;
    private DateTime _lastSimTime = default;

    /// <summary>Initialises the launcher window and registers with the message bus.</summary>
    public LauncherWindow()
    {
        InitializeComponent();
        WeakReferenceMessenger.Default.RegisterAll(this);
        Closed += (_, _) => WeakReferenceMessenger.Default.UnregisterAll(this);

        SkyViewBtn.Click += OnSkyViewClick;
        PlotBtn.Click    += OnPlotClick;
        ResearchBtn.Click += OnResearchClick;
    }

    private async void OnSkyViewClick(object? sender, RoutedEventArgs e)
    {
        var skyWindow = new SkyViewWindow(_lastLongitude, _lastLatitude, _lastSimTime);
        await skyWindow.ShowDialog(this);
    }

    private async void OnPlotClick(object? sender, RoutedEventArgs e)
    {
        List<EphemerisRecord> allData = [];
        var plotWindow = new EphemerisPlotWindow(allData, "Sun");
        await plotWindow.ShowDialog(this);
    }

    private async void OnResearchClick(object? sender, RoutedEventArgs e)
    {
        var researchWindow = new ResearchWorkspaceWindow();
        await researchWindow.ShowDialog(this);
    }

    /// <inheritdoc/>
    public void Receive(ObserverChangedMessage message)
    {
        _lastLongitude = message.Value.Longitude;
        _lastLatitude  = message.Value.Latitude;
    }

    /// <inheritdoc/>
    public void Receive(SimTimeChangedMessage message) =>
        _lastSimTime = message.Value;
}
