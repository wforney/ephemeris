# Algorithm Reference

This page documents the algorithms, formulae, and references used by the **Ephemeris** library. Each section maps to a namespace in the core library and cites the primary sources.

> **Primary reference:** Jean Meeus, *Astronomical Algorithms*, 2nd ed. (Willmann-Bell, 1998). Cited as "Meeus Ch. N".

---

## Table of Contents

- [Timekeeping (Chronology)](#timekeeping-chronology)
- [Solar Ephemeris (Heliology)](#solar-ephemeris-heliology)
- [Lunar Ephemeris (Selenography)](#lunar-ephemeris-selenography)
- [Lunar Libration (Selenography)](#lunar-libration-selenography)
- [Topocentric Parallax](#topocentric-parallax)
- [Planetary Positions (Planetology)](#planetary-positions-planetology)
- [Asteroid Ephemeris (Planetology)](#asteroid-ephemeris-planetology)
- [Astrological Houses (Astrology)](#astrological-houses-astrology)
- [Coordinate Transforms (Geometry)](#coordinate-transforms-geometry)
- [Nutation & Precession (Geodesy)](#nutation--precession-geodesy)
- [Observable Phenomena (Phenomenology)](#observable-phenomena-phenomenology)
- [Fixed Stars (Stellarography)](#fixed-stars-stellarography)
- [SPICE/BSP Import (Import)](#spicebsp-import-import)

---

## Timekeeping (Chronology)

**Class:** `TimeUtils`, `TimeZoneUtils`  
**Source:** Meeus Ch. 7 (Julian Day), Ch. 10 (ΔT), Ch. 12 (GMST)

### Julian Day

```
JD = 365.25 × (Y + 4716)  +  30.6001 × (M + 1)  +  D  +  B  −  1524.5
```

where B is the Gregorian calendar correction. Dates before 15 Oct 1582 use the Julian calendar (B = 0).

**Julian Century:**
```
T = (JD − 2451545.0) / 36525.0
```

J2000.0 epoch = JD 2451545.0 = 2000 January 1.5 TT.

### ΔT (Difference TT − UTC)

Polynomial approximations by era from Morrison & Stephenson (2004) and the IERS. Five time-range branches are used; the post-2005 branch is:
```
ΔT ≈ 62.92 + 0.32217(y − 2000) + 0.005589(y − 2000)²   (seconds)
```

### Greenwich Mean Sidereal Time (GMST)

Meeus Eq. 12.4 (degrees, normalized to [0, 360)):
```
GMST = 280.46061837 + 360.98564736629 × (JD − J2000)
     + 0.000387933 × T²  −  T³ / 38710000
```

---

## Solar Ephemeris (Heliology)

**Class:** `SunEphemeris`  
**Source:** Meeus Ch. 25 (low-precision solar coordinates), Ch. 22 (aberration), Ch. 22 (nutation)  
**Accuracy:** ~0.01°

### Geometric Mean Longitude & Mean Anomaly

```
L₀ = 280.46646 + 36000.76983 T + 0.0003032 T²   (degrees)
M  = 357.52911 + 35999.05029 T − 0.0001537 T²   (degrees)
```

### Equation of Centre

```
C = (1.914602 − 0.004817T − 0.000014T²) sin M
  + (0.019993 − 0.000101T) sin 2M
  + 0.000289 sin 3M
```

Sun's true longitude: `Θ = L₀ + C`  
Apparent longitude with aberration: `λ = Θ − 0.00569 − 0.00478 sin Ω`

where Ω = Moon's ascending node longitude (Meeus Eq. 25.9).

### Obliquity of the Ecliptic

```
ε₀ = 23° 26′ 21.448″ − 4680.93″T − 1.55″T² + 1999.25″T³ − …
ε  = ε₀ + 0.00256 cos Ω   (apparent obliquity including nutation)
```

---

## Lunar Ephemeris (Selenography)

**Class:** `MoonEphemeris`  
**Source:** Meeus Ch. 47 (ELP-2000/82 truncated series)  
**Accuracy:** geocentric ~0.1°; topocentric after parallax correction ~0.01° additional error

### Fundamental Arguments

| Symbol | Meaning | Meeus Eq. |
|--------|---------|-----------|
| L′ | Moon's mean longitude | 47.1 |
| D  | Moon's mean elongation | 47.2 |
| M  | Sun's mean anomaly | 47.3 |
| M′ | Moon's mean anomaly | 47.4 |
| F  | Moon's argument of latitude | 47.5 |

### Longitude and Latitude Series

Longitude correction Σl: **60-term series** summing `A_i sin(arg_i)`, where each argument is a linear combination of D, M, M′, F.

Latitude correction Σb: **60-term series** summing `B_i sin(arg_i)`.

Distance correction Σr: **25-term series** summing `C_i cos(arg_i)`.

Eccentricity factor `e = 1 − 0.002516T − 0.0000074T²` modifies terms involving M.

---

## Lunar Libration (Selenography)

**Class:** `MoonEphemeris.Libration`  
**Source:** Meeus Ch. 53, Eq. 53.1–53.3  
**Accuracy:** optical libration ~0.1°; physical corrections (~0.02°) intentionally omitted

Libration is the apparent rocking of the Moon that reveals up to 59% of its surface over time. The method computes *optical* libration only (caused by parallax and orbital geometry). Physical libration from the Moon's non-rigid body response is neglected.

### Inputs

Computed from the Ch. 47 fundamental arguments: Moon's mean longitude L′, mean elongation D, Sun's mean anomaly M, Moon's mean anomaly M′, argument of latitude F, and ascending node longitude Ω.

The Moon's apparent ecliptic longitude λ is the geometric longitude plus the nutation correction Δψ.

### Algorithm (Meeus Eq. 53.1–53.3)

```
W   = λ_apparent − Ω
A   = atan2(sin W cos β cos I − sin β sin I,  cos W cos β)
l′  = A − F                                    (libration in longitude, degrees)
b′  = arcsin(−sin W cos β sin I − sin β cos I) (libration in latitude, degrees)
```

where:
- I = 1.5424° — inclination of Moon's equatorial plane to the ecliptic
- β — Moon's geocentric ecliptic latitude
- F — Moon's argument of latitude

**Reference value (Meeus p. 375–376):** 1992 Apr 12, 0h TDT → l′ ≈ −1.23°, b′ ≈ +4.20°.

Longitude libration l′ is normalized to (−180°, +180°] so that callers see the expected ±8° range.

---

## Topocentric Parallax

**Class:** `TopocentricParallax`  
**Source:** Meeus Ch. 40  
**Accuracy:** ~0.01° additional correction to geocentric position

### Equatorial Horizontal Parallax

For the Moon: `sin π = 6378.14 km / Δ` where Δ is geocentric distance.  
For the Sun: π☉ ≈ 8.794″.  
For planets: π = 8.794″ × (1 AU / Δ).

### Parallax Corrections

Observer's reduced latitude:
```
ρ sin φ′ = 0.99664719 sin φ + (h/6378140) sin φ
ρ cos φ′ = cos φ + (h/6378140) cos φ
```

ΔRA (Meeus Eq. 40.6):
```
ΔRA = atan[ −ρ cos φ′ sin π sin H / (cos δ − ρ cos φ′ sin π cos H) ]
```

Topocentric Dec (Meeus Eq. 40.7):
```
δ′ = atan[ (sin δ − ρ sin φ′ sin π) cos ΔRA / (cos δ − ρ cos φ′ sin π cos H) ]
```

---

## Planetary Positions (Planetology)

**Class:** `PlanetEphemeris`, `PlanetPhysicalEphemeris`  
**Source:** Meeus Ch. 33 (simplified orbital elements), Ch. 41 (magnitudes), Ch. 26 (physical ephemeris)  
**Accuracy:** 0.5°–5° depending on planet and epoch

### Simplified Keplerian Model

Each planet is represented by six osculating elements at J2000.0 with linear drift in T:

| Element | Symbol |
|---------|--------|
| Longitude of ascending node | Ω |
| Inclination | i |
| Argument of perihelion | ω |
| Semi-major axis | a (AU) |
| Eccentricity | e |
| Mean anomaly | M |

### Kepler Equation Solver (Newton–Raphson)

The eccentric anomaly E satisfies `E − e sin E = M`. Solved iteratively:
```
E₀ = M
E_{n+1} = E_n + (M − E_n + e sin E_n) / (1 − e cos E_n)
```
Convergence in 10–15 iterations for e < 0.9.

### Heliocentric Ecliptic Cartesian Coordinates

**Method:** `HeliocentricEclipticPosition(T, elements)` → (Xh, Yh, Zh)

```
xh = r [cos Ω cos(ω+ν) − sin Ω sin(ω+ν) cos i]
yh = r [sin Ω cos(ω+ν) + cos Ω sin(ω+ν) cos i]
zh = r  sin(ω+ν) sin i
```

where r is the heliocentric distance and ν is the true anomaly.

### Earth's Heliocentric Position

**Method:** `EarthElements(T)` / `EarthHeliocentricPosition(T)`

Earth's orbital elements use Paul Schlyter's simplified solar system constants (same convention as `PlanetPositionService`):
- Ω = 0°, i = 0° (Earth defines the ecliptic reference plane)
- ω = 282.9404° + 4.70935×10⁻⁵ · T
- a = 1.000000 AU, e = 0.016709 − 1.151×10⁻⁹ · T
- M = 356.0470° + 0.9856002585° · T · 36525

Because i = 0°, Zh_Earth = 0. The resulting vector points from the Sun toward Earth (i.e., 180° from the geocentric direction to the Sun).

### Geocentric Position

**Method:** `GeocentricPosition(T, elements)` → (EquatorialCoordinates, DistanceAu)

More accurate than `SimplifiedPlanetPosition` for bodies where the heliocentric distance significantly differs from geocentric distance (especially near-Earth objects). Algorithm (Meeus Ch. 33, Eq. 33.1–33.3):

```
(Xg, Yg, Zg) = (Xh_body − Xh_Earth, Yh_body − Yh_Earth, Zh_body − Zh_Earth)
lon_g = atan2(Yg, Xg)                   [geocentric ecliptic longitude]
lat_g = atan2(Zg, √(Xg²+Yg²))          [geocentric ecliptic latitude]
RA    = atan2(sin lon_g · cos ε − tan lat_g · sin ε, cos lon_g)
Dec   = arcsin(sin lat_g · cos ε + cos lat_g · sin ε · sin lon_g)
Δ     = √(Xg²+Yg²+Zg²)
```

---

## Asteroid Ephemeris (Planetology)

**Class:** `AsteroidEphemeris`  
**Source:** JPL Small Body Database (SBDB) and IAU Minor Planet Center (MPC) osculating elements at J2000.0  
**Accuracy:** ~1–5° main belt; ~2–8° centaurs; ~5–15° high-eccentricity TNOs

The `AsteroidEphemeris` class provides Keplerian orbital elements for 35 minor planets, feeding directly into `PlanetEphemeris.GeocentricPosition` for position computation.

### Supported Bodies

| Group | Bodies |
|-------|--------|
| Classical Big Four | Ceres, Pallas, Juno, Vesta |
| Main-belt (astrological) | Astraea, Hebe, Iris, Flora, Metis, Hygiea, Victoria, Eunomia, Psyche, Fortuna, Proserpina, Harmonia, Isis, Sappho, Nemesis |
| Near-Earth / Mars-crossing | Eros, Amor, Icarus |
| Comet-like outer belt | Hidalgo |
| Centaurs | Chiron, Pholus, Nessus, Asbolus, Chariklo, Hylonome |
| TNOs / Dwarf planets | Quaoar, Orcus, Haumea, Makemake, Eris, Sedna |

### Element Convention

```
GetElements(name, T) → OrbitalElements(Ω, i, ω, a, e, M)
```

where M already incorporates the linear mean-motion term:
```
M = M₀ + n · T · 36525    (n ≈ 0.9856° / a^1.5  °/day)
```

Linear element-drift terms (Ω̇, i̇, ω̇, ė) are included for the Big Four and Chiron where JPL SBDB provides them; they are omitted for other bodies as they are negligible over astrological time-scales.

### Usage

```csharp
double T = TimeUtils.JulianCentury(jd);
OrbitalElements elems = AsteroidEphemeris.GetElements("ceres", T);
var (coords, distAu) = PlanetEphemeris.GeocentricPosition(T, elems);
```

---

## Astrological Houses (Astrology)

**Class:** `AstrologicalHouses`  
**Source:** Meeus Ch. 14; Holden, *Astrological House Systems* (1977); Koch & Knappich (1932)  
**Namespace:** `Ephemeris.Astrology`

### Foundation

All house systems share the same two primary axes derived from the observer's local sidereal time and ecliptic obliquity:

| Angle | Formula |
|-------|---------|
| RAMC | GMST + observer longitude (normalized to [0, 360)) |
| MC (Midheaven) | `atan2(sin RAMC, cos RAMC · cos ε)` |
| Ascendant | `atan2(−cos RAMC, sin RAMC · cos ε + tan φ · sin ε)` |
| IC | MC + 180° |
| Descendant | ASC + 180° |

where ε = 23.439291° − 0.0130042° · T (mean obliquity) and φ is the observer's geographic latitude.

### House Systems

| System | Method | Algorithm |
|--------|--------|-----------|
| **Equal** | `ComputeEqual` | 12 cusps, each 30° wide, starting from ASC |
| **Whole Signs** | `ComputeWholeSigns` | House 1 starts at 0° of the zodiac sign containing ASC; each house = one whole sign |
| **Porphyry** | `ComputePorphyry` | Trisects each of the four quadrants (MC→ASC, ASC→IC, IC→DSC, DSC→MC) in equal ecliptic arcs |
| **Placidus** | `ComputePlacidus` | Semi-arc-based: intermediate cusps found where an ecliptic degree has completed 1/3 or 2/3 of its diurnal/nocturnal semi-arc; solved iteratively via `PlacidusIntermediate` |
| **Koch** | `ComputeKoch` | Trisects the Diurnal Semi-Arc (DSA) of the MC degree at the birth latitude; falls back to Porphyry at extreme latitudes |
| **Campanus** | `ComputeCampanus` | Divides the prime vertical into 12 × 30° arcs; house boundaries are great circles through the East–West axis and each 30° prime-vertical point |
| **Regiomontanus** | `ComputeRegiomontanus` | Divides the celestial equator into 12 × 30° arcs; house boundaries are great circles through the North–South horizon points |

### Placidus Iterative Solver

For each intermediate cusp at target oblique ascension `Q`:

```
Dec  = arcsin(sin λ · sin ε)
AD   = arcsin(clamp(tan φ · tan Dec, −1, 1))   [Ascensional Difference]
RA   = Q + AD
λ_new = atan2(sin RA · cos ε + tan Dec · sin ε, cos RA)
```

Repeat until |λ_new − λ| < 10⁻⁶ rad (typically 3–5 iterations). If |tan φ · tan Dec| > 1 (circumpolar condition, |φ| > ~66°), the equal-house cusp is returned.

### Koch Semi-Arc Construction

1. Compute RA and Dec of the MC ecliptic degree.
2. Diurnal Semi-Arc: `DSA = arccos(−tan φ · tan Dec_MC)`.
3. Trisect DSA for upper cusps (H11, H12); trisect Nocturnal Semi-Arc `NSA = π − DSA` for lower cusps (H2, H3).
4. Convert each RA back to ecliptic longitude: `λ = atan2(sin RA, cos RA · cos ε)`.

### Output

`HouseCusps` record: twelve cusps (indexed 1–12 in the `Cusps` array as indices 0–11), four Angles (ASC, MC, DSC, IC), and the `HouseSystem` identifier.

### Usage

```csharp
HouseCusps h = AstrologicalHouses.Calculate(jd, longitude, latitude, HouseSystem.Placidus);
// h.Ascendant, h.Midheaven, h.Cusps[0..11]
```

---

## Coordinate Transforms (Geometry)

**Classes:** `ObserverGeometry`, `CoordinateConverter`  
**Source:** Meeus Ch. 13 (equatorial ↔ horizontal), Ch. 93 (atmospheric refraction)

### Equatorial → Horizontal

```
H  = GMST + λ − RA          (local hour angle, degrees)
Alt = arcsin(sin φ sin δ + cos φ cos δ cos H)
Az  = atan2(sin H, cos H sin φ − tan δ cos φ)
Az  = Az + 180°   if sin H > 0   (quadrant correction)
```

### Atmospheric Refraction (Bennett 1982)

For apparent altitude `h_app` in degrees:
```
R = 1.02 / tan(h_app + 10.3 / (h_app + 5.11))   (arcminutes)
```

Cutoff: not applied below h_app = −1°.  
Inverse (Saemundsson): `h_true = h_app − R(h_app)`

### Ecliptic ↔ Equatorial

```
sin δ = sin ε sin λ + cos ε cos λ sin β … (full transform via rotation by ε)
```

---

## Nutation & Precession (Geodesy)

**Classes:** `NutationCalculator`, `PrecessionCalculator`

### Nutation (IAU 1980)

**Source:** Meeus Ch. 22; IAU 1980 nutation theory  
48 of the 106 standard terms retained (covering > 99.9% of amplitude).

Nutation in longitude Δψ (arcseconds):
```
Δψ = Σ (S_i + S′_i T) sin(arg_i)
```

Nutation in obliquity Δε (arcseconds):
```
Δε = Σ (C_i + C′_i T) cos(arg_i)
```

Each argument is a linear combination of: D, M☉, M☽, F, Ω.

### Precession (IAU 2006)

**Source:** IAU 2006 precession model; Meeus Ch. 21  
Accumulated precession angles from J2000.0:
```
ψ_A = 5038.481507″T − 1.0790069″T² − 0.00114045″T³ + …
ω_A = ε₀ − 0.025754″T + 0.0512623″T² − 0.00772503″T³ + …
χ_A = 10.556403″T − 2.3814292″T² − 0.00121197″T³ + …
```

---

## Observable Phenomena (Phenomenology)

### Rise / Set / Transit (Meeus Ch. 15)

**Class:** `RiseSetCalculator`

1. Compute approximate HA at rise/set:
   ```
   cos H₀ = (sin h₀ − sin φ sin δ) / (cos φ cos δ)
   ```
   where h₀ is the standard altitude (−0.8333° Sun, −0.5667° stars).

2. Estimate fractional day: `m_transit = (RA − λ − θ₀) / 360`

3. Three-iteration correction using Meeus Eq. 15.1–15.3 with three-point interpolation for RA/Dec and ΔT correction.

### Eclipse Prediction (Meeus Ch. 49 & 54)

**Class:** `EclipseCalculator`

Lunation index k such that k integer = new moon, k + 0.5 = full moon. Julian centuries T = k / 1236.85.

Quick filter: if |sin F| > 0.36 (Moon far from node), no eclipse possible.

Eclipse parameters:
- **gamma**: shadow axis distance from Earth's centre in Earth radii
- **u**: penumbral cone parameter

Classification thresholds (Meeus Table 54.a):

| Condition | Type |
|-----------|------|
| |gamma| < 0.9972, u < 0 | Total solar |
| |gamma| < 0.9972, u > 0.0047 | Annular solar |
| |gamma| < 0.9972, u in [0, 0.0047] | Hybrid solar |
| 0.9972 < |gamma| < 1.5433+u | Partial solar |
| |sin F₁| ≤ 0.9972 | Lunar (total or partial) |
| 0.9972 < |sin F₁| ≤ 1.0412 | Penumbral lunar |

### Seasons — Equinox / Solstice (Meeus Ch. 27)

**Class:** `SeasonCalculator`

JDE of mean March equinox:
```
JDE₀ = 2451623.80984 + 365242.37404T + 0.05169T² − 0.00411T³ − 0.00057T⁴
```
(analogous polynomials for June solstice, September equinox, December solstice)

Corrections for solar perturbations via 24-coefficient series in W = 2π(JDE₀ − 2451545) / 365.25.

### Planetary Events (Meeus Ch. 33 + scan)

**Class:** `PlanetaryEventCalculator`, `InnerPlanetEventCalculator`

Signed elongation ε ∈ (−180°, +180°] (positive = east of Sun):
```
ε = normalize(λ_planet − λ_sun, −180, +180)
```

Events detected by scanning in 0.5-day steps and detecting the target sign change:

| Event | Detection |
|-------|-----------|
| Opposition | ε wraps from ≈ −180 to ≈ +180 |
| Conjunction | ε crosses 0 (pos → neg) |
| East quadrature | ε decreases through +90° |
| West quadrature | ε decreases through −90° |

Sub-step interpolation gives ~hours precision. Greatest elongation for inner planets is found by a golden-section maximisation of |ε| over the bracketing interval.

---

## Fixed Stars (Stellarography)

**Classes:** `StarEphemeris`, `BrightStarCatalog`, `StarCatalog`  
**Source:** Meeus Ch. 21 (precession), Ch. 21 (proper motion)

### Proper Motion Correction

Position at epoch JD from J2000.0 catalogue position:
```
RA(JD)  = RA₀  + μ_α cos δ × Δt       (μ in arcsec/yr, Δt in Julian years)
Dec(JD) = Dec₀ + μ_δ × Δt
```

### Precession to Current Epoch

Rigorous precession matrix using IAU 2006 angles ψ_A, ω_A, χ_A applied to J2000.0 ICRS unit vector.

---

## SPICE/BSP Import (Import)

**Classes:** `SpkReader`, `SpiceKernelDatabase`, `BspImporter`  
**Reference:** [NAIF DAF/SPK format](SPK-BSP-Format)

### DAF File Structure

Binary SPK kernels use the Double Array File (DAF) format:
- 1024-byte file record (ASCII + binary header)
- Linked list of summary records, each containing segment descriptors
- Each descriptor identifies: target body, centre body, frame, data type, start/end ET

### SPK Type 2 — Chebyshev (position only)

Segment data is divided into equal-length records. Each record contains:
- Start epoch (ET seconds), interval length, N Chebyshev coefficients per axis (X, Y, Z)

Evaluation at time t:
```
t_norm = 2(t − t_mid) / interval   ∈ [−1, 1]
pos[axis] = Σ c_k T_k(t_norm)       (Clenshaw recurrence)
```

### UTC → Ephemeris Time

```
ET = (JD_UTC − J2000_JD) × 86400  +  ΔAT  +  32.184
```

where ΔAT is the number of leap seconds since 1972. The leap second table is embedded in `SpkLeapSeconds`.

---

*See also:* [[SPK-BSP-Format]], [[SE1-Ephemeris-Format]], [[SEFStars-Catalog-Format]], [[Yale-BSC5-Format]]
