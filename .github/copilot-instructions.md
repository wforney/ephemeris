<!-- Updated: 2026-06-29 (model sync) -->
# Copilot Instructions

## Session Checkpoints and Evolution

### Checkpoints — commit as you go
Commit after every logical unit of work; never let more than one cohesive change accumulate uncommitted. Checkpoint triggers:

- Completing a todo item or sub-task
- Any change to `.github/copilot-instructions.md`, `.vscode/mcp.json`, or a workflow file
- After a passing build or test run that validates a change
- Before switching context (e.g. moving from a `fix` to a `feat`)

Checkpoint commits follow the same Conventional Commits format. Use the body to note what remains if the work is mid-flight:
```
feat(selenography): add libration latitude calculation

Checkpoint: longitude term complete, latitude term in progress.
Remaining: node correction + test cases.

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

### Evolution — keep this file current
This instructions file is a living document. Update it whenever:

| Trigger | What to update |
|---------|----------------|
| New Copilot feature ships (agents, slash commands, extensions) | Add usage guidance in the relevant section |
| New MCP server becomes useful for this project | Add to `.vscode/mcp.json` and the MCP Servers section |
| New .NET major version becomes active LTS | Update version references throughout |
| New NuGet package added to the solution | Add to Key Dependencies if non-obvious |
| Domain namespace added or renamed | Update Architecture table |
| Commit or PR convention changes | Update Git Repository Management section |
| Model table drifts (weekly workflow) | Handled automatically by `update-models.yml` |
| Publish profiles added or release workflow changed | Update Release Procedures section |

**Automated audits** run on schedule and open GitHub issues with suggestions:
- `update-models.yml` — Mondays: syncs model catalog, opens PR if table changed
- `dependency-drift.yml` — Tuesdays: checks for newer .NET SDK and outdated NuGet packages, opens issue
- `evolve-instructions.yml` — Wednesdays: checks instruction file age, .NET channel drift, and unconfigured useful MCP servers, opens issue

When acting on an evolution issue: resolve it, update the relevant section, refresh the date stamp, and close the issue in the commit footer:
```
docs(ci): update instructions for .NET 11 and new MCP servers

Closes #42

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

## Agents

Reusable prompt files in `.github/prompts/`. Invoke via `/implement-calculation`, `/write-tests`, etc. in Copilot Chat, or assign to an issue via the GitHub Copilot coding agent.

| Agent | Model tier | Use for |
|-------|------------|---------|
| `implement-calculation` | opus | New astronomical calculations — fetches reference values, enforces static/pure pattern, verifies against JPL Horizons |
| `write-tests` | sonnet | TUnit tests with Rocks mocks, Verify snapshots, and external reference values |
| `add-xml-docs` | haiku | Eliminate all CS1591 warnings; consistent domain terminology |
| `implement-spice` | opus | Implement `ISpaceKernelProvider`, `ITimeConverter`, `IStateVectorProvider` stubs via SpiceSharp-Parser |
| `review-pr` | opus | PR review focused on astronomical correctness, algorithm accuracy, convention adherence |
| `refactor` | sonnet | Internal restructuring — preserves all public API contracts and angle conventions |
| `evolve` | sonnet | Maintain and evolve instructions, MCP config, workflows, and prompt files over time |
| `release` | sonnet | End-to-end release: pre-flight checks, version bump, tag, push, CI verification, rollback |

When creating a new agent: add a row here, pick the lowest model tier sufficient for the task, and follow the `<!-- Updated: -->` + frontmatter schema in the existing prompts.

## Model Selection

**This table is automatically synced weekly** by `.github/workflows/update-models.yml`, which queries the GitHub Models catalog API (`https://models.github.ai/catalog/models`) and opens a PR when the table changes. To trigger a manual sync, run the `Update model table` workflow from the Actions tab.

