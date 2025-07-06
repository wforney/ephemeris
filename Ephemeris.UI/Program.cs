using SkiaSharp;

namespace Ephemeris.UI;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        var sunData = EphemerisData.GetEphemerisData("Sun", 2023, 1, 1, 2023, 12, 31);
        var moonData = EphemerisData.GetEphemerisData("Moon", 2023, 1, 1, 2023, 12, 31);
        var marsData = EphemerisData.GetEphemerisData("Mars", 2023, 1, 1, 2023, 12, 31);

        var allData = sunData.Concat(moonData).Concat(marsData).ToList();

        var plotForm = new EphemerisPlotForm(allData, "Sun");

        Application.Run(plotForm);
    }
}
