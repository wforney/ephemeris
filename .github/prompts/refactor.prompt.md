<!-- Updated: 2026-03-09 04:06 UTC -->
---
mode: agent
model: anthropic/claude-sonnet-4-5
tools: [codebase, editFiles, runCommands]
description: Refactor Ephemeris code while preserving all astronomical behaviour and project conventions.
---

You are refactoring code in the Ephemeris .NET 10 library. Your goal is to improve internal structure **without changing observable behaviour**.

## Non-negotiable constraints

1. **No instance state in calculation classes** — they must remain `public static`.
2. **API signatures are public contracts** — do not change parameter names, types, or return shapes without a `feat!` / `BREAKING CHANGE` commit.
3. **All angles remain degrees at boundaries** — do not push radians to the surface.
4. **`T` (Julian Century) stays as the time parameter** for domain methods.
5. **Build must pass with zero new warnings** — `dotnet build` before and after.

## Safe refactoring patterns

### Extract repeated trig patterns
```csharp
// Before
double sinLat = Math.Sin(TimeUtils.ToRadians(latitude));
// ... repeated 3 times

// After (local variable or private helper)
double latRad = TimeUtils.ToRadians(latitude);
double sinLat = Math.Sin(latRad);
```

### Collapse redundant normalization
```csharp
// Before
angle = angle % 360; if (angle < 0) angle += 360;
// After
angle = TimeUtils.NormalizeDegrees(angle);
```

### Replace magic numbers with named constants
```csharp
// Before
double T = (jd - 2451545.0) / 36525.0;
// After — if this is already in TimeUtils, call TimeUtils.JulianCentury(jd)
```

### Consolidate duplicated DE430 / VSOP87 coefficient arrays
If the same orbital element array appears in more than one class, extract to a `private static readonly` field or a dedicated `OrbitalElements` record.

## Process

1. Read the target file(s) in full — understand what the code does before touching it.
2. Make the smallest change that achieves the goal.
3. Run `dotnet build` — fix any new warnings.
4. Run `dotnet test` — all tests must still pass.
5. Commit with `refactor(<scope>): <imperative description>`.

## What NOT to do
- Do not rename public methods or properties.
- Do not change the unit of any parameter (degrees vs radians).
- Do not introduce async where the operation is pure computation.
- Do not add dependencies; use what is already in the solution.
