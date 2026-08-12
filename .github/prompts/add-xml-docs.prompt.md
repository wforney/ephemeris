<!-- Updated: 2026-08-12 -->
---
mode: agent
model: anthropic/claude-haiku-4.5
tools: [codebase, editFiles, runCommands]
description: Add or complete missing XML doc comments on all public members in the Ephemeris library.
---

You are adding XML documentation comments to public members in the Ephemeris .NET 10 library.

## Find members missing docs

Run the following to find all CS1591 warnings (missing XML comments):

```bash
dotnet build 2>&1 | grep CS1591
```

Each warning line identifies a file, line number, and member name. Work through them systematically.

## Documentation style

Follow the existing pattern in well-documented members. Every `public` member needs at minimum `<summary>`. Add `<param>`, `<returns>`, and `<remarks>` when non-obvious.

```csharp
/// <summary>
/// Converts equatorial coordinates (RA/Dec) to horizontal coordinates (Azimuth/Altitude)
/// for an observer at the given geographic location and time.
/// </summary>
/// <param name="RA">Right ascension in degrees [0, 360).</param>
/// <param name="Dec">Declination in degrees [−90, 90].</param>
/// <param name="jd">Julian Day (UTC).</param>
/// <param name="longitude">Observer longitude in degrees, east positive.</param>
/// <param name="latitude">Observer latitude in degrees, north positive.</param>
/// <returns>
/// A tuple of (Azimuth, Altitude) in degrees.
/// Azimuth is measured from North clockwise [0, 360).
/// Altitude is positive above the horizon [−90, 90].
/// </returns>
public static (double Azimuth, double Altitude) EquatorialToHorizontal(
    double RA, double Dec, double jd, double longitude, double latitude)
```

## Domain-specific terminology to use consistently

| Term | Meaning |
|------|---------|
| Julian Century `T` | `(JD − 2451545.0) / 36525.0` |
| J2000.0 | Epoch Julian Day 2451545.0 |
| GMST | Greenwich Mean Sidereal Time |
| ΔT | Difference between Terrestrial Time and UTC |
| RA | Right ascension (degrees) |
| Dec | Declination (degrees) |

## After adding docs

1. Run `dotnet build` — confirm zero CS1591 warnings.
2. Commit with `docs(<scope>): add XML doc comments to <ClassName>`.