**Self-update rule:** At the start of any session, if you know of models newer or better-suited than those listed below, update this table, refresh the date stamp, and commit before proceeding with the task (use `chore(ci): sync model selection table <date>`).

### Task → model mapping

| Task | Recommended model(s) |
|------|----------------------|
| Orbital mechanics, coordinate transforms, Kepler solvers, VSOP87 | `openai/o3`, `openai/o3-mini`, `openai/o4-mini` | _Reasoning tier required — verify against JPL Horizons_
| Complete SPICE / DE430 provider stubs | `openai/o3`, `openai/o3-mini`, `openai/o4-mini` |
| Design new public API or multi-namespace refactor | `openai/gpt-4.1`, `openai/gpt-4o`, `openai/gpt-4.1-mini` |
| Write or fix TUnit tests | `openai/gpt-4.1`, `openai/gpt-4o`, `openai/gpt-4.1-mini` |
| PR diff review for astronomical correctness | `openai/o3`, `openai/o3-mini`, `openai/o4-mini` |
| Add XML doc comments or update this instructions file | `openai/gpt-4.1`, `openai/gpt-4.1-mini`, `openai/gpt-4.1-nano` |
| Quick single-file edits, typo/style fixes | `openai/gpt-5-mini` |

### Available models catalog

| Model ID | Publisher | Tier |
|----------|-----------|------|
| `cohere/cohere-command-a` | Cohere | standard |
| `deepseek/deepseek-v3-0324` | DeepSeek | premium |
| `deepseek/deepseek-r1` | DeepSeek | custom |
| `deepseek/deepseek-r1-0528` | DeepSeek | custom |
| `meta/llama-3.2-90b-vision-instruct` | Meta | premium |
| `meta/llama-3.3-70b-instruct` | Meta | premium |
| `meta/llama-4-maverick-17b-128e-instruct-fp8` | Meta | premium |
| `meta/llama-4-scout-17b-16e-instruct` | Meta | premium |
| `meta/meta-llama-3.1-405b-instruct` | Meta | premium |
| `meta/llama-3.2-11b-vision-instruct` | Meta | standard |
| `meta/meta-llama-3.1-8b-instruct` | Meta | standard |
| `microsoft/phi-4` | Microsoft | standard |
| `microsoft/phi-4-mini-instruct` | Microsoft | standard |
| `microsoft/phi-4-mini-reasoning` | Microsoft | standard |
| `microsoft/phi-4-multimodal-instruct` | Microsoft | standard |
| `microsoft/phi-4-reasoning` | Microsoft | standard |
| `mistral-ai/codestral-2501` | Mistral AI | standard |
| `mistral-ai/ministral-3b` | Mistral AI | standard |
| `mistral-ai/mistral-medium-2505` | Mistral AI | standard |
| `mistral-ai/mistral-small-2503` | Mistral AI | standard |
| `openai/gpt-4.1` | OpenAI | premium |
| `openai/gpt-4o` | OpenAI | premium |
| `openai/gpt-4.1-mini` | OpenAI | standard |
| `openai/gpt-4.1-nano` | OpenAI | standard |
| `openai/gpt-4o-mini` | OpenAI | standard |
| `openai/gpt-5` | OpenAI | custom |
| `openai/gpt-5-chat` | OpenAI | custom |
| `openai/gpt-5-mini` | OpenAI | custom |
| `openai/gpt-5-nano` | OpenAI | custom |
| `openai/o1` | OpenAI | custom |
| `openai/o1-mini` | OpenAI | custom |
| `openai/o1-preview` | OpenAI | custom |
| `openai/o3` | OpenAI | custom |
| `openai/o3-mini` | OpenAI | custom |
| `openai/o4-mini` | OpenAI | custom |
| `openai/text-embedding-3-large` | OpenAI | embeddings |
| `openai/text-embedding-3-small` | OpenAI | embeddings |

## Project Overview

