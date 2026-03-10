// Updated: 2026-03-10
using Avalonia.Controls;

namespace Ephemeris.UI.Avalonia.Views;

/// <summary>
/// Altitude-vs-time scatter chart for a single celestial body using
/// <b>ScottPlot.Avalonia</b> — the cross-platform equivalent of the
/// WinForms <c>EphemerisPlotForm</c>.
/// </summary>
/// <remarks>
/// The X axis uses OLE Automation dates (<see cref="DateTime.ToOADate"/>) so that
/// ScottPlot's built-in date axis formatter produces readable tick labels.
/// </remarks>
public partial class EphemerisPlotWindow : Window
{
    /// <summary>
    /// Initialises the window and renders the altitude series for <paramref name="body"/>.
    /// </summary>
    /// <param name="records">Pre-computed ephemeris records.</param>
    /// <param name="body">The body name to filter from <paramref name="records"/>.</param>
    public EphemerisPlotWindow(IEnumerable<EphemerisRecord> records, string body)
    {
        InitializeComponent();
        Title = $"Altitude Plot for {body}";
        PlotAltitude(records, body);
    }

    private void PlotAltitude(IEnumerable<EphemerisRecord> records, string body)
    {
        var data = records
            .Where(r => r.Body.Equals(body, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.TimeUtc)
            .ToList();

        if (data.Count == 0)
            return;

        double[] times     = data.Select(r => r.TimeUtc.ToOADate()).ToArray();
        double[] altitudes = data.Select(r => r.Altitude).ToArray();

        ScottPlot.Plot plt = AvaPlot.Plot;
        plt.Clear();
        plt.Add.Scatter(times, altitudes);
        plt.Title($"{body} Altitude vs Time");
        plt.YLabel("Altitude (degrees)");
        _ = plt.Add.Legend();
        plt.Axes.AutoScale();
        AvaPlot.Refresh();
    }
}
