# Ephemeris.UI

Windows Forms visualization application for the `Ephemeris` library.

**Target:** `net10.0-windows` · **Windows only**

> **Linux / macOS users:** See [`Ephemeris.UI.Avalonia`](../Ephemeris.UI.Avalonia/README.md) for
> the cross-platform Avalonia version that runs on Windows, Linux, and macOS.

---

## Running the App

```bash
dotnet run --project Ephemeris.UI/Ephemeris.UI.csproj
```

Or open `Ephemeris.sln` in Visual Studio and set `Ephemeris.UI` as the startup project.

> **Note:** The app launches with an empty dataset. To see real data, replace the placeholder
> in `Program.cs` with a call to `EphemerisBatch.GenerateSunSeries(...)` or similar before
> calling `Application.Run(new LauncherForm(...))`.

---

## Forms

### `LauncherForm`

Application entry point. Allows the user to configure the observer location and simulation time before opening the sky view.

Uses `CommunityToolkit.Mvvm` `WeakReferenceMessenger` to broadcast:
- `ObserverChangedMessage` — when the observer's longitude/latitude/elevation changes
- `SimTimeChangedMessage` — when the simulation time is adjusted

### `SkyViewForm`

Interactive real-time sky visualization using OpenTK 4 (`GLControl`) for OpenGL rendering and SkiaSharp for 2D label overlay.

Renders: stars (from built-in catalog), Sun, Moon, and all planets as coloured dots at their correct azimuth/altitude positions.

### `EphemerisPlotForm`

Altitude-vs-time scatter chart for a single body using **ScottPlot 5 WinForms**.

```csharp
// Construct with pre-computed records and a body name
var form = new EphemerisPlotForm(records, "Moon");
form.Show();
```

Chart details:
- X axis: UTC time (OLE Automation dates via `DateTime.ToOADate()`)
- Y axis: altitude in degrees
- `plt.Axes.AutoScale()` + `formsPlot.Refresh()` to redraw after data updates

---

## MVVM Pattern

`SkyViewModel` is the view model for `SkyViewForm`, built with `CommunityToolkit.Mvvm`:

```csharp
// Observable properties auto-generate INotifyPropertyChanged via source generator
[ObservableProperty] private DateTime _simulationTime;
[ObservableProperty] private double _observerLongitude;
```

---

## Key Dependencies

| Package | Purpose |
|---------|---------|
| `CommunityToolkit.Mvvm` 8.4.0 | MVVM helpers, `ObservableProperty`, `WeakReferenceMessenger` |
| `OpenTK` 4.9.4 | OpenGL rendering context |
| `OpenTK.GLControl` 4.0.2 | WinForms-hosted OpenGL control |
| `SkiaSharp` (transitive) | 2D label overlay on sky view |
| `ScottPlot.WinForms` 5.1.59 | Altitude-vs-time chart |

> **Note:** `SkyViewModel` and the messaging types live in `Ephemeris.UI.Shared`
> (a cross-platform class library) so they can be reused by `Ephemeris.UI.Avalonia`
> without any WinForms dependency.

---

## Architecture Notes

- `SkyViewForm` owns the OpenGL render loop; Skia is composited on top for text labels.
- Cross-form state (observer location, sim time) flows via `WeakReferenceMessenger` — no direct form-to-form references.
- All ephemeris calculations are delegated to `Ephemeris` core library classes; the UI contains no astronomical math.

---

## Further Reading

- [Root README](../README.md) — solution overview and usage examples
- [Core Library README](../Ephemeris/README.md) — API reference
