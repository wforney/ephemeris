<!-- Updated: 2026-08-12 -->
---
mode: agent
model: anthropic/claude-sonnet-4-5
tools: [codebase, editFiles, fetch]
description: Update the GitHub wiki Algorithm Reference page to reflect new or changed astronomical calculations.
---

You are updating the Ephemeris project wiki to keep the Algorithm Reference in sync with the codebase.

## When to use this skill

Invoke this agent after any of the following changes:
- A new calculation method is added to a domain namespace
- An existing calculation is corrected or replaced with a higher-precision formula
- A new orbital element table, coefficient array, or polynomial is introduced
- A coordinate transform pipeline step is added or reordered

## Step 1 — Identify what changed

1. Read the diff or the changed files to enumerate every new or modified formula.
2. For each changed method, note:
   - **Class and method name** (e.g., `SunEphemeris.EquationOfCentre`)
   - **Algorithm source** (e.g., "Meeus Ch. 25 Eq. 25.4")
   - **Input parameters and units** (degrees, Julian century `T`, etc.)
   - **Output and units**
   - **Accuracy claim** (from source, e.g., "±0.01°")

## Step 2 — Fetch the current wiki page

Use the `fetch` tool to retrieve the current Algorithm Reference wiki page:
```
https://raw.githubusercontent.com/wiki/wforney/ephemeris/Algorithm-Reference.md
```

If the page does not exist or is empty, create a new page with the standard structure below.

## Wiki page structure

The Algorithm Reference wiki page uses the following section hierarchy. Add new entries under the correct section; do not reorder existing entries.

```markdown
# Algorithm Reference

## Chronology
### Julian Day Number
### ΔT (Terrestrial–Universal Time difference)
### Greenwich Mean Sidereal Time (GMST)

## Heliology (Solar)
### Mean Longitude and Anomaly
### Equation of Centre
### Apparent RA and Dec
### Solar Distance

## Selenography (Lunar)
### Mean Longitude and Anomaly
### 60-Term Longitude/Latitude Series
### Phase and Illumination
### Libration

## Planetology
### Orbital Elements
### Kepler Equation Solver

## Geometry / Coordinate Transforms
### Equatorial → Horizontal
### Ecliptic → Equatorial
### GMST and Hour Angle

## Geodesy
### IAU 1980 Nutation (50-term)
### IAU 2006 Precession

## Phenomenology
### Rise / Set / Transit
### Eclipse Detection
```

## Step 3 — Format a new algorithm entry

Each algorithm entry follows this template:

```markdown
### <Algorithm Name>

**Class:** `<Namespace>.<ClassName>.<MethodName>`  
**Source:** <Author>, <Title>, <Edition>, <Chapter/Section/Equation>  
**Accuracy:** <stated accuracy from source>

**Inputs:**
| Parameter | Symbol | Unit | Description |
|-----------|--------|------|-------------|
| `T` | T | Julian centuries | Julian centuries since J2000.0 |

**Formula:**
$$
<LaTeX formula>
$$

Or in plain text if LaTeX is not available:
```
result = a + b*T + c*T^2
```

**Notes:** <edge cases, known limitations, ΔT corrections required, etc.>
```

## Step 4 — Write the updated wiki content

Compose the complete updated wiki page (or the relevant section diff) and write it to:

```
wiki/Algorithm-Reference.md
```

Create the `wiki/` directory at the workspace root if it does not exist. The CI does not sync this automatically — note in the commit message that the wiki file must be pushed to the `wforney/ephemeris.wiki` repository separately, or use the `fetch` tool to POST the update via the GitHub API if credentials are available.

## Step 5 — Verify cross-references

Check that the updated wiki entry matches the inline `<remarks>` XML doc on the corresponding C# method. The wiki and the doc comment must cite the same source and equation number. If they diverge, update the C# `<remarks>` to match.

## Step 6 — Commit

```bash
docs(wiki): update Algorithm Reference for <scope> — <brief description>
```

Do not commit the wiki directory to the main repo; it belongs in the `.wiki` repository. Flag this in the PR description so a maintainer can sync it.
