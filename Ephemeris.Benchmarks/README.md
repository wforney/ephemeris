# Ephemeris.Benchmarks

Performance benchmarks for the `Ephemeris` core library using [BenchmarkDotNet](https://benchmarkdotnet.org).

**Framework:** BenchmarkDotNet 0.15.8 · **Target:** `net10.0`

---

## Running Benchmarks

Benchmarks **must be run in Release configuration**. Debug builds produce meaningless numbers.

```bash
# Run all benchmarks (interactive menu)
dotnet run --project Ephemeris.Benchmarks/Ephemeris.Benchmarks.csproj -c Release

# Run a specific benchmark class
dotnet run --project Ephemeris.Benchmarks/Ephemeris.Benchmarks.csproj -c Release \
  -- --filter "*SunEphemeris*"

# Export results to CSV and HTML
dotnet run --project Ephemeris.Benchmarks/Ephemeris.Benchmarks.csproj -c Release \
  -- --exporters csv html
```

Results are written to `BenchmarkDotNet.Artifacts/` in the project directory.

---

## Benchmark Classes

All benchmarks use `[MemoryDiagnoser]` to report allocation alongside timing.

### `SunEphemerisBenchmark`

| Benchmark | What it measures |
|-----------|-----------------|
| `SingleCall` | One `SunEphemeris.ApparentEquatorialCoordinates(T)` call |
| `Batch1440` | `EphemerisBatch.GenerateSunSeries` — 1440 records (1 per minute for 24 h) |

### `MoonEphemerisBenchmark`

| Benchmark | What it measures |
|-----------|-----------------|
| `SingleCall` | One `MoonEphemeris.GeocentricEquatorialCoordinates(T)` call |
| `Batch1440` | `EphemerisBatch.GenerateMoonSeries` — 1440 records |

### `PlanetBenchmark`

| Benchmark | What it measures |
|-----------|-----------------|
| `AllPlanets1440` | All 8 planets (Mercury → Pluto) × 1440 records each |

---

## Adding Benchmarks

1. Add a new class to `Benchmarks.cs` (or a new `.cs` file).
2. Annotate the class with `[MemoryDiagnoser]` and methods with `[Benchmark]`.
3. Use `[GlobalSetup]` to prepare expensive state (e.g. pre-computing Julian Century).

```csharp
[MemoryDiagnoser]
public class MyBenchmark
{
    private double _T;

    [GlobalSetup]
    public void Setup() => _T = TimeUtils.JulianCentury(TimeUtils.JulianDay(2024, 6, 21, 12.0));

    [Benchmark]
    public double MyCalculation() => SomeCalculator.Compute(_T);
}
```

---

## Interpreting Results

| Column | Meaning |
|--------|---------|
| Mean | Average execution time per operation |
| Error | Half the 99.9% confidence interval |
| StdDev | Standard deviation |
| Gen0 / Gen1 | GC collections per 1000 operations |
| Alloc | Bytes allocated per operation |

A good calculation function should show **0 B** allocation (no heap pressure). The `EphemerisBatch` generators allocate per record (expected).

---

## Further Reading

- [Algorithm Reference](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference) — algorithm complexity notes per domain
- [Root README](../README.md) — solution overview
