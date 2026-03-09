# Ephemeris

A .NET 10 library for computing positions of celestial bodies (Sun, Moon, planets) as seen from any observer location on Earth.

[![CI](https://github.com/wforney/ephemeris/actions/workflows/ci.yml/badge.svg)](https://github.com/wforney/ephemeris/actions/workflows/ci.yml)

## Features

| Category | Status |
|---|---|
| **Timekeeping** — Julian Day, Julian Century, GMST, UTC↔JD | ✅ |
| **Solar ephemeris** — Meeus Ch. 25: equation of center, aberration, nutation, R (AU) | ✅ |
| **Lunar ephemeris** — Meeus Ch. 47: 60-term Σl/Σb/Σr series, phase name, illumination | ✅ |
| **Planetary positions** — Mercury–Pluto via iterative Kepler + orbital elements | ✅ |
| **Observer geometry** — equatorial→horizontal (Az/Alt), atmospheric refraction | ✅ |
| **Coordinate conversion** — ecliptic↔equatorial, angular separation | ✅ |
| **Nutation & precession** — IAU 1980 50-term nutation, IAU 2006 precession | ✅ |
| **Rise/set/transit** — Sun, Moon, and planet rise/set/transit (Meeus Ch. 15) | ✅ |
| **Eclipse prediction** — solar and lunar eclipse finder (Meeus Ch. 54) | ✅ |
| **Seasons** — equinox and solstice times (Meeus Ch. 27) | ✅ |
| **Next-event queries** — `NextFullMoon`, `NextSunrise`, `NextVernalEquinox`, etc. | ✅ |
| **Visibility windows** — `EphemerisBatch.VisibilityWindows(body, altThreshold)` | ✅ |
| **Planet physical ephemeris** — apparent magnitude, angular diameter, elongation | ✅ |
| **EphemerisRecord** — `Distance`, `Magnitude`, `AngularDiameter` fields | ✅ |
| **Batch generation** — time-series `EphemerisRecord` collections | ✅ |
| **Data export** — CSV and JSON serialization | ✅ |
| **NuGet package** — `Wforney.Ephemeris` 0.1.0 with CI release workflow | ✅ |
| **Benchmarks** — BenchmarkDotNet project for Sun/Moon/planet series | ✅ |
| **SPICE / DE430 import** — kernel loading, ET conversion, BSP reader (stub) | 🚧 |
| **WinForms visualizer** — altitude-vs-time ScottPlot chart | ✅ |
| **Stellar catalog** — Yale BSC positions with proper motion | 🔜 |

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
| `Ephemeris.Heliology` | Solar ephemeris — Meeus Ch. 25 (RA/Dec, aberration, nutation, R) |
| `Ephemeris.Selenography` | Lunar ephemeris — Meeus Ch. 47 (60-term series, phase, illumination) |
| `Ephemeris.Planetology` | Planetary positions via iterative Kepler + orbital elements |
| `Ephemeris.Geometry` | Equatorial↔horizontal coordinate transforms, refraction |
| `Ephemeris.Geodesy` | Nutation (IAU 1980 50-term) and precession (IAU 2006) |
| `Ephemeris.Phenomenology` | Rise/set/transit, eclipses, seasons, visibility windows |
| `Ephemeris.Export` | CSV/JSON serialization of `EphemerisRecord` |
| `Ephemeris.Import` | SPICE kernel and DE430 ephemeris data import |
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

// Moon phase and illumination
var moon = EphemerisCalculator.GetMoonPosition(DateTime.UtcNow, -87.65, 41.85);
Console.WriteLine($"Phase: {moon.Illumination * 100:F1}%  Alt: {moon.Alt:F2}°");

// Next-event queries
var calc = new EphemerisCalculator();
var nextFull = calc.NextFullMoon(DateTime.UtcNow);
var nextSunrise = calc.NextSunrise(DateTime.UtcNow, longitude: -87.65, latitude: 41.85);
var nextEquinox = calc.NextVernalEquinox(DateTime.UtcNow.Year);

// Rise/set/transit for today
var riseSet = RiseSetCalculator.Sun(DateTime.UtcNow, longitude: -87.65, latitude: 41.85);
Console.WriteLine($"Sunrise: {riseSet.Rise}  Transit: {riseSet.Transit}  Sunset: {riseSet.Set}");

// Eclipse prediction
var nextSolar = EclipseCalculator.NextSolarEclipse(DateTime.UtcNow);
Console.WriteLine($"Next solar eclipse: {nextSolar.DateTime} ({nextSolar.Type})");

var nextLunar = EclipseCalculator.NextLunarEclipse(DateTime.UtcNow);
Console.WriteLine($"Next lunar eclipse: {nextLunar.DateTime} ({nextLunar.Type})");

// Time-series batch for the Moon
var records = EphemerisBatch.GenerateMoonSeries(
    DateTime.UtcNow, intervalMinutes: 10, count: 144,
    longitude: -87.65, latitude: 41.85);

// Visibility windows (when altitude > 10°)
var windows = EphemerisBatch.VisibilityWindows("Moon",
    DateTime.UtcNow, TimeSpan.FromDays(7),
    longitude: -87.65, latitude: 41.85, altThreshold: 10.0);
```

## Accuracy

| Body | Algorithm | Accuracy |
|------|-----------|---------|
| Sun | Meeus Ch. 25 (equation of center, aberration, nutation) | ~0.01° |
| Moon | Meeus Ch. 47 (60-term Σl/Σb/Σr ELP-2000 series) | ~0.1° |
| Planets | Iterative Kepler + simplified orbital elements | 0.5–5° |
| Rise/Set times | Meeus Ch. 15, 3-iteration convergence | ~1 min |
| Eclipse times | Meeus Ch. 54 Besselian elements | ~5 min |

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

### Completed
- ✅ Phases 1–4: Foundation, accuracy upgrades, phenomena, API completeness
- ✅ Iterative Kepler solver, nutation/precession, atmospheric refraction
- ✅ Full Meeus Ch. 25 solar and Ch. 47 lunar ephemerides
- ✅ Rise/set/transit (Meeus Ch. 15), seasons (Ch. 27), eclipses (Ch. 54)
- ✅ 47 unit tests verified against JPL Horizons reference values
- ✅ BenchmarkDotNet project, NuGet packaging, CI coverage reporting

### Remaining
- 🔜 **Stellar catalog** — Yale Bright Star Catalog subset with proper-motion corrections
- 🔜 **SPICE/BSP full implementation** — pending NAIF-compatible .NET library
- 🔜 **Topocentric parallax** — Moon parallax for observer-specific positions

---

## Dependencies

- [ScottPlot 5](https://scottplot.net) — charting
- [Scrutor](https://github.com/khellang/Scrutor) — DI assembly scanning
- [DotNext](https://github.com/dotnet/dotNext) — advanced .NET utilities
- [SpiceSharp-Parser](https://github.com/SpiceSharp/SpiceSharpParser) — SPICE kernel parsing (BSP reader pending)
- [TUnit](https://github.com/thomhurst/TUnit) — test framework

## License

See [LICENSE](LICENSE) for details.
