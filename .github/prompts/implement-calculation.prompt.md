<!-- Updated: 2026-08-12 -->
---
mode: agent
model: anthropic/claude-opus-4.5
tools: [codebase, editFiles, runCommands, fetch]
description: Implement a new astronomical calculation in the correct domain namespace following all project conventions.
---

You are implementing a new astronomical calculation for the Ephemeris .NET 10 library.

## Before writing any code

1. Identify the correct domain namespace from the table below:
   - `Ephemeris.Chronology` — time, Julian Day, ΔT, GMST, sidereal time
   - `Ephemeris.Heliology` — solar position and ecliptic coordinates
   - `Ephemeris.Selenography` — lunar position, phase, illumination
   - `Ephemeris.Planetology` — planetary positions via Kepler's equations
   - `Ephemeris.Geometry` — equatorial↔horizontal coordinate transforms
   - `Ephemeris.Export` / `Ephemeris.Import` — data serialization

2. Fetch the authoritative algorithm source. Use the `fetch` tool to retrieve reference values or formulae from:
   - JPL Horizons (https://ssd.jpl.nasa.gov/horizons/) for validation data
   - USNO Astronomical Almanac or Meeus "Astronomical Algorithms" errata pages
   - IERS Bulletin A (https://datacenter.iers.org/products/eop/bulletinA/) for ΔT values

3. Check existing neighbouring classes in the target namespace to match patterns exactly.

## Implementation rules

- Class must be `public static` — no instance state, no DI marker interface needed.
- All angles are **degrees at the API boundary**; convert to radians inline for `Math.Sin/Cos/Asin/Acos`.
- Time parameter is **Julian Century `T`** (`(JD − 2451545.0) / 36525.0`), not `DateTime`. Use `TimeUtils` for conversions.
- Return multiple related values as **named value tuples**, not new classes:
  ```csharp
  public static (double RA, double Dec) ApparentCoordinates(double T)
  ```
- Normalize output angles with `TimeUtils.NormalizeDegrees()` where appropriate.
- Add **XML doc comments** to every `public` member — the build enforces this.
- Overflow checking is enabled — avoid unchecked arithmetic.

## After implementation

1. Run `dotnet build` and fix any warnings.
2. Add at least one TUnit test in `Ephemeris.Tests` using a known reference value (cite the source in a comment).
3. If this adds a public API, update `EphemerisCalculator` or `EphemerisBatch` if appropriate.
4. Commit with `feat(<scope>): <imperative description>` where scope is the lowercase namespace name.
