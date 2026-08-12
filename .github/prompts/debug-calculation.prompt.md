<!-- Updated: 2026-08-12 -->
---
mode: agent
model: anthropic/claude-opus-4-5
tools: [codebase, editFiles, runCommands, fetch]
description: Debug incorrect astronomical calculation results by comparing against JPL Horizons, IERS tables, or other authoritative reference sources.
---

You are debugging an incorrect astronomical calculation in the Ephemeris .NET 10 library.

## Goal

Identify and fix the root cause of a wrong numerical result by comparing computed output against an authoritative external reference. Never accept "close enough" — astronomical accuracy targets are defined by the algorithm source.

## Step 1 — Reproduce the failure

1. Read the test or code that is producing the wrong value.
2. Run `dotnet test --filter "<TestName>"` to confirm the failure and record the actual vs expected values.
3. Note the epoch (Julian Day or UTC datetime) and observer coordinates used.

## Step 2 — Obtain authoritative reference values

Fetch reference values from an authoritative source for the **same epoch and observer**. Use the `fetch` tool:

### JPL Horizons (primary reference)
```
https://ssd.jpl.nasa.gov/api/horizons.api?format=text&COMMAND='Sun'&OBJ_DATA='NO'&MAKE_EPHEM='YES'&EPHEM_TYPE='OBSERVER'&CENTER='coord@399'&COORD_TYPE='GEODETIC'&SITE_COORD='-122.4,37.8,0'&START_TIME='2000-Jan-1 12:00'&STOP_TIME='2000-Jan-1 12:01'&STEP_SIZE='1m'&QUANTITIES='1,2,4,14,15,17,20'
```

Adjust `COMMAND`, `SITE_COORD`, `START_TIME`, and `QUANTITIES` as needed:
- Quantity 1: Astrometric RA & Dec
- Quantity 2: Apparent RA & Dec
- Quantity 4: Apparent Az & El
- Quantity 14: Observer's apparent distance (AU)
- Quantity 20: Observer's apparent distance (km)

### IERS Bulletin A (ΔT / Earth orientation)
```
https://datacenter.iers.org/products/eop/bulletinA/
```

### USNO Astronomical Almanac errata / online data
```
https://aa.usno.navy.mil/data/
```

## Step 3 — Isolate the discrepancy

Narrow down which stage of the pipeline is wrong. Typical failure points:

| Stage | What to check |
|-------|---------------|
| Julian Day conversion | Off-by-half-day error; noon vs midnight epoch |
| Julian Century `T` | Wrong J2000.0 base JD (`2451545.0`) |
| ΔT | Missing or incorrect ΔT applied to TT vs UT1 |
| Mean anomaly / longitude | Missing conversion to radians before `Math.Sin` |
| Nutation / aberration | Omitted or wrong sign |
| Precession | Wrong epoch; J2000 vs B1950 |
| Equatorial → Horizontal | Wrong quadrant correction on Azimuth |
| Angle normalization | Off by 360° or 180° |

Add temporary trace output in the calculation method to inspect intermediate values, then run `dotnet test` with verbose output:
```bash
dotnet test --filter "<TestName>" -- TUnit.Core.Verbosity=Verbose
```

## Step 4 — Fix the code

Apply the smallest change that brings computed values within the algorithm's stated accuracy. Do **not** widen tolerances to mask errors.

Accuracy targets by algorithm tier:

| Algorithm | Position accuracy | Source |
|-----------|-------------------|--------|
| Meeus low-precision (Ch. 25 truncated) | ±0.01° | Meeus Table 25.a |
| Meeus full series (Ch. 47) | ±0.0001° | Meeus §47 |
| VSOP87 | ±1″ (~0.0003°) | Bretagnon & Francou 1988 |
| DE430 / BSP kernels | sub-arcsecond | JPL IOM 392R-14-004 |

## Step 5 — Verify the fix

1. Run `dotnet build` — zero new warnings.
2. Run `dotnet test` — all tests pass.
3. Add a regression test citing the external reference value used:
   ```csharp
   [Test]
   public async Task SunAltitude_SpecificEpoch_MatchesJplHorizons()
   {
       // Reference: JPL Horizons query, 2000-Jan-1 12:00 TT, observer 37.8°N 122.4°W
       // Expected El = 27.43° (APDEC: 2000-01-01, Quantities=4)
       double T = 0.0; // J2000.0
       var (_, alt) = ObserverGeometry.EquatorialToHorizontal(...);
       await Assert.That(alt).IsEqualTo(27.43).Within(0.05);
   }
   ```
4. Commit with `fix(<scope>): <imperative description of what was wrong>`.

## What NOT to do

- Do not widen test tolerances to make the test pass without fixing the root cause.
- Do not change reference values to match the buggy output.
- Do not mix UT1 and TT epochs without explicit ΔT correction.
- Do not introduce `Math.Round` as a workaround for precision issues.
