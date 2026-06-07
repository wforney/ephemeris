# Ephemeris.Tests

Test suite for the `Ephemeris` core library. **420 tests** covering all calculation domains.

**Framework:** [TUnit](https://github.com/thomhurst/TUnit) · **Target:** `net10.0`

---

## Running Tests

```bash
# All tests
dotnet test --project Ephemeris.Tests/Ephemeris.Tests.csproj

# With coverage (Cobertura XML)
dotnet test --project Ephemeris.Tests/Ephemeris.Tests.csproj \
  --collect:"XPlat Code Coverage"

# Filter to a specific class or method
dotnet test --project Ephemeris.Tests/Ephemeris.Tests.csproj \
  --filter "FullyQualifiedName~SolarEphemerisTests"
```

Tests that require local ephemeris kernel files (`.bsp`, `.se1`) are automatically skipped when the files are not present.

---

## Test Categories

| File | Domain | What it tests |
|------|--------|---------------|
| `SolarEphemerisTests.cs` | Heliology | Sun RA/Dec/R against JPL Horizons reference values |
| `LunarEphemerisTests.cs` | Selenography | Moon geocentric position, phase, illumination |
| `TopocentricParallaxTests.cs` | Selenography | Parallax corrections for Moon, Sun, planets |
| `PlanetEphemerisTests.cs` | Planetology | Heliocentric/geocentric planet positions; Earth heliocentric |
| `AsteroidEphemerisTests.cs` | Planetology | Ceres/Pallas/Juno/Vesta/all 35 bodies: orbital elements and positions |
| `AstrologicalHousesTests.cs` | Astrology | Placidus, Equal, WholeSigns, Porphyry, Koch, Campanus, Regiomontanus cusps; MC/Asc angle helpers |
| `PlanetPhysicalEphemerisTests.cs` | Planetology | Angular diameter, magnitude, elongation |
| `PlanetaryEventTests.cs` | Phenomenology | Opposition, conjunction, quadrature, greatest elongation |
| `RiseSetCalculatorTests.cs` | Phenomenology | Sunrise/sunset/transit times |
| `CelestialEventDetectorTests.cs` | Phenomenology | Full/new moon scans, equinox/solstice detection, eclipse events, ordering, `CelestialEvent.CompareTo` |
| `ProlepticDateTests.cs` | Chronology | JD round-trip, `FromBce` factory, formatting, comparison operators, validation guards |
| `BiblicalCalendarHelperTests.cs` | Phenomenology | Sign boundaries, season mapping, month/year approximation, crescent logic |
| `GeometryTests.cs` | Geometry | Coordinate transforms, angular separation, refraction |
| `NutationCalculatorTests.cs` | Geodesy | IAU 1980 nutation Δψ/Δε |
| `PrecessionCalculatorTests.cs` | Geodesy | IAU 2006 precession angles |
| `StarCatalogTests.cs` | Stellarography | Built-in 25-star catalog, proper-motion, region queries |
| `StarEphemerisTests.cs` | Stellarography | Star position at current epoch |
| `SpkReaderTests.cs` | Import | DAF header parsing, segment graph, Chebyshev evaluation |
| `Se1EphemerisReaderTests.cs` | Import | SE1 binary header and segment parsing |
| `SpiceKernelDatabaseTests.cs` | Import | SPICE kernel load and position query |
| `BatchExportTests.cs` | Export | CSV/JSON round-trip for `EphemerisRecord` series |
| `TimeUtilitiesTests.cs` | Chronology | Julian Day, Julian Century, GMST, ΔT |
| `TimeZoneUtilsTests.cs` | Chronology | IANA/Windows timezone round-trips |
| `Phase3FeatureTests.cs` | Multi-domain | Integration tests across phase-3 features |
| `EphemerisExampleRun.cs` | Integration | End-to-end example run (non-asserting, smoke test) |

---

## Adding Tests

1. Add a new `*Tests.cs` file to `Ephemeris.Tests/Ephemeris.Tests/`.
2. Annotate test methods with TUnit's `[Test]` attribute.
3. Use `Assert.*` from TUnit (not xUnit/NUnit).

### Snapshot Testing (Verify)

For complex output (export files, multi-record series), use Verify snapshots:

```csharp
await Verify(records).UseDirectory("Snapshots");
```

- On first run a `.received.` file is created — review it, then rename to `.verified.`
- Commit only `*.verified.*` files; `*.received.*` are git-ignored.

### Mocking ([Imposter](https://github.com/themidnightgospel/Imposter))

Declare impostors at assembly level (one declaration per interface):

```csharp
[assembly: GenerateImposter(typeof(IMyService))]
```

Use in tests:

```csharp
var mock = IMyService.Imposter();
mock.Setup(x => x.Calculate(Arg.Any<double>())).Returns(42.0);
```

---

## Reference Values

Test assertions are calibrated against:

- **JPL Horizons** (`https://ssd.jpl.nasa.gov/horizons/`) — Sun, Moon, planet RA/Dec at specific epochs
- **Synthetic values** — computed from a trusted implementation and fixed as regression baselines
- Algorithm accuracy tolerances are documented in the [Algorithm Reference](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference)

---

## Dependencies

| Package | Purpose |
|---------|---------|
| `TUnit` 1.19.22 | Test framework and assertions |
| `Verify.TUnit` 31.13.2 | Snapshot assertions |
| `Imposter` 0.1.7 | Source-generated mocks |
| `Microsoft.AspNetCore.Mvc.Testing` | Integration test host |
