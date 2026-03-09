<!-- Updated: 2026-03-09 -->
# NAIF DAF/SPK Binary Ephemeris Format Reference

This document describes the **DAF (Double-precision Array File)** and **SPK (Spacecraft and Planet Kernel)** binary formats used by NASA/NAIF's SPICE toolkit. The Ephemeris library implements a native reader for these files without external dependencies.

## Overview

SPK files store Chebyshev polynomial coefficients that represent the positions (and optionally velocities) of celestial bodies over time. The file uses the DAF container format, which organizes data into fixed-size 1024-byte records.

**Key concepts:**
- All numeric data is IEEE 754 double-precision (8 bytes) or 32-bit integer (4 bytes)
- Files can be little-endian (`LTL-IEEE`) or big-endian (`BIG-IEEE`)
- Addresses are **1-based double-word indices** (each "word" is 8 bytes)
- Each record is 128 double-words = 1024 bytes
- Record N (1-based) starts at address `(N-1) × 128 + 1`
- To seek to address A: `file.Seek((A-1) × 8, SeekOrigin.Begin)`

## File Record (Record 1)

The first 1024 bytes contain the file header:

| Offset | Size | Field | Description |
|--------|------|-------|-------------|
| 0 | 8 | LOCIDW | ASCII `"NAIF/DAF"` — magic identifier |
| 8 | 4 | ND | Number of doubles per segment descriptor (2 for SPK) |
| 12 | 4 | NI | Number of integers per segment descriptor (6 for SPK) |
| 16 | 60 | LOCIFN | Internal file name (ASCII, space/null padded) |
| 76 | 4 | FWARD | 1-based record number of first summary record |
| 80 | 4 | BWARD | 1-based record number of last summary record |
| 84 | 4 | FREE | First free DAF address (1-based double-word index) |
| 88 | 8 | LOCFMT | Endianness: `"LTL-IEEE"` or `"BIG-IEEE"` |
| 96 | 928 | — | Reserved padding (zeros) |

## Summary Records

Summary records form a linked list (via NEXT/PREV pointers) and contain segment descriptors.

| Offset | Size | Field | Description |
|--------|------|-------|-------------|
| 0 | 8 | NEXT | Next summary record number (double; 0.0 = no more) |
| 8 | 8 | PREV | Previous summary record number (double; 0.0 = none) |
| 16 | 8 | NSUM | Number of segment descriptors in this record (double) |
| 24+ | var | — | NSUM segment descriptors, each 40 bytes for SPK |

> **FORTRAN convention:** NEXT, PREV, and NSUM are stored as `DOUBLE PRECISION` values even though they represent integers.

Each summary record is followed by a **name record** at the next record number, containing ASCII segment names (40 bytes each, space-padded).

## SPK Segment Descriptor (40 bytes)

For SPK files (ND=2, NI=6), each descriptor is:

| Offset | Size | Type | Field | Description |
|--------|------|------|-------|-------------|
| 0 | 8 | double | start_et | Start epoch (ET seconds past J2000.0 TDB) |
| 8 | 8 | double | end_et | End epoch (ET seconds past J2000.0 TDB) |
| 16 | 4 | int32 | target | NAIF target body ID |
| 20 | 4 | int32 | center | NAIF center body ID |
| 24 | 4 | int32 | frame | Reference frame ID (1 = J2000/ICRF) |
| 28 | 4 | int32 | dtype | SPK data type (2 or 3) |
| 32 | 4 | int32 | begin | First DAF address of segment data (1-based) |
| 36 | 4 | int32 | end | Last DAF address of segment data (1-based, inclusive) |

## SPK Type 2 — Chebyshev Polynomials (Position Only)

### Segment Layout

The last 4 doubles of the segment (addresses `end-3` through `end`) form the **directory**:

| Address | Field | Description |
|---------|-------|-------------|
| end − 3 | INIT | Initial epoch (ET) of the first sub-interval |
| end − 2 | INTLEN | Length (seconds) of each sub-interval |
| end − 1 | RSIZE | Number of doubles per Chebyshev coefficient record |
| end | N | Number of Chebyshev coefficient records |

### Chebyshev Coefficient Record (RSIZE doubles)

```
[0]:                  MID    — midpoint epoch of this sub-interval (ET seconds)
[1]:                  RADIUS — half-interval width (seconds)
[2 .. NCOEF+1]:       X position Chebyshev coefficients
[NCOEF+2 .. 2×NCOEF+1]: Y position Chebyshev coefficients
[2×NCOEF+2 .. 3×NCOEF+1]: Z position Chebyshev coefficients
```

Where `NCOEF = (RSIZE − 2) / 3`.

### Record Selection

To find the correct record for ephemeris time `t`:

```
recordIndex = floor((t − INIT) / INTLEN)       // 0-based
recordIndex = clamp(recordIndex, 0, N − 1)
recordAddress = begin + recordIndex × RSIZE     // 1-based DAF address
```

