# Ephemeris.UI.Shared

Cross-platform class library holding the view-model and messaging types
shared between `Ephemeris.UI` (WinForms) and `Ephemeris.UI.Avalonia`.

**Target:** `net10.0` · **No UI framework dependency**

---

## Contents

| Type | Namespace | Description |
|---|---|---|
| `SkyViewModel` | `Ephemeris.UI` | Observable view-model: observer position, sim time, camera, animation |
| `ObserverChangedMessage` | `Ephemeris.UI.Messages` | Sent when observer longitude or latitude changes |
| `ObserverLocation` | `Ephemeris.UI.Messages` | Value record carrying longitude + latitude snapshot |
| `SimTimeChangedMessage` | `Ephemeris.UI.Messages` | Sent when simulation time advances or is reset |

---

## Usage

Both UI projects reference this library:

```xml
<ProjectReference Include="..\Ephemeris.UI.Shared\Ephemeris.UI.Shared.csproj" />
```

The `SkyViewModel` exposes relay commands and observable properties via
`CommunityToolkit.Mvvm`. The messages are sent via `WeakReferenceMessenger`
so that the launcher window can track state across sky view sessions without
holding a direct reference to the sky view.

---

## Further Reading

- [Ephemeris.UI README](../Ephemeris.UI/README.md) — WinForms UI (Windows-only)
- [Ephemeris.UI.Avalonia README](../Ephemeris.UI.Avalonia/README.md) — Avalonia UI (cross-platform)
