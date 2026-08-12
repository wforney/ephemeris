<!-- Updated: 2026-08-12 -->
---
mode: agent
model: anthropic/claude-opus-4.5
tools: [codebase, editFiles, runCommands, fetch]
description: Implement the SPICE kernel and DE430 provider stubs to enable high-precision ephemeris data loading.
---

You are implementing the SPICE / DE430 import pipeline in the Ephemeris .NET 10 library.

## Current state

Three internal stub interfaces in `Ephemeris/Import/SpiceKernelDatabase.cs` all throw `NotImplementedException`:

```csharp
internal interface ISpaceKernelProvider  { void Load(string kernelPath); }
internal interface ITimeConverter        { double UtcToEt(DateTime utc); }
internal interface IStateVectorProvider  { double[] GetStateVector(string target, double et, string frame, string observer); }
```

The NuGet package `SpiceSharp-Parser` v3.2.5 is already referenced in `Ephemeris.csproj`.

## Implementation steps

### 1. Research SpiceSharp-Parser API
Use the `fetch` tool to retrieve:
- https://github.com/SpiceSharp/SpiceSharp — understand the SpiceSharp core API
- https://github.com/SpiceSharp/SpiceSharpParser — understand the parser layer
- Check NuGet for the latest compatible SpiceSharp-Parser version

### 2. Implement DefaultKernelProvider
Load `.bsp` / `.bpc` binary SPICE kernel files into a SpiceSharp simulation context.

### 3. Implement DefaultTimeConverter
Convert `DateTime` UTC → SPICE Ephemeris Time (ET = seconds past J2000.0):
```
ET = (JD − 2451545.0) × 86400.0   (approximate; ignore leap seconds for now)
```
Note this in XML docs and a `// TODO: incorporate IERS leap-second table` comment.

### 4. Implement DefaultStateVectorProvider
Query the loaded kernel for the Cartesian state vector `[x, y, z]` in the J2000 frame (AU or km — normalise to km).

## Coordinate conventions to preserve
- `BspImporter.CartesianToRaDec` expects `double[3]` in km, J2000 equatorial frame
- RA output: degrees [0, 360)
- Dec output: degrees [−90, 90]

## Validation
After implementing, validate against JPL Horizons:
1. Use the `fetch` tool to query https://ssd.jpl.nasa.gov/api/horizons.api for a known body/date
2. Compare RA/Dec from your implementation within 0.01° tolerance
3. Write at least one TUnit test with the Horizons reference value

## Commit
```
feat(import): implement SPICE provider stubs via SpiceSharp-Parser

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```
