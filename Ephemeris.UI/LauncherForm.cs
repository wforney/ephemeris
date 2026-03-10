// Updated: 2026-03-10
using CommunityToolkit.Mvvm.Messaging;
using Ephemeris.UI.Messages;

namespace Ephemeris.UI;

/// <summary>
/// Simple launcher form that opens either the 3D sky view or the 2D altitude chart.
/// Implements <see cref="IRecipient{TMessage}"/> for <see cref="ObserverChangedMessage"/>
/// and <see cref="SimTimeChangedMessage"/> so that observer location and simulation time
/// are remembered between sky view sessions without coupling the two form types directly.
/// </summary>
internal sealed class LauncherForm : Form,
    IRecipient<ObserverChangedMessage>,
    IRecipient<SimTimeChangedMessage>
{
    // Last-used state — restored to SkyViewForm on next open
    private double _lastLongitude = 0.0;
    private double _lastLatitude  = 51.5;
    private DateTime _lastSimTime = default; // default → SkyViewModel uses UtcNow

    public LauncherForm()
    {
        Text            = "Ephemeris";
        Width           = 360;
        Height          = 200;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox     = false;
        StartPosition   = FormStartPosition.CenterScreen;

        // Register with the message bus to track observer location and sim time
        // across sky view sessions. Unregister when this form closes.
        WeakReferenceMessenger.Default.RegisterAll(this);
        FormClosed += (_, _) => WeakReferenceMessenger.Default.UnregisterAll(this);

        var layout = new TableLayoutPanel
        {
            Dock        = DockStyle.Fill,
            RowCount    = 3,
            ColumnCount = 1,
            Padding     = new Padding(24, 16, 24, 16),
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 40));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));

        var title = new Label
        {
            Text      = "Ephemeris — Astronomical Calculator",
            Dock      = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font      = new Font(Font.FontFamily, 10f, FontStyle.Bold),
        };
        layout.Controls.Add(title, 0, 0);

        var skyBtn = new Button
        {
            Text = "🌌  3D Sky View  (OpenGL)",
            Dock = DockStyle.Fill,
        };
        skyBtn.Click += (_, _) =>
        {
            Hide();
            // Resume at the last-used observer location and simulation time.
            using var form = new SkyViewForm(_lastLongitude, _lastLatitude, _lastSimTime);
            form.ShowDialog(this);
            Show();
        };
        layout.Controls.Add(skyBtn, 0, 1);

        var plotBtn = new Button
        {
            Text = "📈  Altitude Chart  (ScottPlot)",
            Dock = DockStyle.Fill,
        };
        plotBtn.Click += (_, _) =>
        {
            List<EphemerisRecord> allData = [];
            using var form = new EphemerisPlotForm(allData, "Sun");
            form.ShowDialog(this);
        };
        layout.Controls.Add(plotBtn, 0, 2);

        Controls.Add(layout);
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
