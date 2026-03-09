<!-- Updated: 2026-03-09 03:49 UTC -->
# Copilot Instructions

## Project Overview

**Ephemeris** is a .NET 9.0 astronomical calculations library that computes positions of celestial bodies (Sun, Moon, planets) as seen from any observer location on Earth. The solution has three projects:

- **Ephemeris** — Core class library (the calculation engine)
- **Ephemeris.Tests** — Test suite using TUnit
- **Ephemeris.UI** — WinForms visualization app (Windows only, `net9.0-windows`)

## Build & Test Commands

```bash
dotnet restore
dotnet build
dotnet build -c Release

# Run all tests
dotnet test

# Run tests in the test project specifically
dotnet test Ephemeris.Tests

# Run a single named test (TUnit)
dotnet test --filter "FullyQualifiedName~<TestMethodName>"

# Run the WinForms UI
dotnet run --project Ephemeris.UI
```

Code style is enforced at build time via `EnforceCodeStyleInBuild` and `.editorconfig`. Overflow checking is enabled in both Debug and Release configurations.

## Architecture

The core library (`Ephemeris/`) is organized into domain namespaces that mirror astronomical subdisciplines:

| Namespace | Domain |
|-----------|--------|
| `Ephemeris.Chronology` | Julian Day, ΔT, GMST, sidereal time |
| `Ephemeris.Heliology` | Solar ephemeris (RA/Dec, ecliptic coords) |
| `Ephemeris.Selenography` | Lunar ephemeris (RA/Dec, phase, illumination) |
| `Ephemeris.Planetology` | Planetary positions via Kepler's equations |
| `Ephemeris.Geometry` | Equatorial↔Horizontal coordinate transforms |
| `Ephemeris.Export` | CSV/JSON serialization of `EphemerisRecord` |
| `Ephemeris.Import` | SPICE kernel and DE430 ephemeris data import |

**Public entry points** are in the root `Ephemeris` namespace:
- `EphemerisCalculator` — high-level API for single-instant position queries
- `EphemerisBatch` — generates time-series `EphemerisRecord` collections
- `EphemerisPlotter` — ASCII console visualization

## Key Conventions

### Static, Pure Calculation Classes
Domain logic lives in static classes with pure functions (no mutable state). Example: `SunEphemeris`, `MoonEphemeris`, `ObserverGeometry`, `TimeUtils`. Do not introduce instance state into these classes.

### Value Tuple Returns
Multiple related values are returned as named value tuples, not separate classes:
```csharp
public static (double RA, double Dec, double Az, double Alt) GetSunPosition(...)
public static (double RA, double Dec, double distanceKm) GeocentricEquatorialCoordinates(double T)
```

### Julian Century `T` Parameter
The internal convention for time is **Julian Century** (`T`) — the number of Julian centuries since J2000.0 (JD 2451545.0). Most calculation methods in the domain namespaces take `T`, not `DateTime`. `TimeUtils` provides conversions.

### Dependency Injection Marker Interfaces
Services that need DI registration implement one of three marker interfaces:
```csharp
public interface IScopedService;
public interface ISingletonService;
public interface ITransientService;
```
Scrutor's assembly scanning handles automatic registration via `services.AddEphemerisServices()`. Add the appropriate marker interface to any new injectable service.

### Data Record
`EphemerisRecord` is a `readonly record struct` used as the universal data transfer type for batch results and export:
```csharp
public readonly record struct EphemerisRecord(
    DateTime TimeUtc, string Body,
    double RightAscension, double Declination,
    double Azimuth, double Altitude,
    double? Illumination);
```

### Code Style
- 4-space indentation, CRLF line endings (enforced by `.editorconfig`)
- Nullable reference types fully enabled (`<Nullable>enable</Nullable>`)
- Implicit usings enabled
- XML documentation required (doc file is generated as part of build)

## WinForms UI (Ephemeris.UI)

`EphemerisPlotForm` is the sole form. It takes `IEnumerable<EphemerisRecord>` and a body name, then renders an altitude-vs-time scatter chart:

```csharp
public EphemerisPlotForm(IEnumerable<EphemerisRecord> records, string body)
```

**ScottPlot pattern:**
- `ScottPlot.WinForms.FormsPlot` docked `Fill` to the form
- Time axis uses `DateTime.ToOADate()` (OLE Automation dates)
- Altitude axis is raw degrees
- `plt.Axes.AutoScale()` + `formsPlot.Refresh()` to redraw

