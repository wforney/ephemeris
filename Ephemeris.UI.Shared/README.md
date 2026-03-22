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
| `ICelestialResearchService` | Interface: `GetDataAsync(utcTime, lon, lat)` → `CelestialResearchData` |
| `CelestialResearchService` | Default implementation; wraps `EphemerisCalculator` + `RiseSetCalculator`; singleton via `ISingletonService` |
| `CelestialResearchData` | Immutable record: Sun/Moon observations, rise/set times, next lunar phase events |

### Models (`Ephemeris.UI.Models`)

| Type | Description |
|---|---|
| `SimulationOverride` | Observable overrides: freeze motion, Sun altitude offset, extended daylight |
| `SessionModel` | Persisted session (JSON); `FromWorkspace`, `LoadAsync`, `SaveAsync` |
| `ScenarioModel` | Immutable preset record: name, scripture reference, date, location |
| `BuiltInScenarios` | Static presets: `HezekiahSundial`, `JoshuasLongDay`, `All` |

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

// Apply a built-in scenario preset
vm.LoadScenarioCommand.Execute(BuiltInScenarios.HezekiahSundial);

// Manually trigger a data load
await vm.LoadDataCommand.ExecuteAsync(null);

// Access results
CelestialResearchData? data = vm.CelestialData;
```

---

## Further Reading

- [Ephemeris.UI README](../Ephemeris.UI/README.md) — WinForms UI (Windows-only)
- [Ephemeris.UI.Avalonia README](../Ephemeris.UI.Avalonia/README.md) — Avalonia UI (cross-platform)
- [Algorithm Reference](../docs/algorithm-reference.md) — Meeus formula citations
