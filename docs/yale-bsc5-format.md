<!-- Updated: 2026-03-09 -->
# Yale Bright Star Catalog, 5th Edition (BSC5) Format

This document describes the fixed-width ASCII record format of the Yale Bright Star Catalog
(BSC5), 5th revised edition. The catalog data is **not included in this repository**; it must
be obtained separately from an astronomical data archive.

## Overview

The Yale BSC5 contains 9,096 stars with visual magnitudes brighter than approximately 7.1.
Every star record is a single line of exactly **197 characters** (plus line ending). Fields are
positional — column numbers below are 0-based and the end column is exclusive (Python/C# slice
convention: `line[start..end]`).

All coordinates are J2000.0 mean equatorial (ICRS-compatible for the vast majority of bright
stars).

## Field Definitions

| Field | Cols (0-based) | Width | Type | Units / Notes |
|-------|---------------|-------|------|---------------|
| HR number | 0–3 | 4 | int | Harvard Revised (HR) catalog number. Right-justified. Blank = not applicable (skip record). |
| Star name | 4–13 | 10 | string | Name as given in the catalog; trimmed. May be blank. |
| RA hours (J2000) | 75–76 | 2 | int | Hours component of right ascension. |
| RA minutes | 77–78 | 2 | int | Minutes component of right ascension. |
| RA seconds | 79–82 | 4 | float | Decimal seconds component of right ascension (e.g. `23.46`). |
| Dec sign | 83 | 1 | char | `+` or `-`. |
| Dec degrees | 84–85 | 2 | int | Degrees component of declination (absolute value). |
| Dec arcminutes | 86–87 | 2 | int | Arcminutes component of declination. |
| Dec arcseconds | 88–89 | 2 | int | Integer arcseconds component of declination. |
| Visual magnitude | 102–106 | 5 | float | V-band apparent magnitude. May include sign (e.g. `-1.46`, ` 0.03`). |
| Spectral type | 127–146 | 20 | string | MK spectral classification (trimmed). May be blank. |
| PM RA (s/yr) | 148–153 | 6 | float | Annual proper motion in RA, in **seconds of time per year**. This is the rate of change of the RA coordinate itself (already includes cos δ factor implicitly at the catalog epoch). |
| PM Dec (arcsec/yr) | 154–159 | 6 | float | Annual proper motion in declination, in arcseconds per year. |
| Parallax (arcsec) | 161–165 | 5 | float | Trigonometric parallax in arcseconds. Multiply by 1000 for mas. |

## Unit Conversions Used by `YaleBsc5Reader`

| BSC5 field | BSC5 unit | `FixedStar` field | `FixedStar` unit | Conversion |
|------------|-----------|-------------------|-----------------|------------|
| PM RA | s of time / yr | `ProperMotionRaCosD` | mas / yr | `value × 15.0 × 1000.0 × cos(δ)` |
| PM Dec | arcsec / yr | `ProperMotionDec` | mas / yr | `value × 1000.0` |
| Parallax | arcsec | `ParallaxMas` | mas | `value × 1000.0` |

> **Note on PM RA convention:** The BSC5 records proper motion in RA as the change in the RA
> coordinate itself (in time units). To convert to the `μα cos δ` convention used by `FixedStar`
> (linear motion on the sky), multiply by `cos(δ)`. The factor of 15 converts seconds of time
> to arcseconds; multiplying by 1000 gives mas/yr.

## Obtaining the Catalog

The BSC5 catalog file (typically named `catalog` with no extension) is available at:

- **VizieR CDS** — catalogue `V/50` at <https://vizier.cds.unistra.fr/viz-bin/VizieR?-source=V/50>
- **NASA ADC** — Historical NASA Astronomical Data Center mirror sites
- **Simbad** — Individual star queries, not a bulk download

Download the file, verify it contains ~9,096 lines of 197 characters each, then pass its
absolute path to `YaleBsc5Reader.Load(filePath)`.

## Example Record

```
   1Alp And          ....  006 44 47.0  23 12 06  ...  2.07  A0pSiSrCrEu  .........
^  ^                                                   ^     ^
|  |                                                   |     Spectral type (cols 127–146)
|  Star name (cols 4–13)                               Visual magnitude (cols 102–106)
HR number (cols 0–3)
```

(Field positions above are illustrative; real records do not contain spaces as shown.)

## Related Source Files

- `Ephemeris/Stellarography/YaleBsc5Reader.cs` — implementation
- `Ephemeris/Stellarography/FixedStar.cs` — output record type
- `docs/sefstars-format.md` — format documentation for the SE1 / sefstars.txt catalog