**Ephemeris** is a .NET 10.0 astronomical calculations library that computes positions of celestial bodies (Sun, Moon, planets) as seen from any observer location on Earth. It powers the **Ephemeris Research App** — a celestial visualization and simulation platform for Biblical cosmology researchers studying the Mazzaroth and scriptural events (Hezekiah's Sundial, Joshua's Long Day).

The solution has six projects:

- **Ephemeris** — Core class library (the calculation engine)
- **Ephemeris.Tests** — Test suite using TUnit
- **Ephemeris.UI** — WinForms visualization app (Windows only, `net10.0-windows`)
- **Ephemeris.UI.Avalonia** — Cross-platform Avalonia UI (primary research app host; OpenGL sky canvas via `OpenGlControlBase`, charts via ScottPlot.Avalonia)
- **Ephemeris.UI.Shared** — Shared ViewModels, app state (`SkyViewModel`), and messaging used by both UI projects
- **Ephemeris.Benchmarks** — BenchmarkDotNet performance benchmarks

### Research App Context

The primary product goal is the **Ephemeris Research App** — see [`docs/research-app.md`](../docs/research-app.md) for user persona, use cases, and architecture. Key design intent:

- **User**: Biblical Cosmology & Astronomy Researcher studying celestial timekeeping and scriptural events
- **Core scenarios**: visualize any historical sky, simulate altered motion (freeze/reverse/extend daylight), compare normal vs. simulated views side by side, load predefined scriptural event presets
- **Architecture**: MVVM over a `CelestialResearchService` wrapper → Ephemeris core library
- **Build roadmap**: 25 GitHub issues across 4 milestones; tracked at https://github.com/wforney/ephemeris/issues

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
| `Ephemeris.Heliology` | Solar ephemeris (Meeus Ch. 25: RA/Dec, aberration, nutation, R) |
| `Ephemeris.Selenography` | Lunar ephemeris (Meeus Ch. 47: 60-term series, phase, illumination, libration) |
| `Ephemeris.Planetology` | Planetary and asteroid positions via iterative Kepler + orbital elements |
| `Ephemeris.Astrology` | Astrological house cusps — 7 systems (Placidus, Equal, Whole Signs, Porphyry, Koch, Campanus, Regiomontanus) |
| `Ephemeris.Geometry` | Equatorial↔Horizontal coordinate transforms |
| `Ephemeris.Geodesy` | Nutation (IAU 1980 50-term) and precession (IAU 2006) |
| `Ephemeris.Phenomenology` | Rise/set/transit, equinox/solstice, eclipses, visibility windows, planetary events |
| `Ephemeris.Stellarography` | Fixed star catalog (Yale BSC5), proper motion, precession to current epoch |
| `Ephemeris.Export` | CSV/JSON serialization of `EphemerisRecord` |
| `Ephemeris.Import` | Native DAF/SPK BSP reader, SE1 ephemeris reader, DE430 binary importer |

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

### Inline Documentation Standard

**All methods with non-trivial algorithms must have `<remarks>` explaining the algorithm**, whether public or private. This is a core project convention — astronomical code must be traceable to its source.

Required for every algorithmic method:
1. **`<summary>`** — one-line description of what it computes
2. **`<remarks>`** — cite the primary reference (e.g. "Meeus Ch. 25, Eq. 25.4"), list key formula steps, note accuracy and edge cases

```csharp
/// <summary>Computes the equation of centre for the Sun.</summary>
/// <param name="M">Sun's mean anomaly in degrees.</param>
/// <param name="T">Julian centuries since J2000.0.</param>
/// <returns>Equation of centre C in degrees.</returns>
/// <remarks>
/// Meeus Ch. 25, Eq. 25.4 — three-term sine series:
/// <code>
///   C = (1.914602 − 0.004817T − 0.000014T²) sin M
///     + (0.019993 − 0.000101T) sin 2M
///     + 0.000289 sin 3M
/// </code>
/// </remarks>
```

