# SE1 Binary Planet Ephemeris File Format

<!-- Updated: 2026-03-09 -->

This document describes the binary file format used by `.se1` planet ephemeris files
(e.g., `sepl_18.se1`, `semo_18.se1`). The format was established by reverse-engineering
the binary layout empirically and verified against `sepl_18.se1` (DE431, 1800–2400 CE).

> **These files are external data, not included in this repository.**

---

## Overview

SE1 files encode Chebyshev polynomial series for planetary/lunar positions. Each file covers a fixed
span of Julian days, divided into equal-length segments per body. Coefficients are stored in a
variable-precision integer-packing scheme to minimize file size.

File naming convention: `sepl_NN.se1` (planets), `semo_NN.se1` (Moon), `seas_NN.se1` (asteroids),
where `NN` is the century start year (e.g., 18 = 1800 CE).

---

## File Layout

```
[0..115]      ASCII text header (3 CRLF-terminated lines)
[116..119]    Endianness marker (4 bytes)
[120..123]    int32: total file size in bytes
[124..127]    int32: JPL DE version number (e.g., 431)
[128..135]    double: file start Julian Day (ET / TT)
[136..143]    double: file end Julian Day (ET / TT)
[144..145]    int16: npl (number of bodies in this file)
[146..165]    int16[npl]: SEI body IDs (see Body IDs table)
[at fpos]     int32: CRC32 checksum (over all preceding bytes)
[fpos+4]      5 × double: physical constants (see Constants)
[fpos+44]     per-body metadata, npl entries (variable length)
[...]         segment indices and Chebyshev coefficient blocks
```

### ASCII Text Header (bytes 0–115)

Three CRLF-terminated lines padded with spaces to exactly 116 bytes total:
- Line 1: Software version string (e.g., `SE 2.10.03`)
- Line 2: Filename (e.g., `sepl_18.se1`)
- Line 3: Copyright notice

### Endianness Marker (bytes 116–119)

| Value (4 bytes) | Meaning |
|-----------------|---------|
| `63 62 61 00` (`cba\0`) | Little-endian file |
| `61 62 63 00` (`abc\0`) | Big-endian file |

Modern SE1 files are always little-endian.

### Constants Block (5 × double = 40 bytes, at fpos+4)

| Index | Name | Value (example) | Description |
|-------|------|-----------------|-------------|
| 0 | `clight` | 173.1446 | Speed of light in AU/day |
| 1 | `aunit` | 1.495978707e11 | AU in metres |
| 2 | `helgravconst` | 0.01720209895 | Gaussian gravitational constant |
| 3 | `ratme` | 81.30057 | Earth/Moon mass ratio |
| 4 | `sunradius` | 0.00465247 | Solar radius in AU |

---

## Per-Body Metadata

After the constants block there are `npl` consecutive metadata records — one per body.
Records are NOT padded; they are laid out sequentially with no alignment gaps.

```
int32   lndx0       File offset of this body's segment index
uint8   iflg        Body flags (see Flags table)
uint8   ncoe        Number of Chebyshev coefficients per coordinate per segment
int32   rmax_lng    rmax × 1000 (normalization factor; rmax in AU)
double  tfstart     Body's valid-from Julian Day
double  tfend       Body's valid-to Julian Day
double  dseg        Segment duration in Julian days (e.g., 365.26 for planets)
double  telem       Reference epoch for orbital elements (Julian Day)
double  prot        Inclination parameter p (radians)
double  dprot       Rate of change of p (radians/Julian century)
double  qrot        Inclination parameter q (radians)
double  dqrot       Rate of change of q (radians/Julian century)
double  peri        Argument of perihelion (radians) at telem
double  dperi       Rate of change of peri (radians/Julian day)
[if iflg & SEI_FLG_ELLIPSE]
  double[ncoe]  refepx   X-component Chebyshev coefficients of reference ellipse
  double[ncoe]  refepy   Y-component Chebyshev coefficients of reference ellipse
```

### Body Flags (`iflg`)

| Bit | Constant | Value | Meaning |
|-----|----------|-------|---------|
| 0 | `SEI_FLG_HELIO` | 0x01 | Coordinates are heliocentric (not barycentric) |
| 1 | `SEI_FLG_ROTATE` | 0x02 | Apply `rot_back` rotation to equatorial J2000 |
| 2 | `SEI_FLG_ELLIPSE` | 0x04 | Reference ellipse `refepx`/`refepy` is stored |
| 3 | `SEI_FLG_EMBHEL` | 0x08 | Body is EMBHEL (affects SEI_SUNBARY computation only) |

