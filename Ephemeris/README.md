# Ephemeris — Core Library

The `Ephemeris` class library is the astronomical calculation engine. It computes positions of the Sun, Moon, planets, and fixed stars for any observer location on Earth.

**NuGet package:** `WilliamForney.Ephemeris` · **Target:** `net10.0` · **License:** MIT

---

## Public Entry Points

Three static classes in the root `Ephemeris` namespace cover the most common use cases:

| Class | Purpose |
|-------|---------|
| `EphemerisCalculator` | Single-instant position queries (Sun, Moon, planets) with topocentric parallax |
| `EphemerisBatch` | Time-series `IEnumerable<EphemerisRecord>` generation with optional visibility windows |
| `EphemerisPlotter` | ASCII console altitude chart |

```csharp
// Single position
var sun = EphemerisCalculator.GetSunPosition(DateTime.UtcNow, longitude: -87.65, latitude: 41.85);

// Time series
var records = EphemerisBatch.GenerateMoonSeries(
    DateTime.UtcNow, intervalMinutes: 10, count: 144,
    longitude: -87.65, latitude: 41.85);

// Visibility windows (altitude > 15°)
var windows = EphemerisBatch.VisibilityWindows("Mars", DateTime.UtcNow,
    TimeSpan.FromDays(7), longitude: -87.65, latitude: 41.85, altThreshold: 15.0);
```

---

## Namespace Map

| Namespace | Domain | Key Classes | Algorithm Reference |
|-----------|--------|-------------|---------------------|
| `Ephemeris.Chronology` | Timekeeping | `TimeUtils`, `TimeZoneUtils` | [Timekeeping](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#timekeeping-chronology) |
| `Ephemeris.Heliology` | Solar ephemeris | `SunEphemeris` | [Solar](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#solar-ephemeris-heliology) |
| `Ephemeris.Selenography` | Lunar ephemeris | `MoonEphemeris`, `TopocentricParallax` | [Lunar](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#lunar-ephemeris-selenography) |
| `Ephemeris.Planetology` | Planetary positions | `PlanetEphemeris`, `PlanetPhysicalEphemeris`, `PlanetPositionService` | [Planetary](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#planetary-positions-planetology) |
| `Ephemeris.Geometry` | Coordinate types & transforms | `ObserverGeometry`, `CoordinateConverter`, `EquatorialCoordinates`, `HorizontalCoordinates` | [Transforms](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#coordinate-transforms-geometry) |
| `Ephemeris.Geodesy` | Earth corrections | `NutationCalculator`, `PrecessionCalculator`, `RefractionCalculator` | [Nutation & Precession](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#nutation--precession-geodesy) |
| `Ephemeris.Phenomenology` | Observable events | `RiseSetCalculator`, `EclipseCalculator`, `SeasonCalculator`, `PlanetaryEventCalculator`, `InnerPlanetEventCalculator` | [Phenomena](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#observable-phenomena-phenomenology) |
| `Ephemeris.Stellarography` | Fixed stars | `StarCatalog`, `BrightStarCatalog`, `StarEphemeris`, `YaleBsc5Reader` | [Fixed Stars](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#fixed-stars-stellarography) |
| `Ephemeris.Export` | Serialization | `EphemerisExporter` | — |
| `Ephemeris.Import` | Data import | `SpkReader`, `SpiceKernelDatabase`, `BspImporter`, `Se1EphemerisReader`, `DE430Importer` | [SPICE/BSP](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#spicebsp-import-import) |

---

## Data Types

| Type | Kind | Description |
|------|------|-------------|
| `EphemerisRecord` | `readonly record struct` | Universal time-series record: UTC, body name, RA, Dec, Az, Alt, Illumination, Distance, AngularDiameter, Magnitude |
| `CelestialObservation` | `readonly record struct` | Single-instant observation result: RA, Dec, Azimuth, Altitude, Illumination |
| `EquatorialCoordinates` | `readonly record struct` | (RightAscension°, Declination°) |
| `HorizontalCoordinates` | `readonly record struct` | (Azimuth°, Altitude°) |
| `EclipticCoordinates` | `readonly record struct` | (Longitude°, Latitude°) |
| `CartesianPosition` | `readonly record struct` | (X, Y, Z) in AU or km |
| `OrbitalElements` | `readonly record struct` | 6-element Keplerian set |
| `FixedStar` | `readonly record struct` | J2000.0 position + proper motion + parallax |

---

## Coordinate Conventions

All angles are **degrees** at the API boundary.

| Value | Range | Notes |
|-------|-------|-------|
| Right Ascension | [0, 360) | degrees (not hours) |
| Declination | [−90, 90] | degrees |
| Azimuth | [0, 360) | from North, clockwise; E = 90° |
| Altitude | [−90, 90] | positive = above horizon |
| Julian Century `T` | — | `(JD − 2451545.0) / 36525.0` |

---

## Dependency Injection

Services are registered automatically via Scrutor assembly scanning:

```csharp
services.AddEphemerisServices(); // registers all IScopedService / ISingletonService / ITransientService
```

New injectable services must implement one of `IScopedService`, `ISingletonService`, or `ITransientService`.

---

## Key Dependencies

| Package | Purpose |
|---------|---------|
| `DotNext` / `DotNext.Threading` | Advanced .NET utilities |
| `Generator.Equals` | Source-generated equality for record types |
| `Microsoft.Extensions.Hosting` | DI and service hosting |
| `ScottPlot` | Charting (used by `EphemerisPlotter`) |
| `Scrutor` | DI assembly scanning |

---

## Further Reading

- [Algorithm Reference](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference) — all algorithms with formulas and source citations
- [SPK/BSP Format](https://github.com/wforney/ephemeris/wiki/SPK-BSP-Format) — NAIF DAF binary kernel format
- [SE1 Format](https://github.com/wforney/ephemeris/wiki/SE1-Ephemeris-Format) — Swiss Ephemeris binary format
- [Root README](../README.md) — solution overview, build instructions, usage examples
