# Ephemeris

A .NET 10 library for computing positions of celestial bodies (Sun, Moon, planets) as seen from any observer location on Earth.

[![CI](https://github.com/wforney/ephemeris/actions/workflows/ci.yml/badge.svg)](https://github.com/wforney/ephemeris/actions/workflows/ci.yml)

## Features

| Category | Status |
|---|---|
| **Timekeeping** — Julian Day, Julian Century, GMST, UTC↔JD | ✅ |
| **Solar ephemeris** — RA/Dec, ecliptic coords, heliocentric longitude | ✅ |
| **Lunar ephemeris** — RA/Dec, phase angle, illumination fraction | ✅ |
| **Planetary positions** — Mercury–Pluto via simplified orbital elements | ✅ |
| **Observer geometry** — equatorial→horizontal (Az/Alt), hour angle | ✅ |
| **Coordinate conversion** — ecliptic↔equatorial | ✅ |
| **Batch generation** — time-series `EphemerisRecord` collections | ✅ |
| **Data export** — CSV and JSON serialization | ✅ |
| **SPICE / DE430 import** — kernel loading, ET conversion, BSP reader (stub) | 🚧 |
| **WinForms visualizer** — altitude-vs-time ScottPlot chart | ✅ |

## Projects

| Project | Description |
|---|---|
| `Ephemeris` | Core class library — calculation engine |
| `Ephemeris.Tests` | TUnit test suite |
| `Ephemeris.UI` | WinForms visualization app (Windows only) |

## Architecture

Domain namespaces mirror astronomical subdisciplines:

| Namespace | Domain |
|---|---|
| `Ephemeris.Chronology` | Julian Day, ΔT, GMST, sidereal time |
| `Ephemeris.Heliology` | Solar ephemeris (RA/Dec, ecliptic coords) |
| `Ephemeris.Selenography` | Lunar ephemeris (RA/Dec, phase, illumination) |
| `Ephemeris.Planetology` | Planetary positions via Kepler's equations |
| `Ephemeris.Geometry` | Equatorial↔horizontal coordinate transforms |
| `Ephemeris.Export` | CSV/JSON serialization of `EphemerisRecord` |
| `Ephemeris.Import` | SPICE kernel and DE430 ephemeris data import |

Public entry points are in the root `Ephemeris` namespace:

- **`EphemerisCalculator`** — single-instant position queries for Sun, Moon, and planets
- **`EphemerisBatch`** — generates time-series `EphemerisRecord` collections
- **`EphemerisPlotter`** — ASCII console visualization

## Build & Test

```bash
dotnet restore
dotnet build
dotnet test
```

## Usage

```csharp
// Single-instant Sun position
var result = EphemerisCalculator.GetSunPosition(
    DateTime.UtcNow, longitude: -87.65, latitude: 41.85);

Console.WriteLine($"Az: {result.Az:F2}°  Alt: {result.Alt:F2}°");

// Time-series batch for the Moon
var records = EphemerisBatch.GenerateMoonSeries(
    DateTime.UtcNow, intervalMinutes: 10, count: 144,
    longitude: -87.65, latitude: 41.85);
```

## Coordinate conventions

All angles are **degrees** at the API boundary.

| Value | Range | Notes |
|---|---|---|
| Right Ascension | [0, 360) | degrees |
| Declination | [−90, 90] | degrees |
| Azimuth | [0, 360) | from North, clockwise; E = 90° |
| Altitude | [−90, 90] | positive = above horizon |
| Julian Day | fractional JD | UTC epoch |
| `T` | Julian centuries | `(JD − 2451545.0) / 36525.0` |

## Dependencies

- [ScottPlot 5](https://scottplot.net) — charting
- [Scrutor](https://github.com/khellang/Scrutor) — DI assembly scanning
- [DotNext](https://github.com/dotnet/dotNext) — advanced .NET utilities
- [SpiceSharp-Parser](https://github.com/SpiceSharp/SpiceSharpParser) — SPICE kernel parsing (BSP reader pending)
- [TUnit](https://github.com/thomhurst/TUnit) — test framework

## License

See [LICENSE](LICENSE) for details.
