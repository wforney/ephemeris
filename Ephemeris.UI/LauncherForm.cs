// Updated: 2026-03-10
namespace Ephemeris.UI;

/// <summary>
/// Simple launcher form that opens either the 3D sky view or the 2D altitude chart.
/// </summary>
internal sealed class LauncherForm : Form
{
    public LauncherForm()
    {
        Text          = "Ephemeris";
        Width         = 360;
        Height        = 200;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox   = false;
        StartPosition = FormStartPosition.CenterScreen;

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
            using var form = new SkyViewForm(longitude: 0.0, latitude: 51.5);
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
}
