using BenchmarkDotNet.Attributes;
using Ephemeris;
using Ephemeris.Heliology;
using Ephemeris.Selenography;
using Ephemeris.Chronology;

namespace Ephemeris.Benchmarks;

/// <summary>
/// Benchmarks for solar ephemeris calculations.
/// </summary>
[MemoryDiagnoser]
public class SunEphemerisBenchmark
{
    private double _T;

    [GlobalSetup]
    public void Setup() => _T = TimeUtils.JulianCentury(TimeUtils.JulianDay(2024, 6, 21, 12.0));

    [Benchmark(Description = "SunEphemeris single call")]
    public (double RA, double Dec) SingleCall()
        => SunEphemeris.ApparentEquatorialCoordinates(_T);

    [Benchmark(Description = "EphemerisBatch.GenerateSunSeries 1440 records")]
    public List<EphemerisRecord> Batch1440()
        => EphemerisBatch.GenerateSunSeries(
            new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc),
            intervalMinutes: 1, count: 1440,
            longitude: -87.65, latitude: 41.85);
}

/// <summary>
/// Benchmarks for lunar ephemeris calculations.
/// </summary>
[MemoryDiagnoser]
public class MoonEphemerisBenchmark
{
    private double _T;

    [GlobalSetup]
    public void Setup() => _T = TimeUtils.JulianCentury(TimeUtils.JulianDay(2024, 6, 21, 12.0));

    [Benchmark(Description = "MoonEphemeris single call")]
    public (double RA, double Dec, double Dist) SingleCall()
        => MoonEphemeris.GeocentricEquatorialCoordinates(_T);

    [Benchmark(Description = "EphemerisBatch.GenerateMoonSeries 1440 records")]
    public List<EphemerisRecord> Batch1440()
        => EphemerisBatch.GenerateMoonSeries(
            new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc),
            intervalMinutes: 1, count: 1440,
            longitude: -87.65, latitude: 41.85);
}

/// <summary>
/// Benchmarks for planetary ephemeris calculations (all 8 planets × 1440 records).
/// </summary>
[MemoryDiagnoser]
public class PlanetBenchmark
{
    private static readonly string[] s_planets =
        ["mercury", "venus", "mars", "jupiter", "saturn", "uranus", "neptune", "pluto"];

    [Benchmark(Description = "All planets 1440 records each")]
    public List<EphemerisRecord> AllPlanets1440()
    {
        var all = new List<EphemerisRecord>();
        var start = new DateTime(2024, 6, 21, 0, 0, 0, DateTimeKind.Utc);
        foreach (string planet in s_planets)
            all.AddRange(EphemerisBatch.GeneratePlanetSeries(
                planet, start, intervalMinutes: 1, count: 1440,
                longitude: -87.65, latitude: 41.85));
        return all;
    }
}
