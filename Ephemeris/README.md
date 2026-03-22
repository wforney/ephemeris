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
| `Ephemeris.Chronology` | Timekeeping | `TimeUtils`, `TimeZoneUtils`, `ProlepticDate` | [Timekeeping](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#timekeeping-chronology) |
| `Ephemeris.Heliology` | Solar ephemeris | `SunEphemeris` | [Solar](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#solar-ephemeris-heliology) |
| `Ephemeris.Selenography` | Lunar ephemeris | `MoonEphemeris`, `TopocentricParallax` | [Lunar](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#lunar-ephemeris-selenography) |
| `Ephemeris.Planetology` | Planetary positions | `PlanetEphemeris`, `PlanetPhysicalEphemeris`, `PlanetPositionService`, `AsteroidEphemeris` | [Planetary](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#planetary-positions-planetology) |
| `Ephemeris.Geometry` | Coordinate types & transforms | `ObserverGeometry`, `CoordinateConverter`, `EquatorialCoordinates`, `HorizontalCoordinates` | [Transforms](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#coordinate-transforms-geometry) |
| `Ephemeris.Geodesy` | Earth corrections | `NutationCalculator`, `PrecessionCalculator`, `RefractionCalculator` | [Nutation & Precession](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#nutation--precession-geodesy) |
| `Ephemeris.Phenomenology` | Observable events | `RiseSetCalculator`, `EclipseCalculator`, `SeasonCalculator`, `PlanetaryEventCalculator`, `InnerPlanetEventCalculator`, `CelestialEventDetector`, `BiblicalCalendarHelper` | [Phenomena](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference#observable-phenomena-phenomenology) |
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
| `ProlepticDate` | `readonly struct` | BCE/BC date using Julian Day Number; `FromBce(year,month,day)`, `ToJulianDay()`, `FromJulianDay(jd)`, `ToHistoricalString()`, `ToAstronomicalString()`; `IEquatable`/`IComparable` (Meeus Ch. 7) |
| `FixedStar` | `readonly record struct` | J2000.0 position + proper motion + parallax |
| `HouseCusps` | `readonly record struct` | 12 astrological house cusps + four Angles (ASC, MC, DSC, IC) in ecliptic degrees |
| `HouseSystem` | `enum` | Placidus, Equal, WholeSigns, Porphyry, Koch, Campanus, Regiomontanus (all implemented) |

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

`AsteroidEphemeris` in `Ephemeris.Planetology` computes geocentric equatorial coordinates and observer-relative horizontal coordinates for 35 minor planets using J2000.0 Keplerian osculating elements from the JPL Small Body Database / IAU MPC.

**Supported bodies (35 total):**

| Category | Bodies |
|----------|--------|
| Classical Big Four | Ceres, Pallas, Juno, Vesta |
| Main belt | Astraea (5), Hebe (6), Iris (7), Flora (8), Metis (9), Hygiea (10), Victoria (12), Eunomia (15), Psyche (16), Fortuna (19), Proserpina (26), Harmonia (40), Isis (42), Sappho (80), Nemesis (128) |
| Near-Earth / Mars-crossing | Eros (433), Amor (1221), Icarus (1566) |
| Comet-like orbit | Hidalgo (944) |
| Centaurs | Chiron (2060), Pholus (5145), Nessus (7066), Asbolus (8405), Chariklo (10199), Hylonome (10370) |
| TNOs / Dwarf planets | Quaoar (50000), Orcus (90482), Haumea (136108), Makemake (136472), Eris (136199), Sedna (90377) |

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

// Enumerate all supported bodies
foreach (string name in AsteroidEphemeris.SupportedAsteroids)
    Console.WriteLine(name);
```

> [!NOTE]
> [!NOTE]
> `GetPosition` returns true **geocentric** RA/Dec by subtracting Earth's heliocentric position vector from the asteroid's (Meeus Ch. 33). `GetObservation` derives altitude/azimuth from these geocentric coordinates.
> RA/Dec accuracy: ~0.5–3° for main-belt asteroids; ~1–5° for centaurs; ~3–10° for high-eccentricity TNOs (Sedna, Eris).

---

## Astrological Houses

`AstrologicalHouses` in `Ephemeris.Astrology` computes house cusps using the RAMC (Right Ascension of the Midheaven Circle) and the obliquity of the ecliptic.

**Implemented systems (all seven):**

| System | Description |
|--------|-------------|
| Placidus | Time-based semi-arc division; most widely used modern Western system |
| Equal | Each house exactly 30°, starting from the Ascendant |
| Whole Signs | Each house = one complete zodiac sign, anchored to the Ascendant's sign |
| Porphyry | Each quadrant (MC–ASC–IC–DSC) trisected into equal ecliptic arcs |
| Koch | Trisects the diurnal semi-arc of the MC degree at birth latitude |
| Campanus | Prime vertical divided into 12 equal arcs; great circles through East/West horizon |
| Regiomontanus | Celestial equator divided into 12 equal arcs; great circles through N/S horizon |

```csharp
using Ephemeris.Astrology;

double jd = TimeZoneUtils.ToJulianDay(DateTime.UtcNow);

// Full 12-house chart
HouseCusps chart = AstrologicalHouses.Calculate(
    jd, longitude: -87.65, latitude: 41.85, HouseSystem.Placidus);

double ascendant  = chart.Ascendant;   // ecliptic degree of Asc (0–360°)
double midheaven  = chart.Midheaven;   // ecliptic degree of MC  (0–360°)
double house1Cusp = chart.Cusps[0];    // same as Ascendant for quadrant-based systems

// Individual angle helpers
double ramc = TimeUtils.NormalizeDegrees(TimeUtils.GMST(jd) + (-87.65));
double obliquity = AstrologicalHouses.ObliquityOfEcliptic(TimeUtils.JulianCentury(jd));
double mc  = AstrologicalHouses.ComputeMC(ramc, obliquity);
double asc = AstrologicalHouses.ComputeAscendant(ramc, obliquity, latitude: 41.85);
```

> [!NOTE]
> WholeSigns sets H1 cusp to the sign boundary (a multiple of 30°), not the Ascendant itself.
> Equal House sets H10 = ASC + 270°, not the Midheaven.

---

## Proleptic Dates (BCE/BC Support)

`ProlepticDate` in `Ephemeris.Chronology` represents calendar dates before year 1 CE using
Julian Day Number internally (Meeus Ch. 7), enabling the Ephemeris engine to compute
celestial positions for any historical epoch including Hezekiah's Sundial (~701 BCE) and
Joshua's Long Day (~1406 BCE).

```csharp
using Ephemeris.Chronology;

// Create a BCE date
var date = ProlepticDate.FromBce(701, 8, 1);     // 701 BCE, August 1 → Year = -700
double jd = date.ToJulianDay();                  // ≈ 1502917
double T  = TimeUtils.JulianCentury(jd);        // pass to SunEphemeris, MoonEphemeris, etc.

// Formatting
Console.WriteLine(date.ToHistoricalString());    // "701 BCE Aug 01"
Console.WriteLine(date.ToAstronomicalString());  // "-0700-08-01"

// Round-trip from Julian Day
ProlepticDate back = ProlepticDate.FromJulianDay(jd);

// Comparison
bool earlier = ProlepticDate.FromBce(1406, 6, 21) < date; // true (1406 BCE is earlier)
```

---

## Celestial Event Detection

`CelestialEventDetector` in `Ephemeris.Phenomenology` scans date ranges for notable
astronomical events — full/new moons, equinoxes, solstices, and eclipses.

```csharp
using Ephemeris.Phenomenology;

// Scan for events in a window
IReadOnlyList<CelestialEventDetector.CelestialEvent> events =
    CelestialEventDetector.Scan(DateTime.UtcNow, DateTime.UtcNow.AddMonths(6));

// Get the next N events
IReadOnlyList<CelestialEventDetector.CelestialEvent> next10 =
    CelestialEventDetector.GetNext(DateTime.UtcNow, count: 10);

foreach (var ev in next10)
    Console.WriteLine($"{ev.UtcTime:yyyy-MM-dd} — {ev.Description}");
// e.g. "2026-04-13 — Full Moon"
//      "2026-03-20 — Vernal Equinox"
```

---

## Biblical Calendar

`BiblicalCalendarHelper` in `Ephemeris.Phenomenology` computes approximate Hebrew luni-solar
calendar data from Julian Day and observer location.

```csharp
using Ephemeris.Phenomenology;

double jd = TimeZoneUtils.ToJulianDay(DateTime.UtcNow);
BiblicalCalendarHelper.BiblicalDate date =
    BiblicalCalendarHelper.GetBiblicalDate(jd, longitude: 35.22, latitude: 31.77);

Console.WriteLine($"Hebrew year: {date.Year}");          // e.g. 5786
Console.WriteLine($"Month: {date.MonthName}");           // e.g. "Nisan"
Console.WriteLine($"Sun in: {date.SolarSign}");          // e.g. "Aries (Taleh)"
Console.WriteLine($"Crescent: {date.IsNewMoonVisibility}"); // true/false

// Direct helpers
string sign = BiblicalCalendarHelper.GetMazzarothSign(sunLon: 45.0); // "Taurus (Shor)"
bool visible = BiblicalCalendarHelper.IsCrescentVisible(jd, 35.22, 31.77);
```

---

## Further Reading

- [Algorithm Reference](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference) — all algorithms with formulas and source citations
- [SPK/BSP Format](https://github.com/wforney/ephemeris/wiki/SPK-BSP-Format) — NAIF DAF binary kernel format
- [SE1 Format](https://github.com/wforney/ephemeris/wiki/SE1-Ephemeris-Format) — Swiss Ephemeris binary format
- [Root README](../README.md) — solution overview, build instructions, usage examples
