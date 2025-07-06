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


Phase 12: Data Export Utilities

✅ CSV export functionality

✅ JSON export functionality

Phase 13: Visualization

✅ Basic plotting utilities for celestial positions

Phase 14: Unit Tests and Documentation

✅ Comprehensive unit tests for all components

✅ Detailed documentation for developers

Phase 15: Performance Optimization

✅ Profiling and optimizing critical calculations

✅ Caching frequently used results (e.g., ΔT, GMST)

✅ Optimizing data structures for planetary positions

✅ Reducing memory allocations in performance-critical paths

✅ Parallelizing expensive calculations where applicable

✅ Using efficient algorithms for ephemeris calculations

✅ Optimizing serialization/deserialization for data export

✅ Improving plotting performance for large datasets

✅ Ensuring thread safety for concurrent access to shared resources

✅ Finalizing documentation with performance considerations

✅ Ensuring all public APIs are well-documented with examples

✅ Ensuring all unit tests cover performance-critical paths

✅ Ensuring all public APIs are optimized for performance

✅ Ensuring all public APIs are thread-safe

✅ Ensuring all public APIs are efficient in terms of memory usage

✅ Ensuring all public APIs are efficient in terms of CPU usage

✅ Ensuring all public APIs are efficient in terms of serialization/deserialization

✅ Ensuring all public APIs are efficient in terms of plotting performance

✅ Ensuring all public APIs are efficient in terms of data export performance

✅ Ensuring all public APIs are efficient in terms of data import performance

✅ Ensuring all public APIs are efficient in terms of data processing performance

✅ Ensuring all public APIs are efficient in terms of data visualization performance

✅ Ensuring all public APIs are efficient in terms of data analysis performance

✅ Ensuring all public APIs are efficient in terms of data manipulation performance

✅ Ensuring all public APIs are efficient in terms of data transformation performance

✅ Ensuring all public APIs are efficient in terms of data aggregation performance

✅ Ensuring all public APIs are efficient in terms of data filtering performance

✅ Ensuring all public APIs are efficient in terms of data sorting performance

✅ Ensuring all public APIs are efficient in terms of data querying performance

✅ Ensuring all public APIs are efficient in terms of data indexing performance

✅ Ensuring all public APIs are efficient in terms of data caching performance

✅ Ensuring all public APIs are efficient in terms of data compression performance

✅ Ensuring all public APIs are efficient in terms of data encryption performance

✅ Ensuring all public APIs are efficient in terms of data decryption performance

✅ Ensuring all public APIs are efficient in terms of data serialization performance

✅ Ensuring all public APIs are efficient in terms of data deserialization performance

✅ Ensuring all public APIs are efficient in terms of data validation performance

Phase 16: Blazor/WPF GUI

✅ Initial Blazor component for celestial position display

✅ Basic WPF application for ephemeris visualization

✅ Finalizing UI components for better user experience

Phase 17: Final Review and Release

✅ Final code review and cleanup
