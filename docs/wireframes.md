<!-- Updated: 2026-03-22 -->
# Ephemeris Research App — Clickable Wireframes

Screen-by-screen wireframes for the Ephemeris Research App.

---

## Navigation Structure

```
[HOME]
  ↓
[MAIN SKY WORKSPACE]
  ↔ [COMPARISON MODE]
  ↔ [DATA PANEL]
  ↔ [NOTES PANEL]

[HOME]
  ↓
[SCRIPTURAL EVENT MODE]
  ↓
[MAIN SKY WORKSPACE (PRELOADED)]
```

---

## Screen 1 — Home / Launch

**Purpose:** Start research quickly.

```
--------------------------------------------------
|        EPHEMERIS RESEARCH APP                  |
|  Celestial Visualization & Research Tool       |
--------------------------------------------------

[ New Research Session ]

[ Load Scriptural Event ]

[ Resume Previous Session ]

--------------------------------------------------
Quick Start:
Date:      [  YYYY-MM-DD   ]
Time:      [  HH:MM       ]
Location:  [  Enter City / Coordinates ]

                [ LOAD SKY ]
--------------------------------------------------
```

**Click Behavior:**
- `New Research Session` → Main Sky Workspace
- `Load Scriptural Event` → Scriptural Event Screen
- `LOAD SKY` → Main Sky Workspace (with inputs applied)

---

## Screen 2 — Main Sky Workspace (Core Screen)

**Purpose:** Primary research environment.

```
--------------------------------------------------
| Date: YYYY-MM-DD   Time: HH:MM   Location: ____ |
--------------------------------------------------

|                                                |
|              🌌 SKY VISUALIZATION               |
|                                                |
|     (Sun, Moon, Stars, Horizon, Labels)        |
|                                                |
--------------------------------------------------

[⏪] [⏸] [▶] [⏩]   Speed: [----|------]

--------------------------------------------------
DATA PANEL:

Sun: Alt ___  Az ___
Moon: Phase ___  Alt ___
Rise/Set Times:
- Sunrise __
- Sunset __

[ Open Comparison Mode ]
[ Open Notes ]
--------------------------------------------------
```

**Click Behavior:**
- Time controls → Update sky in real-time
- `Open Comparison Mode` → Screen 4
- `Open Notes` → Slide-out Notes Panel

---

## Screen 3 — Scriptural Event Mode

**Purpose:** Load meaningful research scenarios.

```
--------------------------------------------------
|         SCRIPTURAL EVENT LIBRARY               |
--------------------------------------------------

[ Hezekiah's Sundial ]
2 Kings 20:8–11
→ Sun reversed 10 degrees
[ LOAD EVENT ]

---------------------------------------------

[ Joshua's Long Day ]
Joshua 10:12–14
→ Sun and moon stood still
[ LOAD EVENT ]

---------------------------------------------

[ Custom Scenario ]
[ Create Your Own ]
--------------------------------------------------
```

**Click Behavior:**
- `LOAD EVENT` → Main Sky Workspace (preloaded date/location)

---

## Screen 4 — Comparison Mode (Most Important Screen)

**Purpose:** Compare normal vs. simulated celestial motion.

```
--------------------------------------------------
|        COMPARISON MODE                         |
--------------------------------------------------

| NORMAL SKY           | MODIFIED SKY            |
|----------------------|-------------------------|
|                      |                         |
|      🌌 SKY           |        🌌 SKY           |
|                      |                         |
|                      |                         |
--------------------------------------------------

TIME CONTROLS (SYNCED)
[⏪] [⏸] [▶] [⏩]

--------------------------------------------------
SIMULATION CONTROLS:

[ Pause Sun Motion ]
[ Reverse by ___ degrees ]
[ Extend Daylight ___ hrs ]

[ Reset Simulation ]
--------------------------------------------------
```

**Click Behavior:**
- Left panel = baseline (unmodified)
- Right panel = simulation applied
- Directly supports Hezekiah (reverse) and Joshua (pause)

---

## Screen 5 — Notes Panel (Slide-Out)

**Purpose:** Capture research insights.

```
-------------------------------
|        NOTES PANEL          |
-------------------------------

Session Name:
[______________________]

Notes:
[                                      ]
[                                      ]
[                                      ]

Timestamp:
[ Save Current Time Marker ]

[ Save Session ]
[ Export Notes ]
-------------------------------
```

**Saves:**
- Date
- Location
- Time
- Notes text

---

## Future Screen — Mazzaroth Mapping (Optional)

```
--------------------------------------------------
| MAZZAROTH VIEW                                 |
--------------------------------------------------

| 🌌 Highlighted Constellations                  |
| - Seasonal overlays                            |
| - Movement across time                         |
--------------------------------------------------
```

---

## Suggested Build Order

1. **Main Sky Workspace only** — sky view, time controls, data panel
2. **Add Navigation** — Home → Workspace, button to open Comparison Mode
3. **Add Simulation Layer** — pause sun, reverse degrees
4. **Duplicate Workspace for Comparison** — side-by-side view
5. **Add Notes**

### Copilot Build Sequence

1. Create main layout (Grid)
2. Add sky canvas component
3. Bind time slider → calculations
4. Add data panel (read-only)
5. Add play/pause logic
6. Duplicate view → comparison mode
7. Add simulation override logic
