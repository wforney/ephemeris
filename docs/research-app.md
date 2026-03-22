<!-- Updated: 2026-03-22 -->
# Ephemeris Research App

A celestial research platform built on the Ephemeris calculation engine, designed for
Biblical cosmology researchers and astronomers who want to visualize, simulate, and
study scriptural celestial events.

> **Product definition:** A research-focused celestial visualization tool that helps users
> study the sun, moon, and stars across time and location, with special value for
> investigating Scriptural astronomy and Biblical cosmology scenarios.

---

## User Persona

**Biblical Cosmology & Astronomy Researcher**

A researcher focused on studying the Mazzaroth, scriptural events, and celestial
timekeeping in order to verify and deepen understanding of Scripture through the
heavens.

**Current challenges without the app:**
- Research relies on ancient writings, historical records, and manual cross-referencing
- Observation is limited by environmental conditions (e.g., cloud cover)
- No system exists to visualize or simulate celestial events, making verification difficult

**How the app helps:**
- Introduces a structured, visual, and time-based system for celestial analysis
- Transforms research from manual interpretation into interactive verification
- Provides a research companion for Biblical scholars, educators, and astronomy enthusiasts

---

## UX Principles

| Principle | Implementation |
|-----------|---------------|
| **Clear** | Interface should not feel overly technical or cluttered |
| **Visual** | Emphasize sky visualization and motion over raw numbers alone |
| **Research-oriented** | Help the user investigate, compare, and annotate findings |
| **Reverent and Serious** | Calm, thoughtful, substantial — not playful or gamified |
| **Fast to use** | Load a date, location, and sky view quickly |

**Visual tone:** Dark background (observatory-style), clean typography, soft contrast,
minimal clutter. Avoid bright arcade-style colors, overly playful icons, or
overcrowded engineering-dashboard layouts.

---

## Core Use Cases

### The Sign of King Hezekiah (2 Kings 20 / Isaiah 38)

The researcher investigates the event in which the Sun's shadow moved backward ten
degrees on the sundial of Ahaz.

**Research flow:**
1. Input the historical date and observer location (Jerusalem, ~701 BCE)
2. App visualizes normal solar motion for that day
3. Rewind or freeze time to the moment of the sign
4. Compare the expected solar trajectory versus the altered trajectory
5. Annotate findings in the Notes panel and save the session

### Joshua's Long Day (Joshua 10:12–14)

The Sun and Moon stood still as YAH granted victory over the Amorites.

**Research questions the app can address:**
- What does it mean for the Sun and Moon to "stand still"?
- How does paused celestial motion affect time and daylight?
- What was the dual positioning of Sun and Moon at that moment?

**Research flow:**
1. Input the date and location (Gibeon region, ~1406 BCE)
2. Observe natural solar and lunar motion
3. Pause celestial movement using the simulation controls
4. Extend daylight duration
5. Compare normal versus altered motion in side-by-side Comparison Mode
6. Study solar authority over time and the difference between paused vs. extended time

---

## User Flows

### Flow 1 — Historical Sky Research
1. Open app → enter date, time, location
2. Load sky
3. View celestial positions
4. Scrub forward/backward in time
5. Save notes or observations

### Flow 2 — Scriptural Event Investigation
1. Open app → select a Scriptural event preset (or enter custom scenario)
2. Load suggested date/location
3. View normal celestial motion
4. Enable simulation controls (reverse, pause, or extend motion)
5. Compare normal vs. altered motion
6. Record findings in Notes panel

### Flow 3 — Ongoing Calendar and Mazzaroth Study
1. Open app → select current or future date
2. View moon phase, sun path, and star positions
3. Move through days/months
4. Track patterns and note observations

---

## Architecture Strategy

The app uses an **MVVM** architecture layered over the existing Ephemeris calculation engine.