**OpenTK/SkiaSharp** are referenced but not yet wired up — no `GLControl` instances exist. Do not remove those references; they are reserved for future 3D rendering.

`Program.cs` bootstraps with an empty dataset; replace the `List<EphemerisRecord> allData = []` with real batch output before running.

## SPICE / DE430 Import Pipeline

### Data flow
```
SPICE .bsp kernel
  → SpiceKernelDatabase.LoadKernel()
  → ConvertUtcToEphemerisTime()   (DateTime → ET)
  → GetPosition(target, ET, "J2000", observer)  → double[3] Cartesian
  → BspImporter.CartesianToRaDec()  → (RA°, Dec°)
  → ObserverGeometry.EquatorialToHorizontal()
  → EphemerisRecord
```

### Status
All three internal provider interfaces (`ISpaceKernelProvider`, `ITimeConverter`, `IStateVectorProvider`) throw `NotImplementedException` — they are stubs awaiting full SpiceSharp-Parser integration. The public API surface and data flow are complete; only the provider bodies need implementing.

### DE430 binary format
Each record is exactly 24 bytes: `int64` ticks → `double` RA (°) → `double` Dec (°). `DE430Importer.LoadFromBinary()` reads sequentially with `BinaryReader`.

## Coordinate System Conventions

All angles are **degrees at the API boundary**; internal trigonometry converts to radians inline.

| Value | Range | Notes |
|-------|-------|-------|
| RA | [0, 360) | Right ascension, degrees |
| Dec | [−90, 90] | Declination, degrees |
| Azimuth | [0, 360) | From North, clockwise; E = 90° |
| Altitude | [−90, 90] | Positive = above horizon |
| Longitude | degrees | East positive |
| Latitude | degrees | North positive |
| Julian Day | fractional JD | UTC epoch |
| `T` | Julian centuries | `(JD − 2451545.0) / 36525.0` |

**Obliquity of ecliptic** (used by `CoordinateConverter`):
```
ε = 23.439291° − 0.0130042° × T
```

**GMST** (degrees, normalized to [0, 360)):
```
GMST = 280.46061837 + 360.98564736629 × (JD − J2000) + 0.000387933 × T² − T³/38710000
```

`ObserverGeometry.EquatorialToHorizontal` pipeline:
1. `LST = GMST(JD) + longitude` → normalize
2. `H = LST − RA` → Hour Angle
3. Standard spherical trig → Altitude, Azimuth
4. If `sin(H) > 0`, mirror azimuth: `Az = 2π − Az`

## Auto-Updating Scripts, Agents, and Prompts

When modifying any of the following file types, **automatically update all related files** in the same change set and add or refresh a date stamp comment at the top:

- Shell / PowerShell scripts (`.sh`, `.ps1`, `.cmd`)
- GitHub Actions workflows (`.github/workflows/*.yml`)
- Copilot prompt files and agent definitions (`.github/copilot-instructions.md`, `*.prompt.md`, `*.agent.md`)
- Skill / tool definitions

**Date stamp format** — use an ISO 8601 date on the first or second line, in a comment appropriate for the file type:

```bash
# Updated: 2026-03-09
```
```yaml
# Updated: 2026-03-09
```
```csharp
// Updated: 2026-03-09
```
```markdown
<!-- Updated: 2026-03-09 -->
```

**Trigger rules:**
- A script that calls a method whose signature changed → update the script and refresh its date stamp.
- A workflow that builds, tests, or deploys → re-verify command flags match current project structure and refresh the stamp.
- This instructions file → refresh the stamp whenever content changes.
- Any prompt or agent file that references a class, namespace, or method → update references and refresh the stamp.

## Git Repository Management

**Remote:** `https://github.com/wforney/ephemeris.git`  
**Default branch:** `main` (single branch; no long-lived feature branches in history)

### Commit message convention
Use **Conventional Commits** format. **All commits** must follow this style — no exceptions. Subject line ≤ 72 characters, **imperative mood** ("Add", not "Added"; "Fix", not "Fixed"; "Remove", not "Removed"):