When adding or editing any calculation class:
- Add or update `<remarks>` on every non-trivial private helper
- Reference the specific equation number or section, not just the chapter
- Update the **[[Algorithm Reference|Algorithm-Reference]]** wiki page with the new formula
- Keep the wiki in sync: the wiki is the human-readable complement to the inline docs

### Per-Project README Standard

**Every project must have a `README.md`** in its project root directory. Update it whenever the project's API, dependencies, or behaviour changes.

Required sections per project type:

| Project type | Required sections |
|---|---|
| Class library | What it is, public entry points, namespace map with wiki links, data types, coordinate conventions, DI setup, key dependencies, further reading |
| Test project | Test count, how to run, test category table, how to add tests, snapshot/mock patterns, reference value sources |
| Benchmark project | How to run (Release only), benchmark classes table, how to add benchmarks, result interpretation |
| App (WinForms/console) | How to run, forms/components, MVVM pattern, key dependencies, architecture notes |

**Wiki links are required** in any README section that covers algorithms, formulas, or file formats — link to the relevant anchor in the [Algorithm Reference](https://github.com/wforney/ephemeris/wiki/Algorithm-Reference) or format spec pages.

The root `README.md` is the solution-level overview. It should always link to per-project READMEs and the wiki.

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

## Avalonia UI (Ephemeris.UI.Avalonia) — Research App

`Ephemeris.UI.Avalonia` is the **primary research application** and the main product UI. It is cross-platform (`net10.0`) and uses Avalonia 11.3.12.

**Key packages:** `Avalonia`, `Avalonia.Desktop`, `ScottPlot.Avalonia`  
**OpenGL:** sky rendering via `OpenGlControlBase` + `GlInterface.GetProcAddress`  
**Shared code:** ViewModels and messaging live in `Ephemeris.UI.Shared` (`SkyViewModel`, `Messages`)

### Research App Components

When building new UI features for the research app, follow this MVVM pattern:

| Layer | Location | Responsibilities |
|-------|----------|-----------------|
| View | `Ephemeris.UI.Avalonia/Views/` | AXAML + code-behind; data binding only |
| ViewModel | `Ephemeris.UI.Shared/ViewModels/` | Commands, state, observable properties |
| Service | `Ephemeris.UI.Shared/Services/` | `CelestialResearchService` — wraps Ephemeris core |
| Model | `Ephemeris.UI.Shared/Models/` | `SessionModel`, `ScenarioModel`, `SimulationOverride` |

**Scriptural presets** (Hezekiah's Sundial, Joshua's Long Day) are encoded as `ScenarioModel` instances with a historical date, observer location (lat/lon), and optional simulation parameters. See `docs/research-app.md` for full use-case descriptions.

**Simulation layer:** UI-level overrides (freeze motion, reverse, extend daylight) are stored in `SimulationOverride` and passed through `CelestialResearchService` before reaching the Ephemeris core. They do not modify the core library — all overrides are applied post-calculation in the service layer.



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
The native DAF/SPK BSP reader (`SpkReader`), leap-second-aware UTC→ET conversion (`SpkLeapSeconds`), and `BspImporter` pipeline are fully implemented. `SpiceKernelDatabase` provides the high-level API for loading kernels and querying positions. The SE1 binary reader (`Se1EphemerisReader`) and DE430 binary importer are also complete.

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
<!-- Updated: 2026-03-11 -->
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

`chronology` · `heliology` · `selenography` · `planetology` · `astrology` · `stellarography` · `geometry` · `geodesy` · `phenomenology` · `import` · `export` · `ui` · `batch` · `calculator` · `deps` · `ci`

**Examples:**
```
feat(selenography): add libration calculation to MoonEphemeris

fix(geometry): correct azimuth quadrant flip for southern hemisphere

perf(batch): replace List<T> allocation with ArrayPool in GenerateSunSeries
Before: 120ms / 1440 records   After: 34ms / 1440 records

chore(deps): bump ScottPlot from 5.0.55 to 5.1.57

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

### Pull requests

**Creating a PR:** Use the GitHub MCP server or `gh pr create`. Every PR should use the template at `.github/pull_request_template.md` — fill in the Summary, Changes, and tick the checklist before requesting review.

**Branch → PR flow:**
```
# Short-lived feature branch
git checkout -b feat/selenography/libration
# ... commits ...
gh pr create --fill --base main
```

**Merging:** Squash-merge into `main` with a Conventional Commits subject line. The squash commit message becomes the canonical history entry — ensure it follows the format exactly.

**Reviewing with MCP:** Use the `github` MCP server to:
```
# List open PRs
# Get PR diff and review comments
# Approve or request changes
# Check CI status on a PR head commit
# Merge when checks pass
```

**CI:** `.github/workflows/ci.yml` runs `dotnet build` + `dotnet test` on every push to `main` and on every PR. A PR must be green before merging.

### GitHub operations via MCP
Use the **GitHub MCP server** (configured in `.vscode/mcp.json`) for:
- Creating and reviewing issues and pull requests
- Searching code and commit history
- Checking workflow run status

Do **not** push secrets, binary ephemeris kernels (`.bsp`, `.bpc`), or large DE430 data files — add them to `.gitignore` instead.

### Auto-update on git operations
When creating a commit that modifies scripts, workflows, or prompt files, refresh their date stamps in the same commit (see [Auto-Updating Scripts, Agents, and Prompts](#auto-updating-scripts-agents-and-prompts) below).

## Release Procedures

Releases are triggered by pushing a **`v*` tag** to `main`. The `.github/workflows/release.yml` workflow runs two parallel jobs:

| Job | Runner | What it does |
|-----|--------|-------------|
| `nuget` | ubuntu-latest | Packs `Ephemeris.csproj` → uploads `.nupkg` to the GitHub Release |
| `publish-ui` (matrix × 4) | win/linux/macos | Publishes single-file UI executable per RID → uploads to GitHub Release |

### Release artifacts

Each GitHub Release contains:

| File | Description |
|------|-------------|
| `*.nupkg` | Ephemeris core library (NuGet) |
| `EphemerisApp-win-x64.exe` | Windows x64 self-contained single-file (~144 MB) |
| `EphemerisApp-linux-x64` | Linux x64 self-contained single-file |
| `EphemerisApp-osx-x64` | macOS Intel self-contained single-file |
| `EphemerisApp-osx-arm64` | macOS Apple Silicon self-contained single-file |

### Publish profiles

Four publish profiles live in `Ephemeris.UI.Avalonia/Properties/PublishProfiles/`. All set `SelfContained=true`, `PublishSingleFile=true`, `PublishReadyToRun=true`, `IncludeNativeLibrariesForSelfExtract=true`.

**Publish locally** (profiles work reliably on Windows):
```bash
dotnet publish Ephemeris.UI.Avalonia/Ephemeris.UI.Avalonia.csproj /p:PublishProfile=win-x64
# Output: Ephemeris.UI.Avalonia/bin/publish/win-x64/Ephemeris.UI.Avalonia.exe
```

**CI** passes flags explicitly (avoids `NETSDK1198` profile-not-found on macOS/Linux):
```bash
dotnet publish ... -c Release -r <rid> --self-contained true \
  -p:PublishSingleFile=true -p:PublishReadyToRun=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o Ephemeris.UI.Avalonia/bin/publish/<rid>
```

### Creating a release (use the `release` agent)

Invoke `/release` in Copilot Chat. The agent will:
1. Run pre-flight build and test checks
2. Bump `<Version>` in all project files
3. Commit, tag `v<VERSION>`, and push
4. Monitor CI and confirm all artifacts are attached

**Manual steps:**
```bash
# Edit <Version> in csproj files, then:
dotnet build -c Release && dotnet test
git add -A && git commit -m "chore(release): bump version to X.Y.Z"
git tag vX.Y.Z && git push origin main && git push origin vX.Y.Z
```

### Avalonia window constructor rule

All Avalonia `Window` subclasses that accept constructor parameters **must also have a public parameterless constructor** that calls only `InitializeComponent()` — required by the Avalonia XAML runtime loader (suppresses AVLN3001). Fields initialized only in the parameterized constructor should be declared `= null!` rather than `readonly`.

## MCP Servers

Seven servers are currently configured in `.vscode/mcp.json`:

| Server | Type | Purpose |
|--------|------|---------|
| `github` | HTTP (Copilot-hosted) | Issues, PRs, code search, workflow runs, branch management |
| `dotnet` | stdio (`Community.Mcp.DotNet` via `dnx`) | Build, test, add/update NuGet packages, scaffold projects, query SDK templates |
| `filesystem` | stdio (`@modelcontextprotocol/server-filesystem`) | Extended file read/write/search within the workspace |
| `fetch` | stdio (`python3 -m mcp_server_fetch`) | Fetch JPL Horizons data, IERS ΔT tables, SPICE documentation, or any HTTP resource |
| `sequential-thinking` | stdio (`@modelcontextprotocol/server-sequential-thinking`) | Structured multi-step reasoning for complex algorithm design and debugging plans |
| `memory` | stdio (`@modelcontextprotocol/server-memory`) | Persistent cross-session knowledge graph for durable project context |
| `brave-search` | stdio (`@modelcontextprotocol/server-brave-search`) | Web search for structured queries when `fetch` alone is less efficient (requires `BRAVE_API_KEY`) |

> **Note:** The `dotnet` server requires .NET 10 SDK (for the `dnx` runner). It works alongside this project's .NET 10 target — `dnx` runs the MCP server tool itself, not the project.
> **Note:** `brave-search` prompts for a Brave Search API key on first use. Obtain one at https://api.search.brave.com/app/keys.

### Typical uses in this project
- **`github`** — open an issue for a failing lunar calculation, check CI status, search for prior SPICE integration attempts
- **`dotnet`** — add a NuGet package (`dotnet add package`), run tests, scaffold a new domain class from a template, check available SDK versions
- **`filesystem`** — bulk-read or pattern-scan source files beyond what grep provides
- **`fetch`** — retrieve current ΔT values from IERS Bulletin A, download SPICE kernel metadata from NAIF, or pull the latest VSOP87 coefficient tables
- **`sequential-thinking`** — plan multi-step orbital mechanics implementations, structure debugging approaches for complex calculation failures
- **`memory`** — persist algorithm decisions, reference-value notes, and investigation findings across Copilot sessions
- **`brave-search`** — search for SPICE kernel documentation, IAU standards, or ephemeris algorithm references when `fetch` is less effective

## Key Dependencies

- **ScottPlot 5** — charting in both the core library and WinForms UI
- **Scrutor** — assembly scanning for automatic DI registration
- **DotNext / DotNext.Threading** — advanced .NET utilities and async threading
- **CommunityToolkit.Mvvm** — MVVM helpers
- **OpenTK + SkiaSharp** — OpenGL and Skia rendering in the WinForms UI (`SkyViewForm`)
- **TUnit** — test framework (not xUnit/NUnit)
- **[Rocks](https://github.com/JasonBock/Rocks)** — compile-time source-generated mocks (Roslyn); declare with `[assembly: Rock(typeof(IMyInterface), BuildType.Create)]`, use `new IMyInterfaceCreateExpectations()` in tests. Imposter (`[assembly: GenerateImposter(typeof(IMyInterface))]`) remains a valid alternative with a more fluent API.
- **Verify.TUnit** — snapshot assertions for complex outputs (`EphemerisRecord` series, CSV/JSON export); `*.received.*` files are git-ignored, commit only `*.verified.*`
