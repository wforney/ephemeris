<!-- Updated: 2026-08-12 -->
---
mode: agent
model: anthropic/claude-sonnet-4-5
tools: [codebase, editFiles, runCommands]
description: Run BenchmarkDotNet benchmarks, interpret results, identify bottlenecks, and propose perf-type fixes for the Ephemeris library.
---

You are profiling and optimizing the Ephemeris .NET 10 library using BenchmarkDotNet.

## Important: always run in Release configuration

BenchmarkDotNet requires Release builds. Debug builds produce meaningless results.

```bash
dotnet run --project Ephemeris.Benchmarks -c Release
```

To run a single benchmark class:
```bash
dotnet run --project Ephemeris.Benchmarks -c Release -- --filter "*<ClassName>*"
```

To run a quick, lower-precision run for iteration (not for reporting):
```bash
dotnet run --project Ephemeris.Benchmarks -c Release -- --job Short --filter "*<ClassName>*"
```

## Step 1 — Identify the target

Read the user's request or the changed code and decide which benchmark class is relevant. Benchmark classes live in `Ephemeris.Benchmarks/`. If no benchmark exists for the area under investigation, create one (see Step 2b).

## Step 2a — Run existing benchmarks

```bash
dotnet run --project Ephemeris.Benchmarks -c Release -- --filter "*<Target>*" --exporters json
```

BenchmarkDotNet writes results to `BenchmarkDotNet.Artifacts/results/`. Read the JSON or Markdown summary after the run completes.

Key metrics to read:
| Metric | Meaning |
|--------|---------|
| **Mean** | Average execution time — primary optimization target |
| **Allocated** | Heap bytes allocated per operation — secondary target |
| **Gen0 / Gen1** | GC collections per 1000 operations — high values indicate allocation pressure |
| **Ratio** | Relative to baseline (1.00 = same as baseline) |

## Step 2b — Create a new benchmark (if needed)

Benchmark classes follow this pattern:

```csharp
using BenchmarkDotNet.Attributes;
using Ephemeris.Chronology;

namespace Ephemeris.Benchmarks;

[MemoryDiagnoser]
public class SunEphemerisBenchmarks
{
    private const double T = 0.0; // J2000.0

    [Benchmark(Baseline = true)]
    public (double RA, double Dec) ApparentCoordinates() =>
        SunEphemeris.ApparentEquatorialCoordinates(T);

    [Benchmark]
    public double EquationOfCentre() =>
        SunEphemeris.EquationOfCentre(0.0, T);
}
```

Rules:
- Always include `[MemoryDiagnoser]`.
- Mark the current implementation as `[Benchmark(Baseline = true)]`.
- Add a second `[Benchmark]` for each candidate optimization.
- Keep benchmarks **pure** — no I/O, no randomness, fixed inputs.

## Step 3 — Interpret results and identify bottlenecks

Common patterns and their causes:

| Symptom | Likely cause | Fix |
|---------|-------------|-----|
| High `Allocated` on pure math | Boxing value types, `params` arrays, LINQ over arrays | Inline loops, use `Span<T>`, avoid LINQ |
| High `Gen0` | Short-lived heap objects in hot path | `ArrayPool<T>`, `stackalloc`, struct returns |
| Mean ≫ expected for trig | Repeated `Math.ToRadians` wrapping same constant | Cache `double radLat = lat * Math.PI / 180` outside loop |
| Large `Ratio` for batch path | Repeated JD→T conversions per record | Precompute `T` once per batch, pass to inner loop |

## Step 4 — Propose and apply the fix

Apply the smallest optimization that achieves measurable improvement. Do **not**:
- Change the observable return values (astronomical accuracy must be preserved).
- Introduce unsafe code unless the gain is > 2× and the area is a proven hot path.
- Rewrite algorithms just for style.

After editing, re-run the benchmark to confirm improvement:
```bash
dotnet run --project Ephemeris.Benchmarks -c Release -- --filter "*<Target>*" --job Short
```

Confirm tests still pass:
```bash
dotnet test
```

## Step 5 — Commit

Include before/after numbers in the commit body:

```
perf(<scope>): <imperative description>

Before: Mean=120ms, Allocated=48KB, Gen0=23.4
After:  Mean=34ms,  Allocated=0B,   Gen0=0

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

Scope is the lowercase domain namespace (e.g., `heliology`, `selenography`, `batch`).

## Adding a new benchmark class

If you created a new benchmark class, add a row to the benchmark class table in `Ephemeris.Benchmarks/README.md`:

| Class | Method(s) | Domain |
|-------|-----------|--------|
| `SunEphemerisBenchmarks` | `ApparentCoordinates`, `EquationOfCentre` | Heliology |