### Chebyshev Evaluation (Standard)

Normalize time to [−1, 1]:

```
x = (t − MID) / RADIUS
```

Evaluate using **standard Clenshaw recurrence** (NOT the half-normalization variant):

```
P(x) = c[0]·T₀(x) + c[1]·T₁(x) + ... + c[N-1]·T_{N-1}(x)
```

Clenshaw algorithm:

```
b[N+1] = 0
b[N]   = 0
for k = N-1 down to 1:
    b[k] = c[k] + 2x·b[k+1] − b[k+2]
result = c[0] + x·b[1] − b[2]
```

> **Important:** This differs from the Clenshaw-Curtis half-normalization `(b − b'')/2` used in some other ephemeris formats. SPK uses the standard convention where `c[0]` is the full zeroth coefficient.

## SPK Type 3 — Chebyshev Polynomials (Position + Velocity)

Identical structure to Type 2, but each record contains 6 sets of coefficients:

```
[0]:                       MID
[1]:                       RADIUS
[2 .. NCOEF+1]:            X position coefficients
[NCOEF+2 .. 2×NCOEF+1]:   Y position coefficients
[2×NCOEF+2 .. 3×NCOEF+1]: Z position coefficients
[3×NCOEF+2 .. 4×NCOEF+1]: X velocity coefficients
[4×NCOEF+2 .. 5×NCOEF+1]: Y velocity coefficients
[5×NCOEF+2 .. 6×NCOEF+1]: Z velocity coefficients
```

Where `NCOEF = (RSIZE − 2) / 6` and `RSIZE = 6×NCOEF + 2`.

For position-only queries, evaluate only the first three component sets using the same Chebyshev algorithm as Type 2.

## Time System

SPK files use **Ephemeris Time (ET)**, which is equivalent to **Barycentric Dynamical Time (TDB)** for most purposes. ET is measured in seconds past J2000.0 (2000-Jan-01 12:00:00.000 TT).

### UTC → ET Conversion

```
TT  = UTC + leap_seconds + 32.184 s
ET  ≈ TT + TDB_correction
```

Where:
- `leap_seconds` = cumulative TAI − UTC (integer seconds, from NAIF leap-second table)
- `32.184 s` = fixed TT − TAI offset (IAU standard)
- `TDB_correction = 0.001657 × sin(628.3076 × T + 6.2401)` (max ~1.66 ms)
- `T` = Julian centuries of TDB past J2000.0 = `ET / (36525 × 86400)`

At J2000.0 (UTC 2000-Jan-01 12:00:00):
- Leap seconds = 32
- ET = 0 + 32 + 32.184 + correction ≈ 64.184 s (UTC noon is NOT ET = 0)

## NAIF Body ID Table

Common body identifiers used in SPK segment descriptors:

| ID | Name(s) |
|----|---------|
| 0 | Solar System Barycenter (SSB) |
| 1 | Mercury Barycenter |
| 2 | Venus Barycenter |
| 3 | Earth-Moon Barycenter (EMB) |
| 4 | Mars Barycenter |
| 5 | Jupiter Barycenter |
| 6 | Saturn Barycenter |
| 7 | Uranus Barycenter |
| 8 | Neptune Barycenter |
| 9 | Pluto Barycenter |
| 10 | Sun |
| 199 | Mercury |
| 299 | Venus |
| 301 | Moon |
| 399 | Earth |
| 499 | Mars |
| 599 | Jupiter |
| 699 | Saturn |
| 799 | Uranus |
| 899 | Neptune |
| 999 | Pluto |

## Obtaining BSP Kernels

NAIF provides SPK/BSP kernel files for free via HTTPS:

- **Generic kernels:** <https://naif.jpl.nasa.gov/pub/naif/generic_kernels/spk/planets/>
  - `de440s.bsp` — short-interval DE440 (1550–2650), ~32 MB
  - `de440.bsp` — full DE440 (−13200 to 17191), ~114 MB
  - `de430.bsp` — DE430 (1550–2650), ~112 MB
- **Leap-second kernels:** <https://naif.jpl.nasa.gov/pub/naif/generic_kernels/lsk/>

**Convention for this project:** place kernel files in `~/ephem-data/` (e.g., `~/ephem-data/de440s.bsp`). No data files are committed to the repository.

## References

- [NAIF DAF Required Reading](https://naif.jpl.nasa.gov/pub/naif/toolkit_docs/C/req/daf.html)
- [NAIF SPK Required Reading](https://naif.jpl.nasa.gov/pub/naif/toolkit_docs/C/req/spk.html)
- [SPK Type 2 FORTRAN source (spkr02.f)](https://naif.jpl.nasa.gov/pub/naif/toolkit_docs/FORTRAN/src/spicelib/spkr02.f)
- [NAIF Integer ID Codes](https://naif.jpl.nasa.gov/pub/naif/toolkit_docs/C/req/naif_ids.html)
