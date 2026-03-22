<!-- Updated: 2026-03-22 -->
# Ephemeris Wiki

A .NET 10 astronomical ephemeris library — compute positions of the Sun, Moon, planets,
asteroids, and stars from any observer location on Earth. Includes astrological house
systems. Powers the **Ephemeris Research App**, a celestial visualization and simulation
platform for Biblical cosmology researchers.

[![CI](https://github.com/wforney/ephemeris/actions/workflows/ci.yml/badge.svg)](https://github.com/wforney/ephemeris/actions/workflows/ci.yml)

## Documentation

### Algorithm Reference

| Page | Contents |
|------|----------|
| [[Algorithm-Reference]] | All algorithms, formulae, and source references (Meeus chapters, IAU standards) — timekeeping, solar, lunar, lunar libration, planetary, asteroid ephemeris, astrological houses, nutation, precession, rise/set, eclipses, stellar, SPICE |

### Reference Format Specifications

These documents describe the binary and text file formats supported by the `Ephemeris.Import` namespace.

| Page | Contents |
|------|----------|
| [[SPK-BSP-Format]] | NAIF DAF/SPK binary ephemeris format — file records, summary records, segment descriptors, Type 2/3 Chebyshev layout |
| [[SE1-Ephemeris-Format]] | Swiss Ephemeris SE1 binary format — Chebyshev polynomial segments, body metadata, coordinate pipeline |
| [[SEFStars-Catalog-Format]] | Fixed star catalog text format (`sefstars.txt`) — column definitions, proper-motion & parallax corrections |
| [[Yale-BSC5-Format]] | Yale Bright Star Catalog 5th edition — fixed-width ASCII record layout, field definitions |

## Project READMEs

Each project in the solution has its own README with setup instructions, key classes, and wiki links.

| Project | README | Description |
|---------|--------|-------------|
| `Ephemeris` | [README](https://github.com/wforney/ephemeris/blob/main/Ephemeris/README.md) | Core library — namespace map, public API, coordinate conventions, DI |
| `Ephemeris.Tests` | [README](https://github.com/wforney/ephemeris/blob/main/Ephemeris.Tests/README.md) | TUnit test suite — how to run, test categories, snapshot and mock patterns |
| `Ephemeris.Benchmarks` | [README](https://github.com/wforney/ephemeris/blob/main/Ephemeris.Benchmarks/README.md) | BenchmarkDotNet — how to run, benchmark classes, result interpretation |
| `Ephemeris.UI` | [README](https://github.com/wforney/ephemeris/blob/main/Ephemeris.UI/README.md) | WinForms app — forms, MVVM pattern, ScottPlot/OpenTK/SkiaSharp |
| `Ephemeris.UI.Avalonia` | [README](https://github.com/wforney/ephemeris/blob/main/Ephemeris.UI.Avalonia/README.md) | Cross-platform Avalonia app — altitude chart, sky view (OpenGL), ScottPlot |

## Research App

Documentation for the Ephemeris Research App — the celestial visualization and
simulation platform built on this library.

| Page | Contents |
|------|----------|
| [[Research-App]] | User persona, UX principles, user flows, scriptural use cases, screen specs, simulation requirements, and feature roadmap |
| [[Wireframes]] | Screen-by-screen clickable wireframes for all 5 app screens |

## Project Links

- [Source code](https://github.com/wforney/ephemeris)
- [NuGet package](https://www.nuget.org/packages/WilliamForney.Ephemeris)
- [Issue tracker](https://github.com/wforney/ephemeris/issues)
