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

Phase 6 - Coordinate Converter

✅ Ecliptic to Equatorial conversion

✅ Geocentric to Topocentric conversion

Phase 7 - Ephemeris Calculator

✅ High-level GetSunPosition, GetMoonPosition methods

✅ Combines solar/lunar ephemerides, observer geometry, and illumination

✅ Inputs: date, time, observer location

✅ Outputs: Right Ascension, Declination, Azimuth, Altitude, and (for Moon) Illumination

Phase 8 - Planet Position Wrapper

✅ High-level GetPlanetPosition method

✅ Supports Mercury, Venus, Mars, Jupiter, Saturn

✅ Uses simplified orbital elements and converts to equatorial and horizontal coordinates

Phase 9: Add outer planets and Pluto

✅ PlanetPositionService now supports Uranus, Neptune, and Pluto using simplified orbital elements

Phase 10: Time and Time Zone Utilities

✅ UTC to local time conversions

✅ Local time to UTC conversions

✅ Handling time zones with TimeZoneInfo

✅ Converting DateTime to Julian Day and back

Phase 11: Enhanced EphemerisCalculator with DateTime and TimeZone support

✅ Accept DateTime inputs (with time zones) and internally handle conversions seamlessly


Phase ?: Output formatting or data export (e.g. CSV, JSON)?

✅ 
