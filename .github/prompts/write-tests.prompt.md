<!-- Updated: 2026-03-09 04:06 UTC -->
---
mode: agent
model: anthropic/claude-sonnet-4-5
tools: [codebase, editFiles, runCommands]
description: Write TUnit tests for Ephemeris calculation methods using known reference values.
---

You are writing TUnit tests for the Ephemeris .NET 10 library.

## Test project layout

- Test files live in `Ephemeris.Tests/Ephemeris.Tests/`
- Framework: **TUnit** (not xUnit, not NUnit) — use `[Test]` attribute
- Namespace: `namespace Ephemeris.Tests`
- Assembly is decorated `[assembly: ExcludeFromCodeCoverage]`

## What makes a good test here

Astronomical calculations must be verified against **external reference values**, not just round-tripped through the same code. Always cite the source:

```csharp
[Test]
public async Task SunRA_J2000_MatchesUsno()
{
    // Reference: USNO Astronomical Almanac 2000, Table C-3
    // Sun RA at J2000.0 (JD 2451545.0) ≈ 18h 45m 39.8s = 281.416°
    double T = 0.0;
    var (ra, _) = SunEphemeris.ApparentEquatorialCoordinates(T);
    await Assert.That(ra).IsEqualTo(281.416).Within(0.1);
}
```

Good reference sources:
- JPL Horizons web interface (https://ssd.jpl.nasa.gov/horizons/) — use "Observer Table" output
- USNO Solar/Lunar tables
- Published ephemeris test vectors (cite URL or publication)

## Test naming convention

`<MethodUnderTest>_<Scenario>_<ExpectedBehaviour>`

Examples:
- `GeocentricCoordinates_AtJ2000_MatchesJplHorizons`
- `EquatorialToHorizontal_BodyDueNorth_AzimuthIsZero`
- `NormalizeDegrees_NegativeInput_ReturnsPositive`

## Coverage priorities

Write tests in this order:
1. Known reference value at a specific epoch (J2000.0 or a dated event)
2. Edge cases: body at zenith, body below horizon, midnight sun, polar observer
3. Round-trip: `EclipticToEquatorial` → `EquatorialToEcliptic` returns original
4. Public API wrappers: `EphemerisCalculator.GetSunPosition`, `GetMoonPosition`, `GetPlanetPosition`

## After writing tests

1. Run `dotnet test` and confirm they pass.
2. Commit with `test(<scope>): <imperative description>`.
