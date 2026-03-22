# Ephemeris.UI.Shared

Cross-platform class library holding view-models, services, models, and messaging
types shared between `Ephemeris.UI` (WinForms) and `Ephemeris.UI.Avalonia`.

**Target:** `net10.0` · **No UI framework dependency**

---

## Contents

### View-models (`Ephemeris.UI` / `Ephemeris.UI.ViewModels`)

| Type | Namespace | Description |
|---|---|---|
| `SkyViewModel` | `Ephemeris.UI` | Sky-view-specific: observer position, sim time, camera (Yaw/Pitch/FoV), animation |
| `WorkspaceViewModel` | `Ephemeris.UI.ViewModels` | Full research workspace: scenario loading, `CelestialData` retrieval, debounced auto-refresh |

### Services (`Ephemeris.UI.Services`)

| Type | Description |
|---|---|
| `ICelestialResearchService` | Interface: `GetDataAsync`, `GetDataForJulianDayAsync` (BCE/JD), `GetUpcomingEventsAsync` |
| `CelestialResearchService` | Default implementation; wraps `EphemerisCalculator` + `RiseSetCalculator` + `BiblicalCalendarHelper`; singleton via `ISingletonService` |
| `CelestialResearchData` | Immutable record: Sun/Moon observations, rise/set times, next lunar phase events, `BiblicalDate?` |
| `PlaybackEngine` | Drives simulation time forward/backward at configurable speed; fires `SimTimeChangedMessage` |

### Models (`Ephemeris.UI.Models`)

| Type | Description |
|---|---|
| `SimulationOverride` | Observable overrides: freeze motion, Sun altitude offset, extended daylight |
| `SessionModel` | Persisted session (JSON); `FromWorkspace`, `LoadAsync`, `SaveAsync` |
| `ScenarioModel` | Immutable sealed class: name, scripture reference, date, location, `ProlepticDate? HistoricalDate` |
| `BuiltInScenarios` | Static presets: `HezekiahSundial` (701 BCE), `JoshuasLongDay` (1406 BCE), `All` |

### ViewModels (`Ephemeris.UI.ViewModels`)

| Type | Description |
|---|---|
| `WorkspaceViewModel` | Full research workspace: scenario loading, `CelestialData` retrieval, debounced auto-refresh, BCE historical mode |
| `ComparisonViewModel` | Side-by-side comparison state: simulation override bindings, comparison data |

**`WorkspaceViewModel` key properties:**

| Property | Description |
|---|---|
| `CelestialData` | Latest `CelestialResearchData` from service |
| `HistoricalDate` | `ProlepticDate?` — set when a BCE scenario is loaded |
| `IsHistoricalMode` | `true` when `HistoricalDate` has value |
| `DisplayDate` | `"701 BCE Aug 01"` (historical) or `"2026-03-22 12:00 UTC"` (modern) |
| `CurrentJulianDay` | JD for current sim time or historical date |
| `UpcomingEvents` | `IReadOnlyList<CelestialEvent>` from `GetUpcomingEventsAsync` |

### Messages (`Ephemeris.UI.Messages`)

| Type | Description |
|---|---|
| `ObserverChangedMessage` | Sent when observer longitude or latitude changes |
| `ObserverLocation` | Value record carrying longitude + latitude snapshot |
| `SimTimeChangedMessage` | Sent when simulation time advances or is reset |

---

## Usage

Both UI projects reference this library:

```xml
<ProjectReference Include="..\Ephemeris.UI.Shared\Ephemeris.UI.Shared.csproj" />
```

### Dependency injection

Register `CelestialResearchService` by extending the Scrutor scan to include this assembly,
or register manually:

```csharp
services.AddSingleton<ICelestialResearchService, CelestialResearchService>();
```

### WorkspaceViewModel

```csharp
var vm = new WorkspaceViewModel(service, longitude: 35.2, latitude: 31.8);

// Apply a built-in scenario preset (BCE scenario → sets HistoricalDate)
vm.LoadScenarioCommand.Execute(BuiltInScenarios.HezekiahSundial);

// Check mode
Console.WriteLine(vm.IsHistoricalMode);  // true
Console.WriteLine(vm.DisplayDate);       // "701 BCE Aug 01"
Console.WriteLine(vm.CurrentJulianDay);  // ≈ 1502917

// Manually trigger a data load
await vm.LoadDataCommand.ExecuteAsync(null);

// Access results
CelestialResearchData? data = vm.CelestialData;
IReadOnlyList<CelestialEvent> events = vm.UpcomingEvents;
```

---

## Further Reading

- [Ephemeris.UI README](../Ephemeris.UI/README.md) — WinForms UI (Windows-only)
- [Ephemeris.UI.Avalonia README](../Ephemeris.UI.Avalonia/README.md) — Avalonia UI (cross-platform)
- [Algorithm Reference](../docs/algorithm-reference.md) — Meeus formula citations
