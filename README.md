# Ephemeris

## A comprehensive ephemeris library for .NET

### Features

| Category                   | Components                                                             |
| -------------------------- | ---------------------------------------------------------------------- |
| **Timekeeping**            | ✅ Julian Day, Julian Century, ΔT                                      |
| **Sidereal Time**          | ✅ GMST, Apparent Sidereal Time                                        |
| **Solar System**           | ✅ Sun RA/Dec, Ecliptic Coordinates, Heliocentric/Geocentric positions |
| **Lunar Motion**           | ✅ Moon RA/Dec, Phase, Distance                                        |
| **Planetary Positions**    | ✅ Mercury–Pluto (via VSOP87 / DE430, or simplified)                   |
| **Nutation/Obliquity**     | ✅ Δψ (delta psi), Δε (delta epsilon), mean and true obliquity         |
| **Observer Location**      | ✅ Horizontal coords (Azimuth/Altitude), topocentric correction        |
| **Conversion Utilities**   | ✅ Angle conversion, time formats (UTC → TT, JD ↔ DateTime)            |
| **House Systems**          | ✅ Placidus, Koch, Equal, Whole Sign, etc.                             |
| **Ephemeris Calculations** | ✅ Celestial positions, velocities                                     |

### 🔧 Implementation Plan (Phased)

Phase 1 – Foundation
✅ Julian Day/Century

✅ ΔT (delta-T) estimation

✅ GMST / Apparent Sidereal Time

Phase 2 – Solar Ephemeris
✅ Sun: mean longitude, anomaly, RA/Dec, distance

✅ Equation of time

✅ Apparent position

Phase 3 – Lunar Ephemeris
✅ Moon's RA/Dec, phase, distance

✅ Basic lunar elongation

Phase 4 – Planetary Positions (simplified VSOP87)
✅ Mercury to Pluto orbital elements

✅ Position calculation (heliocentric + geocentric)

Phase 5 – Observer Geometry
✅ Horizontal coordinates

✅ Topocentric adjustment