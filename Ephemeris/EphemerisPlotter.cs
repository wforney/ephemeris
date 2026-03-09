using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ephemeris;

/// <summary>
/// Provides console-based ASCII plotting of celestial body positions.
/// </summary>
public static class EphemerisPlotter
{
    /// <summary>
    /// Plots altitude over time as an ASCII chart in the console.
    /// </summary>
    /// <param name="records">The collection of ephemeris records to plot.</param>
    /// <param name="body">The name of the celestial body to filter and plot.</param>
    public static void PlotAltitudes(IEnumerable<EphemerisRecord> records, string body)
    {
        var data = records
            .Where(r => r.Body.Equals(body, StringComparison.OrdinalIgnoreCase))
            .OrderBy(r => r.TimeUtc)
            .ToList();

        if (data.Count == 0)
        {
            Console.WriteLine($"No data found for body: {body}");
            return;
        }

        Console.WriteLine($"Altitude over time for {body}:");

        double minAlt = data.Min(r => r.Altitude);
        double maxAlt = data.Max(r => r.Altitude);
        int chartHeight = 10;

        foreach (var record in data)
        {
            double scaled = (record.Altitude - minAlt) / (maxAlt - minAlt);
            int level = (int)(scaled * chartHeight);
            Console.WriteLine($"{record.TimeUtc:HH:mm} | " + new string(' ', level) + "*");
        }

        Console.WriteLine();
    }
}
