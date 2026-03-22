# Ephemeris.UI.Avalonia

Cross-platform visualization application for the `Ephemeris` library, built with
**[Avalonia UI 11](https://avaloniaui.net/)**.

Runs on **Windows, Linux, and macOS** — the Linux-compatible counterpart to `Ephemeris.UI`
(which targets `net10.0-windows` with WinForms).

**Target:** `net10.0` · **Cross-platform** (Windows / Linux / macOS)

---

## Why Avalonia?

| Requirement | Avalonia 11 |
|---|---|
| Linux / macOS support | ✅ — X11, Wayland (via XDG desktop portal), Quartz |
| SkiaSharp rendering | ✅ — Avalonia uses SkiaSharp internally; already a project dep |
| OpenGL sky view | ✅ — `OpenGlControlBase` (X11/EGL on Linux, WGL on Windows, CGL on macOS) |
| ScottPlot chart | ✅ — `ScottPlot.Avalonia` 5.1.57 |
| CommunityToolkit.Mvvm | ✅ — no WinForms dependency; shared via `Ephemeris.UI.Shared` |
| No GLFW / native window | ✅ — uses Avalonia's own platform abstraction; no `OpenTK.GLControl` |

---

## Running the App

```bash
dotnet run --project Ephemeris.UI.Avalonia/Ephemeris.UI.Avalonia.csproj
```

On Linux you need X11 or Wayland libraries installed:

```bash
# Debian/Ubuntu
sudo apt-get install libx11-dev libxrandr-dev libxi-dev libxcursor-dev libgl1-mesa-dev

# Fedora
sudo dnf install libX11-devel libXrandr-devel libXi-devel libXcursor-devel mesa-libGL-devel
```

The app opens `HomeWindow` — choose **New Research Session** to start, or select a scriptural preset.

---

## Windows / Views

### `HomeWindow` *(startup)*

Application entry point — replaces the legacy `LauncherWindow` as `MainWindow`.

- **New Research Session** → opens `ResearchWorkspaceWindow`
- **Load Scriptural Event** → opens `ScripturalEventLibraryWindow`
- **Resume Previous Session** → file picker for `.json` session files
- **Quick-Start form** — date / time / location → **LOAD SKY**
- **Recent sessions** list — last 3 `.json` files from `%APPDATA%/EphemerisResearch/sessions/`
- Legacy shortcuts: **3D Sky View** → `SkyViewWindow` · **Altitude Chart** → `EphemerisPlotWindow`

### `ResearchWorkspaceWindow`

Main research workspace:
- Sidebar: Sun/Moon Az/Alt readouts, rise/set times, next lunar phase, `BiblicalCalendarCard` section
- Scenario picker (`ComboBox` bound to `BuiltInScenarios.All`) — includes BCE presets
- Historical Mode: shows `DisplayDate` (e.g. "701 BCE Aug 01") + 📜 badge when BCE scenario loaded
- `EmptyStateControl` placeholder (🌌 "No Data") until first sky data loads

### `ComparisonWindow`

Side-by-side simulation comparison:
- Freeze motion toggle, Sun altitude offset slider, extended daylight toggle
- `EmptyStateControl` (⚖ "No Simulation Active") when no overrides are active
- `SkyDisplayToggleBar` for sky overlay controls

### `ScripturalEventLibraryWindow`

Preset library browser — lists `BuiltInScenarios.All` with scripture reference and description.
`EmptyStateControl` shown if no events are configured.

### `NotesPanel`

Collapsible research notes panel — auto-saved to `SessionModel.Notes`.

### `SkyViewWindow`

3D interactive sky view (existing, unchanged entry point from `HomeWindow`).

### `LauncherWindow`

Legacy entry point — still present; `HomeWindow` is the new default startup window.

---

## Controls

### `SkyGlControl`

Derives from `Avalonia.OpenGL.Controls.OpenGlControlBase`. Full feature set:

**Rendering:**
- Stars (magnitude/spectral-type coloured points, magnitude limit configurable)
- Sun, Moon, and inner/outer planets as coloured dots with optional labels
- Horizon ring (green line loop)
- Constellation line overlays (15 constellations, drawn as dim blue-white `GL_LINES`)
- Sun path arc (yellow, 24-hour daily arc) and Moon path arc (silver-blue)
- Mazzaroth ecliptic overlay (12 coloured bands with Hebrew names, `GL_LINES`)

**Display toggle properties:**

| Property | Default | Description |
|----------|---------|-------------|
| `ShowConstellations` | `false` | Constellation line pairs |
| `ShowStarLabels` | `false` | Labels for stars brighter than magnitude 2.0 |
| `ShowPlanetLabels` | `true` | Labels next to Sun/Moon/planets |
| `ShowHorizonGrid` | `true` | Horizon ring |
| `StarMagnitudeLimit` | `5.5` | Stars dimmer than this are hidden |
| `ShowSunPath` | `false` | 24-hour Sun altitude arc |
| `ShowMoonPath` | `false` | 24-hour Moon altitude arc |
| `ShowMazzarothOverlay` | `false` | 12 ecliptic Mazzaroth bands |
| `SimulationOverride` | `null` | Freeze / Sun-offset / daylight override |

### `SkyDisplayToggleBar`

`UserControl` toolbar with 7 `ToggleButton`s + magnitude `Slider` (0–7, default 5.5).
`Attach(SkyGlControl)` wires all toggles to the control. Used in `SkyViewWindow` and `ComparisonWindow`.

### `EmptyStateControl`

Reusable empty-state placeholder `UserControl` with `Icon`, `Title`, `Subtitle` styled properties.

### `BiblicalCalendarCard`

Sidebar `UserControl` displaying Hebrew calendar data:
- Hebrew Year, Month (with ordinal), Day
- Season, Sun-in-Mazzaroth sign, Crescent visibility status
- Populated via `Update(BiblicalCalendarHelper.BiblicalDate?)`

---

## Styles

### `ResearchTheme.axaml`

Resource dictionary merged into `App.axaml`. Provides the dark observatory colour palette:

| Resource | Value | Use |
|----------|-------|-----|
| `ResearchBackground` | `#0D0D1A` | Window background (very dark navy) |
| `ResearchSurface` | `#1A1A2E` | Panel background |
| `ResearchSidebar` | `#16213E` | Sidebar background |
| `ResearchAccent` | `#4A9EFF` | Buttons, highlights (blue) |
| `ResearchTextPrimary` | `#EEEEFF` | Primary text |
| `ResearchTextSecondary` | `#AAAACC` | Secondary / metadata text |

Class-based styles: `Button.Research`, `Button.ResearchSecondary`, `TextBox.Research`,
`TextBlock.Monospace` (Courier New), `Border.ResearchPanel`, `Border.ResearchSidebar`.

---

## OpenGL Architecture

### `SkyGlControl` (Controls/SkyGlControl.cs)

Derives from `Avalonia.OpenGL.Controls.OpenGlControlBase`. Key differences from the
WinForms `GLControl` approach:

| WinForms (`OpenTK.GLControl`) | Avalonia (`OpenGlControlBase`) |
|---|---|
| `Load` event | `OnOpenGlInit(GlInterface gl)` override |
| `Paint` event + `SwapBuffers` | `OnOpenGlRender(GlInterface gl, int fb)` override |
| `Dispose` cleanup | `OnOpenGlDeinit(GlInterface gl)` override |
| `gl.Invalidate()` | `RequestNextFrameRendering()` |
| OpenTK `GL.*` static methods | `GlInterface` methods + `GetProcAddress` delegates |

OpenGL 3.0+ functions not present in `GlInterface` base (VAO operations,
`glGetBufferSubData`, `glUniformMatrix4fv`) are loaded via
`GlInterface.GetProcAddress` and `Marshal.GetDelegateForFunctionPointer` at init time.

Matrix math uses `System.Numerics.Matrix4x4` (built into .NET) instead of `OpenTK.Mathematics`.

---

## Shared Code

`Ephemeris.UI.Shared` is a cross-platform class library that holds the
view-model and messaging types shared between the WinForms and Avalonia projects:

| Type | Namespace |
|---|---|
| `SkyViewModel` | `Ephemeris.UI` |
| `ObserverChangedMessage`, `ObserverLocation` | `Ephemeris.UI.Messages` |
| `SimTimeChangedMessage` | `Ephemeris.UI.Messages` |

---

## Key Dependencies

| Package | Purpose |
|---------|---------|
| `Avalonia` 11.3.12 | Core cross-platform UI framework |
| `Avalonia.Desktop` 11.3.12 | Desktop platform backends (X11, WGL, CGL) |
| `Avalonia.Themes.Fluent` 11.3.12 | Fluent design theme |
| `CommunityToolkit.Mvvm` 8.4.0 | MVVM helpers (shared with WinForms project) |
| `ScottPlot.Avalonia` 5.1.57 | Cross-platform `AvaPlot` chart control |

---

## Further Reading

- [Root README](../README.md) — solution overview
- [Ephemeris.UI README](../Ephemeris.UI/README.md) — WinForms (Windows-only) UI
- [Ephemeris.UI.Shared README](../Ephemeris.UI.Shared/README.md) — shared view-model and messages
- [Core Library README](../Ephemeris/README.md) — API reference
- [Avalonia OpenGL documentation](https://docs.avaloniaui.net/docs/guides/graphics-and-animation/opengl)
- [ScottPlot Avalonia quickstart](https://scottplot.net/quickstart/avalonia/)
