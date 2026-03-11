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
| `Ephemeris.Planetology` | Planetary positions | `PlanetEphemeris`, `PlanetPhysicalEphemeris`, `PlanetPositionService`, `AsteroidEphemeris` | [Planetary](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#planetary-positions-planetology) |
| `Ephemeris.Geometry` | Coordinate types & transforms | `ObserverGeometry`, `CoordinateConverter`, `EquatorialCoordinates`, `HorizontalCoordinates` | [Transforms](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#coordinate-transforms-geometry) |
| `Ephemeris.Geodesy` | Earth corrections | `NutationCalculator`, `PrecessionCalculator`, `RefractionCalculator` | [Nutation & Precession](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#nutation--precession-geodesy) |
| `Ephemeris.Phenomenology` | Observable events | `RiseSetCalculator`, `EclipseCalculator`, `SeasonCalculator`, `PlanetaryEventCalculator`, `InnerPlanetEventCalculator` | [Phenomena](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#observable-phenomena-phenomenology) |
| `Ephemeris.Stellarography` | Fixed stars | `StarCatalog`, `BrightStarCatalog`, `StarEphemeris`, `YaleBsc5Reader` | [Fixed Stars](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#fixed-stars-stellarography) |
| `Ephemeris.Export` | Serialization | `EphemerisExporter` | — |
| `Ephemeris.Import` | Data import | `SpkReader`, `SpiceKernelDatabase`, `BspImporter`, `Se1EphemerisReader`, `DE430Importer` | [SPICE/BSP](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#spicebsp-import-import) |
| `Ephemeris.Astrology` | Astrological house systems | `AstrologicalHouses`, `HouseCusps`, `HouseSystem` | [Astrology](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#astrological-houses-astrology) |

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
| `HouseCusps` | `readonly record struct` | 12 astrological house cusps + four Angles (ASC, MC, DSC, IC) in ecliptic degrees |
| `HouseSystem` | `enum` | Placidus, Equal, WholeSigns, Porphyry (implemented); Koch, Campanus, Regiomontanus (stub) |

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

## Asteroid Ephemeris

`AsteroidEphemeris` in `Ephemeris.Planetology` computes geocentric equatorial coordinates and observer-relative horizontal coordinates for six minor planets using J2000.0 Keplerian osculating elements from the JPL Small Body Database / IAU MPC.

**Supported bodies:** (1) Ceres, (2) Pallas, (3) Juno, (4) Vesta, (2060) Chiron, (136199) Eris.

```csharp
using Ephemeris.Planetology;

// Heliocentric equatorial coordinates + distance in AU
double T = TimeUtils.JulianCentury(TimeZoneUtils.ToJulianDay(DateTime.UtcNow));
var (coords, distAu) = AsteroidEphemeris.GetPosition("ceres", T);

// Observer-relative horizontal coordinates (azimuth + altitude)
double jd = TimeZoneUtils.ToJulianDay(DateTime.UtcNow);
var obs = AsteroidEphemeris.GetObservation("ceres", jd, longitude: -87.65, latitude: 41.85);
// obs.Azimuth, obs.Altitude in degrees

// Inspect raw orbital elements
var elements = AsteroidEphemeris.GetElements("chiron", T);
// elements.SemiMajorAxisAu, elements.Eccentricity, etc.
```

> [!NOTE]
> The position algorithm (`SimplifiedPlanetPosition`) uses geocentric ecliptic longitude/latitude rather than a proper Earth-vector subtraction.
> RA/Dec are accurate to ~1° for main-belt asteroids; centaurs and TNOs are less precise.

---

## Astrological Houses

`AstrologicalHouses` in `Ephemeris.Astrology` computes house cusps using the RAMC (Right Ascension of the Midheaven Circle) and the obliquity of the ecliptic.

**Implemented systems:** Placidus, Equal, Whole Signs, Porphyry.  
**Stub systems** (throw `NotSupportedException`): Koch, Campanus, Regiomontanus.

```csharp
using Ephemeris.Astrology;

double jd = TimeZoneUtils.ToJulianDay(DateTime.UtcNow);

// Full 12-house chart
HouseCusps chart = AstrologicalHouses.Calculate(
    jd, longitude: -87.65, latitude: 41.85, HouseSystem.Placidus);

double ascendant  = chart.Ascendant;   // ecliptic degree of Asc (0–360°)
double midheaven  = chart.Midheaven;   // ecliptic degree of MC  (0–360°)
double house1Cusp = chart.Cusps[0];    // same as Ascendant for Placidus

// Individual angle helpers
double ramc = TimeUtils.GMST(jd) + (-87.65); // RAMC = GMST + longitude
double obliquity = AstrologicalHouses.ObliquityOfEcliptic(TimeUtils.JulianCentury(jd));
double mc  = AstrologicalHouses.ComputeMC(ramc, obliquity);
double asc = AstrologicalHouses.ComputeAscendant(ramc, obliquity, latitude: 41.85);
```

---

## Further Reading

- [Algorithm Reference](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference) — all algorithms with formulas and source citations
- [SPK/BSP Format](https://github.com/wforney/ephemeris/wiki/SPK-BSP-Format) — NAIF DAF binary kernel format
- [SE1 Format](https://github.com/wforney/ephemeris/wiki/SE1-Ephemeris-Format) — Swiss Ephemeris binary format
- [Root README](../README.md) — solution overview, build instructions, usage examples
