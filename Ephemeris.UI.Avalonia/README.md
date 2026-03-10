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

> **Note:** The app launches with an empty dataset. To see real data, replace the placeholder
> in `LauncherWindow.axaml.cs` (`List<EphemerisRecord> allData = []`) with a call to
> `EphemerisBatch.GenerateSunSeries(...)` or similar before opening the plot window.

---

## Windows / Views

### `LauncherWindow`

Application entry point (equivalent of `Ephemeris.UI.LauncherForm`).
Opens either the 3D sky view or the altitude chart.
Uses `CommunityToolkit.Mvvm.WeakReferenceMessenger` to track:
- `ObserverChangedMessage` — observer longitude/latitude changes
- `SimTimeChangedMessage` — simulation time advances

### `SkyViewWindow`

3D interactive sky view (equivalent of `Ephemeris.UI.SkyViewForm`).
Contains a `SkyGlControl` that derives from `Avalonia.OpenGL.Controls.OpenGlControlBase`.

Renders via OpenGL 3.3 shaders:
- Stars from the built-in catalog (magnitude/spectral-type coloured points)
- Sun, Moon, and inner/outer planets as coloured dots
- Horizon ring (green line loop)

Mouse drag = rotate view · scroll wheel = zoom · Space = play/pause · ←/→ = step day · F = now

### `EphemerisPlotWindow`

Altitude-vs-time scatter chart (equivalent of `Ephemeris.UI.EphemerisPlotForm`).
Uses **ScottPlot.Avalonia** (`AvaPlot` control).

```csharp
var window = new EphemerisPlotWindow(records, "Moon");
await window.ShowDialog(parent);
```

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