```
┌──────────────────────────────────────────┐
│           UI Layer (Avalonia)            │
│  WorkspaceView  │  ComparisonView        │
│  SkyCanvas      │  SimulationPanel       │
│  TimeControlBar │  NotesPanel            │
│  DataSidebar    │  HomeScreen            │
└────────────────────┬─────────────────────┘
                     │ bindings / commands
┌────────────────────▼─────────────────────┐
│         ViewModel / App State            │
│  WorkspaceViewModel  │  ComparisonVM     │
│  PlaybackEngine      │  SessionModel     │
└────────────────────┬─────────────────────┘
                     │ service calls
┌────────────────────▼─────────────────────┐
│     CelestialResearchService (wrapper)   │
│     ICelestialResearchService            │
└────────────────────┬─────────────────────┘
                     │
┌────────────────────▼─────────────────────┐
│     Ephemeris Core Library               │
│  SunEphemeris  MoonEphemeris             │
│  PlanetEphemeris  RiseSetCalculator      │
│  StarEphemeris  EclipseCalculator        │
└──────────────────────────────────────────┘
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `WorkspaceView` | Main research workspace layout (toolbar, sky view, sidebar, time bar) |
| `SkyCanvas` | OpenGL-based sky rendering — Sun, Moon, stars, constellation overlays |
| `TimeControlBar` | Playback controls — play, pause, rewind, fast-forward, speed |
| `DataSidebar` | Live celestial data — RA/Dec, Az/Alt, rise/set times, phase, illumination |
| `SimulationPanel` | Overrides — freeze motion, reverse, extend daylight duration |
| `ComparisonView` | Side-by-side baseline vs. simulation sky views |
| `NotesPanel` | Research notes, associated with the current session |
| `HomeScreen` | Entry point — new session, load session, scriptural event library |
| `EventPresetCard` | Hezekiah's Sundial and Joshua's Long Day preset loader |
| `SessionSaveDialog` | Save/load sessions as JSON |

---

## Screen Specifications

### Screen 1 — Home / Launch
- App title + purpose subtitle
- Primary actions: New Research Session, Load Scriptural Event, Resume Previous Session
- Quick input fields: Date, Time, Location
- Optional: recent sessions panel

### Screen 2 — Main Sky Research Workspace
**Section A — Sky Visualization Panel** (largest area):
- Sun, Moon, visible stars, constellation outlines (if enabled), horizon line, cardinal directions
- Sun path overlay and Moon path overlay (optional toggles)

**Section B — Time and Motion Control Panel:**
- Play, Pause, Rewind, Fast-forward
- Speed slider
- Step backward/forward
- Jump to sunrise/sunset/moonrise/moonset

**Section C — Data/Details Sidebar:**
- Date, time, location
- Sun altitude + azimuth; Moon altitude + azimuth + phase
- Rise/set times
- Selected star/constellation information
- Notes section

**Display Toggles:**
- Show/hide constellation lines
- Show/hide labels
- Show/hide stars
- Show/hide planetary bodies
- Show/hide horizon grid

### Screen 3 — Scriptural Event Mode
- Hezekiah's Sundial (2 Kings 20:8–11) preset card
- Joshua's Long Day (Joshua 10:12–14) preset card
- Custom Scenario creator
- Each preset: event title, scripture reference, description, suggested location, suggested date, `Load Event` button, `Open in Comparison Mode` button

### Screen 4 — Comparison Mode (Most Important)
- Two synchronized sky panels (Normal | Modified)
- Synced time controls
- Simulation controls: Pause Sun Motion, Reverse by ___ degrees, Extend Daylight ___ hrs, Reset Simulation
- Matching date/time/location labels above both panels
- Simulation controls visually separated from standard viewing controls

### Screen 5 — Research Notes / Session Summary
- Session title, date/location studied
- Notes field, key observations
- Saved screenshots or chart snapshots (if available)
- Export button

See [`wireframes.md`](wireframes.md) for the detailed screen-by-screen wireframe layouts.

---

## Input Requirements

| Input | Requirements |
|-------|-------------|
| **Date** | Historical + future dates; minute-level precision; BC/BCE support (design for it even if not in v1) |
| **Time** | HH:mm:ss precision; time zone awareness |
| **Location** | Manual coordinates (lat/lon); optional city/place name search |
| **Time controls** | Smooth playback; step by second/minute/hour/day/month/year; fast-forward |

---

## Simulation Requirements

> Simulation controls must be **clearly separated** from standard viewing controls so the
> user knows when they are observing natural motion versus testing a custom scenario.

| Control | Purpose |
|---------|---------|
| Pause motion | Freeze celestial bodies at current position |
| Reverse motion | Move sun backwards by N degrees |
| Shift sun position | Offset altitude/azimuth by N degrees |
| Extend daylight | Hold sun above horizon for N additional hours |
| Compare mode | Side-by-side baseline vs. simulation |
| Reset | Return to unmodified natural motion |

**Architecture note:** Simulation overrides are applied at the `CelestialResearchService`
layer — they do not modify the Ephemeris core library.

---

## Data Requirements

**Minimum display:**
- Date, time, location
- Sun position (altitude, azimuth)
- Moon position, phase, illumination
- Rise and set times for Sun and Moon
- Selected object details

**Nice-to-have:**
- Angular separation between bodies
- Seasonal marker events
- Visibility windows
- Event markers (full moon, equinox, solstice, eclipse)

---

## Feature Roadmap

### Milestone 1 — MVP Workspace (Issues #57–#65)

| # | Feature | GitHub Issue |
|---|---------|-------------|
| 1 | Create Main Research Workspace Layout | #57 |
| 2 | Create Workspace ViewModel and App State | #64 |
| 3 | Add Date and Time Input Controls | #60 |
| 4 | Add Location Input Control | #65 |
| 5 | Create Celestial Research Service Wrapper | #58 |
| 6 | Build Sky Canvas Control | #61 |
| 7 | Add Celestial Data Sidebar | #62 |
| 8 | Create Time Control Bar UI | #59 |
| 9 | Implement Playback Engine | #63 |

### Milestone 2 — Scriptural Research Flow (Issues #66–#71)

| # | Feature | GitHub Issue |
|---|---------|-------------|
| 10 | Create Scriptural Event Library Screen | #66 |
| 11 | Create Scenario Model | #71 |
| 12 | Load Scriptural Presets into Workspace | #68 |
| 13 | Create Notes Panel | #69 |
| 14 | Add Session Model and JSON Persistence | #70 |

### Milestone 3 — Comparison + Simulation (Issues #72–#74)

| # | Feature | GitHub Issue |
|---|---------|-------------|
| 15 | Create Comparison Mode Layout | #72 |
| 16 | Create Comparison ViewModel | #73 |
| 17 | Build Simulation Control Panel | #74 |
| 18 | Implement Simulation Layer | #68 |

### Milestone 4 — Polish + Expansion (Issues #75–#81)

| # | Feature | GitHub Issue |
|---|---------|-------------|
| 19 | Add Home Screen | #81 |
| 20 | Apply Dark Theme | #77 |
| 21 | Add Empty States | #80 |
| 22 | Add Star Rendering | #75 |
| 23 | Add Constellation Overlays | #78 |
| 24 | Add Event Detection Helpers | #79 |
| 25 | Add Export Support | #76 |

### Phase 3 — Advanced Research (Future Issues)

| Feature | Notes |
|---------|-------|
| Biblical Calendar Assistant | Determine biblical day/month/year from sun/moon/star positions |
| Mazzaroth Seasonal Mapping | Highlight constellations by season; track movement across months/years |
| Sun/Moon Path Overlays | Show arc of travel across the sky for the current day |
| Sky View Display Toggles | Show/hide constellations, labels, stars, planets, horizon grid independently |
| BC/BCE Historical Date Support | Full support for dates before year 0 (proleptic Julian calendar) |

### Phase 4 — Visionary Expansion (Future)

| Feature | Notes |
|---------|-------|
| Research Workspace | Organize projects, notes, and scenarios |
| Shared Research | Share findings with other researchers / educators |
| Educational Mode | Guided learning: "Understanding the Mazzaroth", "Celestial timekeeping" |

---

## Build Phases (Developer Handoff)

The app is built in seven phases using Copilot-assisted development.

**Phase 1 — Workspace Foundation**
Create the main workspace view, layout, and ViewModel. Bind date, time, and location to
app state. Wire up the `CelestialResearchService` wrapper over `EphemerisCalculator`.

**Phase 2 — Sky Visualization**
Build the sky canvas control using `OpenGlControlBase`. Render Sun and Moon first.
Extend later to stars (Yale BSC5) and constellations.

**Phase 3 — Time Controls**
Implement the playback engine with play, pause, rewind, fast-forward, and speed selector.

**Phase 4 — Scriptural Presets**
Add predefined scenarios for Hezekiah's Sundial and Joshua's Long Day. Load them into
the workspace.

**Phase 5 — Comparison Mode**
Create side-by-side sky views. Implement baseline versus simulation state management.

**Phase 6 — Simulation Layer**
Add UI-level overrides for freezing motion, reversing degrees, and extending daylight.

**Phase 7 — Notes & Sessions**
Allow saving sessions with notes and exporting data. Use JSON for persistence.

---

## MVP Success Criteria

A user can:

1. Input a date, time, and observer location
2. Visualize the sky (Sun, Moon, visible stars) for that moment
3. Control time playback (play, pause, rewind, fast-forward)
4. Simulate altered motion (freeze, reverse, extend daylight)
5. Compare normal versus simulated sky side by side
6. Load a predefined scriptural event preset (Hezekiah, Joshua)
7. Take research notes and save/load a session

---

## Expanded Applications

Beyond the core scriptural use cases, the app supports:

- **Solar event analysis** — track the Sun's path across any historical or future date
- **Lunar calendar tracking** — phases, eclipses, and rise/set aligned to ancient calendars
- **Mazzaroth exploration** — study the constellations (the Hebrew Mazzaroth / Zodiac) in
  their celestial context
- **Restoration of celestial-based timekeeping** — verify ancient calendars against computed
  positions

---

## Related Documentation

- [Wireframes](wireframes.md) — screen-by-screen UI wireframes
- [GitHub Issues](https://github.com/wforney/ephemeris/issues) — full backlog of app features
- [Algorithm Reference](algorithm-reference.md) — the Ephemeris engine algorithms the app relies on
- [Wiki Home](wiki-home.md) — project overview and documentation index
- `Ephemeris.UI.Avalonia` — cross-platform Avalonia UI project (primary app host)
- `Ephemeris.UI.Shared` — shared ViewModels and messaging


A celestial research platform built on the Ephemeris calculation engine, designed for
Biblical cosmology researchers and astronomers who want to visualize, simulate, and
study scriptural celestial events.

---

## User Persona

**Biblical Cosmology & Astronomy Researcher**

A researcher focused on studying the Mazzaroth, scriptural events, and celestial
timekeeping in order to verify and deepen understanding of Scripture through the
heavens.

**Current challenges without the app:**
- Research relies on ancient writings, historical records, and manual cross-referencing
- Observation is limited by environmental conditions (e.g., cloud cover)
- No system exists to visualize or simulate celestial events, making verification difficult

**How the app helps:**
- Introduces a structured, visual, and time-based system for celestial analysis
- Transforms research from manual interpretation into interactive verification
- Provides a research companion for Biblical scholars, educators, and astronomy enthusiasts

---

## Core Use Cases

### The Sign of King Hezekiah (2 Kings 20 / Isaiah 38)

The researcher investigates the event in which the Sun's shadow moved backward ten
degrees on the sundial of Ahaz.

**Research flow:**
1. Input the historical date and observer location (Jerusalem, ~701 BCE)
2. App visualizes normal solar motion for that day
3. Rewind or freeze time to the moment of the sign
4. Compare the expected solar trajectory versus the altered trajectory
5. Annotate findings in the Notes panel and save the session

### Joshua's Long Day (Joshua 10:12–14)

The Sun and Moon stood still as YAH granted victory over the Amorites.

**Research questions the app can address:**
- What does it mean for the Sun and Moon to "stand still"?
- How does paused celestial motion affect time and daylight?
- What was the dual positioning of Sun and Moon at that moment?

**Research flow:**
1. Input the date and location (Gibeon region, ~1406 BCE)
2. Observe natural solar and lunar motion
3. Pause celestial movement using the simulation controls
4. Extend daylight duration
5. Compare normal versus altered motion in side-by-side Comparison Mode
6. Study solar authority over time and the difference between paused vs. extended time

---

## Architecture Strategy

The app uses an **MVVM** architecture layered over the existing Ephemeris calculation engine.

```
┌──────────────────────────────────────────┐
│           UI Layer (Avalonia)            │
│  WorkspaceView  │  ComparisonView        │
│  SkyCanvas      │  SimulationPanel       │
│  TimeControlBar │  NotesPanel            │
│  DataSidebar    │  HomeScreen            │
└────────────────────┬─────────────────────┘
                     │ bindings / commands
