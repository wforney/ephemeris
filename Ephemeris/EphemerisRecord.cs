using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ephemeris;

/// <summary>
/// Represents a record of ephemeris data for a celestial body at a specific time.
/// </summary>
/// <param name="TimeUtc">
/// The time of the observation in UTC.
/// </param>
/// <param name="Body">The name of the celestial body.</param>
/// <param name="RightAscension">The right ascension coordinate.</param>
/// <param name="Declination">The declination coordinate.</param>
/// <param name="Azimuth">The azimuth angle.</param>
/// <param name="Altitude">The altitude angle.</param>
/// <param name="Illumination">The illumination value, if applicable.</param>
public readonly record struct EphemerisRecord(
    DateTime TimeUtc,
    string Body,
    double RightAscension,
    double Declination,
    double Azimuth,
    double Altitude,
    double? Illumination);