```
<type>(<scope>): <short description>

[optional body — wrap at 72 chars, explain *why* not *what*]

[optional footer(s)]
Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

**Types:**

| Type | Use for |
|------|---------|
| `feat` | New public API, new calculation, new UI control |
| `fix` | Wrong astronomical result, incorrect algorithm, crash |
| `perf` | Measurable performance improvement (include before/after) |
| `refactor` | Internal restructuring with no behavior change |
| `test` | Adding or updating TUnit tests |
| `docs` | XML doc comments, README, `copilot-instructions.md` |
| `chore` | Build config, `.csproj`, NuGet bumps, CI workflow tweaks |
| `style` | `.editorconfig`-driven formatting only (no logic change) |
| `revert` | Reverting a prior commit (reference it in the body) |

**Scopes** (use the domain namespace, lowercase):

`chronology` · `heliology` · `selenography` · `planetology` · `geometry` · `import` · `export` · `ui` · `batch` · `calculator` · `deps` · `ci`

**Examples:**
```
feat(selenography): add libration calculation to MoonEphemeris

fix(geometry): correct azimuth quadrant flip for southern hemisphere

perf(batch): replace List<T> allocation with ArrayPool in GenerateSunSeries
Before: 120ms / 1440 records   After: 34ms / 1440 records

chore(deps): bump ScottPlot from 5.0.55 to 5.0.60

docs(chronology): add XML doc to DeltaT polynomial branches
```

**Breaking changes:** append `!` after the scope and add a `BREAKING CHANGE:` footer:
```
feat(calculator)!: rename GetSunPosition parameters to use TimeZoneInfo

BREAKING CHANGE: timeZoneId string parameter replaced with TimeZoneInfo object
```

### Branching
- Work directly on `main` for small changes.
- For larger features create a short-lived branch: `feat/<scope>/<description>` (e.g., `feat/import/spicesharp-providers`).
- Delete branches after merge.

### GitHub operations via MCP
Use the **GitHub MCP server** (configured in `.vscode/mcp.json`) for:
- Creating and reviewing issues and pull requests
- Searching code and commit history
- Checking workflow run status

Do **not** push secrets, binary ephemeris kernels (`.bsp`, `.bpc`), or large DE430 data files — add them to `.gitignore` instead.

### Auto-update on git operations
When creating a commit that modifies scripts, workflows, or prompt files, refresh their date stamps in the same commit (see [Auto-Updating Scripts, Agents, and Prompts](#auto-updating-scripts-agents-and-prompts) below).

## MCP Servers

Configured in `.vscode/mcp.json`. Four servers are available:

| Server | Type | Purpose |
|--------|------|---------|
| `github` | HTTP (Copilot-hosted) | Issues, PRs, code search, workflow runs, branch management |
| `dotnet` | stdio (`Community.Mcp.DotNet` via `dnx`) | Build, test, add/update NuGet packages, scaffold projects, query SDK templates |
| `filesystem` | stdio (`@modelcontextprotocol/server-filesystem`) | Extended file read/write/search within the workspace |
| `fetch` | stdio (`@modelcontextprotocol/server-fetch`) | Fetch JPL Horizons data, IERS ΔT tables, SPICE documentation, or any HTTP resource |

> **Note:** The `dotnet` server requires .NET 10 SDK (for the `dnx` runner). It works alongside this project's .NET 9 target — `dnx` runs the MCP server tool itself, not the project.

### Typical uses in this project
- **`github`** — open an issue for a failing lunar calculation, check CI status, search for prior SPICE integration attempts
- **`dotnet`** — add a NuGet package (`dotnet add package`), run tests, scaffold a new domain class from a template, check available SDK versions
- **`filesystem`** — bulk-read or pattern-scan source files beyond what grep provides
- **`fetch`** — retrieve current ΔT values from IERS Bulletin A, download SPICE kernel metadata from NAIF, or pull the latest VSOP87 coefficient tables

## Key Dependencies

- **ScottPlot 5** — charting in both the core library and WinForms UI
- **Scrutor** — assembly scanning for automatic DI registration
- **DotNext / DotNext.Threading** — advanced .NET utilities and async threading
- **CommunityToolkit.Mvvm** — MVVM helpers
- **OpenTK + SkiaSharp** — OpenGL and Skia rendering in the WinForms UI (reserved for future use)
- **SpiceSharp-Parser** — SPICE kernel parsing (provider stubs not yet implemented)
- **TUnit** — test framework (not xUnit/NUnit)