┌────────────────────▼─────────────────────┐
│         ViewModel / App State            │
│  WorkspaceViewModel  │  ComparisonVM     │
│  PlaybackEngine      │  SessionModel     │
└────────────────────┬─────────────────────┘
                     │ service calls
┌────────────────────▼─────────────────────┐
│     CelestialResearchService (wrapper)   │
└────────────────────┬─────────────────────┘
                     │
┌────────────────────▼─────────────────────┐
│     Ephemeris Core Library               │
│  SunEphemeris  MoonEphemeris             │
│  PlanetEphemeris  RiseSetCalculator      │
│  StarEphemeris  EclipseCalculator        │
└──────────────────────────────────────────┘
```

### Key Components

| Component | Purpose |
|-----------|---------|
| `WorkspaceView` | Main research workspace layout (toolbar, sky view, sidebar, time bar) |
| `SkyCanvas` | OpenGL-based sky rendering — Sun, Moon, stars, constellation overlays |
| `TimeControlBar` | Playback controls — play, pause, rewind, fast-forward, speed |
| `DataSidebar` | Live celestial data — RA/Dec, Az/Alt, rise/set times, phase, illumination |
| `SimulationPanel` | Overrides — freeze motion, reverse, extend daylight duration |
| `ComparisonView` | Side-by-side baseline vs. simulation sky views |
| `NotesPanel` | Research notes, associated with the current session |
| `HomeScreen` | Entry point — new session, load session, scriptural event library |

---

## Build Phases

The app is built in seven phases, each corresponding to a GitHub milestone.

### Phase 1 — Workspace Foundation (Milestone 1)
Create the main workspace view, layout, and ViewModel. Bind date, time, and location to
app state. Wire up the `CelestialResearchService` wrapper.

**GitHub issues:** #57 Create Main Research Workspace Layout, #58 Create Celestial
Research Service Wrapper, #59 Create Time Control Bar UI, #60 Add Date and Time Input
Controls, #61 Build Sky Canvas Control, #62 Add Celestial Data Sidebar, #63 Implement
Playback Engine, #64 Create Workspace ViewModel and App State, #65 Add Location Input
Control

### Phase 2 — Sky Visualization (Milestone 1 → 4)
Build the sky canvas control. Render Sun and Moon first, then extend to stars and
constellations.

**GitHub issues:** #61 Build Sky Canvas Control, #75 Add Star Rendering, #78 Add
Constellation Overlays

### Phase 3 — Time Controls (Milestone 1)
Implement the playback engine with play, pause, rewind, fast-forward, and speed
selector.

**GitHub issues:** #59 Create Time Control Bar UI, #63 Implement Playback Engine

### Phase 4 — Scriptural Presets (Milestone 2)
Add predefined scenarios for Hezekiah's Sundial and Joshua's Long Day. Load them into
the workspace.

**GitHub issues:** #66 Create Scriptural Event Library Screen, #67 Create Scenario Model,
#68 Load Scriptural Presets into Workspace

### Phase 5 — Comparison Mode (Milestone 3)
Create side-by-side sky views. Implement baseline versus simulation state management.

**GitHub issues:** #72 Create Comparison Mode Layout, #73 Create Comparison ViewModel

### Phase 6 — Simulation Layer (Milestone 3)
Add UI-level overrides for freezing motion, reversing degrees, and extending daylight.

**GitHub issues:** #74 Build Simulation Control Panel, #68 Implement Simulation Layer

### Phase 7 — Notes & Sessions (Milestone 2)
Allow saving sessions with notes and exporting data. Use JSON for persistence.

**GitHub issues:** #69 Create Notes Panel, #70 Add Session Model and JSON Persistence,
#76 Add Export Support

---

## Expanded Applications

Beyond the core scriptural use cases, the app supports:

- **Solar event analysis** — track the Sun's path across any historical or future date
- **Lunar calendar tracking** — phases, eclipses, and rise/set aligned to ancient calendars
- **Mazzaroth exploration** — study the constellations (the Hebrew Mazzaroth / Zodiac) in
  their celestial context
- **Restoration of celestial-based timekeeping** — verify ancient calendars against computed
  positions

---

## MVP Success Criteria

A user can:

1. Input a date, time, and observer location
2. Visualize the sky (Sun, Moon, visible stars) for that moment
3. Control time playback (play, pause, rewind, fast-forward)
4. Simulate altered motion (freeze, reverse, extend daylight)
5. Compare normal versus simulated sky side by side
6. Load a predefined scriptural event preset
7. Take research notes and save/load a session

---

## Related Documentation

- [GitHub Issues](https://github.com/wforney/ephemeris/issues) — full backlog of app features
- [Algorithm Reference](algorithm-reference.md) — the Ephemeris engine algorithms the app relies on
- [Wiki Home](wiki-home.md) — project overview and documentation index
- Ephemeris.UI.Avalonia — cross-platform Avalonia UI project (primary app host)
- Ephemeris.UI.Shared — shared ViewModels and messaging
