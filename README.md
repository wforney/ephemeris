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
| **Nutation & precession** — IAU 1980 apparent-place corrections | 🔜 |
| **Rise/set/transit** — sunrise, moonrise, twilight, solar noon | 🔜 |
| **Atmospheric refraction** — Bennett formula for horizon correction | 🔜 |
| **Eclipse prediction** — solar and lunar eclipse finder | 🔜 |
| **Seasons** — equinox and solstice times | 🔜 |
| **Angular separation** — haversine distance between any two objects | 🔜 |
| **Stellar catalog** — Yale BSC positions with proper motion | 🔜 |
| **NuGet package** — distributable library package | 🔜 |

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
| `Ephemeris.Geodesy` | Nutation, precession *(planned)* |
| `Ephemeris.Phenomenology` | Rise/set, eclipses, seasons *(planned)* |
| `Ephemeris.Stellarography` | Stellar catalog and positions *(planned)* |

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

## Roadmap

### Phase 1 — Foundation
- Iterative (Newton–Raphson) Kepler equation solver for high-eccentricity orbits
- Atmospheric refraction correction (Bennett formula)
- Nutation and precession (IAU 1980)
- Extend `EphemerisRecord` with `Distance`, `Magnitude`, `AngularDiameter` fields
- Angular separation (`CoordinateConverter.AngularSeparation`)
- Unit tests for geometry, coordinate converter, batch, and exporter

### Phase 2 — Accuracy
- Full solar ephemeris: equation of center, aberration, nutation (Meeus Ch. 25–27) — sub-arcminute accuracy
- Full lunar ephemeris: Meeus Ch. 47 ~60-term series, libration, topocentric parallax — sub-arcminute accuracy
- Planet apparent magnitude, angular diameter, and elongation from Sun

### Phase 3 — Phenomena
- Rise/set/transit and twilight times for Sun, Moon, and planets (Meeus Ch. 15)
- Equinox and solstice calculator (Meeus Ch. 27)
- Solar and lunar eclipse prediction (Meeus Ch. 54)
- Yale Bright Star Catalog subset with proper-motion corrections

### Phase 4 — API Completeness
- `EphemerisCalculator.NextFullMoon`, `NextSolstice`, `NextRise`, `NextSet`
- `EphemerisBatch.VisibilityWindows(body, altitudeThreshold)`
- JPL Horizons–verified reference tests for Sun, Moon, and planets

### Phase 5 — Infrastructure
- NuGet package with semantic versioning and CI release
- BenchmarkDotNet benchmark project

---

## Dependencies

- [ScottPlot 5](https://scottplot.net) — charting
- [Scrutor](https://github.com/khellang/Scrutor) — DI assembly scanning
- [DotNext](https://github.com/dotnet/dotNext) — advanced .NET utilities
- [SpiceSharp-Parser](https://github.com/SpiceSharp/SpiceSharpParser) — SPICE kernel parsing (BSP reader pending)
- [TUnit](https://github.com/thomhurst/TUnit) — test framework

## License

See [LICENSE](LICENSE) for details.