---

## Body IDs

| SEI ID | Body |
|--------|------|
| 0 | EMB — Earth-Moon Barycenter (barycentric) |
| 1 | Moon (in `semo_NN.se1`) |
| 2 | Mercury |
| 3 | Venus |
| 4 | Mars |
| 5 | Jupiter |
| 6 | Saturn |
| 7 | Uranus |
| 8 | Neptune |
| 9 | Pluto |
| 10 | SEI_SUNBARY — encodes heliocentric Earth; BarycentricSun = EMB − body10 |

---

## Segment Index

Located at `lndx0` (file offset stored in the body's metadata).  
The number of segments is: `nndx = floor((tfend − tfstart + 0.1) / dseg)`

Each index entry is a **3-byte little-endian integer** giving the absolute file offset of that
segment's Chebyshev coefficient block.

```
Byte layout of one index entry (3 bytes):
  offset = byte[0] | (byte[1] << 8) | (byte[2] << 16)
```

To find the segment for Julian Day `tjd`:
```
iseg  = floor((tjd − tfstart) / dseg)
tseg0 = tfstart + iseg × dseg
tseg1 = tseg0 + dseg
```

---

## Chebyshev Coefficient Block

One block per segment per body. Coefficients for **X, Y, Z** are packed sequentially (all X first,
then all Y, then all Z). Each coordinate has `ncoe` coefficients.

### Packing Header

The block begins with a 2-byte or 4-byte header that describes how many coefficients fall into
each precision group:

```
Byte c[0]:
  bit 7 = 0  → 4 groups (2-byte header total)
    high nibble of c[0]: count of 4-byte integers (group 0)
    low  nibble of c[0]: count of 3-byte integers (group 1)
    high nibble of c[1]: count of 2-byte integers (group 2)
    low  nibble of c[1]: count of 1-byte integers (group 3)

  bit 7 = 1  → 6 groups (4-byte header total, read c[2] and c[3] as well)
    (nibbles of c[0]&0x7F, c[1], c[2], c[3])
    high nibble of c[0]&0x7F: count of 4-byte integers (group 0)
    low  nibble of c[0]:       count of 3-byte integers (group 1)
    high nibble of c[1]:       count of 2-byte integers (group 2)
    low  nibble of c[1]:       count of 1-byte integers (group 3)
    high nibble of c[2]:       count of half-nibble (4-bit) values (group 4)
    low  nibble of c[2]:       count of quarter-nibble (2-bit) values (group 5)
    c[3]: unused / padding
```

> **Important**: When `c[0] & 0x80`, the first byte's high nibble is `(c[0] & 0x7F) >> 4`.

### Precision Groups

Coefficients are written in descending precision order. The groups are:

| Group | Bytes per value | Precision | Notes |
|-------|-----------------|-----------|-------|
| 0 | 4 | highest | 32-bit unsigned integer |
| 1 | 3 | high | 24-bit unsigned integer |
| 2 | 2 | medium | 16-bit unsigned integer |
| 3 | 1 | low | 8-bit unsigned integer |
| 4 | 0.5 | very low | 4-bit (two per byte), high nibble first |
| 5 | 0.25 | minimal | 2-bit (four per byte), high pair first |

### Integer Decoding

All packed values are unsigned integers with sign encoded in the **least-significant bit**:

```
if (packed_value & 1) == 1:
    coefficient = −((packed_value + 1) / 2) / 1e9 × rmax / 2
else:
    coefficient = (packed_value / 2) / 1e9 × rmax / 2
```

where `rmax` is the normalization factor from the body's metadata.

For **group 4** (half-nibble, 4-bit values), the value range is 0–15. The sign bit is bit 3
(value & 8), and the magnitude is the upper 3 bits:
```
coefficient = ((nibble >> 1) × rmax / 2 / 1e9) × sign
```

For **group 5** (quarter-nibble, 2-bit values), the value range is 0–3. Sign bit is bit 1:
```
coefficient = ((bits >> 1) × rmax / 2 / 1e9) × sign
```

### Coefficient Order

Coefficients are written in **Chebyshev-polynomial order** (T0 first) within each coordinate.
The X coefficients precede Y coefficients, which precede Z coefficients.

---

## Chebyshev Polynomial Evaluation

The SE1 format uses the **Clenshaw-Curtis** normalization, where the zeroth coefficient is
halved relative to the standard Chebyshev sum:

```
f(x) = c[0]/2 + c[1]·T₁(x) + c[2]·T₂(x) + ···
```

This is implemented via Clenshaw's recursion:

```csharp
// x is in [-1, 1]; coef is the coefficient array; ncf is the count
static double EvalChebyshev(double x, double[] coef, int ncf)
{
    double x2 = x * 2.0, br = 0, brp2 = 0, brpp = 0;
    for (int j = ncf - 1; j >= 0; j--)
    {
        brp2 = brpp;
        brpp = br;
        br   = x2 * brpp - brp2 + coef[j];
    }
    return (br - brp2) * 0.5;   // ← the * 0.5 is essential
}
```

The time parameter `x` is computed from the Julian Day and the segment boundaries:
```
t_norm = (tjd − tseg0) / dseg        // [0, 1] across segment
x      = t_norm × 2 − 1              // mapped to [−1, 1]
```

---

## Coordinate Frame: `rot_back` Transformation

After unpacking the raw Chebyshev coefficients, they must be rotated from the **equinoctal
orbital frame** to **J2000 equatorial (ICRS)** coordinates. This is done if `SEI_FLG_ROTATE` is set.

### Step 1 — Compute orbital element rates at segment midpoint

```
t_mid = tseg0 + dseg / 2
tdiff = (t_mid − telem) / 365250.0           // in Julian millennia
qav   = qrot + tdiff × dqrot
pav   = prot + tdiff × dprot
```

### Step 2 — Add reference ellipse (if `SEI_FLG_ELLIPSE`)

The reference ellipse encodes the main Keplerian orbit as Chebyshev series. It is rotated to the
current perihelion direction and added to the perturbation coefficients:

```
omtild = peri + tdiff × dperi   (mod 2π)
for i = 0 to ncoe−1:
    cx[i] += cos(omtild) × refepx[i] − sin(omtild) × refepy[i]
    cy[i] += cos(omtild) × refepy[i] + sin(omtild) × refepx[i]
    cz[i] += 0   (unchanged)
```

### Step 3 — Build rotation matrix from equinoctal to equatorial

```
cosih2 = 1 / (1 + qav² + pav²)

uix = [(1 + qav² − pav²) × cosih2,   2 × qav × pav × cosih2,  −2 × pav × cosih2]
uiy = [2 × qav × pav × cosih2,   (1 − qav² + pav²) × cosih2,   2 × qav × cosih2]
uiz = [2 × pav × cosih2,             −2 × qav × cosih2,     (1 − qav² − pav²) × cosih2]
```

### Step 4 — Rotate each coefficient

For each Chebyshev index `i`:
```
rx[i] = cx[i] × uix[0]  +  cy[i] × uiy[0]  +  cz[i] × uiz[0]
ry[i] = cx[i] × uix[1]  +  cy[i] × uiy[1]  +  cz[i] × uiz[1]
rz[i] = cx[i] × uix[2]  +  cy[i] × uiy[2]  +  cz[i] × uiz[2]
```

---

## Barycentric Sun Special Case

The file stores **heliocentric Earth** at body slot `SEI_SUNBARY` (ID 10). The true barycentric
Sun position must be computed as:

```
BarycentricSun = EMB (ID 0) − HelioCentricEarth (ID 10)
```

---

## Coordinate Pipeline for a Geocentric Position

1. Parse header; load per-body metadata for the target body and for EMB (ID 0).
2. Find segment for `tjd` using `lndx0` + segment index.
3. Read and unpack Chebyshev coefficients.
4. Apply `rot_back` (add rotated refep, rotate to J2000 equatorial).
5. Evaluate `swi_echeb` at `t = (tjd − tseg0) / dseg × 2 − 1` for X, Y, Z.
6. Repeat steps 2–5 for EMB.
7. Geocentric = Planet_XYZ − EMB_XYZ.
8. Convert Cartesian J2000 equatorial → RA (degrees), Dec (degrees), distance (AU).

---

## Verified Example

From `sepl_18.se1`, DE431, at J2000.0 (JD 2451545.0):

| Body | X (AU) | Y (AU) | Z (AU) | r (AU) | RA (°) | Dec (°) |
|------|--------|--------|--------|--------|--------|---------|
| EMB | −0.1843 | 0.8848 | 0.3838 | 0.9819 | — | — |
| Jupiter (barycentric) | 3.9940 | 2.7339 | 1.0746 | 4.9580 | — | — |
| Jupiter (geocentric) | — | — | — | 4.621 | 23.9° | 8.6° |

---

## References

- JPL Horizons: <https://ssd.jpl.nasa.gov/horizons/>
- DE431 ephemeris documentation: <https://naif.jpl.nasa.gov/pub/naif/generic_kernels/spk/planets/>
