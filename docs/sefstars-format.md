# Fixed Star Catalog Format (`sefstars.txt`)

<!-- Updated: 2026-03-09 -->

This document describes the text format of the `sefstars.txt` fixed-star catalog file.
The format was established by empirical analysis of the file contents.

> **This file is external data, not included in this repository.**

---

## Overview

`sefstars.txt` is a comma-delimited text catalog of ~1,400 fixed stars. Each non-comment line
encodes the star's position at epoch J2000.0, proper-motion rates, parallax, radial velocity,
and apparent magnitude.

---

## Line Format

### Comment Lines

Lines beginning with `#` are comments and must be skipped.

### Data Lines

```
common_name , bayer_desig , frame , ra_h , ra_m , ra_s , dec_d , dec_m , dec_s , pm_ra , pm_dec , radvel , parallax , mag [, color_index , catalog_no]
```

All fields are comma-delimited; leading and trailing whitespace within each field should be trimmed.

| Column | Field | Type | Unit | Notes |
|--------|-------|------|------|-------|
| 1 | `common_name` | string | — | E.g., `Aldebaran`, `Sirius` |
| 2 | `bayer_desig` | string | — | Bayer designation, e.g., `alTau`, `alCMa` |
| 3 | `frame` | string | — | Coordinate frame: `ICRS`, `FK5`, or `2000` (= FK5 J2000) |
| 4 | `ra_h` | integer | hours | Right ascension — hours component |
| 5 | `ra_m` | integer | minutes | Right ascension — minutes component |
| 6 | `ra_s` | decimal | seconds | Right ascension — seconds component |
| 7 | `dec_d` | integer | degrees | Declination — degrees (sign included, e.g., `+16`, `-26`) |
| 8 | `dec_m` | integer | arcmin | Declination — arcminutes |
| 9 | `dec_s` | decimal | arcsec | Declination — arcseconds |
| 10 | `pm_ra` | decimal | mas/yr | Proper motion in RA **already multiplied by cos(Dec)** |
| 11 | `pm_dec` | decimal | mas/yr | Proper motion in Dec |
| 12 | `radvel` | decimal | km/s | Radial velocity (positive = receding) |
| 13 | `parallax` | decimal | mas | Annual trigonometric parallax |
| 14 | `mag` | decimal | mag | Visual (V-band) apparent magnitude |
| 15 | `color_index` | integer | — | Optional; spectral colour index |
| 16 | `catalog_no` | integer | — | Optional; catalogue number |

> **Column 7 sign**: The degrees field carries a leading `+` or `−` sign for the declination.
> Arcminutes and arcseconds are always positive; the sign from degrees propagates to the full Dec.

---

## Converting to Decimal Degrees

### Right Ascension

```
RA_degrees = (ra_h + ra_m / 60.0 + ra_s / 3600.0) × 15.0
```

### Declination

```
sign   = −1 if dec_d starts with '−', else +1
Dec    = sign × (|dec_d| + dec_m / 60.0 + dec_s / 3600.0)
```

---

## Proper-Motion Correction

To apply proper motion and advance positions from J2000.0 to a target Julian Day `JD`:

```
dt_years = (JD − 2451545.0) / 365.25

RA_corrected  = RA_J2000  + (pm_ra  / 3600000.0) × dt_years / cos(Dec_rad)
Dec_corrected = Dec_J2000 + (pm_dec / 3600000.0) × dt_years
```

> Note: `pm_ra` in the file already includes the `cos(Dec)` factor; divide it back out when
> applying the RA shift in degrees. Alternatively, directly shift in RA × cos(Dec) space and then
> divide by cos(Dec) at the end.

---

## Parallax Correction

The parallax gives the annual trigonometric parallax `π` in mas. To shift the star from
barycentric to geocentric, the maximum positional shift is `π / 1000.0 / 3600.0` degrees
(sub-arcsecond for all but the nearest stars).

For most purposes the parallax correction is negligible and may be omitted.

---

## Example Lines

```
Aldebaran  ,alTau,ICRS,04,35,55.2387,+16,30,33.485,62.78,-189.35,54.26,50.09,0.985, 16,  629
Sirius     ,alCMa,ICRS,06,45,08.9173,-16,42,58.017,-546.05,-1223.14,-7.6,379.21,-1.47,-16, 1591
Polaris    ,alUMi,ICRS,02,31,49.0837,+89,15,50.794,44.22,-11.75,-17.4,7.56,2.005, 88,    8
```

For **Sirius** (alCMa):
- RA = (6h 45m 08.9173s) = 101.2872°
- Dec = −16° 42′ 58.017″ = −16.7161°
- pm_ra = −546.05 mas/yr (× cos(Dec)); pm_dec = −1223.14 mas/yr
- parallax = 379.21 mas ≈ 2.64 pc
- mag = −1.47

---

## References

- JPL Horizons: <https://ssd.jpl.nasa.gov/horizons/>
- SIMBAD Astronomical Database: <https://simbad.u-strasbg.fr/simbad/>
