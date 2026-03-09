using Ephemeris.Export;

namespace Ephemeris.Tests;

public static class EphemerisExampleRun
{
    public static void GenerateAndExport()
    {
        var startUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        int intervalMinutes = 60;
        int count = 24;
        double longitude = -122.4194; // San Francisco
        double latitude = 37.7749;

        IEnumerable<EphemerisRecord> sunData = EphemerisBatch.GenerateSunSeries(startUtc, intervalMinutes, count, longitude, latitude);
        IEnumerable<EphemerisRecord> moonData = EphemerisBatch.GenerateMoonSeries(startUtc, intervalMinutes, count, longitude, latitude);
        IEnumerable<EphemerisRecord> marsData = EphemerisBatch.GeneratePlanetSeries("Mars", startUtc, intervalMinutes, count, longitude, latitude);

        var allData = sunData.Concat(moonData).Concat(marsData).ToList();

        string exportFolder = "EphemerisOutput";
        _ = Directory.CreateDirectory(exportFolder);

        EphemerisExporter.SaveCsvToFile(allData, Path.Combine(exportFolder, "ephemeris_data.csv"));
        EphemerisExporter.SaveJsonToFile(allData, Path.Combine(exportFolder, "ephemeris_data.json"), indented: true);
    }
}
