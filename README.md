# Ephemeris

A .NET 10 library for computing positions of celestial bodies (Sun, Moon, planets, and stars) as seen from any observer location on Earth.

[![CI](https://github.com/wforney/ephemeris/actions/workflows/ci.yml/badge.svg)](https://github.com/wforney/ephemeris/actions/workflows/ci.yml)

## Features

| Category | Status |
|---|---|
| **Timekeeping** — Julian Day, Julian Century, GMST, UTC↔JD | ✅ |
| **Solar ephemeris** — Meeus Ch. 25: equation of center, aberration, nutation, R (AU) | ✅ |
| **Lunar ephemeris** — Meeus Ch. 47: 60-term Σl/Σb/Σr series, phase name, illumination | ✅ |
| **Topocentric parallax** — Meeus Ch. 40 diurnal parallax for Moon, Sun, and all planets | ✅ |
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
| **Planetary events** — opposition, conjunction, quadrature (outer); greatest elongation (inner) | ✅ |
| **Batch generation** — time-series `EphemerisRecord` collections | ✅ |
| **Data export** — CSV and JSON serialization | ✅ |
| **Stellar catalog** — 25-star embedded catalog + Yale BSC5 reader, proper-motion & precession | ✅ |
| **Native BSP/SPK reader** — DAF binary parser, Type 2/3 Chebyshev, BFS segment graph traversal | ✅ |
| **NuGet package** — `WilliamForney.Ephemeris` 0.1.0 with CI release workflow | ✅ |
| **Benchmarks** — BenchmarkDotNet project for Sun/Moon/planet series | ✅ |
| **WinForms visualizer** — altitude-vs-time ScottPlot chart | ✅ |

## Projects

| Project | Description |
|---|---|
| `Ephemeris` | Core class library — calculation engine |
| `Ephemeris.Tests` | TUnit test suite (252 tests) |
| `Ephemeris.Benchmarks` | BenchmarkDotNet performance suite |
| `Ephemeris.UI` | WinForms visualization app (Windows only) |

## Architecture

Domain namespaces mirror astronomical subdisciplines:

| Namespace | Domain |
|---|---|
| `Ephemeris.Chronology` | Julian Day, ΔT, GMST, sidereal time |
| `Ephemeris.Heliology` | Solar ephemeris — Meeus Ch. 25 (RA/Dec, aberration, nutation, R) |
| `Ephemeris.Selenography` | Lunar ephemeris — Meeus Ch. 47 (60-term series, phase, illumination, topocentric parallax) |
| `Ephemeris.Planetology` | Planetary positions via iterative Kepler + orbital elements |
| `Ephemeris.Geometry` | Equatorial↔horizontal coordinate transforms, refraction, coordinate record structs |
| `Ephemeris.Geodesy` | Nutation (IAU 1980 50-term) and precession (IAU 2006) |
| `Ephemeris.Phenomenology` | Rise/set/transit, eclipses, seasons, visibility windows, planetary events (opposition/conjunction/elongation) |
| `Ephemeris.Export` | CSV/JSON serialization of `EphemerisRecord` |
| `Ephemeris.Import` | Native DAF/SPK BSP reader, DE430 binary importer |
| `Ephemeris.Stellarography` | Fixed star catalog, proper-motion corrections, Yale BSC5 reader |

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

Integration tests that require local ephemeris kernel files (BSP, SE1) are skipped automatically when the files are not present.

## Usage

```csharp
// Single-instant Sun position
var result = EphemerisCalculator.GetSunPosition(
    DateTime.UtcNow, longitude: -87.65, latitude: 41.85);
Console.WriteLine($"Az: {result.Azimuth:F2}°  Alt: {result.Altitude:F2}°");

// Moon position with topocentric parallax (observer at 200 m elevation)
var moon = EphemerisCalculator.GetMoonPosition(
    DateTime.UtcNow, -87.65, 41.85, altitudeMeters: 200);
Console.WriteLine($"Phase: {moon.Illumination * 100:F1}%  Alt: {moon.Altitude:F2}°");

// Next-event queries
var calc = new EphemerisCalculator();
var nextFull    = calc.NextFullMoon(DateTime.UtcNow);
var nextSunrise = calc.NextSunrise(DateTime.UtcNow, longitude: -87.65, latitude: 41.85);
var nextEquinox = calc.NextVernalEquinox(DateTime.UtcNow.Year);

// Rise/set/transit for today
var riseSet = RiseSetCalculator.Sun(DateTime.UtcNow, longitude: -87.65, latitude: 41.85);
Console.WriteLine($"Sunrise: {riseSet.Rise}  Transit: {riseSet.Transit}  Sunset: {riseSet.Set}");

// Eclipse prediction
var nextSolar = EclipseCalculator.NextSolarEclipse(DateTime.UtcNow);
Console.WriteLine($"Next solar eclipse: {nextSolar.DateTime} ({nextSolar.Type})");

// Time-series batch for the Moon
var records = EphemerisBatch.GenerateMoonSeries(
    DateTime.UtcNow, intervalMinutes: 10, count: 144,
    longitude: -87.65, latitude: 41.85);

// Visibility windows (when altitude > 10°)
var windows = EphemerisBatch.VisibilityWindows("Moon",
    DateTime.UtcNow, TimeSpan.FromDays(7),
    longitude: -87.65, latitude: 41.85, altThreshold: 10.0);

// Stellar catalog — built-in 25-star catalog, or load from Yale BSC5
var catalog = StarCatalog.LoadBuiltIn();
var sirius   = catalog.GetByName("Sirius");
var bright   = catalog.GetBrighter(2.0);                    // stars brighter than magnitude 2
var inRegion = catalog.GetInRegion(ra: 80, dec: -10, radiusDeg: 20);

// Apply proper-motion + precession to current epoch
var siriusNow = sirius!.AtEpoch(TimeZoneUtils.ToJulianDay(DateTime.UtcNow));

// Native SPICE BSP kernel reader (place de440s.bsp or similar in ~/ephem-data/)
var db = new SpiceKernelDatabase();
db.LoadKernel("/path/to/de440s.bsp");
double et = db.ConvertUtcToEphemerisTime(DateTime.UtcNow); // leap-second-aware ET
double[] pos = db.GetPosition("SUN", et, "J2000", "EARTH"); // [x, y, z] km (ICRF)

// Or use BspImporter for a full time-series pipeline
var bspRecords = BspImporter.LoadFromBspKernel(
    kernelPaths: ["/path/to/de440s.bsp"],
    target: "SUN", observer: "EARTH",
    startUtc: DateTime.UtcNow, intervalMinutes: 60, count: 24,
    longitude: -87.65, latitude: 41.85);
```

## Accuracy

| Body / Topic | Algorithm | Accuracy |
|---|---|---|
| Sun | Meeus Ch. 25 (equation of center, aberration, nutation) | ~0.01° |
| Moon (geocentric) | Meeus Ch. 47 (60-term Σl/Σb/Σr ELP-2000 series) | ~0.1° |
| Moon/Sun/Planets (topocentric) | Meeus Ch. 40 diurnal parallax applied after geocentric calc | ~0.01° additional |
| Planets | Iterative Kepler + simplified orbital elements | 0.5–5° |
| Rise/Set times | Meeus Ch. 15, 3-iteration convergence | ~1 min |
| Eclipse times | Meeus Ch. 54 Besselian elements | ~5 min |
| Stellar positions | J2000.0 ICRS + linear proper-motion + IAU 2006 precession | arcsec-level |
| BSP/SPK positions | Native DAF reader, SPK Type 2/3 Chebyshev interpolation | sub-km (kernel-limited) |

## Coordinate conventions

All angles are **degrees** at the API boundary; internal trigonometry converts to radians inline.

| Value | Range | Notes |
|---|---|---|
| Right Ascension | [0, 360) | degrees |
| Declination | [−90, 90] | degrees |
| Azimuth | [0, 360) | from North, clockwise; E = 90° |
| Altitude | [−90, 90] | positive = above horizon |
| Julian Day | fractional JD | UTC epoch |
| `T` | Julian centuries | `(JD − 2451545.0) / 36525.0` |
| Ephemeris Time (ET) | seconds past J2000.0 | TDB ≈ TT = UTC + leap seconds + 32.184 s |

Coordinate record types (`readonly record struct`) in `Ephemeris.Geometry`:
`EquatorialCoordinates`, `HorizontalCoordinates`, `EclipticCoordinates`, `CartesianPosition`

## Format documentation

Reference documents in `docs/`:

| File | Contents |
|---|---|
| [`docs/spk-format.md`](docs/spk-format.md) | DAF/SPK binary format — file record, summary records, segment descriptors, Type 2/3 Chebyshev layout |
| [`docs/se1-format.md`](docs/se1-format.md) | SE1 binary ephemeris format |
| [`docs/sefstars-format.md`](docs/sefstars-format.md) | Star catalog text format |
| [`docs/yale-bsc5-format.md`](docs/yale-bsc5-format.md) | Yale Bright Star Catalog 5th edition fixed-width format |

## Roadmap

### Completed
- ✅ Foundation: Julian Day, GMST, sidereal time, coordinate transforms
- ✅ Solar ephemeris (Meeus Ch. 25), lunar ephemeris (Meeus Ch. 47)
- ✅ Topocentric parallax (Meeus Ch. 40) for Moon, Sun, and all planets
- ✅ Planetary positions, nutation, precession, atmospheric refraction
- ✅ Rise/set/transit (Meeus Ch. 15), seasons (Ch. 27), eclipses (Ch. 54)
- ✅ Stellar catalog — embedded 25-star subset + Yale BSC5 reader, proper-motion & IAU 2006 precession
- ✅ Native DAF/SPK BSP reader — Type 2/3 Chebyshev, leap-second-aware UTC→ET, BFS segment graph for arbitrary multi-hop chaining
- ✅ OpenGL/Skia 3D sky view — `SkyViewForm` renders stars, Sun, Moon, planets with OpenTK 4 GLControl + SkiaSharp label overlay; launcher (`LauncherForm`) added to `Ephemeris.UI`
- ✅ Planetary event calculators — opposition, conjunction, quadrature for outer planets; greatest elongation for inner planets
- ✅ 252 unit tests verified against JPL Horizons and synthetic reference values
- ✅ BenchmarkDotNet project, NuGet packaging, CI coverage reporting

### Future

No planned items at this time — contributions welcome!

---

## Dependencies

- [ScottPlot 5](https://scottplot.net) — charting (core library and WinForms UI)
- [Scrutor](https://github.com/khellang/Scrutor) — DI assembly scanning
- [DotNext](https://github.com/dotnet/dotNext) — advanced .NET utilities and async threading
- [Generator.Equals](https://github.com/diegofrata/Generator.Equals) — source-generated equality for record types
- [Microsoft.Extensions.Hosting](https://learn.microsoft.com/dotnet/core/extensions/generic-host) — dependency injection and service hosting
- [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) — MVVM helpers (UI)
- [OpenTK + SkiaSharp](https://opentk.net) — OpenGL and Skia rendering (used in `SkyViewForm`)
- [BenchmarkDotNet](https://benchmarkdotnet.org) — performance benchmarking
- [TUnit](https://github.com/thomhurst/TUnit) — test framework
- [Verify.TUnit](https://github.com/VerifyTests/Verify) — snapshot testing
- [Imposter](https://github.com/pdevito3/Imposter) — compile-time source-generated mocks

## License

See [LICENSE](LICENSE) for details.
