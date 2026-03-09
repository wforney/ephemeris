using Ephemeris.Export;

namespace Ephemeris.Tests;

/// <summary>Integration tests for <see cref="EphemerisBatch"/> and <see cref="EphemerisExporter"/>.</summary>
public class BatchExportTests
{
    private static readonly DateTime s_start = new(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
    private const double Lon = -87.65;
    private const double Lat = 41.85;

    // ── EphemerisBatch ───────────────────────────────────────────────────────

    [Test]
    public async Task GenerateSunSeries_Returns24Records()
    {
        var records = EphemerisBatch.GenerateSunSeries(s_start, intervalMinutes: 60, count: 24, Lon, Lat);
        await Assert.That(records.Count).IsEqualTo(24);
    }

    [Test]
    public async Task GenerateSunSeries_BodyIsSun()
    {
        var records = EphemerisBatch.GenerateSunSeries(s_start, 60, 3, Lon, Lat);
        foreach (var r in records)
            await Assert.That(r.Body).IsEqualTo("Sun");
    }

    [Test]
    public async Task GenerateSunSeries_CoordinatesInRange()
    {
        var records = EphemerisBatch.GenerateSunSeries(s_start, 60, 24, Lon, Lat);
        foreach (var r in records)
        {
            await Assert.That(r.RightAscension).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(r.RightAscension).IsLessThan(360.0);
            await Assert.That(r.Declination).IsGreaterThanOrEqualTo(-90.0);
            await Assert.That(r.Declination).IsLessThanOrEqualTo(90.0);
            await Assert.That(r.Azimuth).IsGreaterThanOrEqualTo(0.0);
            await Assert.That(r.Azimuth).IsLessThan(360.0);
            await Assert.That(r.Altitude).IsGreaterThanOrEqualTo(-90.0);
            await Assert.That(r.Altitude).IsLessThanOrEqualTo(90.0);
        }
    }

    [Test]
    public async Task GenerateMoonSeries_IlluminationInRange()
    {
        var records = EphemerisBatch.GenerateMoonSeries(s_start, 60, 24, Lon, Lat);
        await Assert.That(records.Count).IsEqualTo(24);
        foreach (var r in records)
        {
            if (r.Illumination.HasValue)
            {
                await Assert.That(r.Illumination.Value).IsGreaterThanOrEqualTo(0.0);
                await Assert.That(r.Illumination.Value).IsLessThanOrEqualTo(1.0);
            }
        }
    }

    [Test]
    public async Task GenerateMoonSeries_DistanceInReasonableRange()
    {
        var records = EphemerisBatch.GenerateMoonSeries(s_start, 60, 1, Lon, Lat);
        var r = records[0];
        // Moon distance: perigee ~356,500 km, apogee ~406,700 km
        if (r.Distance.HasValue)
        {
            await Assert.That(r.Distance.Value).IsGreaterThan(340_000.0);
            await Assert.That(r.Distance.Value).IsLessThan(420_000.0);
        }
    }

    [Test]
    public async Task GeneratePlanetSeries_Mars_Returns10Records()
    {
        var records = EphemerisBatch.GeneratePlanetSeries("mars", s_start, 60, 10, Lon, Lat);
        await Assert.That(records.Count).IsEqualTo(10);
        foreach (var r in records)
            await Assert.That(r.Body).IsEqualTo("mars");
    }

    // ── EphemerisExporter ────────────────────────────────────────────────────

    [Test]
    public async Task ToCsv_ThreeRecords_HasHeaderAndThreeDataRows()
    {
        var records = EphemerisBatch.GenerateSunSeries(s_start, 60, 3, Lon, Lat);
        string csv = EphemerisExporter.ToCsv(records);
        string[] lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // 1 header + 3 data rows
        await Assert.That(lines.Length).IsEqualTo(4);
        // Header should contain known field names
        await Assert.That(lines[0]).Contains("RightAscension");
        await Assert.That(lines[0]).Contains("Altitude");
    }

    [Test]
    public async Task ToJson_ThreeRecords_IsValidJson()
    {
        var records = EphemerisBatch.GenerateSunSeries(s_start, 60, 3, Lon, Lat);
        string json = EphemerisExporter.ToJson(records);
        // Basic sanity: non-empty, starts and ends with array brackets
        await Assert.That(json.TrimStart()).StartsWith("[");
        await Assert.That(json.TrimEnd()).EndsWith("]");
    }

    [Test]
    public async Task SaveCsvToFile_WritesAndReadsFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"ephemeris_test_{Guid.NewGuid():N}.csv");
        try
        {
            var records = EphemerisBatch.GenerateSunSeries(s_start, 60, 2, Lon, Lat);
            EphemerisExporter.SaveCsvToFile(records, path);
            await Assert.That(File.Exists(path)).IsTrue();
            string content = File.ReadAllText(path);
            await Assert.That(content).Contains("Sun");
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
