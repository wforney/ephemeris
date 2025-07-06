using DotNext;

namespace Ephemeris.UI;

public partial class EphemerisPlotForm : Form
{
    private readonly ScottPlot.WinForms.FormsPlot formsPlot;

    public EphemerisPlotForm(IEnumerable<EphemerisRecord> records, string body)
    {
        InitializeComponent();

        Text = $"Altitude Plot for {body}";
        Width = 800;
        Height = 600;

        formsPlot = new ScottPlot.WinForms.FormsPlot
        {
            Dock = DockStyle.Fill
        };

        Controls.Add(formsPlot);

        PlotAltitude(records, body);
    }

    private void PlotAltitude(IEnumerable<EphemerisRecord> records, string body)
    {
        var data = records
            .Where(r => r.Body.Equals(body, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.TimeUtc)
            .ToList();

        if (data.Count == 0)
        {
            _ = MessageBox.Show($"No data for {body}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        double[] times = data.Select(r => r.TimeUtc.ToOADate()).ToArray();
        double[] altitudes = data.Select(r => r.Altitude).ToArray();

        ScottPlot.Plot plt = formsPlot.Plot;
        plt.Clear();


        plt.Add.Scatter(times, altitudes);
        plt.Title($"{body} Altitude vs Time");
        plt.YLabel("Altitude (degrees)");
        _ = plt.Add.Legend();

        plt.Axes.AutoScale();
        formsPlot.Refresh();
    }
}
